using Unity.Entities;
using Unity.Mathematics;
using RTS.Core.Components;
using RTS.Core.Tags;

namespace RTS.Core.Systems
{
    [UpdateInGroup(typeof(RTSGameplaySystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(UnitMovementSystem))]
    public partial struct FormationSystem : ISystem
    {
        private bool _settingsCreated;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
            _settingsCreated = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Create formation settings singleton if needed
            if (!_settingsCreated && !SystemAPI.HasSingleton<FormationSettings>())
            {
                Entity settingsEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(settingsEntity, new FormationSettings
                {
                    UnitSpacing = 2.5f,
                    UnitsPerRow = 5,
                    Type = FormationType.Box
                });

                #if UNITY_EDITOR
                state.EntityManager.SetName(settingsEntity, "FormationSettings");
                #endif

                _settingsCreated = true;
                return;
            }

            //Only run on simulation ticks
            if (!SystemAPI.HasSingleton<RTSGameTime>())
                return;

            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();
            if (!SystemAPI.IsComponentEnabled<SimulationTickEvent>(timeEntity))
                return;

            if (!SystemAPI.HasSingleton<FormationSettings>())
                return;

            var settings = SystemAPI.GetSingleton<FormationSettings>();

            //First pass: count and collect data
            int count = 0;
            float3 totalDestination = float3.zero;
            float3 totalPosition = float3.zero;

            foreach (var (dest, auth, entity) in
                SystemAPI.Query<RefRO<MoveDestination>, RefRO<AuthoritativeTransform>>()
                    .WithAll<UnitTag>()
                    .WithAll<SelectedState>()
                    .WithAll<AliveState>()
                    .WithAll<FormationSlot>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<MoveDestination>(entity))
                    continue;
                if (!SystemAPI.IsComponentEnabled<SelectedState>(entity))
                    continue;

                totalDestination += dest.ValueRO.Destination;
                totalPosition += auth.ValueRO.Position;
                count++;
            }

            if (count == 0)
                return;

            //Calculate formation vectors
            float3 formationCenter = totalDestination / count;
            float3 avgPosition = totalPosition / count;

            float3 formationDir = math.normalizesafe(formationCenter - avgPosition);
            if (math.lengthsq(formationDir) < 0.001f)
                formationDir = new float3(0, 0, 1);

            float3 formationRight = math.cross(new float3(0, 1, 0), formationDir);
            if (math.lengthsq(formationRight) < 0.001f)
                formationRight = new float3(1, 0, 0);
            formationRight = math.normalize(formationRight);

            float spacing = settings.UnitSpacing;
            int perRow = settings.UnitsPerRow;

            //Second pass: assign slots (Box formation only - inlined)
            int slotIndex = 0;
            foreach (var (dest, formation, entity) in
                SystemAPI.Query<RefRO<MoveDestination>, RefRW<FormationSlot>>()
                    .WithAll<UnitTag>()
                    .WithAll<SelectedState>()
                    .WithAll<AliveState>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<MoveDestination>(entity))
                    continue;
                if (!SystemAPI.IsComponentEnabled<SelectedState>(entity))
                    continue;

                //INLINED BOX FORMATION CALCULATION
                int row = slotIndex / perRow;
                int col = slotIndex % perRow;
                int colsInThisRow = math.min(perRow, count - row * perRow);
                float xOffset = (col - (colsInThisRow - 1) * 0.5f) * spacing;
                float zOffset = -row * spacing;
                float3 slotOffset = new float3(xOffset, 0, zOffset);
                //END INLINED

                float3 worldOffset = formationRight * slotOffset.x + formationDir * slotOffset.z;
                float3 targetPosition = formationCenter + worldOffset;
                targetPosition.y = 1f;

                formation.ValueRW = new FormationSlot
                {
                    LocalOffset = slotOffset,
                    TargetPosition = targetPosition,
                    SlotIndex = slotIndex
                };

                slotIndex++;
            }
        }

        public void OnDestroy(ref SystemState state) { }
    }
}