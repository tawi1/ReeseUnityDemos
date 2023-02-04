using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Reese.Path
{
    /// <summary>Manages destinations for agents.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
    public partial class PathDestinationSystem : SystemBase
    {
        PathSystem pathSystem => World.GetOrCreateSystemManaged<PathSystem>();
        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            var settings = pathSystem.Settings;

            Entities
                .WithNone<PathProblem>()
                .WithChangeFilter<PathDestination>()
                .ForEach((Entity entity, int entityInQueryIndex, ref PathAgent agent, in PathDestination destination) =>
                {
                    if (elapsedSeconds - agent.DestinationSeconds < settings.DestinationRateLimitSeconds)
                    {
                        commandBuffer.AddComponent(entityInQueryIndex, entity, destination); // So that the change filter applies next frame.
                        return;
                    }

                    agent.WorldDestination = destination.WorldPoint + agent.Offset;
                    agent.DestinationSeconds = elapsedSeconds;

                    commandBuffer.AddComponent<PathPlanning>(entityInQueryIndex, entity);
                })
                .WithName("PathDestinationJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
