using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Reese.Utility
{
    //[UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial class StickySystem : SystemBase
    {
        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            var localToWorldFromEntity = GetComponentLookup<LocalToWorld>(true);

            Entities
                .WithAll<LocalToWorld>()
                .WithReadOnly(localToWorldFromEntity)
                .WithReadOnly(physicsWorld)
                .ForEach((Entity entity, int entityInQueryIndex, ref Sticky sticky) =>
                {
                    if (sticky.StickAttempts-- <= 0)
                    {
                        commandBuffer.RemoveComponent<Sticky>(entityInQueryIndex, entity);
                        commandBuffer.AddComponent<StickyFailed>(entityInQueryIndex, entity);
                        return;
                    }

                    var worldPosition = localToWorldFromEntity[entity].Position;

                    var collider = SphereCollider.Create(
                        new SphereGeometry()
                        {
                            Center = worldPosition,
                            Radius = sticky.Radius
                        },
                        sticky.Filter
                    );

                    unsafe
                    {
                        var castInput = new ColliderCastInput()
                        {
                            Collider = (Collider*)collider.GetUnsafePtr(),
                            Orientation = quaternion.LookRotationSafe(sticky.WorldDirection, math.up())
                        };

                        if (!physicsWorld.CastCollider(castInput, out ColliderCastHit hit)) return;

                        commandBuffer.AddComponent(entityInQueryIndex, entity, new Parent
                        {
                            Value = hit.Entity
                        });

                        commandBuffer.RemoveComponent<Sticky>(entityInQueryIndex, entity);
                    }
                })
                .WithName("StickyJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
