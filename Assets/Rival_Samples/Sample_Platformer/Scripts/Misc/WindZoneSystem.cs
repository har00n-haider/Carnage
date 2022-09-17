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
    [UpdateAfter(typeof(KinematicCharacterUpdateGroup))]
    public partial class WindZoneSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            Dependency = Entities
                .ForEach((Entity entity, in WindZone windZone, in DynamicBuffer<StatefulTriggerEvent> triggerEventsBuffer) =>
                {
                    for (int i = 0; i < triggerEventsBuffer.Length; i++)
                    {
                        StatefulTriggerEvent triggerEvent = triggerEventsBuffer[i];
                        Entity otherEntity = triggerEvent.GetOtherEntity(entity);

                        if (triggerEvent.State == EventOverlapState.Stay)
                        {
                            // Characters
                            if(HasComponent<KinematicCharacterBody>(otherEntity) && HasComponent<PlatformerCharacterStateMachine>(otherEntity))
                            {
                                PlatformerCharacterStateMachine platformerCharacterStateMachine = GetComponent<PlatformerCharacterStateMachine>(otherEntity);

                                if(PlatformerCharacterUtilities.CanBeAffectedByWindZone(platformerCharacterStateMachine.CurrentCharacterState))
                                {
                                    KinematicCharacterBody characterBody = GetComponent<KinematicCharacterBody>(otherEntity);
                                    characterBody.RelativeVelocity += windZone.WindForce * deltaTime;
                                    SetComponent(otherEntity, characterBody);
                                }
                            }
                            // Dynamic physics bodies
                            else if (HasComponent<PhysicsVelocity>(otherEntity) && HasComponent<PhysicsMass>(otherEntity))
                            {
                                PhysicsMass physicsMass = GetComponent<PhysicsMass>(otherEntity);
                                if (physicsMass.InverseMass > 0f)
                                {
                                    PhysicsVelocity physicsVelocity = GetComponent<PhysicsVelocity>(otherEntity);
                                    physicsVelocity.Linear += windZone.WindForce * deltaTime;
                                    SetComponent(otherEntity, physicsVelocity);
                                }
                            }
                        }
                    }
                }).Schedule(Dependency);
        }
    }
}
