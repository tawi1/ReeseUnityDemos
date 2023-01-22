using System.Collections.Generic;
using Reese.Math;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using BuildPhysicsWorld = Unity.Physics.Systems.BuildPhysicsWorld;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Reese.Nav
{
    /// <summary>This system tracks the surface (or lack thereof) underneath a given agent. It also maintains parent-child relationships.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(NavBasisSystem))]
    public partial class NavSurfaceSystem : SystemBase
    {
        NavSystem navSystem => World.GetOrCreateSystemManaged<NavSystem>();
        Dictionary<int, GameObject> gameObjectMap = new Dictionary<int, GameObject>();
        EntityCommandBufferSystem barrier => World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        public bool GameObjectMapContainsKey(int key)
            => gameObjectMap.ContainsKey(key);

        public bool GameObjectMapContainsValue(GameObject go)
            => gameObjectMap.ContainsValue(go);

        public int GameObjectMapCount()
            => gameObjectMap.Count;

        public Dictionary<int, GameObject>.KeyCollection GameObjectMapKeys()
            => gameObjectMap.Keys;

        public Dictionary<int, GameObject>.ValueCollection GameObjectMapValues()
            => gameObjectMap.Values;

        public void GameObjectMapAdd(int key, GameObject value)
            => gameObjectMap.Add(key, value);

        public bool GameObjectMapRemove(int key)
            => gameObjectMap.Remove(key);

        public bool GameObjectMapTryGetValue(int key, out GameObject value)
            => gameObjectMap.TryGetValue(key, out value);

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var defaultBasis = World.GetExistingSystemManaged<NavBasisSystem>().DefaultBasis;

            // Prevents Unity.Physics from removing the Parent component from dynamic bodies:
            Entities
                .WithNone<Parent>()
                .ForEach((Entity entity, int entityInQueryIndex, in NavSurface surface) =>
                {
                    if (surface.Basis.Equals(Entity.Null))
                    {
                        commandBuffer.AddComponent(entityInQueryIndex, entity, new Parent
                        {
                            Value = defaultBasis
                        });
                    }
                    else
                    {
                        commandBuffer.AddComponent(entityInQueryIndex, entity, new Parent
                        {
                            Value = surface.Basis
                        });
                    }

                    commandBuffer.AddComponent<ParentTransform>(entityInQueryIndex, entity);
                })
                .WithName("NavAddParentToSurfaceJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);

            // Adds Parent and ParentTransform components when to agents:
            Entities
                .WithNone<NavProblem, Parent>()
                .ForEach((Entity entity, int entityInQueryIndex, in NavAgent agent) =>
                {
                    commandBuffer.AddComponent<Parent>(entityInQueryIndex, entity);
                    commandBuffer.AddComponent<ParentTransform>(entityInQueryIndex, entity);
                })
                .WithName("NavAddParentToAgentJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);

            // Prevents Unity.Transforms from assuming that children should be scaled by their parent:
            //Entities
            //    .WithAll<CompositeScale>()
            //    .WithAny<NavSurface, NavBasis>()
            //    .ForEach((Entity entity, int entityInQueryIndex) =>
            //    {
            //        commandBuffer.RemoveComponent<CompositeScale>(entityInQueryIndex, entity);
            //    })
            //    .WithName("NavRemoveCompositeScaleJob")
            //    .ScheduleParallel();

            //barrier.AddJobHandleForProducer(Dependency);

            var elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var settings = navSystem.Settings;
            var jumpBufferFromEntity = GetBufferLookup<NavJumpBufferElement>();

            Entities
                .WithNone<NavProblem, NavFalling, NavJumping>()
                .WithAll<NavNeedsSurface, ParentTransform>()
                .WithReadOnly(physicsWorld)
                .WithNativeDisableParallelForRestriction(jumpBufferFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref NavAgent agent, ref Parent surface, ref LocalTransform transform, in LocalToWorld localToWorld) =>
                {
                    var rayInput = new RaycastInput
                    {
                        Start = localToWorld.Position + agent.Offset,
                        End = -localToWorld.Up * settings.SurfaceRaycastDistanceMax,
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = NavUtil.ToBitMask(settings.ColliderLayer),
                            CollidesWith = NavUtil.ToBitMask(settings.SurfaceLayer),
                        }
                    };

                    if (!physicsWorld.CastRay(rayInput, out RaycastHit hit))
                    {
                        if (++agent.SurfaceRaycastCount >= settings.SurfaceRaycastMax)
                        {
                            agent.FallSeconds = elapsedSeconds;

                            commandBuffer.RemoveComponent<NavNeedsSurface>(entityInQueryIndex, entity);
                            commandBuffer.AddComponent<NavFalling>(entityInQueryIndex, entity);
                        }

                        return;
                    }

                    agent.SurfaceRaycastCount = 0;
                    surface.Value = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                    commandBuffer.RemoveComponent<NavNeedsSurface>(entityInQueryIndex, entity);

                    transform.Position.y = hit.Position.y + agent.Offset.y;

                    if (!jumpBufferFromEntity.HasComponent(entity)) return;
                    var jumpBuffer = jumpBufferFromEntity[entity];
                    if (jumpBuffer.Length < 1) return;

                    transform.Position = jumpBuffer[0].Value + agent.Offset;

                    jumpBuffer.Clear();

                    commandBuffer.AddComponent<NavPlanning>(entityInQueryIndex, entity);
                })
                .WithName("NavSurfaceTrackingJob")
                .ScheduleParallel();

            var localToWorldFromEntity = GetComponentLookup<LocalToWorld>(true);

            // Corrects the translation of children with a parent not at the origin:
            Entities
                .WithChangeFilter<PreviousParent>()
                .WithAny<NavFixTranslation>()
                .WithReadOnly(localToWorldFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform, in PreviousParent previousParent, in Parent parent) =>
                {
                    if (previousParent.Value.Equals(Entity.Null) || !localToWorldFromEntity.HasComponent(parent.Value)) return;

                    var parentTransform = localToWorldFromEntity[parent.Value];

                    if (parentTransform.Position.Equals(float3.zero))
                    {
                        commandBuffer.RemoveComponent<NavFixTranslation>(entityInQueryIndex, entity);
                        return;
                    }

                    transform.Position = transform.Position.ToLocal(parentTransform);

                    commandBuffer.RemoveComponent<NavFixTranslation>(entityInQueryIndex, entity);
                })
                .WithName("NavFixTranslationJob")
                .ScheduleParallel();

            // Re-parents entities to ensure correct transform:
            Entities
                .WithNone<NavAgent, ParentTransform>()
                .ForEach((Entity entity, int entityInQueryIndex, in Parent parent) =>
                {
                    commandBuffer.RemoveComponent<Parent>(entityInQueryIndex, entity);

                    commandBuffer.AddComponent(entityInQueryIndex, entity, new Parent
                    {
                        Value = parent.Value
                    });

                    commandBuffer.AddComponent<ParentTransform>(entityInQueryIndex, entity);
                })
                .WithName("NavReparentingJob")
                .ScheduleParallel();

            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
