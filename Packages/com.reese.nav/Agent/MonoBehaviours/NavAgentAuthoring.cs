using Unity.Entities;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Reese.Nav
{
    /// <summary>Authors a NavAgent.</summary>
    public class NavAgentAuthoring : MonoBehaviour
    {
        /// <summary>The agent's jump angle in degrees.</summary>
        [SerializeField]
        float jumpDegrees = 45;

        /// <summary>Artificial gravity applied to the agent.</summary>
        [SerializeField]
        float jumpGravity = 100;

        /// <summary>The agent's horizontal jump speed multiplier.</summary>
        [SerializeField]
        float jumpSpeedMultiplierX = 1.5f;

        /// <summary>The agent's vertical jump speed mulitiplier.</summary>
        [SerializeField]
        float jumpSpeedMultiplierY = 2;

        /// <summary>The agent's translation speed.</summary>
        [SerializeField]
        float translationSpeed = 20;

        /// <summary>The agent's rotation speed.</summary>
        [SerializeField]
        float rotationSpeed = 0.3f;

        /// <summary>The agent's type.</summary>
        [SerializeField]
        string type = NavConstants.HUMANOID;

        /// <summary>The agent's offset.</summary>
        [SerializeField]
        Vector3 offset = default;

        /// <summary>True if the agent is terrain-capable, false if not.</summary>
        [SerializeField]
        bool isTerrainCapable = default;

        class NavAgentAuthoringBaker : Baker<NavAgentAuthoring>
        {
            public override void Bake(NavAgentAuthoring authoring)
            {
                AddComponent(new NavAgent
                {
                    JumpDegrees = authoring.jumpDegrees,
                    JumpGravity = authoring.jumpGravity,
                    JumpSpeedMultiplierX = authoring.jumpSpeedMultiplierX,
                    JumpSpeedMultiplierY = authoring.jumpSpeedMultiplierY,
                    TranslationSpeed = authoring.translationSpeed,
                    RotationSpeed = authoring.rotationSpeed,
                    TypeID = NavUtil.GetAgentType(authoring.type),
                    Offset = authoring.offset
                });

                AddComponent<NavNeedsSurface>();
                AddComponent<NavFixTranslation>();

                if (authoring.isTerrainCapable) AddComponent<NavTerrainCapable>();
            }
        }
    }
}
