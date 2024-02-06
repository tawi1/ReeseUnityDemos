using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Reese.Path
{
    /// <summary>Manages destinations for agents.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct PathDestinationSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;
        private PathSystem.PathSettings pathSettings;

        public void OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<PathProblem>()
                .WithAllRW<PathAgent>()
                .WithAll<PathDestination>()
                .Build();

            updateQuery.AddChangedVersionFilter(ComponentType.ReadOnly<PathDestination>());
            state.RequireForUpdate(updateQuery);
        }

        public void OnStartRunning(ref SystemState state)
        {
            pathSettings = PathSystem.SettingsStaticRef;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pathDestinationJob = new PathDestinationJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                PathSettings = pathSettings,
                ElapsedSeconds = (float)SystemAPI.Time.ElapsedTime
            };

            pathDestinationJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(PathProblem))]
        [BurstCompile]
        public partial struct PathDestinationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public PathSystem.PathSettings PathSettings;

            [ReadOnly]
            public float ElapsedSeconds;

            void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex, ref PathAgent agent, in PathDestination destination)
            {
                if (ElapsedSeconds - agent.DestinationSeconds < PathSettings.DestinationRateLimitSeconds)
                {
                    CommandBuffer.AddComponent(entityInQueryIndex, entity, destination); // So that the change filter applies next frame.
                    return;
                }

                agent.WorldDestination = destination.WorldPoint + agent.Offset;
                agent.DestinationSeconds = ElapsedSeconds;

                CommandBuffer.SetComponentEnabled<PathPlanning>(entityInQueryIndex, entity, true);
            }
        }
    }
}
