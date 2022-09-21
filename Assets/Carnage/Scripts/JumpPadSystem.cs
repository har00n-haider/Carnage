using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

// Update after physics but before characters
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
public partial class JumpPadSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Iterate on all jump pads with trigger event buffers
        Entities
            .WithoutBurst()
            .ForEach((Entity entity, in Rotation rotation, in JumpPad jumpPad, in DynamicBuffer<StatefulTriggerEventH> triggerEventsBuffer) =>
            {
                // Go through each trigger event of the jump pad...
                for (int i = 0; i < triggerEventsBuffer.Length; i++)
                {
                    StatefulTriggerEventH triggerEvent = triggerEventsBuffer[i];
                    Entity otherEntity = triggerEvent.GetOtherEntity(entity);

                    // If a character has entered the trigger...
                    if (triggerEvent.State == StatefulEventState.Enter && HasComponent<KinematicCharacterBody>(otherEntity))
                    {
                        KinematicCharacterBody characterBody = GetComponent<KinematicCharacterBody>(otherEntity);

                        // Cancel out character velocity in the jump force's direction
                        // (this helps make the character jump up even if it is falling down on the jump pad at high speed)
                        characterBody.RelativeVelocity = MathUtilities.ProjectOnPlane(characterBody.RelativeVelocity, math.normalizesafe(jumpPad.JumpForce));

                        // Add the jump pad force to the character
                        characterBody.RelativeVelocity += jumpPad.JumpForce;

                        // Unground the character
                        // (without this, the character would snap right back to the ground on the next frame)
                        characterBody.Unground();

                        // Don't forget to write back to the component
                        SetComponent(otherEntity, characterBody);
                    }
                }
            }).Run();
    }
}