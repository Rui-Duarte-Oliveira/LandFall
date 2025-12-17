using Unity.Entities;
using Unity.Mathematics;

namespace RTS.Core.Components
{
    /// <summary>
    /// Movement data for mobile entities.
    /// </summary>
    public struct Movement : IComponentData
    {
        public float MaxSpeed;

        /// <summary>
        /// Current velocity vector.
        /// </summary>
        public float3 Velocity;

        public float Acceleration;

        /// <summary>
        /// Turn rate in radians/second.
        /// </summary>
        public float TurnRate;
    }

    /// <summary>
    /// Combat statistics.
    /// </summary>
    public struct CombatStats : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float Armor;
        public float AttackDamage;
        public float AttackRange;

        /// <summary>
        /// Cooldown in simulation ticks.
        /// </summary>
        public uint AttackCooldownTicks;

        /// <summary>
        /// Tick when attack will be ready again.
        /// </summary>
        public uint NextAttackTick;
    }

    /// <summary>
    /// Faction/team ownership.
    /// </summary>
    public struct FactionMembership : IComponentData
    {
        /// <summary>
        /// Faction ID (0 = neutral, 1-8 = players).
        /// </summary>
        public byte FactionId;
    }

    /// <summary>
    /// Current target for attack/follow commands.
    /// </summary>
    public struct Target : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetPosition; //Fallback if entity is destroyed
    }

    /// <summary>
    /// Pathfinding destination.
    /// </summary>
    public struct MoveDestination : IComponentData, IEnableableComponent
    {
        public float3 Destination;
        public float StoppingDistance;
    }

    /// <summary>
    /// Spatial hash cell assignment (updated by SpatialIndexingSystem).
    /// </summary>
    public struct SpatialCell : IComponentData
    {
        /// <summary>
        /// Computed cell index for spatial hash lookup.
        /// </summary>
        public int CellIndex;

        /// <summary>
        /// Tick when this was last updated (for cache validation).
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Configuration for the input/command system.
    /// Singleton component.
    /// </summary>
    public struct InputConfig : IComponentData
    {
        public int GroundLayerMask;
        public float MaxRaycastDistance;
    }

    /// <summary>
    /// Stores a pending move command from input.
    /// Added to units that should move.
    /// </summary>
    public struct MoveCommand : IComponentData, IEnableableComponent
    {
        public float3 TargetPosition;
        public uint IssuedAtTick;
    }
}