using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace Reese.EntityPrefabGroups
{
    /// <summary>Initializes a group of entity prefabs.</summary>
    [DisallowMultipleComponent]
    public class EntityPrefabGroup : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If true, collects prefabs referenced from other authoring scripts (attached to this GameObject) into the group.")]
        bool collectOtherPrefabs = true;

        [SerializeField]
        GameObject[] prefabs = default;

        class EntityPrefabGroupBaker : Baker<EntityPrefabGroup>
        {
            public override void Bake(EntityPrefabGroup authoring)
            {
                var prefabsDeduped = new HashSet<Entity>();

                foreach (var prefab in authoring.prefabs)
                {
                    var prefabEntity = GetEntity(prefab);

                    if (prefabEntity == Entity.Null) continue;

                    prefabsDeduped.Add(prefabEntity);
                }

                if (authoring.collectOtherPrefabs)
                {
                    //var otherPrefabEntities = GetOtherAuthoringPrefabEntities(conversionSystem);

                    //foreach (var otherPrefab in otherPrefabEntities)
                    //    prefabsDeduped.Add(otherPrefab);
                }

                var groupBuffer = AddBuffer<PrefabGroup>();
                foreach (var prefabEntity in prefabsDeduped)
                    groupBuffer.Add(prefabEntity);
            }
        }

        //List<Entity> GetOtherAuthoringPrefabEntities(GameObjectConversionSystem conversionSystem)
        //{
        //    var referencedPrefabsArray = GetComponents<IDeclareReferencedPrefabs>();

        //    var entities = new List<Entity>();
        //    foreach (var referencedPrefabs in referencedPrefabsArray)
        //    {
        //        var fields = referencedPrefabs.GetType()
        //            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        //            .Where(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
        //            .ToArray();

        //        foreach (var field in fields)
        //        {
        //            var go = field.GetValue(referencedPrefabs) as GameObject;

        //            if (go == null) continue;

        //            var entity = conversionSystem.GetPrimaryEntity(go);

        //            if (entity == Entity.Null) continue;

        //            entities.Add(entity);
        //        }
        //    }

        //    return entities;
        //}
    }
}
