using UnityEngine;
using Unity.Entities;

namespace Reese.Nav
{
    public struct NavSurfaceInitialize : IComponentData, IEnableableComponent { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
    partial class NavSurfaceInitializeSystem : SystemBase
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        private NavSurfaceSystem surfaceSystem => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NavSurfaceSystem>();

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();

            var initGroup = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<NavSurfaceInitialize>()
                .Build(this);

            RequireForUpdate(initGroup);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();

            Entities
            .WithoutBurst()
            .WithAll<NavSurfaceInitialize>()
            .ForEach((
                Entity entity,
                GameObject gameObject) =>
            {
                surfaceSystem.GameObjectMapAdd(gameObject.transform.GetInstanceID(), gameObject);

                commandBuffer.SetComponentEnabled<NavSurfaceInitialize>(entity, false);

            }).Run();

            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}