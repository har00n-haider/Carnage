using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial class FixedStepTimeSystem : SystemBase
    {
        public uint Tick;
        public double LastFixedStepTime;

        protected override void OnUpdate()
        {
            Tick++;
            LastFixedStepTime = Time.ElapsedTime;
        }
    }
}
