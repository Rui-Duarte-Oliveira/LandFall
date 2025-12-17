using Unity.Entities;
using UnityEngine;
using RTS.Core.Tags;

namespace RTS.Core.Authoring
{
    [RequireComponent(typeof(Collider))]
    public class GroundAuthoring : MonoBehaviour
    {
        [Header("Ground Settings")]
        [Tooltip("Enable pathfinding on this surface")]
        public bool isWalkable = true;

        [Tooltip("Movement speed modifier (1.0 = normal)")]
        [Range(0.1f, 2f)]
        public float speedModifier = 1f;

        private void OnValidate()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[GroundAuthoring] {gameObject.name} needs a Collider for raycasting!", this);
            }
        }

        private void Reset()
        {
            //Set layer to Ground if it exists
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                gameObject.layer = groundLayer;
            }
            else
            {
                Debug.LogWarning("[GroundAuthoring] 'Ground' layer not found. Please create it in Tags & Layers.");
            }
        }
    }

    /// <summary>
    /// Baker for ground entities.
    /// </summary>
    public class GroundBaker : Baker<GroundAuthoring>
    {
        public override void Bake(GroundAuthoring authoring)
        {
            //Use Static transform (ground doesn't move)
            var entity = GetEntity(TransformUsageFlags.Renderable);

            //Add ground tag
            AddComponent<GroundTag>(entity);

            //Could add additional ground data here:
            //AddComponent(entity, new GroundData { ... });
        }
    }
}