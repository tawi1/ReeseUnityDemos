using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Reese.Demo
{
    partial class RotationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;

            Entities
                .ForEach((ref LocalTransform transform, in Rotator rotator) =>
                    transform.Rotation = math.slerp(
                        quaternion.Euler(rotator.FromRelativeAngles),
                        quaternion.Euler(rotator.ToRelativeAngles),
                        (math.sin(math.PI * rotator.Frequency * elapsedSeconds) + 1) * 0.5f
                    )
                )
                .WithName("DemoRotatorJob")
                .ScheduleParallel();
        }
    }
}
