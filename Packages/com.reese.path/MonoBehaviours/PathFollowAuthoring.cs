using Unity.Entities;
using UnityEngine;

namespace Reese.Path
{
    /// <summary>Authors a PathFollow.</summary>
    public class PathFollowAuthoring : MonoBehaviour
    {
        [SerializeField]
        GameObject target = default;

        [SerializeField]
        float maxDistance = default;

        [SerializeField]
        float minDistance = default;

        class PathFollowAuthoringBaker : Baker<PathFollowAuthoring>
        {
            public override void Bake(PathFollowAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PathFollow
                {
                    Target = GetEntity(authoring.target, TransformUsageFlags.Dynamic),
                    MaxDistance = authoring.maxDistance,
                    MinDistance = authoring.minDistance
                });
            }
        }
    }
}
