using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RTS.Core.Components;
using RTS.Core.Tags;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTS.Core.Systems
{
    /// <summary>
    /// High-level input handler. 
    /// Resolves selection (point or box) and routes movement requests 
    /// through the formation system to keep unit groups organized.
    /// </summary>
    [UpdateInGroup(typeof(RTSInputSystemGroup))]
    public partial class UnitCommandSystem : SystemBase
    {
        private Camera _mainCamera;
        private bool _isDragging;
        private Vector2 _dragStart;
        private float _minDragDistance = 10f; //Pixels
        private int _nextSelectionOrder;

        protected override void OnCreate()
        {
            RequireForUpdate<RTSGameTime>();
        }

        protected override void OnStartRunning()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[RTS] No main camera found.");
            }
        }

        protected override void OnUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) 
                    return;
            }

            var mouse = Mouse.current;
            if (mouse == null) 
                return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStart = mousePos;
            }

            if (mouse.leftButton.wasReleasedThisFrame && _isDragging)
            {
                _isDragging = false;
                float dragDistance = Vector2.Distance(_dragStart, mousePos);

                if (dragDistance < _minDragDistance)
                {
                    //Click select
                    HandleClickSelection(mousePos);
                }
                else
                {
                    //Box select
                    HandleBoxSelection(_dragStart, mousePos);
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                HandleMoveCommand(mousePos);
            }

            Keyboard keyboard = Keyboard.current;
            bool addToSelection = keyboard != null && keyboard.shiftKey.isPressed;

            //Draw selection box (debug)
            if (_isDragging)
            {
                float dragDistance = Vector2.Distance(_dragStart, mousePos);
                if (dragDistance >= _minDragDistance)
                {
                    DrawSelectionBox(_dragStart, mousePos);
                }
            }
        }

        private void HandleClickSelection(Vector2 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            var keyboard = Keyboard.current;
            bool addToSelection = keyboard != null && keyboard.shiftKey.isPressed;

            //Deselect all if not shift-clicking
            if (!addToSelection)
            {
                DeselectAll();
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                float3 clickPos = hit.point;

                //Find closest unit to click
                Entity closestUnit = Entity.Null;
                float closestDist = 2f; //Max click radius

                foreach (var (auth, entity) in
                    SystemAPI.Query<RefRO<AuthoritativeTransform>>()
                        .WithAll<UnitTag, SelectableTag, AliveState>()
                        .WithEntityAccess())
                {
                    float dist = math.distance(auth.ValueRO.Position, clickPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestUnit = entity;
                    }
                }

                if (closestUnit != Entity.Null)
                {
                    SelectUnit(closestUnit);
                    Debug.Log($"[RTS] Selected unit {closestUnit.Index}");
                }
            }
        }

        private void HandleBoxSelection(Vector2 start, Vector2 end)
        {
            Keyboard keyboard = Keyboard.current;
            bool addToSelection = keyboard != null && keyboard.shiftKey.isPressed;

            if (!addToSelection)
            {
                DeselectAll();
            }

            // Calculate screen-space box
            float minX = math.min(start.x, end.x);
            float maxX = math.max(start.x, end.x);
            float minY = math.min(start.y, end.y);
            float maxY = math.max(start.y, end.y);

            int selectedCount = 0;

            // Check all selectable units
            foreach (var (auth, entity) in
                SystemAPI.Query<RefRO<AuthoritativeTransform>>()
                    .WithAll<UnitTag, SelectableTag, AliveState>()
                    .WithEntityAccess())
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(auth.ValueRO.Position);

                //Check if on screen and within box
                if (screenPos.z > 0 &&
                    screenPos.x >= minX && screenPos.x <= maxX &&
                    screenPos.y >= minY && screenPos.y <= maxY)
                {
                    SelectUnit(entity);
                    selectedCount++;
                }
            }

            Debug.Log($"[RTS] Box selected {selectedCount} units");
        }

        private void HandleMoveCommand(Vector2 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                float3 targetPosition = hit.point;
                targetPosition.y = 1f; //Ground level for units

                int unitCount = 0;

                //Issue move command to all selected units
                foreach (var (movement, entity) in
                    SystemAPI.Query<RefRO<Movement>>()
                        .WithAll<UnitTag, SelectedState, AliveState>()
                        .WithEntityAccess())
                {
                    //Set destination
                    EntityManager.SetComponentData(entity, new MoveDestination
                    {
                        Destination = targetPosition,
                        StoppingDistance = 0.5f
                    });
                    EntityManager.SetComponentEnabled<MoveDestination>(entity, true);

                    unitCount++;
                }

                if (unitCount > 0)
                {
                    Debug.Log($"[RTS] Move command: {unitCount} units to {targetPosition}");
                }
            }
        }

        private void SelectUnit(Entity entity)
        {
            EntityManager.SetComponentEnabled<SelectedState>(entity, true);

            //Set selection order for formation
            if (EntityManager.HasComponent<SelectionGroup>(entity))
            {
                EntityManager.SetComponentData(entity, new SelectionGroup
                {
                    GroupId = 0,
                    SelectionOrder = _nextSelectionOrder++
                });
            }
        }

        private void DeselectAll()
        {
            _nextSelectionOrder = 0;

            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<UnitTag, SelectableTag>()
                    .WithEntityAccess())
            {
                EntityManager.SetComponentEnabled<SelectedState>(entity, false);
            }
        }

        private void DrawSelectionBox(Vector2 start, Vector2 end)
        {
            //Convert to viewport coordinates for debug drawing
            float minX = math.min(start.x, end.x);
            float maxX = math.max(start.x, end.x);
            float minY = math.min(start.y, end.y);
            float maxY = math.max(start.y, end.y);

            //Draw box corners in world space (on ground plane)
            float groundY = 0.1f;

            Vector3 bl = ScreenToGroundPoint(new Vector2(minX, minY), groundY);
            Vector3 br = ScreenToGroundPoint(new Vector2(maxX, minY), groundY);
            Vector3 tl = ScreenToGroundPoint(new Vector2(minX, maxY), groundY);
            Vector3 tr = ScreenToGroundPoint(new Vector2(maxX, maxY), groundY);

            Debug.DrawLine(bl, br, Color.green);
            Debug.DrawLine(br, tr, Color.green);
            Debug.DrawLine(tr, tl, Color.green);
            Debug.DrawLine(tl, bl, Color.green);
        }

        private Vector3 ScreenToGroundPoint(Vector2 screenPos, float groundY)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return Vector3.zero;
        }
    }
}