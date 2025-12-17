using Unity.Entities;
using Unity.Burst;
using RTS.Core.Components;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Creates simulation singleton entities at startup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct SimulationBootstrapSystem : ISystem
    {
        private const uint TICKS_PER_SECOND = 20;

        private bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            _initialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_initialized)
            {
                state.Enabled = false;
                return;
            }

            if (SystemAPI.HasSingleton<RTSGameTime>())
            {
                _initialized = true;
                state.Enabled = false;
                return;
            }

            //Create time singleton
            Entity timeEntity = state.EntityManager.CreateEntity();

            #if UNITY_EDITOR
            state.EntityManager.SetName(timeEntity, "SimulationTime");
            #endif

            RTSGameTime gameTime = RTSGameTime.Create(TICKS_PER_SECOND);
            SimulationTimeState timeState = SimulationTimeState.CreateDefault(gameTime.TicksPerSecond);

            state.EntityManager.AddComponentData(timeEntity, gameTime);
            state.EntityManager.AddComponentData(timeEntity, timeState);
            state.EntityManager.AddComponentData(timeEntity, new SimulationTickEvent { Tick = 0 });
            state.EntityManager.SetComponentEnabled<SimulationTickEvent>(timeEntity, false);

            _initialized = true;

            UnityEngine.Debug.Log($"[RTS] Bootstrap complete. TPS: {TICKS_PER_SECOND} | Interpolation: Enabled");
        }

        public void OnDestroy(ref SystemState state) { }
    }
}