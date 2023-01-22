using Unity.Entities;
using UnityEngine;

namespace Reese.Nav
{
    /// <summary>Authors a NavFlocking.</summary>
    public class NavFlockingAuthoring : MonoBehaviour
    {
        class NavFlockingAuthoringBaker : Baker<NavFlockingAuthoring>
        {
            public override void Bake(NavFlockingAuthoring authoring)
            {
                AddComponent<NavFlocking>();
            }
        }
    }
}
