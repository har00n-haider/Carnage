using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Rival.Samples.OnlineFPS
{
    [DisallowMultipleComponent]
    public class GhostPrefabsReferenceAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public List<GameObject> Prefabs = new List<GameObject>();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            DynamicBuffer<GhostPrefabsReference> buffer = dstManager.AddBuffer<GhostPrefabsReference>(entity);

            foreach (var p in Prefabs)
            {
                buffer.Add(new GhostPrefabsReference { Value = conversionSystem.GetPrimaryEntity(p) } );
            }
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            foreach (var p in Prefabs)
            {
                referencedPrefabs.Add(p);
            }
        }
    }
}