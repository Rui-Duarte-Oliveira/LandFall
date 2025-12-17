using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using RTS.Core.Components;

namespace RTS.Core.Systems
{
    // Before simulation - Restore authoritative positions
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(TimeAccumulationSystem))]
    public partial struct SyncAuthoritativeBeforeSimSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new RestoreJob().ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(InterpolateMovement))]
        private partial struct RestoreJob : IJobEntity
        {
            void Execute(ref LocalTransform transform, in AuthoritativeTransform auth)
            {
                transform.Position = auth.Position;
                transform.Rotation = auth.Rotation;
            }
        }
    }

    //At tick start - Save current as previous
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SimulationTickSystem))]
    [UpdateBefore(typeof(RTSGameplaySystemGroup))]
    public partial struct SavePreviousTransformSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<RTSGameTime>())
                return;

            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();
            if (!SystemAPI.IsComponentEnabled<SimulationTickEvent>(timeEntity))
                return;

            new SavePreviousJob().ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(InterpolateMovement))]
        private partial struct SavePreviousJob : IJobEntity
        {
            void Execute(in AuthoritativeTransform auth, ref PreviousTransform prev)
            {
                prev.Position = auth.Position;
                prev.Rotation = auth.Rotation;
            }
        }
    }


    //After simulation - Sync LocalTransform from Authoritative
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SyncLocalFromAuthoritativeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<RTSGameTime>())
                return;

            Entity timeEntity = SystemAPI.GetSingletonEntity<RTSGameTime>();
            if (!SystemAPI.IsComponentEnabled<SimulationTickEvent>(timeEntity))
                return;

            new SyncLocalJob().ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        private partial struct SyncLocalJob : IJobEntity
        {
            void Execute(ref LocalTransform transform, in AuthoritativeTransform auth)
            {
                transform.Position = auth.Position;
                transform.Rotation = auth.Rotation;
            }
        }
    }

    //Every frame - Interpolate for smooth rendering
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct InterpolateTransformSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RTSGameTime>();
            state.RequireForUpdate<SimulationTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SimulationTimeState>())
                return;

            var timeState = SystemAPI.GetSingleton<SimulationTimeState>();

            if (timeState.IsPaused)
                return;

            new InterpolateJob { Alpha = timeState.InterpolationAlpha }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(InterpolateMovement))]
        private partial struct InterpolateJob : IJobEntity
        {
            public float Alpha;

            void Execute(ref LocalTransform transform, in AuthoritativeTransform auth, in PreviousTransform prev)
            {
                transform.Position = math.lerp(prev.Position, auth.Position, Alpha);
                transform.Rotation = math.slerp(prev.Rotation, auth.Rotation, Alpha);
            }
        }
    }
}