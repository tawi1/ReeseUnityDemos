using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Reese.Utility
{
    /// <summary>Authors a sticky.</summary>
    public class StickyAuthoring : MonoBehaviour
    {
        /// <summary>World direction unit vector in which the object should attempt to stick to another.</summary>
        [SerializeField]
        Vector3 worldDirection = Vector3.down;

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

        /// <summary>Radius of collider-casting SphereGeometry used to stick this object to another.</summary>
        [SerializeField]
        float radius = default;

        /// <summary>Number of attempts the StickySystem has to stick the object. The StickyFailed component will be added to it in case of failure.</summary>
        [SerializeField]
        int stickAttempts = 10;

        class StickyAuthoringBaker : Baker<StickyAuthoring>
        {
            public override void Bake(StickyAuthoring authoring)
            {
                var filter = new CollisionFilter
                {
                    BelongsTo = Util.ToBitMask(authoring.belongsToLayer),
                    CollidesWith = Util.ToBitMask(authoring.collidesWithLayer),
                    GroupIndex = authoring.groupIndex
                };

                if (authoring.useDefaultCollisionFilter) filter = CollisionFilter.Default;

                AddComponent(new Sticky
                {
                    Filter = filter,
                    WorldDirection = authoring.worldDirection,
                    Radius = authoring.radius,
                    StickAttempts = authoring.stickAttempts
                });

                //var convertToEntity = GetComponent<ConvertToEntity>();

                //if (
                //    convertToEntity != null &&
                //    convertToEntity.ConversionMode.Equals(Mode.ConvertAndInjectGameObject)
                //) AddComponent(typeof(CopyTransformToGameObject));

                AddComponent<FixTranslation>();
            }
        }
    }
}
