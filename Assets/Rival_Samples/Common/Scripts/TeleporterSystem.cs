using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Rival;
using Unity.Physics.Stateful;

namespace Rival.Samples
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(TriggerEventConversionSystem))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public partial class TeleporterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency = Entities
                .ForEach((Entity entity, in Teleporter teleporter, in DynamicBuffer<StatefulTriggerEvent> triggerEventsBuffer) =>
                {
                    // Only teleport if there is a destination
                    if (teleporter.DestinationEntity != Entity.Null)
                    {
                        for (int i = 0; i < triggerEventsBuffer.Length; i++)
                        {
                            StatefulTriggerEvent triggerEvent = triggerEventsBuffer[i];
                            Entity otherEntity = triggerEvent.GetOtherEntity(entity);

                            // If a character has entered the trigger, move its translation to the destination
                            if (triggerEvent.State == EventOverlapState.Enter && HasComponent<KinematicCharacterBody>(otherEntity))
                            {
                                Translation t = GetComponent<Translation>(otherEntity);
                                t = GetComponent<Translation>(teleporter.DestinationEntity);
                                SetComponent(otherEntity, t);

                                // Bypass interpolation
                                if(HasComponent<CharacterInterpolation>(otherEntity))
                                {
                                    CharacterInterpolation interpolation = GetComponent<CharacterInterpolation>(otherEntity);
                                    interpolation.SkipNextInterpolation();
                                    SetComponent(otherEntity, interpolation);
                                }
                            }
                        }
                    }
                }).Schedule(Dependency);
        }
    }
}