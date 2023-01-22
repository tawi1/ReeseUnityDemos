using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Reese.Spatial
{
    /// <summary>Authors a SpatialActivator.</summary>
    public class SpatialActivatorAuthoring : MonoBehaviour
    {
        /// <summary>This activator will activate any overlapping triggers belonging to the same tag.</summary>
        [SerializeField]
        List<string> tags = new List<string>();

        class SpatialActivatorAuthoringBaker : Baker<SpatialActivatorAuthoring>
        {
            public override void Bake(SpatialActivatorAuthoring authoring)
            {
                AddComponent<SpatialActivator>();

                var tagBuffer = AddBuffer<SpatialTag>();

                authoring.tags.Distinct().ToList().ForEach(group => tagBuffer.Add(group));
            }
        }
    }
}
