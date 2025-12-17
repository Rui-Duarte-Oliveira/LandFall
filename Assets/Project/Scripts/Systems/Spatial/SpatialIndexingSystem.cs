using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using RTS.Core.Tags;
using RTS.Core.Components;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Singleton component that exposes the spatial map to other systems.
    /// </summary>
    public struct SpatialMapData : IComponentData
    {
        public NativeParallelMultiHashMap<int, Entity> Map;
    }

    /// <summary>
    /// Spatial partition for fast proximity lookups. 
    /// 
    /// Uses a fixed-grid hash to avoid the O(n�) bottleneck of checking every unit against
    /// every other unit. This is the primary system for local avoidance, targeting, 
    /// and range checks.
    /// 
    /// Adjust the cell size in the config if the unit density changes; 
    /// if cells are too large, we lose the performance gain, if too small, 
    /// we waste memory on empty buckets.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(RTSGameplaySystemGroup), OrderFirst = true)]
    public partial struct SpatialIndexingSystem : ISystem
    {
        /// <summary>
        /// Size of each spatial cell in world units.
        /// </summary>
        public const float CELL_SIZE = 10f;

        /// <summary>
        /// Inverse cell size for fast division.
        /// </summary>
        public const float INV_CELL_SIZE = 1f / CELL_SIZE;

        /// <summary>
        /// Grid dimensions (total grid = GRID_SIZE x GRID_SIZE cells).
        /// </summary>
        public const int GRID_SIZE = 256;

        private NativeParallelMultiHashMap<int, Entity> _spatialMap;

        public void OnCreate(ref SystemState state)
        {
            //Initial capacity - will grow as needed
            _spatialMap = new NativeParallelMultiHashMap<int, Entity>(
                1000,
                Allocator.Persistent
            );

            //Create singleton to expose map to other systems
            Entity singleton = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singleton, new SpatialMapData { Map = _spatialMap });
            
            #if UNITY_EDITOR
            state.EntityManager.SetName(singleton, "SpatialMapData");
            #endif

            state.RequireForUpdate<RTSGameTime>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_spatialMap.IsCreated)
                _spatialMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Only rebuild on simulation tick
            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();
            if (!SystemAPI.IsComponentEnabled<SimulationTickEvent>(timeEntity))
            {
                return;
            }

            RTSGameTime gameTime = SystemAPI.GetSingleton<RTSGameTime>();

            //Clear previous frame's data
            _spatialMap.Clear();

            // Schedule parallel job to populate the spatial map
            var populateJob = new PopulateSpatialMapJob
            {
                SpatialMap = _spatialMap.AsParallelWriter(),
                CurrentTick = gameTime.CurrentTick
            };

            state.Dependency = populateJob.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Get the spatial map for read-only queries.
        /// Call this from other systems that need neighbor lookups.
        /// </summary>
        public NativeParallelMultiHashMap<int, Entity> GetSpatialMap() => _spatialMap;

        /// <summary>
        /// Compute cell index from world position.
        /// </summary>
        public static int PositionToCell(float3 position)
        {
            //Offset to handle negative positions
            int x = (int)math.floor(position.x * INV_CELL_SIZE) + GRID_SIZE / 2;
            int z = (int)math.floor(position.z * INV_CELL_SIZE) + GRID_SIZE / 2;

            //Clamp to grid bounds
            x = math.clamp(x, 0, GRID_SIZE - 1);
            z = math.clamp(z, 0, GRID_SIZE - 1);

            return z * GRID_SIZE + x;
        }

        /// <summary>
        /// Get cell indices for a circular query region.
        /// </summary>
        public static void GetCellsInRadius(
            float3 center,
            float radius,
            ref NativeList<int> cells)
        {
            cells.Clear();

            int cellRadius = (int)math.ceil(radius * INV_CELL_SIZE);
            int centerX = (int)math.floor(center.x * INV_CELL_SIZE) + GRID_SIZE / 2;
            int centerZ = (int)math.floor(center.z * INV_CELL_SIZE) + GRID_SIZE / 2;

            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    if (x >= 0 && x < GRID_SIZE && z >= 0 && z < GRID_SIZE)
                    {
                        cells.Add(z * GRID_SIZE + x);
                    }
                }
            }
        }

        /// <summary>
        /// Job that populates the spatial hash map in parallel.
        /// </summary>
        [BurstCompile]
        public partial struct PopulateSpatialMapJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter SpatialMap;
            public uint CurrentTick;

            void Execute(
                Entity entity,
                in AuthoritativeTransform transform,
                ref SpatialCell cell,
                in SpatiallyIndexedTag tag)
            {
                int cellIndex = PositionToCell(transform.Position);
                cell.CellIndex = cellIndex;
                cell.LastUpdateTick = CurrentTick;

                SpatialMap.Add(cellIndex, entity);
            }
        }
    }

    /// <summary>
    /// High-level helpers for spatial queries. 
    /// 
    /// These are intended for readability in gameplay code, but be aware they 
    /// aren't Burst-compatible due to the delegate/interface overhead. 
    /// 
    /// If you're calling these from a heavy loop (like local avoidance), 
    /// you'll need to bypass these and implement the raw grid lookup 
    /// directly inside your Job to keep it on the worker threads.
    /// </summary>
    public static class SpatialQueryExtensions
    {
        /// <summary>
        /// Find all entities within radius of a position.
        /// </summary>
        public static void FindEntitiesInRadius(
            this NativeParallelMultiHashMap<int, Entity> spatialMap,
            float3 center,
            float radius,
            ref NativeList<Entity> results,
            ComponentLookup<LocalTransform> transformLookup)
        {
            results.Clear();
            float radiusSq = radius * radius;

            //Get all cells that might contain entities in range
            var cells = new NativeList<int>(16, Allocator.Temp);
            SpatialIndexingSystem.GetCellsInRadius(center, radius, ref cells);

            //Check each cell
            foreach (int cellIndex in cells)
            {
                if (spatialMap.TryGetFirstValue(cellIndex, out Entity entity, out var iterator))
                {
                    do
                    {
                        //Precise distance check
                        if (transformLookup.TryGetComponent(entity, out LocalTransform transform))
                        {
                            float distSq = math.distancesq(center, transform.Position);
                            if (distSq <= radiusSq)
                            {
                                results.Add(entity);
                            }
                        }
                    }
                    while (spatialMap.TryGetNextValue(out entity, ref iterator));
                }
            }

            cells.Dispose();
        }

        /// <summary>
        /// Find the closest entity to a position within max range.
        /// Returns Entity.Null if none found.
        /// </summary>
        public static Entity FindClosestEntity(
            this NativeParallelMultiHashMap<int, Entity> spatialMap,
            float3 center,
            float maxRadius,
            ComponentLookup<LocalTransform> transformLookup,
            Entity excludeEntity = default)
        {
            Entity closest = Entity.Null;
            float closestDistSq = maxRadius * maxRadius;

            var cells = new NativeList<int>(16, Allocator.Temp);
            SpatialIndexingSystem.GetCellsInRadius(center, maxRadius, ref cells);

            foreach (int cellIndex in cells)
            {
                if (spatialMap.TryGetFirstValue(cellIndex, out Entity entity, out var iterator))
                {
                    do
                    {
                        if (entity == excludeEntity) continue;

                        if (transformLookup.TryGetComponent(entity, out LocalTransform transform))
                        {
                            float distSq = math.distancesq(center, transform.Position);
                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closest = entity;
                            }
                        }
                    }
                    while (spatialMap.TryGetNextValue(out entity, ref iterator));
                }
            }

            cells.Dispose();
            return closest;
        }
    }
}