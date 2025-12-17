using UnityEngine;
using UnityEngine.InputSystem;

namespace RTS.Core.Authoring
{
    /// <summary>
    /// Helper component that provides setup instructions and runtime controls.
    /// </summary>
    public class RTSGameSetup : MonoBehaviour
    {
        [Header("== Runtime Controls ==")]
        [Tooltip("Key to pause/unpause simulation")]
        public Key pauseKey = Key.P;

        [Tooltip("Key to speed up simulation")]
        public Key speedUpKey = Key.Equals; // + key

        [Tooltip("Key to slow down simulation")]
        public Key slowDownKey = Key.Minus;

        [Header("== Debug Options ==")]
        public bool showInstructions = true;
        public bool enableTimeControls = true;

        private bool _isPaused;
        private float _timeScale = 1f;

        private void Start()
        {
            // Log setup info
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  RTS ENGINE - Vertical Slice Running");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  CONTROLS:");
            Debug.Log("    Left Click  - Select unit (click near unit)");
            Debug.Log("    Right Click - Move selected unit");
            Debug.Log("    P           - Pause/Unpause");
            Debug.Log("    +/-         - Speed up/slow down");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        private void Update()
        {
            if (!enableTimeControls) 
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) 
                return;

            //Pause toggle
            if (keyboard[pauseKey].wasPressedThisFrame)
            {
                TogglePause();
            }

            //Speed controls
            if (keyboard[speedUpKey].wasPressedThisFrame)
            {
                ChangeTimeScale(0.5f);
            }
            if (keyboard[slowDownKey].wasPressedThisFrame)
            {
                ChangeTimeScale(-0.5f);
            }
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;

            //Find and modify the SimulationTimeState
            //Note: This is a workaround since we can't directly access ECS from MonoBehaviour easily
            //In a real implementation, we should use a system or world accessor
            Debug.Log($"[RTS] Simulation {(_isPaused ? "PAUSED" : "RESUMED")}");

            //Pause via Time.timeScale affects ECS SystemAPI.Time.DeltaTime
            Time.timeScale = _isPaused ? 0f : _timeScale;
        }

        private void ChangeTimeScale(float delta)
        {
            _timeScale = Mathf.Clamp(_timeScale + delta, 0.5f, 4f);

            if (!_isPaused)
            {
                Time.timeScale = _timeScale;
            }

            Debug.Log($"[RTS] Time scale: {_timeScale}x");
        }

        private void OnGUI()
        {
            if (!showInstructions) 
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>RTS Engine - Vertical Slice</b>");
            GUILayout.Space(5);
            GUILayout.Label($"Time: {(_isPaused ? "PAUSED" : $"{_timeScale}x")}");
            GUILayout.Space(5);
            GUILayout.Label("Controls:");
            GUILayout.Label("  Left Click = Select");
            GUILayout.Label("  Right Click = Move");
            GUILayout.Label("  P = Pause");
            GUILayout.Label("  +/- = Speed");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}