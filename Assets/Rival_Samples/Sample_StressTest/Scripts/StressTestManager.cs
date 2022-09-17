using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;

namespace Rival.Samples.StressTest
{
    public class StressTestManager : MonoBehaviour
    {
        [Header("Parameters")]
        public GameObject CharacterPrefab;
        public List<GameObject> EnvironmentPrefabs;
        public float SpawnSpacing = 5f;

        [Header("References")]
        public Camera Camera;
        public Button SpawnButton;
        public InputField SpawnCountInputField;
        public Dropdown EnvironmentPrefabDropdown;
        public Toggle MultithreadedToggle;
        public Toggle PhysicsStepToggle;
        public Toggle RenderingToggle;
        public Toggle StepHandlingToggle;
        public Toggle SlopeChangesToggle;
        public Toggle AddCloseHitsForProjectionToggle;
        public Toggle StatefulHitsToggle;
        public Toggle SimulateDynamicToggle;

        private EntityManager _entityManager;
        private Entity _characterPrefabEntity;
        private List<Entity> _environmentEntities;
        private List<Entity> _spawnedCharacters;
        private Entity _spawnedEnvironment;
        private BlobAssetStore _blobAssetStore;
        private EntityQuery _characterQuery;

        private void OnDisable()
        {
            if (_blobAssetStore != null)
            {
                _blobAssetStore.Dispose();
            }
        }

        void Start()
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FixedStepSimulationSystemGroup>().RateManager = null;

            _blobAssetStore = new BlobAssetStore();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _characterQuery = _entityManager.CreateEntityQuery(typeof(KinematicCharacterBody));

            // Convert to entity
            GameObjectConversionSettings conversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, _blobAssetStore);
            _characterPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(CharacterPrefab, conversionSettings);
            _environmentEntities = new List<Entity>();
            for (int i = 0; i < EnvironmentPrefabs.Count; i++)
            {
                _environmentEntities.Add(GameObjectConversionUtility.ConvertGameObjectHierarchy(EnvironmentPrefabs[i], conversionSettings));
            }

