using Unity.Entities;
using Unity.Mathematics;

namespace RTS.Core.Components
{
    /// <summary>
    /// LocalTransform is only for rendering (interpolated).
    /// </summary>
    public struct AuthoritativeTransform : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        
        public static AuthoritativeTransform FromTransform(float3 pos, quaternion rot)
        {
            return new AuthoritativeTransform
            {
                Position = pos,
                Rotation = rot
            };
        }
    }
    
    /// <summary>
    /// Previous tick's authoritative position (for interpolation).
    /// </summary>
    public struct PreviousTransform : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
    }
    
    /// <summary>
    /// Tag to enable interpolation on an entity.
    /// </summary>
    public struct InterpolateMovement : IComponentData { }
}
