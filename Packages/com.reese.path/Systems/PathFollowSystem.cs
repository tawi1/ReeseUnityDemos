using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Reese.Path
{
    [BurstCompile]
    public partial struct PathFollowSystem : ISystem
    {
        private EntityQuery updateQuery;

        public void OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<PathProblem>()
                .WithAll<PathAgent, PathFollow>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pathFollowJob = new PathFollowJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                LocalToWorldFromEntity = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                DestinationFromEntity = SystemAPI.GetComponentLookup<PathDestination>(true),
            };

            pathFollowJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(PathProblem))]
        [WithAll(typeof(PathAgent))]
        [BurstCompile]
        public partial struct PathFollowJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldFromEntity;

            [ReadOnly]
            public ComponentLookup<PathDestination> DestinationFromEntity;

            void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex, in PathFollow follow)
            {
                if (
                       !LocalToWorldFromEntity.HasComponent(follow.Target) ||
                       !DestinationFromEntity.HasComponent(follow.Target)
                   ) return;

                var followerPosition = LocalToWorldFromEntity[entity].Position;
                var targetPosition = LocalToWorldFromEntity[follow.Target].Position;
                var distance = math.distance(followerPosition, targetPosition);

                if (follow.MaxDistance > 0 && distance > follow.MaxDistance)
                {
                    CommandBuffer.RemoveComponent<PathFollow>(entityInQueryIndex, entity);
                    return;
                }

                if (distance < follow.MinDistance) return;

                var targetDestination = DestinationFromEntity[follow.Target];

                CommandBuffer.SetComponent(entityInQueryIndex, entity, new PathDestination
                {
                    WorldPoint = targetPosition,
                    Tolerance = targetDestination.Tolerance
                });

                CommandBuffer.SetComponentEnabled<PathDestination>(entityInQueryIndex, entity, true);
            }
        }
    }
}
