using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Reese.Utility
{
    /// <summary>Authors a child.</summary>
    public class ChildAuthoring : MonoBehaviour
    {
        [SerializeField]
        ParentAuthoring parent = default;

        class ChildAuthoringBaker : Baker<ChildAuthoring>
        {
            public override void Bake(ChildAuthoring authoring)
            {
                AddComponent(new Parent
                {
                    Value = GetEntity(authoring.parent)
                });

                //var convertToEntity = authoring.GetComponent<ConvertToEntity>();

                //if (
                //    convertToEntity != null &&
                //    convertToEntity.ConversionMode.Equals(Mode.ConvertAndInjectGameObject)
                //) AddComponent(typeof(CopyTransformToGameObject));

                AddComponent<FixTranslation>();
            }
        }
    }
}
