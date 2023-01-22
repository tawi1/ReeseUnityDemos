﻿using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Reese.Spatial
{
    /// <summary>Authors a SpatialTrigger.</summary>
    public class SpatialTriggerAuthoring : MonoBehaviour
    {
        /// <summary>The layer this object belongs to. Valid layers range from 8 to 30, inclusive. All other layers are invalid, and will always result in layer 8, since they are used by Unity internally. See https://docs.unity3d.com/Manual/class-TagManager.html and https://docs.unity3d.com/Manual/Layers.html for more information.</summary>
        [SerializeField]
        int belongsToLayer = default;

        /// <summary>The layer this object can collide with. Valid layers range from 8 to 30, inclusive. All other layers are invalid, and will always result in layer 8, since they are used by Unity internally. See https://docs.unity3d.com/Manual/class-TagManager.html and https://docs.unity3d.com/Manual/Layers.html for more information.</summary>
        [SerializeField]
        int collidesWithLayer = default;

        /// <summary>An optional override for the belongs to / collides with checks. If the value in both objects is equal and positive, the objects always collide. If the value in both objects is equal and negative, the objects never collide.</summary>
        [SerializeField]
        int groupIndex = default;

        /// <summary>True if using the default collision filter, false if not.</summary>
        [SerializeField]
        bool useDefaultCollisionFilter = true;

        /// <summary>This trigger will be activated by any overlapping activators belonging to the same tag.</summary>
        [SerializeField]
        List<string> tags = new List<string>();

        class SpatialTriggerAuthoringBaker : Baker<SpatialTriggerAuthoring>
        {
            public override void Bake(SpatialTriggerAuthoring authoring)
            {
                AddComponent(new SpatialTrigger
                {
                    Filter = authoring.useDefaultCollisionFilter ? CollisionFilter.Default : new CollisionFilter
                    {
                        BelongsTo = SpatialUtil.ToBitMask(authoring.belongsToLayer),
                        CollidesWith = SpatialUtil.ToBitMask(authoring.collidesWithLayer),
                        GroupIndex = authoring.groupIndex
                    }
                });

                var tagBuffer = AddBuffer<SpatialTag>();

                authoring.tags.Distinct().ToList().ForEach(tag => tagBuffer.Add(tag));
            }
        }    
    }
}
