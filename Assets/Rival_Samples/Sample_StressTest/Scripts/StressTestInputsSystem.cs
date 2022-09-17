using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples.StressTest
{
    public partial class StressTestInputsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float3 worldMoveVector = math.mul(quaternion.Euler(math.up() * (float)Time.ElapsedTime), math.forward());

            Entities
                .WithAll<StressTestCharacterComponent>()
                .ForEach((ref StressTestCharacterInputs inputs) =>
            {
                inputs.WorldMoveVector = worldMoveVector;
            }).Schedule();
        }
    }
}