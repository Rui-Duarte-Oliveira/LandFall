using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using RTS.Core.Components;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Converts real-time DeltaTime into discrete simulation ticks.
    /// 
    /// This uses a standard accumulator to ensure the sim runs at a fixed rate, 
    /// regardless of how fast the user is rendering frames. This is the 
    /// foundation for lockstep determinism.
    /// 
    /// The goal is to keep the simulation predictable so that the same 
    /// inputs always yield the same state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct TimeAccumulationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
            state.RequireForUpdate<SimulationTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Get real delta time (Unity's time)
            float deltaTime = SystemAPI.Time.DeltaTime;

            //Get our singletons
            RefRW<SimulationTimeState> timeState = SystemAPI.GetSingletonRW<SimulationTimeState>();

            //Don't accumulate if paused
            if (timeState.ValueRO.IsPaused)
            {
                timeState.ValueRW.PendingTicks = 0;
                timeState.ValueRW.InterpolationAlpha = 1f;
                return;
            }

            //Apply time scale and accumulate
            double scaledDelta = deltaTime * timeState.ValueRO.TimeScale;
            timeState.ValueRW.AccumulatedTime += scaledDelta;

            //Calculate how many ticks we can run
            double tickDuration = timeState.ValueRO.TickDuration;
            int pendingTicks = (int)(timeState.ValueRO.AccumulatedTime / tickDuration);

            //Clamp to prevent death spiral (if we fall behind, drop ticks)
            pendingTicks = math.min(pendingTicks, timeState.ValueRO.MaxTicksPerFrame);

            //Consume the time for pending ticks
            timeState.ValueRW.AccumulatedTime -= pendingTicks * tickDuration;
            timeState.ValueRW.PendingTicks = pendingTicks;

            //Calculate interpolation alpha for rendering
            //This is how far we are between the last tick and the next
            timeState.ValueRW.InterpolationAlpha = (float)(timeState.ValueRO.AccumulatedTime / tickDuration);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}