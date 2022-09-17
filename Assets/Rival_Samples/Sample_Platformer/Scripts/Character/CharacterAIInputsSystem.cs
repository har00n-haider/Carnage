using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class AICharacterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float time = (float)Time.ElapsedTime;

            Dependency = Entities.ForEach((ref PlatformerAICharacter aiCharacter, ref PlatformerCharacterInputs characterInputs) =>
            {
                characterInputs.WorldMoveVector = math.sin(time * aiCharacter.MovementPeriod) * aiCharacter.MovementDirection;
            }).ScheduleParallel(Dependency);
        }
    }
}