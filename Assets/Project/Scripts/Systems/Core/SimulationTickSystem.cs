using Unity.Entities;
using Unity.Burst;
using RTS.Core.Components;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Manages the simulation time-step independently of the render loop.
    /// 
    /// We decouple the tick counter from the frame rate to allow for deterministic 
    /// lockstep behavior. Gameplay systems should never run on Update(); they 
    /// must hook into SimulationTickEvent to ensure they only execute when the 
    /// simulation actually advances. 
    /// 
    /// This allows us to run multiple simulation steps in a single frame or 
    /// skip them entirely if the renderer is ahead of the simulation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TimeAccumulationSystem))]
    public partial struct SimulationTickSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
            state.RequireForUpdate<SimulationTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<SimulationTimeState> timeState = SystemAPI.GetSingletonRW<SimulationTimeState>();
            RefRW<RTSGameTime> gameTime = SystemAPI.GetSingletonRW<RTSGameTime>();

            int pendingTicks = timeState.ValueRO.PendingTicks;

            //Get the tick event singleton entity
            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();

            if (pendingTicks <= 0)
            {
                //No tick this frame - disable the event
                SystemAPI.SetComponentEnabled<SimulationTickEvent>(timeEntity, false);
                return;
            }

            //Process all pending ticks
            //NOTE: For simplicity, we batch multiple ticks.
            //A more sophisticated approach would run gameplay systems
            //multiple times in a loop.
            for (int i = 0; i < pendingTicks; i++)
            {
                AdvanceTick(ref gameTime.ValueRW);
            }

            //Enable tick event and set current tick
            SystemAPI.SetComponent(timeEntity, new SimulationTickEvent { Tick = gameTime.ValueRO.CurrentTick });
            SystemAPI.SetComponentEnabled<SimulationTickEvent>(timeEntity, true);

            //Clear pending ticks (consumed)
            timeState.ValueRW.PendingTicks = 0;
        }

        [BurstCompile]
        private static void AdvanceTick(ref RTSGameTime gameTime)
        {
            gameTime.CurrentTick++;

            //Update derived game time values
            uint ticksPerDay = gameTime.TicksPerGameHour * gameTime.HoursPerGameDay;
            uint totalTicksIntoDay = gameTime.CurrentTick % ticksPerDay;

            gameTime.GameHour = (int)(totalTicksIntoDay / gameTime.TicksPerGameHour);
            gameTime.GameDay = (int)(gameTime.CurrentTick / ticksPerDay) + 1;
            gameTime.NormalizedTimeOfDay = (float)totalTicksIntoDay / ticksPerDay;
        }

        public void OnDestroy(ref SystemState state) { }
    }
}