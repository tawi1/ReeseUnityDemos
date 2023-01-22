using Unity.Entities;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Reese.Nav
{
    /// <summary>Authors a NavFollow.</summary>
    public class NavFollowAuthoring : MonoBehaviour
    {
        [SerializeField]
        GameObject target = default;

        [SerializeField]
        float maxDistance = default;

        [SerializeField]
        float minDistance = default;

        class NavFollowAuthoringBaker : Baker<NavFollowAuthoring>
        {
            public override void Bake(NavFollowAuthoring authoring)
            {
                AddComponent(new NavFollow
                {
                    Target = GetEntity(authoring.target),
                    MaxDistance = authoring.maxDistance,
                    MinDistance = authoring.minDistance
                });
            }
        }
    }
}
