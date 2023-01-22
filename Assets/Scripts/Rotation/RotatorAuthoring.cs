using Unity.Entities;
using UnityEngine;

namespace Reese.Demo
{
    public class RotatorAuthoring : MonoBehaviour
    {
        [SerializeField]
        Vector3 fromRelativeAngles = new Vector3(0, 0, 0);

        [SerializeField]
        Vector3 toRelativeAngles = new Vector3(0, 0, 0);

        [SerializeField]
        float frequency = 1;

        class RotatorAuthoringBaker:Baker<RotatorAuthoring>
        {
            public override void Bake(RotatorAuthoring rotatorAuthoring)
            {
                AddComponent(new Rotator
                {
                    FromRelativeAngles = rotatorAuthoring.fromRelativeAngles + rotatorAuthoring.transform.localRotation.eulerAngles,
                    ToRelativeAngles = rotatorAuthoring.toRelativeAngles + rotatorAuthoring.transform.localRotation.eulerAngles,
                    Frequency = rotatorAuthoring.frequency
                });
            }
        }
    }
}
