using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Physics.Systems;
using Unity.Physics;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct RPCJoinGameRequest : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
    }

    [Serializable]
    public struct RPCDisplayRespawnTimer : IRpcCommand
    {
        public float RespawnTime;
    }

    public partial class CommonGameSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            Entity tickRateEntity = EntityManager.CreateEntity();
            ClientServerTickRate tickRate = new ClientServerTickRate();
            tickRate.ResolveDefaults();
            tickRate.SimulationTickRate = 60;
            EntityManager.AddComponentData(tickRateEntity, tickRate);

            Entity predictedPhysicsConfigSingleton = EntityManager.CreateEntity();
            EntityManager.AddComponentData(predictedPhysicsConfigSingleton, new PredictedPhysicsConfig
            {
                DisableWhenNoConnections = false,
                PhysicsTicksPerSimTick = 1,
            });
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            int localNetworkId = -1;
            if (HasSingleton<NetworkIdComponent>())
            {
                localNetworkId = GetSingleton<NetworkIdComponent>().Value;
            }

            Entities
                .WithName("PlayerControlledCharacterSetupJob")
                .WithoutBurst()
                .ForEach((Entity playerEntity, ref OnlineFPSPlayer fpsPlayer, in GhostOwnerComponent ghostOwner) =>
                {
                    // Detect changes in controlled entity
                    if (HasComponent<OnlineFPSCharacterComponent>(fpsPlayer.ControlledEntity) && fpsPlayer.ControlledEntity != fpsPlayer.PreviousControlledEntity)
                    {
                        // Update the controlled entity's owning player
                        if (!HasComponent<OwningPlayer>(fpsPlayer.ControlledEntity))
                        {
                            commandBuffer.AddComponent<OwningPlayer>(fpsPlayer.ControlledEntity);
                        }
                        commandBuffer.SetComponent(fpsPlayer.ControlledEntity, new OwningPlayer { PlayerEntity = playerEntity });

                        fpsPlayer.PreviousControlledEntity = fpsPlayer.ControlledEntity;
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostSimulationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public partial class ClientGameSystem : SystemBase
    {
        public OnlineFPSGameData OnlineFPSGameData;
        public GhostSimulationSystemGroup GhostSimulationSystemGroup;

        private Unity.Mathematics.Random _random;

        protected override void OnStartRunning()
        {
            base.OnCreate();

            OnlineFPSGameData = OnlineFPSGameData.Load();
            GhostSimulationSystemGroup = World.GetOrCreateSystem<GhostSimulationSystemGroup>();

            RequireSingletonForUpdate<NetworkIdComponent>();
            RequireSingletonForUpdate<OnlineFPSData>();
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<MapIsLoaded>()));

            _random = Unity.Mathematics.Random.CreateFromIndex((uint)1);
        }

        protected override void OnUpdate()
        {
            if(!HasSingleton<OnlineFPSData>())
            {
                return;
            }

            OnlineFPSData data = GetSingleton<OnlineFPSData>();
            int localNetworkId = GetSingleton<NetworkIdComponent>().Value;
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            BufferFromEntity<Child> childBufferFromEntity = GetBufferFromEntity<Child>(true);

            Entities
                .WithName("ClientJoinGameRequestJob")
                .WithNone<NetworkStreamInGame>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref NetworkIdComponent netId) =>
                {
                    // Set connection ready
                    commandBuffer.AddComponent<NetworkStreamInGame>(entity);

                    // Send a game start request to the server
                    Entity requestGameStartEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(requestGameStartEntity, new RPCJoinGameRequest { PlayerName = GetSingleton<LocalGameData>().PlayerName });
                    commandBuffer.AddComponent(requestGameStartEntity, new SendRpcCommandRequestComponent { TargetConnection = entity });
                }).Run();

            Entities
                .WithName("ClientInitSpawnedCharactersJob")
                .WithoutBurst()
                .WithNone<IsInitialized>()
                .ForEach((Entity entity, ref GhostOwnerComponent ghostOwner, in OnlineFPSCharacterComponent character) =>
                {
                    if(ghostOwner.NetworkId == localNetworkId)
                    {
                        commandBuffer.AddComponent(character.ViewEntity, new MainEntityCamera { FoV = 75 });

                        // Make local character meshes rendering be shadow-only
                        OnlineFPSUtilities.SetShadowModeInHierarchy(EntityManager, commandBuffer, character.MeshRootEntity, childBufferFromEntity, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
                    }

                    // Spawn nameplate
                    GameObject nameplate = GameObject.Instantiate(OnlineFPSGameData.NameplatePrefab);
                    NameplateBehaviour nameplateBehaviour = nameplate.GetComponent<NameplateBehaviour>();
                    nameplateBehaviour.Setup(entity, World, 2.5f);

                    commandBuffer.AddComponent<IsInitialized>(entity);
                }).Run();

            Entities
                .WithName("DisplayRespawnTimerJob")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in RPCDisplayRespawnTimer request, in ReceiveRpcCommandRequestComponent receiveRPCCommand) =>
                {
                    GameObject countdownUI = GameObject.Instantiate(OnlineFPSGameData.RespawnCountdownPrefab);
                    RespawnCountdownUIManager respawnCountdownUIManager = countdownUI.GetComponent<RespawnCountdownUIManager>();
                    respawnCountdownUIManager.CountdownTime = request.RespawnTime;

                    commandBuffer.DestroyEntity(entity);
                }).Run();

            Entities
                .WithName("ClientDisconnectJob")
                .WithAll<NetworkStreamDisconnected>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref NetworkIdComponent netId) =>
                {
                    // Return to menu
                    SceneManager.LoadScene(OnlineFPSGameData.MenuSceneName);
                }).Run();

            Unity.Mathematics.Random sparksRandom = _random;
            Entities
                .WithName("ClientCharacterDeathSparksJob")
                .WithoutBurst()
               .ForEach((Entity entity, ref RPCCharacterDeathVFX rpc, in ReceiveRpcCommandRequestComponent receiveRPCCommand) =>
               {
                   for (int s = 0; s < 48; s++)
                   {
                       Entity hitVfxEntity = commandBuffer.Instantiate(data.DeathSparkPrefab);
                       commandBuffer.SetComponent(hitVfxEntity, new Translation { Value = rpc.Position });
                       commandBuffer.SetComponent(hitVfxEntity, new Rotation { Value = sparksRandom.NextQuaternionRotation() });
                   }

                   commandBuffer.DestroyEntity(entity);
               }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [AlwaysSynchronizeSystem]
    public partial class ServerGameSystem : SystemBase
    {
        public EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem;

        public OnlineFPSGameData OnlineFPSGameData;

        private Entity _characterPrefabEntity;
        private Entity _railgunPrefabEntity;
        private Entity _playerPrefabEntity;
        private EntityQuery _spawnPointsQuery;

        protected override void OnStartRunning()
        {
            base.OnCreate();

            OnlineFPSGameData = OnlineFPSGameData.Load();
            _spawnPointsQuery = GetEntityQuery(typeof(CharacterSpawnPoint));

            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<MapIsLoaded>()));

            EndSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<GhostPrefabsReference>())
            {
                return;
            }

            float respawnTime = OnlineFPSGameData.RespawnTime;
            Entity ghostPrefabsReference = GetSingletonEntity<GhostPrefabsReference>();

            // Get prefabs
            if (_characterPrefabEntity == Entity.Null)
            {
                OnlineFPSUtilities.GetGhostPrefabOfType<OnlineFPSCharacterComponent>(EntityManager, ghostPrefabsReference, out _characterPrefabEntity);
            }
            if (_railgunPrefabEntity == Entity.Null)
            {
                OnlineFPSUtilities.GetGhostPrefabOfType<Railgun>(EntityManager, ghostPrefabsReference, out _railgunPrefabEntity);
            }
            if (_playerPrefabEntity == Entity.Null)
            {
                OnlineFPSUtilities.GetGhostPrefabOfType<OnlineFPSPlayer>(EntityManager, ghostPrefabsReference, out _playerPrefabEntity);
            }

            float deltaTime = Time.DeltaTime;
            Entity characterPrefabEntity = _characterPrefabEntity;
            Entity railgunPrefabEntity = _railgunPrefabEntity;
            Entity playerPrefabEntity = _playerPrefabEntity;

            Entities
                .WithName("ConnectionSetupJob")
                .WithNone<ConnectionOwnedEntity>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref NetworkIdComponent netId) =>
                {
                    EntityManager.AddBuffer<ConnectionOwnedEntity>(entity);
                }).Run();

            Entities
                .WithName("ServerHandleClientJoinJob")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in RPCJoinGameRequest request, in ReceiveRpcCommandRequestComponent receiveRPCCommand) =>
                {
                    int forNetworkConnectionId = EntityManager.GetComponentData<NetworkIdComponent>(receiveRPCCommand.SourceConnection).Value;

                    // Spawn player
                    Entity playerEntity = EntityManager.Instantiate(playerPrefabEntity);
                    EntityManager.SetComponentData(playerEntity, new GhostOwnerComponent { NetworkId = forNetworkConnectionId });
                    OnlineFPSPlayer player = EntityManager.GetComponentData<OnlineFPSPlayer>(playerEntity);
                    player.PlayerName = request.PlayerName;
                    player.AssociatedConnectionEntity = receiveRPCCommand.SourceConnection;
                    EntityManager.SetComponentData(playerEntity, player);

                    // Add player to owned entities
                    DynamicBuffer<ConnectionOwnedEntity> connectionOwnedEntitiesBuffer = EntityManager.GetBuffer<ConnectionOwnedEntity>(receiveRPCCommand.SourceConnection);
                    connectionOwnedEntitiesBuffer.Add(new ConnectionOwnedEntity { Entity = playerEntity });

                    // Character spawn request
                    Entity characterSpawnRequest = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(characterSpawnRequest, new CharacterSpawnRequest
                    {
                        Timer = 0f,
                        ForNetworkConnectionId = forNetworkConnectionId,
                        ForPlayerEntity = playerEntity,
                    });

                    // Stream connection in game
                    EntityManager.AddComponent<NetworkStreamInGame>(receiveRPCCommand.SourceConnection);

                    EntityManager.DestroyEntity(entity);
                }).Run();

            Entities
                .WithName("ServerOnClientDisconnectJob")
                .WithAll<NetworkStreamDisconnected>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref NetworkIdComponent netId) =>
                {
                    // Destroy all objects of that connection
                    DynamicBuffer<ConnectionOwnedEntity> connectionOwnedEntitiesBuffer = EntityManager.GetBuffer<ConnectionOwnedEntity>(entity);
                    for (int i = 0; i < connectionOwnedEntitiesBuffer.Length; i++)
                    {
                        EntityManager.DestroyEntity(connectionOwnedEntitiesBuffer[i].Entity);
                    }
                }).Run();

            NativeArray<Entity> characterSpawnPoints = _spawnPointsQuery.ToEntityArray(Allocator.TempJob);
            Entities
                .WithName("ServerSpawnCharactersJob")
                .WithoutBurst()
                .WithStructuralChanges()
                .WithDisposeOnCompletion(characterSpawnPoints)
                .ForEach((Entity entity, ref CharacterSpawnRequest spawnRequest) =>
                {
                    spawnRequest.Timer -= deltaTime;
                    if (spawnRequest.Timer <= 0f)
                    {
                        Entity chosenSpawnPointEntity = characterSpawnPoints[UnityEngine.Random.Range(0, characterSpawnPoints.Length)];
                        LocalToWorld spawnPointLocalToWorld = EntityManager.GetComponentData<LocalToWorld>(chosenSpawnPointEntity);

                        // Spawn player's character
                        Entity characterEntity = EntityManager.Instantiate(characterPrefabEntity);
                        EntityManager.SetComponentData(characterEntity, new GhostOwnerComponent { NetworkId = spawnRequest.ForNetworkConnectionId });
                        EntityManager.AddComponentData(characterEntity, new OwningPlayer { PlayerEntity = spawnRequest.ForPlayerEntity });
                        EntityManager.SetComponentData(characterEntity, new Translation { Value = spawnPointLocalToWorld.Position });
                        EntityManager.SetComponentData(characterEntity, new Rotation { Value = spawnPointLocalToWorld.Rotation });

                        // Add character to connection owned entities
                        OnlineFPSPlayer player = EntityManager.GetComponentData<OnlineFPSPlayer>(spawnRequest.ForPlayerEntity);
                        DynamicBuffer<ConnectionOwnedEntity> connectionOwnedEntitiesBuffer = EntityManager.GetBuffer<ConnectionOwnedEntity>(player.AssociatedConnectionEntity);
                        connectionOwnedEntitiesBuffer.Add(new ConnectionOwnedEntity { Entity = characterEntity });

                        // Setup player controlled entity
                        player.ControlledEntity = characterEntity;
                        EntityManager.SetComponentData(spawnRequest.ForPlayerEntity, player);

                        // Spawn weapon and set as activeWeapon
                        Entity weaponInstance = EntityManager.Instantiate(railgunPrefabEntity);
                        EntityManager.SetComponentData(weaponInstance, new GhostOwnerComponent { NetworkId = spawnRequest.ForNetworkConnectionId });
                        ActiveWeapon activeWeapon = EntityManager.GetComponentData<ActiveWeapon>(characterEntity);
                        activeWeapon.WeaponEntity = weaponInstance;
                        EntityManager.SetComponentData(characterEntity, activeWeapon);

                        EntityManager.DestroyEntity(entity);
                    }
                }).Run();

            EntityCommandBuffer commandBuffer = EndSimulationEntityCommandBufferSystem.CreateCommandBuffer();
            Dependency = Entities
                .WithName("ServerDetectCharacterDeathJob")
                .ForEach((
                    Entity entity,
                    ref Health health,
                    in OnlineFPSCharacterComponent character,
                    in GhostOwnerComponent ghostOwner,
                    in PhysicsCollider physicsCollider,
                    in Translation translation,
                    in Rotation rotation) =>
                {
                    // Handle Y kill
                    if (translation.Value.y < -200f)
                    {
                        health.CurrentHealth = 0f;
                    }

                    if (health.CurrentHealth <= 0f)
                    {
                        commandBuffer.DestroyEntity(entity);

                        Entity owningPlayerEntity = Entity.Null;
                        if (HasComponent<OwningPlayer>(entity))
                        {
                            owningPlayerEntity = GetComponent<OwningPlayer>(entity).PlayerEntity;
                        }

                        // VFX
                        float3 characterCenter = translation.Value + math.mul(rotation.Value, physicsCollider.MassProperties.MassDistribution.Transform.pos);
                        Entity vfxRPCEntity = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent(vfxRPCEntity, new RPCCharacterDeathVFX { Position = characterCenter });
                        commandBuffer.AddComponent(vfxRPCEntity, new SendRpcCommandRequestComponent { TargetConnection = Entity.Null });

                        OnlineFPSPlayer player = GetComponent<OnlineFPSPlayer>(owningPlayerEntity);
                        if (HasComponent<NetworkIdComponent>(player.AssociatedConnectionEntity))
                        {
                            // Respawn
                            Entity respawnRequest = commandBuffer.CreateEntity();
                            commandBuffer.AddComponent(respawnRequest, new CharacterSpawnRequest
                            {
                                Timer = respawnTime,
                                ForNetworkConnectionId = ghostOwner.NetworkId,
                                ForPlayerEntity = owningPlayerEntity,
                            });

                            // Send respawn countdown RPC to owner of character
                            if (HasComponent<OnlineFPSPlayer>(owningPlayerEntity))
                            {
                                if (player.AssociatedConnectionEntity != Entity.Null)
                                {
                                    Entity respawnCountdownRPCEntity = commandBuffer.CreateEntity();
                                    commandBuffer.AddComponent(respawnCountdownRPCEntity, new RPCDisplayRespawnTimer { RespawnTime = respawnTime });
                                    commandBuffer.AddComponent(respawnCountdownRPCEntity, new SendRpcCommandRequestComponent { TargetConnection = player.AssociatedConnectionEntity });
                                }
                            }
                        }
                    }
                }).Schedule(Dependency);
            EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}