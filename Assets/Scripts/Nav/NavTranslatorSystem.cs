using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Reese.Demo
{
    partial class NavTranslatorSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            var deltaSeconds = SystemAPI.Time.DeltaTime;

            Entities
                .WithAny<NavTranslator>()
                .ForEach((ref LocalTransform transform) =>
                {
                    transform.Position.y = math.sin(elapsedSeconds) * 10;
                })
                .WithName("NavTranslatorJob")
                .ScheduleParallel();
        }
    }
}
