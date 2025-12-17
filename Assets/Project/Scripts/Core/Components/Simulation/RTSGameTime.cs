using Unity.Entities;
using Unity.Mathematics;

namespace RTS.Core.Components
{
    /// <summary>
    /// Singleton component representing the authoritative game time state.
    /// This is the "source of truth" for all simulation logic.
    /// </summary>
    public struct RTSGameTime : IComponentData
    {
        //CORE TICK STATE
        public uint CurrentTick;
        public uint TicksPerSecond;
        public float SecondsPerTick;

        //GAME TIME (IN-WORLD CLOCK)
        public uint TicksPerGameHour;
        public uint HoursPerGameDay;

        //CACHED DERIVED STATE
        public int GameHour;
        public int GameDay;
        public float NormalizedTimeOfDay;

        public static RTSGameTime Create(uint ticksPerSecond)
        {
            uint ticksPerGameHour = ticksPerSecond * 60;

            return new RTSGameTime
            {
                CurrentTick = 0,
                TicksPerSecond = ticksPerSecond,
                SecondsPerTick = 1f / ticksPerSecond,
                TicksPerGameHour = ticksPerGameHour,
                HoursPerGameDay = 24,
                GameHour = 6,
                GameDay = 1,
                NormalizedTimeOfDay = 0.25f
            };
        }

        /// <summary>
        /// Default: 20 TPS with interpolation
        /// </summary>
        public static RTSGameTime CreateDefault() => Create(20);

        public readonly uint SecondsToTicks(float seconds)
            => (uint)math.round(seconds * TicksPerSecond);

        public readonly float TicksToSeconds(uint ticks)
            => ticks * SecondsPerTick;
    }

    /// <summary>
    /// Manages the accumulator for fixed timestep.
    /// </summary>
    public struct SimulationTimeState : IComponentData
    {
        public double AccumulatedTime;
        public double TickDuration;
        public int PendingTicks;
        public int MaxTicksPerFrame;
        public float InterpolationAlpha;
        public bool IsPaused;
        public float TimeScale;

        public static SimulationTimeState CreateDefault(uint ticksPerSecond)
        {
            return new SimulationTimeState
            {
                AccumulatedTime = 0.0,
                TickDuration = 1.0 / ticksPerSecond,
                PendingTicks = 0,
                MaxTicksPerFrame = 4,
                InterpolationAlpha = 0f,
                IsPaused = false,
                TimeScale = 1f
            };
        }
    }

    /// <summary>
    /// Enableable component used as an event flag.
    /// </summary>
    public struct SimulationTickEvent : IComponentData, IEnableableComponent
    {
        public uint Tick;
    }
}