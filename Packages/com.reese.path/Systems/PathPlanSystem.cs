using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace Reese.Path
{
    /// <summary>Plans paths using UnityEngine.Experimental.AI. Each entity
    /// gets its own NavMeshQuery by thread index. NavMeshQuery orchestration
    /// here appears to be exemplary usage. Note that it depends on the
    /// third-party PathUtils.</summary>
	[RequireMatchingQueriesForUpdate]
    unsafe public partial class PathPlanSystem : SystemBase
    {
        PathSystem pathSystem => World.GetOrCreateSystemManaged<PathSystem>();

        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var pathBufferFromEntity = GetBufferLookup<PathBufferElement>();
            var navMeshQueryPointerArray = World.GetExistingSystemManaged<PathMeshQuerySystem>().PointerArray;
            var settings = pathSystem.Settings;

            Entities
                .WithNone<PathProblem>()
                .WithAll<PathPlanning>()
                .WithNativeDisableParallelForRestriction(pathBufferFromEntity)
                .WithNativeDisableParallelForRestriction(navMeshQueryPointerArray)
                .ForEach((Entity entity, int entityInQueryIndex, int nativeThreadIndex, ref PathAgent agent, in PathDestination destination, in LocalToWorld localToWorld) =>
                {
                    var worldPosition = localToWorld.Position;
                    var worldDestination = agent.WorldDestination;

                    var navMeshQueryPointer = navMeshQueryPointerArray[nativeThreadIndex];
                    UnsafeUtility.CopyPtrToStructure(navMeshQueryPointer.Value, out NavMeshQuery navMeshQuery);

                    var one = new float3(1);

                    var status = navMeshQuery.BeginFindPath(
                        navMeshQuery.MapLocation(worldPosition, one * settings.PathSearchMax, agent.TypeID),
                        navMeshQuery.MapLocation(worldDestination, one * settings.PathSearchMax, agent.TypeID),
                        NavMesh.AllAreas
                    );

                    while (PathUtil.HasStatus(status, PathQueryStatus.InProgress)) status = navMeshQuery.UpdateFindPath(
                        settings.IterationMax,
                        out int iterationsPerformed
                    );

                    if (!PathUtil.HasStatus(status, PathQueryStatus.Success))
                    {
                        commandBuffer.SetComponentEnabled<PathPlanning>(entityInQueryIndex, entity, false);
                        commandBuffer.SetComponentEnabled<PathDestination>(entityInQueryIndex, entity, false);

                        commandBuffer.AddComponent(entityInQueryIndex, entity, new PathProblem
                        {
                            Value = status
                        });

                        return;
                    }

                    navMeshQuery.EndFindPath(out int pathLength);

                    var polygonIdArray = new NativeArray<PolygonId>(
                        PathConstants.PATH_NODE_MAX,
                        Allocator.Temp
                    );

                    navMeshQuery.GetPathResult(polygonIdArray);

                    var len = pathLength + 1;
                    var straightPath = new NativeArray<NavMeshLocation>(len, Allocator.Temp);
                    var straightPathFlags = new NativeArray<StraightPathFlags>(len, Allocator.Temp);
                    var vertexSide = new NativeArray<float>(len, Allocator.Temp);
                    var straightPathCount = 0;

                    status = PathUtils.FindStraightPath(
                        navMeshQuery,
                        worldPosition,
                        worldDestination,
                        polygonIdArray,
                        pathLength,
                        ref straightPath,
                        ref straightPathFlags,
                        ref vertexSide,
                        ref straightPathCount,
                        PathConstants.PATH_NODE_MAX
                    );

                    var pathBuffer = !pathBufferFromEntity.HasBuffer(entity) ? commandBuffer.AddBuffer<PathBufferElement>(entityInQueryIndex, entity) : pathBufferFromEntity[entity];

                    if (status == PathQueryStatus.Success)
                    {
                        if (pathBuffer.Length > 0) pathBuffer.RemoveAt(pathBuffer.Length - 1);

                        for (var i = straightPathCount - 1; i > 0; --i) pathBuffer.Add((float3)straightPath[i].position + agent.Offset);

                        if (pathBuffer.Length > 0)
                        {
                            commandBuffer.SetComponentEnabled<PathPlanning>(entityInQueryIndex, entity, false);
                            commandBuffer.SetComponentEnabled<PathDestination>(entityInQueryIndex, entity, false);
                        }
                    }

                    polygonIdArray.Dispose();
                    straightPath.Dispose();
                    straightPathFlags.Dispose();
                    vertexSide.Dispose();
                })
                .WithName("PathPlanJob")
                .ScheduleParallel();

            NavMeshWorld.GetDefaultWorld().AddDependency(Dependency);
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
