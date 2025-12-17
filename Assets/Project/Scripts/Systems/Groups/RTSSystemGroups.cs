using Unity.Entities;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Parent group for all RTS simulation systems.
    /// Runs within Unity's SimulationSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RTSSimulationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Systems that handle input and commands.
    /// Runs before gameplay logic.
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup), OrderFirst = true)]
    public partial class RTSInputSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Core gameplay systems.
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(RTSInputSystemGroup))]
    public partial class RTSGameplaySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Systems that react to gameplay events.
    /// Runs after gameplay logic.
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(RTSGameplaySystemGroup))]
    public partial class RTSEventSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Cleanup and preparation for next tick.
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup), OrderLast = true)]
    public partial class RTSCleanupSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Presentation systems (interpolation, VFX).
    /// Runs EVERY frame, not just on ticks.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RTSPresentationSystemGroup : ComponentSystemGroup { }
}