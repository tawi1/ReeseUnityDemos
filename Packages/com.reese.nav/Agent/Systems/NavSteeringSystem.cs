using Reese.Math;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Reese.Nav.NavSystem;

namespace Reese.Nav
{
    /// <summary>Calculates the current heading.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(NavCollisionSystem))]
    public partial class NavSteeringSystem : SystemBase
    {
        NavSystem navSystem => World.GetOrCreateSystemManaged<NavSystem>();
        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();

        static void HandleCompletePath(
            ComponentLookup<LocalToWorld> localToWorldFromEntity,
            Entity entity,
            ref NavAgent agent,
            Parent surface,
            LocalTransform transform,
            PhysicsWorld physicsWorld,
            float elapsedSeconds,
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int entityInQueryIndex,
            NavSettings settings
        )
        {
            var rayInput = new RaycastInput
            {
                Start = localToWorldFromEntity[entity].Position + agent.Offset,
                End = math.forward(transform.Rotation) * settings.ObstacleRaycastDistanceMax,
                Filter = new CollisionFilter
                {
                    BelongsTo = NavUtil.ToBitMask(settings.ColliderLayer),
                    CollidesWith = NavUtil.ToBitMask(settings.ObstacleLayer)
                }
            };

            if (
                !surface.Value.Equals(agent.DestinationSurface) &&
                !NavUtil.ApproxEquals(transform.Position, agent.LocalDestination, settings.StoppingDistance) &&
                !physicsWorld.CastRay(rayInput, out _)
            )
            {
                agent.JumpSeconds = elapsedSeconds;

                commandBuffer.RemoveComponent<NavWalking>(entityInQueryIndex, entity);
                commandBuffer.RemoveComponent<NavSteering>(entityInQueryIndex, entity);
                commandBuffer.AddComponent<NavJumping>(entityInQueryIndex, entity);
                commandBuffer.AddComponent<NavPlanning>(entityInQueryIndex, entity);

                return;
            }

            commandBuffer.RemoveComponent<NavWalking>(entityInQueryIndex, entity);
            commandBuffer.RemoveComponent<NavSteering>(entityInQueryIndex, entity);
            commandBuffer.RemoveComponent<NavDestination>(entityInQueryIndex, entity);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            var deltaSeconds = SystemAPI.Time.DeltaTime;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var settings = navSystem.Settings;
            var pathBufferFromEntity = GetBufferLookup<NavPathBufferElement>();

            var localToWorldFromEntity = GetComponentLookup<LocalToWorld>(true);
            var fallingFromEntity = GetComponentLookup<NavFalling>(true);
            var jumpingFromEntity = GetComponentLookup<NavJumping>(true);
            var flockingFromEntity = GetComponentLookup<NavFlocking>(true);

            Entities
                .WithNone<NavProblem, NavPlanning>()
                .WithAll<NavWalking, ParentTransform>()
                .WithReadOnly(localToWorldFromEntity)
                .WithReadOnly(physicsWorld)
                .WithReadOnly(jumpingFromEntity)
                .WithReadOnly(fallingFromEntity)
                .WithReadOnly(flockingFromEntity)
                .WithNativeDisableParallelForRestriction(pathBufferFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref NavAgent agent, ref LocalTransform transform, ref NavSteering navSteering, in Parent surface) =>
                {
                    if (!pathBufferFromEntity.HasBuffer(entity) || agent.DestinationSurface.Equals(Entity.Null)) return;

                    var pathBuffer = pathBufferFromEntity[entity];

                    if (pathBuffer.Length == 0)
                    {
                        HandleCompletePath(localToWorldFromEntity, entity, ref agent, surface, transform, physicsWorld, elapsedSeconds, commandBuffer, entityInQueryIndex, settings);
                        return;
                    }

                    var pathBufferIndex = pathBuffer.Length - 1;

                    if (NavUtil.ApproxEquals(transform.Position, pathBuffer[pathBufferIndex].Value, settings.StoppingDistance)) pathBuffer.RemoveAt(pathBufferIndex);

                    if (pathBuffer.Length == 0) return;

                    pathBufferIndex = pathBuffer.Length - 1;

                    var heading = math.normalizesafe(pathBuffer[pathBufferIndex].Value - transform.Position);

                    if (
                        !jumpingFromEntity.HasComponent(entity) &&
                        !fallingFromEntity.HasComponent(entity) &&
                        flockingFromEntity.HasComponent(entity)
                    )
                    {
                        navSteering.AgentAvoidanceSteering.y = navSteering.SeparationSteering.y = navSteering.AlignmentSteering.y = navSteering.CohesionSteering.y = 0;

                        heading = math.normalizesafe(
                            heading +
                            navSteering.AgentAvoidanceSteering +
                            navSteering.SeparationSteering +
                            navSteering.AlignmentSteering +
                            navSteering.CohesionSteering
                        );

                        if (!navSteering.CollisionAvoidanceSteering.Equals(float3.zero)) heading = math.normalizesafe(heading + navSteering.CollisionAvoidanceSteering);
                    }

                    navSteering.CurrentHeading = heading;
                })
                .WithName("NavSteeringJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);

            var jumpBufferFromEntity = GetBufferLookup<NavJumpBufferElement>();

            Entities
                .WithNone<NavProblem>()
                .WithAny<NavFalling, NavJumping>()
                .WithAll<ParentTransform>()
                .WithReadOnly(fallingFromEntity)
                .WithReadOnly(jumpBufferFromEntity)
                .WithReadOnly(localToWorldFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform, in NavAgent agent, in Parent surface) =>
                {
                    if (agent.DestinationSurface.Equals(Entity.Null)) return;

                    commandBuffer.AddComponent<NavPlanning>(entityInQueryIndex, entity);

                    if (!jumpBufferFromEntity.HasBuffer(entity)) return;
                    var jumpBuffer = jumpBufferFromEntity[entity];
                    if (jumpBuffer.Length == 0 && !fallingFromEntity.HasComponent(entity)) return;

                    var destinationSurfaceLocalToWorld = localToWorldFromEntity[agent.DestinationSurface];
                    var worldDestination = agent.LocalDestination.ToWorld(destinationSurfaceLocalToWorld);
                    var velocity = math.distance(transform.Position, worldDestination) / (math.sin(2 * math.radians(agent.JumpDegrees)) / agent.JumpGravity);
                    var yVelocity = math.sqrt(velocity) * math.sin(math.radians(agent.JumpDegrees));
                    var waypoint = transform.Position + math.up() * float.NegativeInfinity;

                    if (!fallingFromEntity.HasComponent(entity))
                    {
                        var xVelocity = math.sqrt(velocity) * math.cos(math.radians(agent.JumpDegrees)) * agent.JumpSpeedMultiplierX;
                        var surfaceLocalToWorld = localToWorldFromEntity[surface.Value];

                        waypoint = jumpBuffer[0].Value
                            .ToWorld(destinationSurfaceLocalToWorld)
                            .ToLocal(surfaceLocalToWorld);

                        transform.Position.MoveTowards(waypoint, xVelocity * deltaSeconds);
                    }

                    transform.Position.y += (yVelocity - (elapsedSeconds - agent.JumpSeconds) * agent.JumpGravity) * deltaSeconds * agent.JumpSpeedMultiplierY;

                    if (elapsedSeconds - agent.JumpSeconds >= settings.JumpSecondsMax)
                    {
                        commandBuffer.RemoveComponent<NavJumping>(entityInQueryIndex, entity);
                        commandBuffer.AddComponent<NavFalling>(entityInQueryIndex, entity);
                    }

                    if (!NavUtil.ApproxEquals(transform.Position, waypoint, 1)) return;

                    commandBuffer.AddComponent<NavNeedsSurface>(entityInQueryIndex, entity);
                    commandBuffer.RemoveComponent<NavJumping>(entityInQueryIndex, entity);
                })
                .WithName("NavGravityJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
