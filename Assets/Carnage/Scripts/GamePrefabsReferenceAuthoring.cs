using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class GamePrefabsReferenceAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public List<GameObject> Prefabs = new List<GameObject>();

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        DynamicBuffer<GamePrefabsReference> buffer = dstManager.AddBuffer<GamePrefabsReference>(entity);

        foreach (var p in Prefabs)
        {
            buffer.Add(new GamePrefabsReference { Value = conversionSystem.GetPrimaryEntity(p) } );
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
