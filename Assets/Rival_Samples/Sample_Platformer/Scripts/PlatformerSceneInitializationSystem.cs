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

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlatformerSceneInitializationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(HasSingleton<PlatformerSceneInitialization>())
            {
                PlatformerSceneInitialization sceneInitializer = GetSingleton<PlatformerSceneInitialization>();

                FixedStepSimulationSystemGroup fixedStepGroup = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();
                fixedStepGroup.RateManager = new RateUtils.FixedRateCatchUpManager(1f / sceneInitializer.FixedRate);

                // Spawn main character
                Entity mainCharacterEntity = EntityManager.Instantiate(sceneInitializer.MainCharacterPrefabEntity);
                EntityManager.SetComponentData(mainCharacterEntity, new Translation { Value = sceneInitializer.MainCharacterSpawnPoint.pos });
                EntityManager.SetComponentData(mainCharacterEntity, new Rotation { Value = sceneInitializer.MainCharacterSpawnPoint.rot });
                EntityManager.AddComponentData(mainCharacterEntity, new PlatformerInputs());

                // Setup the camera
                Entity gameCameraEntity = EntityManager.Instantiate(sceneInitializer.GameCameraPrefabEntity);
                OrbitCamera orbitCameraComponent = GetComponent<OrbitCamera>(gameCameraEntity);
                orbitCameraComponent.CharacterEntity = mainCharacterEntity;
                orbitCameraComponent.FollowedEntity = GetComponent<PlatformerCharacterComponent>(mainCharacterEntity).DefaultCameraTargetEntity;
                orbitCameraComponent.FollowedTranslation = GetComponent<Translation>(GetComponent<PlatformerCharacterComponent>(mainCharacterEntity).DefaultCameraTargetEntity).Value;
                orbitCameraComponent.PlanarForward = math.normalizesafe(sceneInitializer.StartingCameraForward);
                EntityManager.GetBuffer<IgnoredEntityBufferElement>(gameCameraEntity).Add(new IgnoredEntityBufferElement { Entity = mainCharacterEntity });
                EntityManager.SetComponentData(gameCameraEntity, orbitCameraComponent);

                PlatformerInputs inputs = GetComponent<PlatformerInputs>(mainCharacterEntity);
                inputs.CameraReference = gameCameraEntity;
                SetComponent(mainCharacterEntity, inputs);

                // Remove sceneInitializer component
                EntityManager.RemoveComponent<PlatformerSceneInitialization>(GetSingletonEntity<PlatformerSceneInitialization>());
            }
        }
    }
}