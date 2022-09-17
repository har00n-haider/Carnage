using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.StressTest
{
    public class PrefabFloorSpawner : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject Prefab;
        public int Count;
        public float Spacing;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PrefabFloorSpawnerComponent 
            { 
                PrefabEntity = conversionSystem.GetPrimaryEntity(Prefab),
                Count = Count,
                Spacing = Spacing,
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }
    }

    [Serializable]
    public struct PrefabFloorSpawnerComponent : IComponentData
    {
        public Entity PrefabEntity;
        public int Count;
        public float Spacing;
    }

    [Serializable]
    public struct BelongToFloorSpawner : IComponentData
    {
        public Entity SpawnerEntity;
    }

    [Serializable]
    public struct CleanupFloorSpawners : IComponentData
    {
    }

    [Serializable]
    public struct PrefabFloorSpawnerState : ISystemStateComponentData
    {
    }

    public partial class PrefabFloorSpawnerSystem : SystemBase
    {
        public EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem;
        public EntityQuery CleanupQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            EndSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            CleanupQuery = GetEntityQuery(typeof(CleanupFloorSpawners));
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = EndSimulationEntityCommandBufferSystem.CreateCommandBuffer();

            // Start
            Dependency = Entities
                .WithName("StartFloorSpawner")
                .WithNone<PrefabFloorSpawnerState>()
                .ForEach((Entity entity, ref Translation translation, ref PrefabFloorSpawnerComponent spawner) =>
                {
                    commandBuffer.AddComponent<PrefabFloorSpawnerState>(entity);

                    int spawnResolution = (int)math.ceil(math.sqrt(spawner.Count));
                    float totalWidth = (spawnResolution - 1f) * spawner.Spacing;
                    float3 spawnBottomCorner = translation.Value + (-math.right() * totalWidth * 0.5f) + (-math.forward() * totalWidth * 0.5f);

                    int counter = 0;
                    for (int x = 0; x < spawnResolution; x++)
                    {
                        for (int z = 0; z < spawnResolution; z++)
                        {
                            if (counter >= spawner.Count)
                            {
                                break;
                            }

                            Entity spawnedPrefabEntity = commandBuffer.Instantiate(spawner.PrefabEntity);
                            commandBuffer.AddComponent(spawnedPrefabEntity, new BelongToFloorSpawner { SpawnerEntity = entity });
                               
                            float3 spawnPos = spawnBottomCorner + (math.right() * x * spawner.Spacing) + (math.forward() * z * spawner.Spacing);
                            commandBuffer.SetComponent(spawnedPrefabEntity, new Translation { Value = spawnPos });

                            counter++;
                        }
                    }
                }).Schedule(Dependency);

            // Destroy
            Dependency = Entities
                .WithName("DestroyFloorSpawner")
                .WithAll<PrefabFloorSpawnerState>()
                .WithNone<PrefabFloorSpawnerComponent>()
                .ForEach((Entity entity) =>
                {
                    commandBuffer.RemoveComponent<PrefabFloorSpawnerState>(entity);
                    Entity cleanupEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(cleanupEntity, new CleanupFloorSpawners());
                }).Schedule(Dependency);

            // Cleanup
            if(CleanupQuery.CalculateEntityCount() > 0)
            {
                ComponentDataFromEntity<PrefabFloorSpawnerComponent> floorSpawnerFromEntity = GetComponentDataFromEntity<PrefabFloorSpawnerComponent>(true);

                Dependency = Entities
                    .WithName("CleanupFloorSpawner")
                    .WithReadOnly(floorSpawnerFromEntity)
                    .ForEach((Entity entity, ref BelongToFloorSpawner belongToFloorSpawner) =>
                    {
                        if(!floorSpawnerFromEntity.HasComponent(belongToFloorSpawner.SpawnerEntity))
                        {
                            commandBuffer.DestroyEntity(entity);
                        }
                    }).Schedule(Dependency);

                Dependency = Entities
                    .WithName("DestroyCleanupEntities")
                    .ForEach((Entity entity, ref CleanupFloorSpawners cleanupFloorSpawners) =>
                    {
                        commandBuffer.DestroyEntity(entity);
                    }).Schedule(Dependency);
            } 

            EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}