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
using Unity.Physics.Stateful;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
    public partial class PlatformerCharacterSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;
        public EndFramePhysicsSystem EndFramePhysicsSystem;
        private EndFixedStepSimulationEntityCommandBufferSystem _endFixedStepSimulationEntityCommandBufferSystem;
        public EntityQuery CharacterQuery;

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
            _endFixedStepSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

            CharacterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = MiscUtilities.CombineArrays(
                    KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                    new ComponentType[]
                    {
                        typeof(PlatformerCharacterComponent),
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
            EntityCommandBuffer commandBuffer = _endFixedStepSimulationEntityCommandBufferSystem.CreateCommandBuffer();

            // Initialize characters
            BufferFromEntity<Child> childBufferFromEntity = GetBufferFromEntity<Child>(true);
            BufferFromEntity<LinkedEntityGroup> linkedEntityGroupFromEntity = GetBufferFromEntity<LinkedEntityGroup>(true);
            Dependency = Entities
                .WithReadOnly(linkedEntityGroupFromEntity)
                .WithNone<PlatformerCharacterInitialized>()
                .ForEach((Entity entity, ref PlatformerCharacterComponent platformerCharacter, ref PlatformerCharacterStateMachine platformerCharacterStateMachine) =>
                {
                    PlatformerCharacterUtilities.InitializeCharacter(
                        ref platformerCharacter,
                        ref platformerCharacterStateMachine,
                        ref commandBuffer,
                        ref childBufferFromEntity,
                        ref linkedEntityGroupFromEntity,
                        entity);
                }).Schedule(Dependency);

            commandBuffer = _endFixedStepSimulationEntityCommandBufferSystem.CreateCommandBuffer();
            Dependency = new PlatformerCharacterJob
            {
                CommandBuffer = commandBuffer.AsParallelWriter(),

                DeltaTime = Time.DeltaTime,
                ElapsedTime = (float)Time.ElapsedTime,
                CollisionWorld = BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,

                EntityType = GetEntityTypeHandle(),
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(false),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(false),
                PhysicsVelocityFromEntity = GetComponentDataFromEntity<PhysicsVelocity>(true),
                PhysicsMassFromEntity = GetComponentDataFromEntity<PhysicsMass>(true),
                StoredKinematicCharacterBodyPropertiesFromEntity = GetComponentDataFromEntity<StoredKinematicCharacterBodyProperties>(true),

                KinematicCharacterBodyType = GetComponentTypeHandle<KinematicCharacterBody>(false),
                PhysicsColliderType = GetComponentTypeHandle<PhysicsCollider>(false),
                CharacterHitsBufferType = GetBufferTypeHandle<KinematicCharacterHit>(false),
                VelocityProjectionHitsBufferType = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false),
                CharacterDeferredImpulsesBufferType = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false),
                StatefulCharacterHitsBufferType = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false),
                TrackedTransformFromEntity = GetComponentDataFromEntity<TrackedTransform>(true),

                PlatformerCharacterFromEntity = GetComponentDataFromEntity<PlatformerCharacterComponent>(false),
                CharacterInputsType = GetComponentTypeHandle<PlatformerCharacterInputs>(true),
                PlatformerCharacterStateMachineType = GetComponentTypeHandle<PlatformerCharacterStateMachine>(false),
                NonUniformScaleFromEntity = GetComponentDataFromEntity<NonUniformScale>(false),
                CustomGravityType = GetComponentTypeHandle<CustomGravity>(true),
                CharacterFrictionModifierFromEntity = GetComponentDataFromEntity<CharacterFrictionModifier>(true),
                TriggerEventsBufferType = GetBufferTypeHandle<StatefulTriggerEvent>(true),
                LinkedEntityGroupFromEntity = GetBufferFromEntity<LinkedEntityGroup>(true),
            }.ScheduleParallel(CharacterQuery, Dependency);

            Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(this, CharacterQuery, Dependency);

            _endFixedStepSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    [BurstCompile]
    public struct PlatformerCharacterJob : IJobEntityBatchWithIndex
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public float DeltaTime;
        public float ElapsedTime;
        [ReadOnly]
        public CollisionWorld CollisionWorld;

        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Rotation> RotationFromEntity;
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
        public ComponentTypeHandle<KinematicCharacterBody> KinematicCharacterBodyType;
        [NativeDisableParallelForRestriction]
        public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;
        public BufferTypeHandle<KinematicCharacterHit> CharacterHitsBufferType;
        public BufferTypeHandle<KinematicVelocityProjectionHit> VelocityProjectionHitsBufferType;
        public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;
        public BufferTypeHandle<StatefulKinematicCharacterHit> StatefulCharacterHitsBufferType;

        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<PlatformerCharacterComponent> PlatformerCharacterFromEntity;
        [ReadOnly]
        public ComponentTypeHandle<PlatformerCharacterInputs> CharacterInputsType;
        public ComponentTypeHandle<PlatformerCharacterStateMachine> PlatformerCharacterStateMachineType;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<NonUniformScale> NonUniformScaleFromEntity;
        [ReadOnly]
        public ComponentTypeHandle<CustomGravity> CustomGravityType;
        [ReadOnly]
        public BufferTypeHandle<StatefulTriggerEvent> TriggerEventsBufferType;
        [ReadOnly]
        public BufferFromEntity<LinkedEntityGroup> LinkedEntityGroupFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<CharacterFrictionModifier> CharacterFrictionModifierFromEntity;

        [NativeDisableContainerSafetyRestriction]
        public NativeList<int> TmpRigidbodyIndexesProcessed;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<Unity.Physics.RaycastHit> TmpRaycastHits;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<ColliderCastHit> TmpColliderCastHits;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<DistanceHit> TmpDistanceHits;

        public void Execute(ArchetypeChunk chunk, int batchIndex, int firstEntityIndex)
        {
            NativeArray<Entity> chunkEntities = chunk.GetNativeArray(EntityType);
            NativeArray<KinematicCharacterBody> chunkCharacterBodies = chunk.GetNativeArray(KinematicCharacterBodyType);
            NativeArray<PhysicsCollider> chunkPhysicsColliders = chunk.GetNativeArray(PhysicsColliderType);
            BufferAccessor<KinematicCharacterHit> chunkCharacterHitBuffers = chunk.GetBufferAccessor(CharacterHitsBufferType);
            BufferAccessor<KinematicVelocityProjectionHit> chunkVelocityProjectionHitBuffers = chunk.GetBufferAccessor(VelocityProjectionHitsBufferType);
            BufferAccessor<KinematicCharacterDeferredImpulse> chunkCharacterDeferredImpulsesBuffers = chunk.GetBufferAccessor(CharacterDeferredImpulsesBufferType);
            BufferAccessor<StatefulKinematicCharacterHit> chunkStatefulCharacterHitsBuffers = chunk.GetBufferAccessor(StatefulCharacterHitsBufferType);
            NativeArray<PlatformerCharacterInputs> chunkCharacterInputs = chunk.GetNativeArray(CharacterInputsType);
            NativeArray<PlatformerCharacterStateMachine> chunkPlatformerCharacterStateMachines = chunk.GetNativeArray(PlatformerCharacterStateMachineType);
            NativeArray<CustomGravity> chunkCustomGravities = chunk.GetNativeArray(CustomGravityType);
            BufferAccessor<StatefulTriggerEvent> chunkTriggerEventsBuffers = chunk.GetBufferAccessor(TriggerEventsBufferType);

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
            PlatformerCharacterProcessor processor = default;
            processor.DeltaTime = DeltaTime;
            processor.ElapsedTime = ElapsedTime;
            processor.CommandBuffer = CommandBuffer;
            processor.CollisionWorld = CollisionWorld;
            processor.StoredKinematicCharacterBodyPropertiesFromEntity = StoredKinematicCharacterBodyPropertiesFromEntity;
            processor.PhysicsMassFromEntity = PhysicsMassFromEntity;
            processor.PhysicsVelocityFromEntity = PhysicsVelocityFromEntity;
            processor.TrackedTransformFromEntity = TrackedTransformFromEntity;
            processor.NonUniformScaleFromEntity = NonUniformScaleFromEntity;
            processor.CharacterFrictionModifierFromEntity = CharacterFrictionModifierFromEntity;
            processor.LinkedEntityGroupFromEntity = LinkedEntityGroupFromEntity;
            processor.TranslationFromEntity = TranslationFromEntity;
            processor.RotationFromEntity = RotationFromEntity;
            processor.TmpRigidbodyIndexesProcessed = TmpRigidbodyIndexesProcessed;
            processor.TmpRaycastHits = TmpRaycastHits;
            processor.TmpColliderCastHits = TmpColliderCastHits;
            processor.TmpDistanceHits = TmpDistanceHits;

            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];

                // Assign the per-character data of the processor
                processor.Entity = entity;
                processor.IndexInChunk = i;
                processor.Translation = TranslationFromEntity[entity].Value;
                processor.Rotation = RotationFromEntity[entity].Value;
                processor.PhysicsCollider = chunkPhysicsColliders[i];
                processor.CharacterBody = chunkCharacterBodies[i];
                processor.CharacterHitsBuffer = chunkCharacterHitBuffers[i];
                processor.CharacterDeferredImpulsesBuffer = chunkCharacterDeferredImpulsesBuffers[i];
                processor.VelocityProjectionHitsBuffer = chunkVelocityProjectionHitBuffers[i];
                processor.StatefulCharacterHitsBuffer = chunkStatefulCharacterHitsBuffers[i];
                processor.PlatformerCharacter = PlatformerCharacterFromEntity[entity];
                processor.CharacterInputs = chunkCharacterInputs[i];
                processor.PlatformerCharacterStateMachine = chunkPlatformerCharacterStateMachines[i];
                processor.CustomGravity = chunkCustomGravities[i];
                processor.TriggerEventsBuffer = chunkTriggerEventsBuffers[i];

                processor.OnUpdate();

                TranslationFromEntity[entity] = new Translation { Value = processor.Translation };
                RotationFromEntity[entity] = new Rotation { Value = processor.Rotation };
                chunkCharacterBodies[i] = processor.CharacterBody;
                PlatformerCharacterFromEntity[entity] = processor.PlatformerCharacter;
                chunkPlatformerCharacterStateMachines[i] = processor.PlatformerCharacterStateMachine;
                chunkPhysicsColliders[i] = processor.PhysicsCollider;
            }
        }
    }
}