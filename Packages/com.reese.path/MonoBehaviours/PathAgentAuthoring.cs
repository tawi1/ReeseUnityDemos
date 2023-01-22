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
                AddComponent(new PathAgent
                {
                    TypeID = PathUtil.GetAgentType(authoring.type),
                    Offset = authoring.offset
                });
            }
        }
    }
}
