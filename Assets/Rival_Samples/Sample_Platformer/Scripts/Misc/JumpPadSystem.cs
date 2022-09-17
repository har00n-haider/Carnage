using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(TriggerEventConversionSystem))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    [UpdateBefore(typeof(KinematicCharacterUpdateGroup))]
    public partial class JumpPadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, in Rotation rotation, in JumpPad jumpPad, in DynamicBuffer<StatefulTriggerEvent> triggerEventsBuffer) =>
            {
                for (int i = 0; i < triggerEventsBuffer.Length; i++)
                {
                    StatefulTriggerEvent triggerEvent = triggerEventsBuffer[i];
                    Entity otherEntity = triggerEvent.GetOtherEntity(entity);

                    // If a character has entered the trigger, add jumppad power to it
                    if (triggerEvent.State == EventOverlapState.Enter && HasComponent<KinematicCharacterBody>(otherEntity))
                    {
                        KinematicCharacterBody characterBody = GetComponent<KinematicCharacterBody>(otherEntity);

                        float3 jumpVelocity = MathUtilities.GetForwardFromRotation(rotation.Value) * jumpPad.JumpPower;
                        characterBody.RelativeVelocity = jumpVelocity;

                        // Unground the character
                        if (characterBody.IsGrounded && math.dot(math.normalizesafe(jumpVelocity), characterBody.GroundHit.Normal) > jumpPad.UngroundingDotThreshold)
                        {
                            characterBody.Unground();
                        }

                        SetComponent(otherEntity, characterBody);
                    }
                }
            }).Run();
        }
    }
}