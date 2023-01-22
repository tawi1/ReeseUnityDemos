using Reese.Path;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.SceneManagement;

namespace Reese.Demo
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PathSteeringSystem))]
    public partial class PathMoveSystem : SystemBase
    {
        public static readonly float TRANSLATION_SPEED = 20;
        public static readonly float ROTATION_SPEED = 0.3f;

        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();

        protected override void OnCreate()
        {
            if (!SceneManager.GetActiveScene().name.Equals("PathDemo")) Enabled = false;
        }
        static void Translate(float deltaSeconds, PathSteering steering, ref LocalTransform transform)
            => transform.Position += steering.CurrentHeading * TRANSLATION_SPEED * deltaSeconds;

        static void Rotate(float deltaSeconds, PathSteering steering, ref LocalTransform transform)
        {
            var lookAt = transform.Position + steering.CurrentHeading;
            lookAt.y = transform.Position.y;

            var lookRotation = quaternion.LookRotationSafe(lookAt - transform.Position, math.up());

            transform.Rotation = math.slerp(transform.Rotation, lookRotation, deltaSeconds / ROTATION_SPEED);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var deltaSeconds = SystemAPI.Time.DeltaTime;

            Entities
                .WithNone<PathProblem, PathDestination, PathPlanning>()
                .ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform, in PathSteering steering) =>
                {
                    Translate(deltaSeconds, steering, ref transform);
                    Rotate(deltaSeconds, steering, ref transform);
                })
                .WithName("PathMoveJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
