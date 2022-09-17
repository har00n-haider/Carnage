using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class CharacterRopeSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = _endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

            Dependency = Entities.ForEach((Entity entity, ref CharacterRope characterRope, ref NonUniformScale nonUniformScale) =>
            {
                if(characterRope.OwningCharacterEntity == Entity.Null)
                {
                    return;
                }

                if (HasComponent<PlatformerCharacterComponent>(characterRope.OwningCharacterEntity) && HasComponent<PlatformerCharacterStateMachine>(characterRope.OwningCharacterEntity))
                {
                    PlatformerCharacterComponent platformerCharacter = GetComponent<PlatformerCharacterComponent>(characterRope.OwningCharacterEntity);
                    PlatformerCharacterStateMachine characterStateMachine = GetComponent<PlatformerCharacterStateMachine>(characterRope.OwningCharacterEntity);
                    LocalToWorld characterLocalToWorld = GetComponent<LocalToWorld>(characterRope.OwningCharacterEntity);

                    // Handle rope positioning
                    {
                        RigidTransform characterTransform = new RigidTransform(characterLocalToWorld.Rotation, characterLocalToWorld.Position);
                        float3 anchorPointOnCharacter = math.transform(characterTransform, platformerCharacter.LocalRopeAnchorPoint);
                        float3 ropeVector = characterStateMachine.RopeSwingState.AnchorPoint - anchorPointOnCharacter;
                        float ropeLength = math.length(ropeVector);
                        float3 ropeMidPoint = anchorPointOnCharacter + (ropeVector * 0.5f);

                        SetComponent(entity, new LocalToWorld { Value = math.mul(new float4x4(MathUtilities.CreateRotationWithUpPriority(math.normalizesafe(ropeVector), math.forward()), ropeMidPoint), float4x4.Scale(new float3(0.04f, ropeLength * 0.5f, 0.04f))) });
                    }

                    // Destroy self when not in rope swing state anymore
                    if (characterStateMachine.CurrentCharacterState != CharacterState.RopeSwing)
                    {
                        commandBuffer.DestroyEntity(entity);
                    }
                }
            }).Schedule(Dependency);

            _endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}