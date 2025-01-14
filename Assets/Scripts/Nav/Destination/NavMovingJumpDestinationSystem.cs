﻿using Reese.Nav;
using Reese.Random;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.SceneManagement;
using Reese.Math;

namespace Reese.Demo
{
    partial class NavMovingJumpDestinationSystem : SystemBase
    {
        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();

        protected override void OnCreate()
        {
            if (!SceneManager.GetActiveScene().name.Equals("NavMovingJumpDemo"))
                Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var jumpableBufferFromEntity = GetBufferLookup<NavJumpableBufferElement>(true);
            var renderBoundsFromEntity = GetComponentLookup<RenderBounds>(true);
            var localToWorldFromEntity = GetComponentLookup<LocalToWorld>(true);
            var randomArray = World.GetExistingSystemManaged<RandomSystem>().RandomArray;

            Entities
                .WithNone<NavProblem, NavDestination>()
                .WithReadOnly(jumpableBufferFromEntity)
                .WithReadOnly(renderBoundsFromEntity)
                .WithReadOnly(localToWorldFromEntity)
                .WithNativeDisableContainerSafetyRestriction(randomArray)
                .ForEach((Entity entity, int entityInQueryIndex, int nativeThreadIndex, ref NavAgent agent, in Parent surface) =>
                {
                    if (
                        surface.Value.Equals(Entity.Null) ||
                        !jumpableBufferFromEntity.HasBuffer(surface.Value)
                    ) return;

                    var jumpableSurfaces = jumpableBufferFromEntity[surface.Value];
                    var random = randomArray[nativeThreadIndex];

                    var destinationSurface = jumpableSurfaces[random.NextInt(0, jumpableSurfaces.Length)];

                    var localPoint = NavUtil.GetRandomPointInBounds(
                        ref random,
                        renderBoundsFromEntity[destinationSurface].Value,
                        3,
                        float3.zero
                    );

                    commandBuffer.AddComponent(entityInQueryIndex, entity, new NavDestination
                    {
                        WorldPoint = localPoint.ToWorld(localToWorldFromEntity[destinationSurface.Value])
                    });

                    randomArray[nativeThreadIndex] = random;
                })
                .WithName("NavMovingJumpDestinationJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
