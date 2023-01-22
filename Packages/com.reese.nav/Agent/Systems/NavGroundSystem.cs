using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Reese.Nav
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(NavMoveSystem))]
    public partial class NavGroundSystem : SystemBase
    {
        public bool IsDebugging = false;

        NavSystem navSystem => World.GetOrCreateSystemManaged<NavSystem>();

        protected override void OnUpdate()
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var settings = navSystem.Settings;
            var isDebugging = IsDebugging;

            Entities
               .WithNone<NavProblem>()
               .WithNone<NavPlanning, NavJumping, NavFalling>()
               .WithAll<NavWalking, ParentTransform, NavTerrainCapable>()
               .WithReadOnly(physicsWorld)
               .ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform, ref NavAgent agent, in LocalToWorld localToWorld, in Parent surface) =>
               {
                   var rayInput = new RaycastInput
                   {
                       Start = localToWorld.Position + agent.Offset,
                       End = -math.up() * settings.SurfaceRaycastDistanceMax,
                       Filter = new CollisionFilter()
                       {
                           BelongsTo = NavUtil.ToBitMask(settings.ColliderLayer),
                           CollidesWith = NavUtil.ToBitMask(settings.SurfaceLayer),
                       }
                   };

                   if (physicsWorld.CastRay(rayInput, out RaycastHit hit))
                   {
                       if (isDebugging)
                       {
                           UnityEngine.Debug.DrawLine(hit.Position, hit.Position + hit.SurfaceNormal * 15, UnityEngine.Color.green);
                           UnityEngine.Debug.DrawLine(hit.Position, hit.Position + localToWorld.Up * 7, UnityEngine.Color.cyan);
                           UnityEngine.Debug.DrawLine(hit.Position, hit.Position + localToWorld.Right * 7, UnityEngine.Color.cyan);
                           UnityEngine.Debug.DrawLine(hit.Position, hit.Position + localToWorld.Forward * 7, UnityEngine.Color.cyan);
                       }

                       agent.SurfacePointNormal = hit.SurfaceNormal;

                       var currentPosition = transform.Position;
                       currentPosition.y = hit.Position.y + agent.Offset.y;
                       transform.Position = currentPosition;
                   }
               })
               .WithName("NavGroundingJob")
               .ScheduleParallel();
        }
    }
}
