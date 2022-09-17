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
using Unity.Profiling;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
    public partial class BasicCharacterSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;
        public EntityQuery CharacterQuery;

        [BurstCompile]
        public struct BasicCharacterJob : IJobEntityBatch
        {
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
            public ComponentTypeHandle<Rotation> RotationType;
            public ComponentTypeHandle<KinematicCharacterBody> KinematicCharacterBodyType;
            public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;
            public BufferTypeHandle<KinematicCharacterHit> CharacterHitsBufferType;
            public BufferTypeHandle<KinematicVelocityProjectionHit> VelocityProjectionHitsBufferType;
            public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;
            public BufferTypeHandle<StatefulKinematicCharacterHit> StatefulCharacterHitsBufferType;

            public ComponentTypeHandle<BasicCharacterComponent> BasicCharacterType;
            [ReadOnly]
            public ComponentTypeHandle<BasicCharacterInputs> CharacterInputsType;
            [ReadOnly]
            public ComponentDataFromEntity<BouncySurface> BouncySurfaceFromEntity;

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
                NativeArray<BasicCharacterComponent> chunkBasicCharacters = chunk.GetNativeArray(BasicCharacterType);
                NativeArray<BasicCharacterInputs> chunkCharacterInputs = chunk.GetNativeArray(CharacterInputsType);

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
                BasicCharacterProcessor processor = default;
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
                processor.BouncySurfaceFromEntity = BouncySurfaceFromEntity;

                for (int i = 0; i < chunk.Count; i++)
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
                    processor.BasicCharacter = chunkBasicCharacters[i];
                    processor.BasicCharacterInputs = chunkCharacterInputs[i];

                    processor.OnUpdate();

                    // Write back updated data
                    chunkTranslations[i] = new Translation { Value = processor.Translation };
                    chunkRotations[i] = new Rotation { Value = processor.Rotation };
                    chunkCharacterBodies[i] = processor.CharacterBody;
                    chunkBasicCharacters[i] = processor.BasicCharacter;
                }
            }
        }

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();

            CharacterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = MiscUtilities.CombineArrays(
                    KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                    new ComponentType[]
                    {
                        typeof(BasicCharacterComponent),
                        typeof(BasicCharacterInputs),
                    }),
            });

            RequireForUpdate(CharacterQuery);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        protected unsafe override void OnUpdate()
        {
            Dependency = new BasicCharacterJob
            {
                DeltaTime = Time.DeltaTime,
                CollisionWorld = BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,

                PhysicsVelocityFromEntity = GetComponentDataFromEntity<PhysicsVelocity>(true),
                PhysicsMassFromEntity = GetComponentDataFromEntity<PhysicsMass>(true),
                StoredKinematicCharacterBodyPropertiesFromEntity = GetComponentDataFromEntity<StoredKinematicCharacterBodyProperties>(true),
                TrackedTransformFromEntity = GetComponentDataFromEntity<TrackedTransform>(true),

                EntityType = GetEntityTypeHandle(),
                TranslationType = GetComponentTypeHandle<Translation>(false),
                RotationType = GetComponentTypeHandle<Rotation>(false),
                KinematicCharacterBodyType = GetComponentTypeHandle<KinematicCharacterBody>(false),
                PhysicsColliderType = GetComponentTypeHandle<PhysicsCollider>(false),
                CharacterHitsBufferType = GetBufferTypeHandle<KinematicCharacterHit>(false),
                VelocityProjectionHitsBufferType = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false),
                CharacterDeferredImpulsesBufferType = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false),
                StatefulCharacterHitsBufferType = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false),

                BasicCharacterType = GetComponentTypeHandle<BasicCharacterComponent>(false),
                CharacterInputsType = GetComponentTypeHandle<BasicCharacterInputs>(true),
                BouncySurfaceFromEntity = GetComponentDataFromEntity<BouncySurface>(true),
            }.ScheduleParallel(CharacterQuery, Dependency);

            Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(this, CharacterQuery, Dependency);
        }
    }
}
