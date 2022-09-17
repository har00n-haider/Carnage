using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;
using Unity.Collections.LowLevel.Unsafe;
using Unity.NetCode;
using UnityEngine;
using System;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct RPCCharacterDeathVFX : IRpcCommand
    {
        public float3 Position;
    }

    [UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
    public partial class OnlineFPSCharacterMovementSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;
        public GhostPredictionSystemGroup GhostPredictionSystemGroup;
        public EntityQuery PredictedCharacterQuery;

        [BurstCompile]
        public struct OnlineFPSCharacterJob : IJobEntityBatch
        {
            public uint Tick;
            public float DeltaTime;
            [ReadOnly]
            public CollisionWorld CollisionWorld;

            [ReadOnly]
            public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
            [ReadOnly]
            public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;
            [ReadOnly]
            public ComponentDataFromEntity<StoredKinematicCharacterBodyProperties> StoredKinematicCharacterBodyPropertiesFromEntity;
            [ReadOnly]
            public ComponentDataFromEntity<TrackedTransform> TrackedTransformFromEntity;

            [ReadOnly]
            public EntityTypeHandle EntityType;
            public ComponentTypeHandle<Translation> TranslationType;
            [ReadOnly]
            public ComponentTypeHandle<Rotation> RotationType;
            public ComponentTypeHandle<KinematicCharacterBody> KinematicCharacterBodyType;
            [ReadOnly]
            public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;
            public BufferTypeHandle<KinematicCharacterHit> CharacterHitsBufferType;
            public BufferTypeHandle<KinematicVelocityProjectionHit> VelocityProjectionHitsBufferType;
            public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;
            public BufferTypeHandle<StatefulKinematicCharacterHit> StatefulCharacterHitsBufferType;

            [ReadOnly]
            public ComponentTypeHandle<PredictedGhostComponent> PredictedGhostType;
            public ComponentTypeHandle<OnlineFPSCharacterComponent> OnlineFPSCharacterType;
            [ReadOnly]
            public ComponentTypeHandle<OnlineFPSCharacterInputs> OnlineFPSCharacterInputsType;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<int> TmpRigidbodyIndexesProcessed;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<Unity.Physics.RaycastHit> TmpRaycastHits;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<ColliderCastHit> TmpColliderCastHits;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<DistanceHit> TmpDistanceHits;

            public void Execute(ArchetypeChunk chunk, int batchIndex)
            {
                NativeArray<Entity> chunkEntities = chunk.GetNativeArray(EntityType);
                NativeArray<Translation> chunkTranslations = chunk.GetNativeArray(TranslationType);
                NativeArray<Rotation> chunkRotations = chunk.GetNativeArray(RotationType);
                NativeArray<KinematicCharacterBody> chunkCharacterBodies = chunk.GetNativeArray(KinematicCharacterBodyType);
                NativeArray<PhysicsCollider> chunkPhysicsColliders = chunk.GetNativeArray(PhysicsColliderType);
                BufferAccessor<KinematicCharacterHit> chunkCharacterHitBuffers = chunk.GetBufferAccessor(CharacterHitsBufferType);
                BufferAccessor<KinematicVelocityProjectionHit> chunkVelocityProjectionHitBuffers = chunk.GetBufferAccessor(VelocityProjectionHitsBufferType);
                BufferAccessor<KinematicCharacterDeferredImpulse> chunkCharacterDeferredImpulsesBuffers = chunk.GetBufferAccessor(CharacterDeferredImpulsesBufferType);
                BufferAccessor<StatefulKinematicCharacterHit> chunkStatefulCharacterHitsBuffers = chunk.GetBufferAccessor(StatefulCharacterHitsBufferType);
                NativeArray<PredictedGhostComponent> chunkPredictedGhosts = chunk.GetNativeArray(PredictedGhostType);
                NativeArray<OnlineFPSCharacterComponent> chunkOnlineFPSCharacters = chunk.GetNativeArray(OnlineFPSCharacterType);
                NativeArray<OnlineFPSCharacterInputs> chunkOnlineFPSCharacterInputs = chunk.GetNativeArray(OnlineFPSCharacterInputsType);

                // Initialize the Temp collections
                if (!TmpRigidbodyIndexesProcessed.IsCreated)
                {
                    TmpRigidbodyIndexesProcessed = new NativeList<int>(24, Allocator.Temp);
                }
                if (!TmpRaycastHits.IsCreated)
                {
                    TmpRaycastHits = new NativeList<Unity.Physics.RaycastHit>(24, Allocator.Temp);
                }
                if (!TmpColliderCastHits.IsCreated)
                {
                    TmpColliderCastHits = new NativeList<ColliderCastHit>(24, Allocator.Temp);
                }
                if (!TmpDistanceHits.IsCreated)
                {
                    TmpDistanceHits = new NativeList<DistanceHit>(24, Allocator.Temp);
                }

                // Assign the global data of the processor
                OnlineFPSCharacterProcessor processor = default;
                processor.DeltaTime = DeltaTime;
                processor.CollisionWorld = CollisionWorld;
                processor.StoredKinematicCharacterBodyPropertiesFromEntity = StoredKinematicCharacterBodyPropertiesFromEntity;
                processor.PhysicsMassFromEntity = PhysicsMassFromEntity;
                processor.PhysicsVelocityFromEntity = PhysicsVelocityFromEntity;
                processor.TrackedTransformFromEntity = TrackedTransformFromEntity;
                processor.TmpRigidbodyIndexesProcessed = TmpRigidbodyIndexesProcessed;
                processor.TmpRaycastHits = TmpRaycastHits;
                processor.TmpColliderCastHits = TmpColliderCastHits;
                processor.TmpDistanceHits = TmpDistanceHits;

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (GhostPredictionSystemGroup.ShouldPredict(Tick, chunkPredictedGhosts[i]))
                    {
                        Entity entity = chunkEntities[i];
                        // Assign the per-character data of the processor
                        processor.Entity = entity;
                        processor.Translation = chunkTranslations[i].Value;
                        processor.Rotation = chunkRotations[i].Value;
                        processor.PhysicsCollider = chunkPhysicsColliders[i];
                        processor.CharacterBody = chunkCharacterBodies[i];
                        processor.CharacterHitsBuffer = chunkCharacterHitBuffers[i];
                        processor.CharacterDeferredImpulsesBuffer = chunkCharacterDeferredImpulsesBuffers[i];
                        processor.VelocityProjectionHitsBuffer = chunkVelocityProjectionHitBuffers[i];
                        processor.StatefulCharacterHitsBuffer = chunkStatefulCharacterHitsBuffers[i];
                        processor.OnlineFPSCharacter = chunkOnlineFPSCharacters[i];
                        processor.CharacterIputs = chunkOnlineFPSCharacterInputs[i];

                        processor.OnUpdate();

                        // Write back updated data
                        chunkTranslations[i] = new Translation { Value = processor.Translation };
                        chunkCharacterBodies[i] = processor.CharacterBody;
                        chunkOnlineFPSCharacters[i] = processor.OnlineFPSCharacter;
                    }
                }
            }
        }

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();

            PredictedCharacterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = MiscUtilities.CombineArrays(
                    KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                    new ComponentType[]
                    {
                        typeof(OnlineFPSCharacterComponent),
                        typeof(OnlineFPSCharacterInputs),
                        typeof(PredictedGhostComponent),
                    }),
            });

            RequireForUpdate(PredictedCharacterQuery);
        }

        protected unsafe override void OnUpdate()
        {
            uint tick = GhostPredictionSystemGroup.PredictingTick;
            CollisionWorld collisionWorld = BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

            Dependency = new OnlineFPSCharacterJob
            {
                Tick = tick,
                DeltaTime = Time.DeltaTime,
                CollisionWorld = collisionWorld,

                PhysicsVelocityFromEntity = GetComponentDataFromEntity<PhysicsVelocity>(true),
                PhysicsMassFromEntity = GetComponentDataFromEntity<PhysicsMass>(true),
                StoredKinematicCharacterBodyPropertiesFromEntity = GetComponentDataFromEntity<StoredKinematicCharacterBodyProperties>(true),
                TrackedTransformFromEntity = GetComponentDataFromEntity<TrackedTransform>(true),

                EntityType = GetEntityTypeHandle(),
                TranslationType = GetComponentTypeHandle<Translation>(false),
                RotationType = GetComponentTypeHandle<Rotation>(true),
                KinematicCharacterBodyType = GetComponentTypeHandle<KinematicCharacterBody>(false),
                PhysicsColliderType = GetComponentTypeHandle<PhysicsCollider>(true),
                CharacterHitsBufferType = GetBufferTypeHandle<KinematicCharacterHit>(false),
                VelocityProjectionHitsBufferType = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false),
                CharacterDeferredImpulsesBufferType = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false),
                StatefulCharacterHitsBufferType = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false),

                PredictedGhostType = GetComponentTypeHandle<PredictedGhostComponent>(true),
                OnlineFPSCharacterType = GetComponentTypeHandle<OnlineFPSCharacterComponent>(false),
                OnlineFPSCharacterInputsType = GetComponentTypeHandle<OnlineFPSCharacterInputs>(true),
            }.Schedule(PredictedCharacterQuery, Dependency);

            BuildPhysicsWorldSystem.AddInputDependencyToComplete(Dependency);
        }
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateBefore(typeof(PredictedPhysicsSystemGroup))]
    [UpdateAfter(typeof(OnlineFPSPlayerControlSystem))]
    public partial class OnlineFPSCharacterRotationSystem : SystemBase
    {
        public GhostPredictionSystemGroup GhostPredictionSystemGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
            RequireSingletonForUpdate<NetworkIdComponent>();
        }

        protected override void OnUpdate()
        {
            uint tick = GhostPredictionSystemGroup.PredictingTick;
            float deltaTime = Time.DeltaTime;

            Entities.ForEach((
                Entity entity,
                ref OnlineFPSCharacterComponent character,
                in OnlineFPSCharacterInputs inputs,
                in KinematicCharacterBody characterBody,
                in PredictedGhostComponent predictedGhost) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
                    return;

                Rotation characterRotation = GetComponent<Rotation>(entity);

                // Camera tilt
                {
                    float3 characterRight = MathUtilities.GetRightFromRotation(characterRotation.Value);
                    float characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;
                    float3 characterLateralVelocity = math.projectsafe(characterBody.RelativeVelocity, characterRight);
                    float characterLateralVelocityRatio = math.clamp(math.length(characterLateralVelocity) / characterMaxSpeed, 0f, 1f);
                    bool velocityIsRight = math.dot(characterBody.RelativeVelocity, characterRight) > 0f;
                    float targetTiltAngle = math.lerp(0f, character.TiltAmount, characterLateralVelocityRatio);
                    targetTiltAngle = velocityIsRight ? -targetTiltAngle : targetTiltAngle;
                    character.CameraTiltAngle = math.lerp(character.CameraTiltAngle, targetTiltAngle, math.saturate(character.TiltSharpness * deltaTime));
                }
                 
                // Compute character & view rotations from rotation input
                OnlineFPSCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                    ref characterRotation.Value,
                    ref character.ViewPitchDegrees,
                    inputs.LookYawPitchDegrees,
                    character.CameraTiltAngle,
                    -89f,
                    89f,
                    out quaternion localViewRotation,
                    out float canceledPitchDegrees);

                SetComponent(entity, characterRotation);
                SetComponent(character.ViewEntity, new Rotation { Value = localViewRotation });
            }).Schedule();
        }
    }
}
