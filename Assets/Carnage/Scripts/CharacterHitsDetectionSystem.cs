using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;

// Update in the fixed step group, and AFTER the character update
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(KinematicCharacterUpdateGroup))]
public partial class CharacterHitsDetectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Iterate on non-stateful hits
        Entities
            .ForEach((Entity entity, ref DynamicBuffer<KinematicCharacterHit> characterHitsBuffer) =>
            {
                for (int i = 0; i < characterHitsBuffer.Length; i++)
                {
                    KinematicCharacterHit hit = characterHitsBuffer[i];
                    if (!hit.IsGroundedOnHit)
                    {
                        //UnityEngine.Debug.Log("Detected an ungrounded hit");
                    }
                }
            }).Run();

        // Iterate on stateful hits
        Entities
            .ForEach((Entity entity, ref DynamicBuffer<StatefulKinematicCharacterHit> statefulCharacterHitsBuffer) =>
            {
                for (int i = 0; i < statefulCharacterHitsBuffer.Length; i++)
                {
                    StatefulKinematicCharacterHit hit = statefulCharacterHitsBuffer[i];
                }
            }).Run();
    }
}