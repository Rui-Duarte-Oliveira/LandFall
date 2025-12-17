using UnityEngine;
using UnityEngine.InputSystem;

namespace RTS.Core.Authoring
{
    /// <summary>
    /// Draws a selection box rectangle on screen when the player drags.
    /// Attach this to a GameObject in your main scene (not subscene).
    /// </summary>
    public class SelectionBoxUI : MonoBehaviour
    {
        [Header("Selection Box Style")]
        public Color boxColor = new Color(0f, 1f, 0f, 0.2f);
        public Color borderColor = new Color(0f, 1f, 0f, 0.8f);
        public float borderWidth = 2f;
        
        [Header("Settings")]
        public float minDragDistance = 10f;
        
        private bool _isDragging;
        private Vector2 _dragStart;
        private Vector2 _dragEnd;
        
        private Texture2D _boxTexture;
        private Texture2D _borderTexture;
        
        private void Start()
        {
            // Create textures for drawing
            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, Color.white);
            _boxTexture.Apply();
            
            _borderTexture = new Texture2D(1, 1);
            _borderTexture.SetPixel(0, 0, Color.white);
            _borderTexture.Apply();
        }
        
        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) 
                return;
            
            Vector2 mousePos = mouse.position.ReadValue();
            
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStart = mousePos;
            }
            
            if (_isDragging)
            {
                _dragEnd = mousePos;
            }
            
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }
        }
        
        private void OnGUI()
        {
            if (!_isDragging)
                return;
            
            float dragDistance = Vector2.Distance(_dragStart, _dragEnd);
            if (dragDistance < minDragDistance) 
                return;
            
            //Convert to GUI coordinates (Y is flipped)
            float startY = Screen.height - _dragStart.y;
            float endY = Screen.height - _dragEnd.y;
            
            float minX = Mathf.Min(_dragStart.x, _dragEnd.x);
            float maxX = Mathf.Max(_dragStart.x, _dragEnd.x);
            float minY = Mathf.Min(startY, endY);
            float maxY = Mathf.Max(startY, endY);
            
            float width = maxX - minX;
            float height = maxY - minY;
            
            Rect boxRect = new Rect(minX, minY, width, height);
            
            //Draw filled box
            GUI.color = boxColor;
            GUI.DrawTexture(boxRect, _boxTexture);
            
            //Draw border
            GUI.color = borderColor;
            
            //Top
            GUI.DrawTexture(new Rect(minX, minY, width, borderWidth), _borderTexture);
            //Bottom
            GUI.DrawTexture(new Rect(minX, maxY - borderWidth, width, borderWidth), _borderTexture);
            //Left
            GUI.DrawTexture(new Rect(minX, minY, borderWidth, height), _borderTexture);
            //Right
            GUI.DrawTexture(new Rect(maxX - borderWidth, minY, borderWidth, height), _borderTexture);
            
            GUI.color = Color.white;
        }
        
        private void OnDestroy()
        {
            if (_boxTexture != null)
                Destroy(_boxTexture);
            if (_borderTexture != null)
                Destroy(_borderTexture);
        }
    }
}