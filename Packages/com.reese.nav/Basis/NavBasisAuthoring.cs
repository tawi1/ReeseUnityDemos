﻿using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Reese.Nav
{
    /// <summary>Authors a NavBasis.</summary>
    public class NavBasisAuthoring : MonoBehaviour
    {
        /// <summary>If true the GameObject's transform will be used and
        /// applied to child surfaces or bases via
        /// CopyTransformFromGameObject. If false the entity's transform
        /// will be used and applied conversely via CopyTransformToGameObject.
        /// </summary>
        public bool HasGameObjectTransform;

        /// <summary>Optional. The parent basis of this basis. If not set,
        /// then this basis will be parented to a generated, default basis at
        /// the origin.</summary>
        public NavBasisAuthoring ParentBasis;

        class NavBasisAuthoringBaker : Baker<NavBasisAuthoring>
        {
            public override void Bake(NavBasisAuthoring authoring)
            {
                AddComponent( new NavBasis
                {
                    ParentBasis = GetEntity(authoring.ParentBasis)
                });

                //if (HasGameObjectTransform) dstManager.AddComponent(entity, typeof(CopyTransformFromGameObject));
                //else dstManager.AddComponent(entity, typeof(CopyTransformToGameObject));

                //dstManager.RemoveComponent(entity, typeof(NonUniformScale));
                //dstManager.RemoveComponent(entity, typeof(MeshRenderer));
            }
        }   
    }
}
