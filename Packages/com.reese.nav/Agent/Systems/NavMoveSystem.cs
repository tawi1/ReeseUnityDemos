using Reese.Math;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Reese.Nav
{
    /// <summary>Translates and rotates agents based on their current heading.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(NavSteeringSystem))]
    public partial class NavMoveSystem : SystemBase
    {
        static void Translate(float deltaSeconds, NavSteering steering, NavAgent agent, ref LocalTransform transform)
            => transform.Position += steering.CurrentHeading * agent.TranslationSpeed * deltaSeconds;

        static void Rotate(float deltaSeconds, LocalToWorld destinationSurfaceLocalToWorld, LocalToWorld surfaceLocalToWorld, NavSteering steering, NavAgent agent, ref LocalTransform transform)
        {
            var lookAt = (transform.Position + steering.CurrentHeading)
                .ToWorld(destinationSurfaceLocalToWorld)
                .ToLocal(surfaceLocalToWorld);

            lookAt.y = transform.Position.y;

            var lookRotation = quaternion.LookRotationSafe(lookAt - transform.Position, math.up());

            if (math.length(agent.SurfacePointNormal) > 0.01f) lookRotation = math.mul(lookRotation, math.up().FromToRotation(agent.SurfacePointNormal));

            transform.Rotation = math.slerp(transform.Rotation, lookRotation, deltaSeconds / agent.RotationSpeed);
        }

        protected override void OnUpdate()
        {
            var deltaSeconds = SystemAPI.Time.DeltaTime;
            var localToWorldFromEntity = GetComponentLookup<LocalToWorld>(true);

            Entities
                .WithNone<NavProblem, NavPlanning>()
                .WithAll<NavWalking, ParentTransform>()
                .WithReadOnly(localToWorldFromEntity)
                .ForEach(
                    (Entity entity, ref LocalTransform transform, in NavAgent agent, in NavSteering steering, in Parent surface) =>
                    {
                        if (agent.DestinationSurface.Equals(Entity.Null)) return;

                        Translate(deltaSeconds, steering, agent, ref transform);

                        var destinationSurfaceLocalToWorld = localToWorldFromEntity[agent.DestinationSurface];
                        var surfaceLocalToWorld = localToWorldFromEntity[surface.Value];

                        Rotate(deltaSeconds, destinationSurfaceLocalToWorld, surfaceLocalToWorld, steering, agent, ref transform);
                    }
                )
                .WithName("NavMoveJob")
                .ScheduleParallel();
        }
    }
}
