using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

namespace Rival.Samples.OnlineFPS
{
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateBefore(typeof(GhostPredictionHistorySystem))]
    [UpdateAfter(typeof(PredictedPhysicsSystemGroup))]
    public partial class TransformsPredictionSystem : SystemBase
    {
        public TransformSystemGroup TransformSystemGroup;
        public CharacterInterpolationVariableUpdateSystem CharacterInterpolationVariableUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            TransformSystemGroup = World.GetOrCreateSystem<TransformSystemGroup>();
            CharacterInterpolationVariableUpdateSystem = World.GetOrCreateSystem<CharacterInterpolationVariableUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            CharacterInterpolationVariableUpdateSystem.Enabled = false;
            TransformSystemGroup.Update();
            CharacterInterpolationVariableUpdateSystem.Enabled = true;
        }
    }
}