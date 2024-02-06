using Unity.Entities;
using UnityEngine;

namespace Reese.Path
{
    /// <summary>Authors an agent.</summary>
    public class PathAgentAuthoring : MonoBehaviour
    {
        /// <summary>The agent's type.</summary>
        [SerializeField]
        string type = PathConstants.HUMANOID;

        /// <summary>The agent's offset.</summary>
        [SerializeField]
        Vector3 offset = default;

        class PathAgentAuthoringBaker : Baker<PathAgentAuthoring>
        {
            public override void Bake(PathAgentAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponents(this, entity, authoring.type, authoring.offset);
            }
        }

        public static void AddComponents(IBaker baker, Entity entity, string type, Vector3 offset)
        {
            baker.AddComponent<PathDestination>(entity);
            baker.AddComponent<PathPlanning>(entity);

            baker.AddComponent(entity, new PathAgent
            {
                TypeID = PathUtil.GetAgentType(type),
                Offset = offset
            });

            baker.SetComponentEnabled<PathDestination>(entity, false);
            baker.SetComponentEnabled<PathPlanning>(entity, false);
        }

        public static void AddComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            AddComponents(ref commandBuffer, entity, PathConstants.HUMANOID, Vector3.zero);
        }

        public static void AddComponents(ref EntityCommandBuffer commandBuffer, Entity entity, string type, Vector3 offset)
        {
            commandBuffer.AddComponent<PathDestination>(entity);
            commandBuffer.AddComponent<PathPlanning>(entity);

            commandBuffer.AddComponent(entity, new PathAgent
            {
                TypeID = PathUtil.GetAgentType(type),
                Offset = offset
            });

            commandBuffer.SetComponentEnabled<PathDestination>(entity, false);
            commandBuffer.SetComponentEnabled<PathPlanning>(entity, false);
        }
    }
}
