using Unity.Entities;

namespace RTS.Core.Tags
{
    /// <summary>
    /// Marks an entity as a mobile unit.
    /// </summary>
    public struct UnitTag : IComponentData { }

    /// <summary>
    /// Marks an entity as a static building.
    /// </summary>
    public struct BuildingTag : IComponentData { }

    /// <summary>
    /// Marks an entity as a resource node.
    /// </summary>
    public struct ResourceNodeTag : IComponentData { }

    /// <summary>
    /// Marks an entity as a projectile.
    /// </summary>
    public struct ProjectileTag : IComponentData { }

    /// <summary>
    /// Marks an entity as ground/terrain for raycasting.
    /// </summary>
    public struct GroundTag : IComponentData { }

    // CAPABILITY TAGS
    // These define what an entity CAN DO, enabling mix-and-match behavior

    /// <summary>
    /// Entity can be selected by the player.
    /// </summary>
    public struct SelectableTag : IComponentData { }

    /// <summary>
    /// Entity can be targeted by attacks.
    /// </summary>
    public struct TargetableTag : IComponentData { }

    /// <summary>
    /// Entity should be culled when out of view (LOD/visibility optimization).
    /// </summary>
    public struct CullableTag : IComponentData { }

    /// <summary>
    /// Entity participates in spatial indexing for neighbor queries.
    /// </summary>
    public struct SpatiallyIndexedTag : IComponentData { }

    /// <summary>
    /// Entity is owned by a player/faction.
    /// </summary>
    public struct OwnedTag : IComponentData { }

    // STATE TAGS (Enableable for fast state transitions)

    /// <summary>
    /// Entity is currently selected by the local player.
    /// Using IEnableableComponent for O(1) state changes.
    /// </summary>
    public struct SelectedState : IComponentData, IEnableableComponent { }

    /// <summary>
    /// Entity is currently visible to the local player (fog of war).
    /// </summary>
    public struct VisibleState : IComponentData, IEnableableComponent { }

    /// <summary>
    /// Entity is alive (disabled = dead but not yet destroyed).
    /// </summary>
    public struct AliveState : IComponentData, IEnableableComponent { }
}