            // Subscribe UI
            SpawnButton.onClick.AddListener(SpawnCharacters);
            EnvironmentPrefabDropdown.onValueChanged.AddListener(SwitchEnvironment);
            for (int i = 0; i < EnvironmentPrefabs.Count; i++)
            {
                EnvironmentPrefabDropdown.AddOptions(new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData(EnvironmentPrefabs[i].name),
                });
            }
            MultithreadedToggle.onValueChanged.AddListener(SetMultithreaded);
            PhysicsStepToggle.onValueChanged.AddListener(SetPhysicsStep);
            RenderingToggle.onValueChanged.AddListener(SetRendering);
            StepHandlingToggle.onValueChanged.AddListener(SetStepHandling);
            SlopeChangesToggle.onValueChanged.AddListener(SetSlopeChanges);
            AddCloseHitsForProjectionToggle.onValueChanged.AddListener(SetAddCloseHitsForProjection);
            StatefulHitsToggle.onValueChanged.AddListener(SetStatefulHits);
            SimulateDynamicToggle.onValueChanged.AddListener(SetSimulateDynamicBody);

            // Initial setup
            _spawnedCharacters = new List<Entity>();
            SwitchEnvironment(EnvironmentPrefabDropdown.value);
            SetMultithreaded(MultithreadedToggle.isOn);
            SetRendering(RenderingToggle.isOn);

            ApplyCharacterSettings();
        }

        public void SpawnCharacters()
        {
            GameObjectConversionSettings conversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, _blobAssetStore);
            _characterPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(CharacterPrefab, conversionSettings);

            // Clear spawned characters
            for (int i = 0; i < _spawnedCharacters.Count; i++)
            {
                _entityManager.DestroyEntity(_spawnedCharacters[i]);
            }

            // Spawn new characters
            if (int.TryParse(SpawnCountInputField.text, out int spawnCount))
            {
                int spawnResolution = Mathf.CeilToInt(Mathf.Sqrt(spawnCount));
                float totalWidth = (spawnResolution - 1) * SpawnSpacing;
                float3 spawnBottomCorner = (-math.right() * totalWidth * 0.5f) + (-math.forward() * totalWidth * 0.5f);

                int counter = 0;
                for (int x = 0; x < spawnResolution; x++)
                {
                    for (int z = 0; z < spawnResolution; z++)
                    {
                        if (counter >= spawnCount)
                        {
                            break;
                        }

                        Entity spawnedCharacter = _entityManager.Instantiate(_characterPrefabEntity);
                        _spawnedCharacters.Add(spawnedCharacter);

                        float3 spawnPos = spawnBottomCorner + (math.right() * x * SpawnSpacing) + (math.forward() * z * SpawnSpacing);
                        _entityManager.SetComponentData(spawnedCharacter, new Translation { Value = spawnPos });

                        counter++;
                    }
                }
            }

            ApplyCharacterSettings();
        }

        private void ApplyCharacterSettings()
        {
            SetStepHandling(StepHandlingToggle.isOn);
            SetSlopeChanges(SlopeChangesToggle.isOn);
            SetAddCloseHitsForProjection(AddCloseHitsForProjectionToggle.isOn);
            SetStatefulHits(StatefulHitsToggle.isOn);
            SetSimulateDynamicBody(SimulateDynamicToggle.isOn);
        }

        public void SwitchEnvironment(int index)
        {
            if (_spawnedEnvironment != Entity.Null)
            {
                _entityManager.DestroyEntity(_spawnedEnvironment);
            }

            _spawnedEnvironment = _entityManager.Instantiate(_environmentEntities[index]);
        }

        public void SetMultithreaded(bool active)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<StressTestCharacterSystem>().Multithreaded = active;
        }

        public void SetPhysicsStep(bool active)
        {
            PhysicsStep physicsStep = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<StressTestCharacterSystem>().GetSingleton<PhysicsStep>();
            physicsStep.SimulationType = active ? SimulationType.UnityPhysics : SimulationType.NoPhysics;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<StressTestCharacterSystem>().SetSingleton<PhysicsStep>(physicsStep);
        }

        public void SetRendering(bool active)
        {
            Camera.enabled = active;
        }

        public void SetStepHandling(bool active)
        {
            NativeArray<Entity> entities = _characterQuery.ToEntityArray(Allocator.TempJob);
            foreach (var ent in entities)
            {
                StressTestCharacterComponent character = _entityManager.GetComponentData<StressTestCharacterComponent>(ent);
                character.StepHandling = active;
                _entityManager.SetComponentData(ent, character);
            }
            entities.Dispose();
        }

        public void SetSlopeChanges(bool active)
        {
            NativeArray<Entity> entities = _characterQuery.ToEntityArray(Allocator.TempJob);
            foreach (var ent in entities)
            {
                StressTestCharacterComponent character = _entityManager.GetComponentData<StressTestCharacterComponent>(ent);
                character.PreventGroundingWhenMovingTowardsNoGrounding = active;
                character.HasMaxDownwardSlopeChangeAngle = active;
                _entityManager.SetComponentData(ent, character);
            }
            entities.Dispose();
        }

        public void SetAddCloseHitsForProjection(bool active)
        {
            NativeArray<Entity> entities = _characterQuery.ToEntityArray(Allocator.TempJob);
            foreach (var ent in entities)
            {
                KinematicCharacterBody characterProperties = _entityManager.GetComponentData<KinematicCharacterBody>(ent);
                characterProperties.ProjectVelocityOnInitialOverlaps = active;
                _entityManager.SetComponentData(ent, characterProperties);
            }
            entities.Dispose();
        }

        public void SetStatefulHits(bool active)
        {
            NativeArray<Entity> entities = _characterQuery.ToEntityArray(Allocator.TempJob);
            foreach (var ent in entities)
            {
                StressTestCharacterComponent character = _entityManager.GetComponentData<StressTestCharacterComponent>(ent);
                character.ProcessStatefulCharacterHits = active;
                _entityManager.SetComponentData(ent, character);
            }
            entities.Dispose();
        }

        public unsafe void SetSimulateDynamicBody(bool value)
        {
            NativeArray<Entity> entities = _characterQuery.ToEntityArray(Allocator.TempJob);
            foreach (var ent in entities)
            {
                KinematicCharacterBody characterBody = _entityManager.GetComponentData<KinematicCharacterBody>(ent);
                characterBody.SimulateDynamicBody = value;
                _entityManager.SetComponentData(ent, characterBody);

                PhysicsCollider physicsCollider = _entityManager.GetComponentData<PhysicsCollider>(ent);
                Unity.Physics.ConvexCollider* collider = (Unity.Physics.ConvexCollider*)physicsCollider.ColliderPtr;
                Unity.Physics.Material material = collider->Material;
                material.CollisionResponse = value ? CollisionResponsePolicy.RaiseTriggerEvents : CollisionResponsePolicy.Collide;
                collider->Material = material;
                _entityManager.SetComponentData(ent, physicsCollider);
            }
            entities.Dispose();
        }
    }
}
