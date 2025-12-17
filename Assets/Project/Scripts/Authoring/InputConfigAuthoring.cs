using Unity.Entities;
using UnityEngine;
using RTS.Core.Components;

namespace RTS.Core.Authoring
{
    /// <summary>
    /// Authoring component for input configuration.
    /// This is optional - the UnitCommandSystem works without it,
    /// but this allows input behavior configuration in the editor.
    /// </summary>
    public class InputConfigAuthoring : MonoBehaviour
    {
        [Header("== Raycast Settings ==")]
        [Tooltip("Layer mask for ground raycasting")]
        public LayerMask groundLayerMask = 1 << 6; //Layer 6 = "Ground"

        [Tooltip("Maximum raycast distance")]
        public float maxRaycastDistance = 1000f;

        [Header("== Selection Settings ==")]
        [Tooltip("Radius for click-to-select detection")]
        public float selectionRadius = 2f;

        [Header("== Debug ==")]
        [Tooltip("Show debug rays in scene view")]
        public bool showDebugRays = true;
    }

    /// <summary>
    /// Baker for input configuration singleton.
    /// </summary>
    public class InputConfigBaker : Baker<InputConfigAuthoring>
    {
        public override void Bake(InputConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new InputConfig
            {
                GroundLayerMask = authoring.groundLayerMask.value,
                MaxRaycastDistance = authoring.maxRaycastDistance
            });
        }
    }
}