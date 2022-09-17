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

namespace Rival.Samples.StressTest
{
    [UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
    public partial class StressTestCharacterSystem : SystemBase
    {
        public bool Multithreaded = true;
        public BuildPhysicsWorld BuildPhysicsWorldSystem;
        public EndFramePhysicsSystem EndFramePhysicsSystem;
        public EntityQuery CharacterQuery;

        [BurstCompile]
        public struct StressTestCharacterJob : IJobEntityBatch
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

            public ComponentTypeHandle<StressTestCharacterComponent> StressTestCharacterType;
            [ReadOnly]
            public ComponentTypeHandle<StressTestCharacterInputs> StressTestCharacterInputsType;

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
                NativeArray<StressTestCharacterComponent> chunkStressTestCharacters = chunk.GetNativeArray(StressTestCharacterType);
                NativeArray<StressTestCharacterInputs> chunkStressTestCharacterInputs = chunk.GetNativeArray(StressTestCharacterInputsType);

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
                StressTestCharacterProcessor processor = default;
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
                    processor.StressTestCharacter = chunkStressTestCharacters[i];
                    processor.StressTestCharacterInputs = chunkStressTestCharacterInputs[i];

                    processor.OnUpdate();

                    chunkTranslations[i] = new Translation { Value = processor.Translation };
                    chunkRotations[i] = new Rotation { Value = processor.Rotation };
                    chunkCharacterBodies[i] = processor.CharacterBody;
                }
            }
        }

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

            CharacterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = MiscUtilities.CombineArrays(
                    KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                    new ComponentType[]
                    {
                        typeof(StressTestCharacterComponent),
                        typeof(StressTestCharacterInputs),
                    }),
            });

            EntityQueryDesc da = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Translation) }
            };
            EntityQueryDesc db = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Rotation) }
            };

            RequireForUpdate(CharacterQuery);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        protected unsafe override void OnUpdate()
        {
            StressTestCharacterJob job = new StressTestCharacterJob
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

                StressTestCharacterType = GetComponentTypeHandle<StressTestCharacterComponent>(false),
                StressTestCharacterInputsType = GetComponentTypeHandle<StressTestCharacterInputs>(true),
            };

            if(Multithreaded)
            {
                Dependency = job.ScheduleParallel(CharacterQuery, Dependency);
            }
            else
            {
                Dependency = job.Schedule(CharacterQuery, Dependency);
            }

            Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(this, CharacterQuery, Dependency);
        }
    }
}
