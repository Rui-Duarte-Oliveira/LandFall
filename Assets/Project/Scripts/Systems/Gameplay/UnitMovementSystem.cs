using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using RTS.Core.Components;
using RTS.Core.Tags;

namespace RTS.Core.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RTSGameplaySystemGroup))]
    public partial struct UnitMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
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

            var gameTime = SystemAPI.GetSingleton<RTSGameTime>();
            float deltaTime = gameTime.SecondsPerTick;

            //Process units WITH interpolation
            foreach (var (authTransform, movement, destination, formation, entity) in
                SystemAPI.Query<RefRW<AuthoritativeTransform>, RefRW<Movement>, RefRW<MoveDestination>, RefRO<FormationSlot>>()
                    .WithAll<UnitTag>()
                    .WithAll<AliveState>()
                    .WithAll<InterpolateMovement>()
                    .WithEntityAccess())
            {
                //Use formation target if valid, otherwise raw destination
                float3 targetPos = destination.ValueRO.Destination;
                if (math.lengthsq(formation.ValueRO.TargetPosition) > 0.1f)
                {
                    targetPos = formation.ValueRO.TargetPosition;
                }

                float stoppingDistance = destination.ValueRO.StoppingDistance;

                //INLINED MOVEMENT CODE
                float3 toTarget = targetPos - authTransform.ValueRW.Position;
                toTarget.y = 0;

                float distance = math.length(toTarget);

                if (distance <= stoppingDistance)
                {
                    movement.ValueRW.Velocity = float3.zero;
                    SystemAPI.SetComponentEnabled<MoveDestination>(entity, false);
                    continue;
                }

                float3 direction = toTarget / distance;

                //Rotation
                if (math.lengthsq(direction) > 0.001f)
                {
                    quaternion targetRot = quaternion.LookRotationSafe(direction, math.up());
                    float maxRadians = movement.ValueRO.TurnRate * deltaTime;

                    float dot = math.abs(math.dot(authTransform.ValueRO.Rotation.value, targetRot.value));
                    float angle = math.acos(math.min(dot, 1f)) * 2f;

                    if (angle > 0.001f)
                    {
                        float t = math.min(1f, maxRadians / angle);
                        authTransform.ValueRW.Rotation = math.slerp(authTransform.ValueRO.Rotation, targetRot, t);
                    }
                }

                //Speed
                float targetSpeed = movement.ValueRO.MaxSpeed;
                float slowdownDist = stoppingDistance * 3f;
                if (distance < slowdownDist && slowdownDist > 0)
                {
                    targetSpeed *= distance / slowdownDist;
                }

                float currentSpeed = math.length(movement.ValueRO.Velocity);
                float speedDiff = targetSpeed - currentSpeed;
                float maxSpeedChange = movement.ValueRO.Acceleration * deltaTime;

                float newSpeed = math.abs(speedDiff) <= maxSpeedChange
                    ? targetSpeed
                    : currentSpeed + math.sign(speedDiff) * maxSpeedChange;

                movement.ValueRW.Velocity = direction * newSpeed;
                authTransform.ValueRW.Position += movement.ValueRO.Velocity * deltaTime;
                authTransform.ValueRW.Position.y = 1f;
                //END INLINED CODE
            }

            //Process units WITHOUT interpolation
            foreach (var (transform, movement, destination, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<Movement>, RefRW<MoveDestination>>()
                    .WithAll<UnitTag>()
                    .WithAll<AliveState>()
                    .WithNone<InterpolateMovement>()
                    .WithEntityAccess())
            {
                float3 targetPos = destination.ValueRO.Destination;
                float stoppingDistance = destination.ValueRO.StoppingDistance;

                //INLINED MOVEMENT CODE
                float3 toTarget = targetPos - transform.ValueRW.Position;
                toTarget.y = 0;

                float distance = math.length(toTarget);

                if (distance <= stoppingDistance)
                {
                    movement.ValueRW.Velocity = float3.zero;
                    SystemAPI.SetComponentEnabled<MoveDestination>(entity, false);
                    continue;
                }

                float3 direction = toTarget / distance;

                //Rotation
                if (math.lengthsq(direction) > 0.001f)
                {
                    quaternion targetRot = quaternion.LookRotationSafe(direction, math.up());
                    float maxRadians = movement.ValueRO.TurnRate * deltaTime;
                    float dot = math.abs(math.dot(transform.ValueRO.Rotation.value, targetRot.value));
                    float angle = math.acos(math.min(dot, 1f)) * 2f;
                    if (angle > 0.001f)
                    {
                        float t = math.min(1f, maxRadians / angle);
                        transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRot, t);
                    }
                }

                //Speed
                float targetSpeed = movement.ValueRO.MaxSpeed;
                float slowdownDist = stoppingDistance * 3f;
                if (distance < slowdownDist && slowdownDist > 0)
                {
                    targetSpeed *= distance / slowdownDist;
                }

                float currentSpeed = math.length(movement.ValueRO.Velocity);
                float speedDiff = targetSpeed - currentSpeed;
                float maxSpeedChange = movement.ValueRO.Acceleration * deltaTime;
                float newSpeed = math.abs(speedDiff) <= maxSpeedChange
                    ? targetSpeed
                    : currentSpeed + math.sign(speedDiff) * maxSpeedChange;

                movement.ValueRW.Velocity = direction * newSpeed;
                transform.ValueRW.Position += movement.ValueRO.Velocity * deltaTime;
                transform.ValueRW.Position.y = 1f;
                //END INLINED CODE
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}