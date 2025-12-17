using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RTS.Core.Tags;
using RTS.Core.Components;

namespace RTS.Core.Authoring
{
    /// <summary>
    /// Authoring component for RTS units with the following feature set:
    /// - Movement with interpolation
    /// - Selection and formation
    /// - Collision avoidance
    /// </summary>
    public class UnitAuthoring : MonoBehaviour
    {
        [Header("Unit Classification")]
        public EntityType entityType = EntityType.Unit;

        [Range(0, 8)]
        public int factionId = 1;

        [Header("Movement")]
        public float maxSpeed = 5f;
        public float acceleration = 10f;
        [Tooltip("Turn rate in degrees/second")]
        public float turnRate = 360f;

        [Header("Combat ")]
        public float maxHealth = 100f;
        public float armor = 0f;
        public float attackDamage = 10f;
        public float attackRange = 5f;
        public float attackCooldown = 1f;

        [Header("Collision & Avoidance")]
        [Tooltip("Collision radius for avoidance")]
        public float collisionRadius = 0.5f;
        [Tooltip("How strongly units push away from each other")]
        public float avoidanceStrength = 2f;
        [Tooltip("Maximum avoidance steering force")]
        public float maxAvoidanceForce = 5f;

        [Header("Capabilities")]
        public bool isSelectable = true;
        public bool isTargetable = true;
        public bool useSpatialIndexing = true;
        public bool useInterpolation = true;
        public bool useAvoidance = true;

        [Header("Debug")]
        public bool startSelected = false;

        public enum EntityType
        {
            Unit,
            Building,
            ResourceNode,
            Projectile
        }

        private void OnDrawGizmosSelected()
        {
            // Draw collision radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            // Draw attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

    public class UnitBaker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            switch (authoring.entityType)
            {
                case UnitAuthoring.EntityType.Unit:
                    AddComponent<UnitTag>(entity);
                    break;
                case UnitAuthoring.EntityType.Building:
                    AddComponent<BuildingTag>(entity);
                    break;
                case UnitAuthoring.EntityType.ResourceNode:
                    AddComponent<ResourceNodeTag>(entity);
                    break;
                case UnitAuthoring.EntityType.Projectile:
                    AddComponent<ProjectileTag>(entity);
                    break;
            }

            // === SELECTION ===
            if (authoring.isSelectable)
            {
                AddComponent<SelectableTag>(entity);
                AddComponent<SelectedState>(entity);
                SetComponentEnabled<SelectedState>(entity, authoring.startSelected);

                //Selection group for formation
                AddComponent(entity, new SelectionGroup
                {
                    GroupId = 0,
                    SelectionOrder = 0
                });

                //Formation slot
                AddComponent(entity, new FormationSlot
                {
                    LocalOffset = float3.zero,
                    TargetPosition = float3.zero,
                    SlotIndex = 0
                });
            }

            if (authoring.isTargetable)
            {
                AddComponent<TargetableTag>(entity);
            }

            if (authoring.useSpatialIndexing)
            {
                AddComponent<SpatiallyIndexedTag>(entity);
                AddComponent(entity, new SpatialCell { CellIndex = -1, LastUpdateTick = 0 });
            }

            AddComponent<AliveState>(entity);
            SetComponentEnabled<AliveState>(entity, true);

            AddComponent(entity, new FactionMembership
            {
                FactionId = (byte)authoring.factionId
            });

            if (authoring.entityType == UnitAuthoring.EntityType.Unit ||
                authoring.entityType == UnitAuthoring.EntityType.Projectile)
            {
                AddComponent(entity, new Movement
                {
                    MaxSpeed = authoring.maxSpeed,
                    Velocity = float3.zero,
                    Acceleration = authoring.acceleration,
                    TurnRate = math.radians(authoring.turnRate)
                });

                AddComponent(entity, new MoveDestination
                {
                    Destination = float3.zero,
                    StoppingDistance = 0.5f
                });
                SetComponentEnabled<MoveDestination>(entity, false);
                
                // Always add authoritative state for simulation
                AddComponent(entity, new AuthoritativeTransform
                {
                    Position = authoring.transform.position,
                    Rotation = authoring.transform.rotation
                });
                AddComponent(entity, new PreviousTransform
                {
                    Position = authoring.transform.position,
                    Rotation = authoring.transform.rotation
                });
            }

            if (authoring.useInterpolation)
            {
                AddComponent<InterpolateMovement>(entity);
            }

            if (authoring.useAvoidance)
            {
                AddComponent(entity, new AvoidanceData
                {
                    Radius = authoring.collisionRadius,
                    AvoidanceStrength = authoring.avoidanceStrength,
                    MaxAvoidanceForce = authoring.maxAvoidanceForce
                });
            }

            if (authoring.maxHealth > 0)
            {
                uint ticksPerSecond = 20;

                AddComponent(entity, new CombatStats
                {
                    MaxHealth = authoring.maxHealth,
                    CurrentHealth = authoring.maxHealth,
                    Armor = authoring.armor,
                    AttackDamage = authoring.attackDamage,
                    AttackRange = authoring.attackRange,
                    AttackCooldownTicks = (uint)(authoring.attackCooldown * ticksPerSecond),
                    NextAttackTick = 0
                });

                AddComponent(entity, new Target
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero
                });
            }
        }
    }
}