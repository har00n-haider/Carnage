using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class BasicCharacterAISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float time = (float)Time.ElapsedTime;

            Dependency = Entities.ForEach((ref BasicAICharacter aiCharacter, ref BasicCharacterInputs characterInputs) =>
            {
                characterInputs.WorldMoveVector = math.sin(time * aiCharacter.MovementPeriod) * aiCharacter.MovementDirection;
                characterInputs.TargetLookDirection = math.normalizesafe(characterInputs.WorldMoveVector);
            }).ScheduleParallel(Dependency);
        }
    }
}