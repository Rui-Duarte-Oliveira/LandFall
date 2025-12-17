using Unity.Entities;
using RTS.Core.Components;
using UnityEngine;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Debug system that logs tick rate to console.
    /// Only runs in Editor for zero runtime overhead.
    /// </summary>
    #if UNITY_EDITOR
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SimulationTickSystem))]
    public partial class TickDebugSystem : SystemBase
    {
        private uint _lastTick;
        private float _lastLogTime;
        private int _frameCount;
        private float _fpsUpdateTime;
        private float _currentFps;

        protected override void OnCreate()
        {
            RequireForUpdate<RTSGameTime>();
        }

        protected override void OnUpdate()
        {
            _frameCount++;
            if (UnityEngine.Time.realtimeSinceStartup - _fpsUpdateTime >= 0.5f)
            {
                _currentFps = _frameCount / (UnityEngine.Time.realtimeSinceStartup - _fpsUpdateTime);
                _frameCount = 0;
                _fpsUpdateTime = UnityEngine.Time.realtimeSinceStartup;
            }

            var gameTime = SystemAPI.GetSingleton<RTSGameTime>();
            var timeState = SystemAPI.GetSingleton<SimulationTimeState>();

            if (UnityEngine.Time.realtimeSinceStartup - _lastLogTime >= 1f)
            {
                uint ticksThisSecond = gameTime.CurrentTick - _lastTick;
                string status = timeState.IsPaused ? " [PAUSED]" : "";

                Debug.Log($"[RTS] Tick: {gameTime.CurrentTick} | TPS: {ticksThisSecond} | FPS: {_currentFps:F0}{status}");

                _lastTick = gameTime.CurrentTick;
                _lastLogTime = UnityEngine.Time.realtimeSinceStartup;
            }
        }
    }
    #endif
}