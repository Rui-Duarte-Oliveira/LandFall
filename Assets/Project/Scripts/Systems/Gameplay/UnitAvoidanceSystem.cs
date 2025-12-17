using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using RTS.Core.Components;
using RTS.Core.Tags;

namespace RTS.Core.Systems
{
    /// <summary>
    /// Applies separation steering to prevent units from overlapping.
    /// Uses parallel job for O(n²) neighbor checks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(RTSGameplaySystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    public partial struct UnitAvoidanceSystem : ISystem
    {
        private EntityQuery _unitsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();

            _unitsQuery = SystemAPI.QueryBuilder()
                .WithAll<UnitTag, AliveState, AuthoritativeTransform, AvoidanceData>()
                .Build();
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

            int unitCount = _unitsQuery.CalculateEntityCount();
            if (unitCount <= 1) return;

            RTSGameTime gameTime = SystemAPI.GetSingleton<RTSGameTime>();
            float deltaTime = gameTime.SecondsPerTick;

            //Collect positions into arrays for fast lookup
            var positions = new NativeArray<float3>(unitCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var radii = new NativeArray<float>(unitCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int index = 0;
            foreach (var (auth, avoidance) in
                SystemAPI.Query<RefRO<AuthoritativeTransform>, RefRO<AvoidanceData>>()
                    .WithAll<UnitTag>()
                    .WithAll<AliveState>())
            {
                positions[index] = auth.ValueRO.Position;
                radii[index] = avoidance.ValueRO.Radius;
                index++;
            }

            //Schedule parallel avoidance job
            AvoidanceJob avoidanceJob = new AvoidanceJob
            {
                DeltaTime = deltaTime,
                AllPositions = positions,
                AllRadii = radii,
                UnitCount = unitCount
            };

            state.Dependency = avoidanceJob.ScheduleParallel(state.Dependency);

            //Dispose arrays after job completes
            state.Dependency = positions.Dispose(state.Dependency);
            state.Dependency = radii.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(UnitTag), typeof(AliveState))]
        private partial struct AvoidanceJob : IJobEntity
        {
            public float DeltaTime;

            [ReadOnly] public NativeArray<float3> AllPositions;
            [ReadOnly] public NativeArray<float> AllRadii;
            public int UnitCount;

            void Execute(ref AuthoritativeTransform authTransform, in AvoidanceData avoidance)
            {
                float3 myPos = authTransform.Position;
                float myRadius = avoidance.Radius;
                float3 separationForce = float3.zero;
                int neighborCount = 0;

                for (int i = 0; i < UnitCount; i++)
                {
                    float3 otherPos = AllPositions[i];
                    float otherRadius = AllRadii[i];

                    float3 toOther = otherPos - myPos;
                    toOther.y = 0;

                    float distanceSq = math.lengthsq(toOther);
                    float combinedRadius = myRadius + otherRadius;
                    float avoidanceRadius = combinedRadius * 2.5f;
                    float avoidanceRadiusSq = avoidanceRadius * avoidanceRadius;

                    //Skip self (distance ~= 0) and units outside range
                    if (distanceSq < 0.001f || distanceSq >= avoidanceRadiusSq)
                        continue;

                    float distance = math.sqrt(distanceSq);
                    float3 awayDir = -toOther / distance;

                    float strength = 1f - (distance / avoidanceRadius);
                    strength = strength * strength;

                    separationForce += awayDir * strength;
                    neighborCount++;
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