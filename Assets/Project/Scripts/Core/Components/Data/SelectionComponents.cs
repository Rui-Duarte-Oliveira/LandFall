using Unity.Entities;
using Unity.Mathematics;

namespace RTS.Core.Components
{
    /// <summary>
    /// Current selection box state (screen space).
    /// </summary>
    public struct SelectionBoxState : IComponentData
    {
        public bool IsDragging;        
        public float2 StartScreenPos;     
        public float2 CurrentScreenPos;
        
        /// <summary>
        /// Minimum drag distance to count as box select (vs click select).
        /// </summary>
        public float MinDragDistance;
    }
    
    /// <summary>
    /// Added to selected units to track their selection group.
    /// </summary>
    public struct SelectionGroup : IComponentData
    {
        /// <summary>
        /// Group ID (0 = ungrouped, 1-9 = control groups).
        /// </summary>
        public byte GroupId;
        
        /// <summary>
        /// Order in which unit was selected (for formation positioning).
        /// </summary>
        public int SelectionOrder;
    }
    
    /// <summary>
    /// Formation slot assignment for a unit.
    /// </summary>
    public struct FormationSlot : IComponentData
    {
        /// <summary>
        /// Offset from formation center in local space.
        /// </summary>
        public float3 LocalOffset;
        
        /// <summary>
        /// Target world position for this unit in formation.
        /// </summary>
        public float3 TargetPosition;
        
        /// <summary>
        /// Index in the formation (0 = leader).
        /// </summary>
        public int SlotIndex;
    }
    
    /// <summary>
    /// Singleton: Formation configuration settings.
    /// </summary>
    public struct FormationSettings : IComponentData
    {
        public float UnitSpacing;       
        public int UnitsPerRow;     
        public FormationType Type;
    }
    
    public enum FormationType : byte
    {
        Box = 0,        //Grid formation
        Line = 1,       //Single line
        Wedge = 2,      //V-shape
        Circle = 3      //Surround point
    }
    
    /// <summary>
    /// Collision avoidance data for steering.
    /// </summary>
    public struct AvoidanceData : IComponentData
    {
        public float Radius;     
        public float AvoidanceStrength;
        public float MaxAvoidanceForce;
    }
}