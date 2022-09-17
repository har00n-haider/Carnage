using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [DisallowMultipleComponent]
    public class PlatformerSceneInitializationAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public float FixedRate = 60;
        public Transform MainCharacterSpawnPoint;

        [Header("Prefabs")]
        public GameObject MainCharacterPrefab;
        public GameObject EntityCameraPrefab;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PlatformerSceneInitialization {
                FixedRate = FixedRate,
                MainCharacterSpawnPoint = new RigidTransform(MainCharacterSpawnPoint.rotation, MainCharacterSpawnPoint.position),
                MainCharacterPrefabEntity = conversionSystem.GetPrimaryEntity(MainCharacterPrefab),
                GameCameraPrefabEntity = conversionSystem.GetPrimaryEntity(EntityCameraPrefab),
                StartingCameraForward = MainCharacterSpawnPoint.forward,
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(MainCharacterPrefab);
            referencedPrefabs.Add(EntityCameraPrefab);
        }
    }

    [System.Serializable]
    public struct PlatformerSceneInitialization : IComponentData
    {
        public float FixedRate;
        public RigidTransform MainCharacterSpawnPoint;
        public Entity MainCharacterPrefabEntity;
        public Entity GameCameraPrefabEntity;
        public float3 StartingCameraForward;
    }
}

