using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class BasicSceneInitializationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            // Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void CreateSpawnRequest(EntityManager entityManager, Entity characterPrefabEntity, Entity currentCharacterEntity)
        {
            Entity spawnRequestEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(spawnRequestEntity, new CharacterSpawnRequest
            {
                CharacterPrefabEntity = characterPrefabEntity,
            });

            // Destroy old character
            entityManager.DestroyEntity(currentCharacterEntity);
        }

        protected override void OnUpdate()
        {
            Entity sceneInitializerEntity = GetSingletonEntity<BasicSceneInitialization>();

            // Handle initial spawn
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<Initialized>()
                .ForEach((Entity entity, ref BasicSceneInitialization sceneInitializer) =>
                {
                    // Initial character spawn request
                    Entity spawnRequestEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(spawnRequestEntity, new CharacterSpawnRequest
                    {
                        CharacterPrefabEntity = sceneInitializer.KinematicCharacterPrefabEntity,
                    });

                    // Fixed Step
                    FixedStepSimulationSystemGroup fixedStepGroup = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();
                    fixedStepGroup.RateManager = new RateUtils.FixedRateCatchUpManager(1f / sceneInitializer.FixedRate);

                    // Camera
                    sceneInitializer.ActiveCameraEntity = EntityManager.Instantiate(sceneInitializer.GameCameraPrefabEntity);

                    EntityManager.AddComponentData(entity, new Initialized());
                }).Run();

            // Create spawn requests
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                BasicSceneInitialization sceneInitializer = EntityManager.GetComponentData<BasicSceneInitialization>(sceneInitializerEntity);
                CreateSpawnRequest(EntityManager, sceneInitializer.KinematicCharacterPrefabEntity, sceneInitializer.ActiveCharacterEntity);
            }

            // Handle spawn requests
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in CharacterSpawnRequest spawnRequest) =>
                {
                    BasicSceneInitialization sceneInitializer = EntityManager.GetComponentData<BasicSceneInitialization>(sceneInitializerEntity);

                    // Spawn new character
                    sceneInitializer.ActiveCharacterEntity = EntityManager.Instantiate(spawnRequest.CharacterPrefabEntity);
                    EntityManager.SetComponentData(sceneInitializer.ActiveCharacterEntity, EntityManager.GetComponentData<Translation>(sceneInitializer.CharacterSpawnPointEntity));
                    EntityManager.SetComponentData(sceneInitializer.ActiveCharacterEntity, EntityManager.GetComponentData<Rotation>(sceneInitializer.CharacterSpawnPointEntity));

                    // Assign camera
                    OrbitCamera orbitCameraComponent = EntityManager.GetComponentData<OrbitCamera>(sceneInitializer.ActiveCameraEntity);
                    orbitCameraComponent.FollowedEntity = EntityManager.GetComponentData<CameraTarget>(sceneInitializer.ActiveCharacterEntity).CameraTargetEntity;
                    orbitCameraComponent.FollowedCharacter = sceneInitializer.ActiveCharacterEntity;
                    EntityManager.GetBuffer<IgnoredEntityBufferElement>(sceneInitializer.ActiveCameraEntity).Add(new IgnoredEntityBufferElement { Entity = sceneInitializer.ActiveCharacterEntity });
                    EntityManager.SetComponentData(sceneInitializer.ActiveCameraEntity, orbitCameraComponent);

                    BasicPlayerInputs inputs = GetComponent<BasicPlayerInputs>(sceneInitializer.ActiveCharacterEntity);
                    inputs.CameraReference = sceneInitializer.ActiveCameraEntity;
                    SetComponent(sceneInitializer.ActiveCharacterEntity, inputs);

                    EntityManager.DestroyEntity(entity);

                    EntityManager.SetComponentData(sceneInitializerEntity, sceneInitializer);
                }).Run();
        }
    }
}