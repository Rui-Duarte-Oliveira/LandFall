using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using RTS.Core.Components;
using RTS.Core.Tags;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Applies separation steering to prevent units from overlapping.
    /// Uses spatial hashing for O(N*k) neighbor checks (was O(N^2)).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(RTSGameplaySystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    public partial struct UnitAvoidanceSystem : ISystem
    {
        private ComponentLookup<AuthoritativeTransform> _transformLookup;
        private ComponentLookup<AvoidanceData> _avoidanceLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
            state.RequireForUpdate<SpatialMapData>();

            _transformLookup = state.GetComponentLookup<AuthoritativeTransform>(true);
            _avoidanceLookup = state.GetComponentLookup<AvoidanceData>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Only run on simulation ticks
            if (!SystemAPI.HasSingleton<RTSGameTime>())
                return;

            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();
            if (!SystemAPI.IsComponentEnabled<SimulationTickEvent>(timeEntity))
                return;

            RTSGameTime gameTime = SystemAPI.GetSingleton<RTSGameTime>();
            float deltaTime = gameTime.SecondsPerTick;
            
            //Update lookups
            _transformLookup.Update(ref state);
            _avoidanceLookup.Update(ref state);

            //Get spatial map
            var spatialData = SystemAPI.GetSingleton<SpatialMapData>();

            //Schedule parallel avoidance job
            AvoidanceJob avoidanceJob = new AvoidanceJob
            {
                DeltaTime = deltaTime,
                SpatialMap = spatialData.Map,
                TransformLookup = _transformLookup,
                AvoidanceLookup = _avoidanceLookup
            };

            state.Dependency = avoidanceJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(UnitTag), typeof(AliveState))]
        private partial struct AvoidanceJob : IJobEntity
        {
            public float DeltaTime;

            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;

            // SAFETY: We disable safety checks here because we need to read AuthoritativeTransform from neighbors
            // while having write access to it on the current entity (via ref in Execute).
            // We guarantee we do not read our own entity from this lookup in the loop.
            [ReadOnly] 
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<AuthoritativeTransform> TransformLookup;

            [ReadOnly] public ComponentLookup<AvoidanceData> AvoidanceLookup;

            void Execute(Entity entity, ref AuthoritativeTransform authTransform, in AvoidanceData avoidance)
            {
                float3 myPos = authTransform.Position;
                float myRadius = avoidance.Radius;
                float3 separationForce = float3.zero;
                int neighborCount = 0;

                //Determine search radius (max possible avoidance interaction)
                //We use a conservative estimate: myRadius + max_possible_other_radius * 2.5f factor
                
                int centerCell = SpatialIndexingSystem.PositionToCell(myPos);
                
                //Decode cell coordinates
                int gridW = SpatialIndexingSystem.GRID_SIZE;
                int cx = centerCell % gridW;
                int cy = centerCell / gridW;

                //Iterate 3x3 neighbors
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx;
                        int ny = cy + dy;

                        //Bounds check
                        if (nx < 0 || nx >= gridW || ny < 0 || ny >= gridW)
                            continue;

                        int neighborCellIndex = ny * gridW + nx;

                        if (SpatialMap.TryGetFirstValue(neighborCellIndex, out Entity neighbor, out var it))
                        {
                            do
                            {
                                //Skip self
                                if (neighbor == entity) continue;

                                //Manual component lookup
                                if (!TransformLookup.HasComponent(neighbor) || !AvoidanceLookup.HasComponent(neighbor))
                                    continue;

                                float3 otherPos = TransformLookup[neighbor].Position;
                                float otherRadius = AvoidanceLookup[neighbor].Radius;

                                float3 toOther = otherPos - myPos;
                                toOther.y = 0;

                                float distanceSq = math.lengthsq(toOther);
                                float combinedRadius = myRadius + otherRadius;
                                float avoidanceRadius = combinedRadius * 2.5f;
                                float avoidanceRadiusSq = avoidanceRadius * avoidanceRadius;

                                //Optimization: Skip if outside strict range
                                if (distanceSq < 0.001f || distanceSq >= avoidanceRadiusSq)
                                    continue;

                                float distance = math.sqrt(distanceSq);
                                float3 awayDir = -toOther / distance;

                                float strength = 1f - (distance / avoidanceRadius);
                                strength = strength * strength;

                                separationForce += awayDir * strength;
                                neighborCount++;

                            } while (SpatialMap.TryGetNextValue(out neighbor, ref it));
                        }
                    }
                }

                if (neighborCount > 0)
                {
                    separationForce /= neighborCount;
                    separationForce = math.normalizesafe(separationForce);

                    float3 avoidanceVelocity = separationForce * avoidance.AvoidanceStrength;

                    float mag = math.length(avoidanceVelocity);
                    if (mag > avoidance.MaxAvoidanceForce)
                    {
                        avoidanceVelocity = (avoidanceVelocity / mag) * avoidance.MaxAvoidanceForce;
                    }

                    authTransform.Position += avoidanceVelocity * DeltaTime;
                    authTransform.Position.y = 1f;
                }
            }
        }
    }
}