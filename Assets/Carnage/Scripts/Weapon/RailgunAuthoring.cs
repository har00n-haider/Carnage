using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


public class RailgunAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Railgun Railgun;
    public GameObject Muzzle;
    public GameObject LazerPrefab;
    public GameObject SparksPrefab;
    public int SparksCount = 16;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Railgun._muzzleEntity = conversionSystem.GetPrimaryEntity(Muzzle);
        Railgun.LazerPrefab = conversionSystem.GetPrimaryEntity(LazerPrefab); 
        Railgun.HitSparkPrefab = conversionSystem.GetPrimaryEntity(SparksPrefab);
        Railgun.HitSparksCount = SparksCount;

        dstManager.AddComponent<Weapon>(entity);
        dstManager.AddComponentData(entity, Railgun);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(LazerPrefab);
        referencedPrefabs.Add(SparksPrefab);
    }
}
