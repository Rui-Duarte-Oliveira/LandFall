using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RTS.Core.Components;
using RTS.Core.Tags;
using UnityEngine;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Draws debug visuals for selection and formation.
    /// Only runs in Editor. Disabled in builds for performance.
    /// </summary>
    #if UNITY_EDITOR
    [UpdateInGroup(typeof(RTSPresentationSystemGroup))]
    public partial class SelectionVisualSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //Selection circles
            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<UnitTag>()
                    .WithAll<SelectedState>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<SelectedState>(entity))
                    continue;

                float3 pos = transform.ValueRO.Position;
                pos.y = 0.1f;

                DrawCircle(pos, 1.2f, Color.green, 12);
            }

            //Formation targets
            foreach (var (formation, dest, transform, entity) in
                SystemAPI.Query<RefRO<FormationSlot>, RefRO<MoveDestination>, RefRO<LocalTransform>>()
                    .WithAll<UnitTag>()
                    .WithAll<SelectedState>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<MoveDestination>(entity))
                    continue;
                if (!SystemAPI.IsComponentEnabled<SelectedState>(entity))
                    continue;

                float3 targetPos = formation.ValueRO.TargetPosition;
                if (math.lengthsq(targetPos) < 0.1f)
                    targetPos = dest.ValueRO.Destination;

                targetPos.y = 0.1f;
                DrawX(targetPos, 0.4f, Color.yellow);
            }
        }

        private static void DrawCircle(float3 center, float radius, Color color, int segments)
        {
            float angleStep = 2f * math.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float a1 = i * angleStep;
                float a2 = (i + 1) * angleStep;
                Debug.DrawLine(
                    new Vector3(center.x + math.cos(a1) * radius, center.y, center.z + math.sin(a1) * radius),
                    new Vector3(center.x + math.cos(a2) * radius, center.y, center.z + math.sin(a2) * radius),
                    color
                );
            }
        }

        private static void DrawX(float3 c, float s, Color color)
        {
            Debug.DrawLine(new Vector3(c.x - s, c.y, c.z - s), new Vector3(c.x + s, c.y, c.z + s), color);
            Debug.DrawLine(new Vector3(c.x - s, c.y, c.z + s), new Vector3(c.x + s, c.y, c.z - s), color);
        }
    }
    #endif
}