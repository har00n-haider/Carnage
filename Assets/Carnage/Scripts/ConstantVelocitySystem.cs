using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


[Serializable]
[GenerateAuthoringComponent]
public struct ConstantVelocity : IComponentData
{
    public float3 LinearLocal;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class ConstantVelocitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref Translation translation, in Rotation rotation, in ConstantVelocity constantVelocity) =>
        {
            translation.Value += (math.mul(rotation.Value, constantVelocity.LinearLocal) * deltaTime);
        }).Schedule();
    }
}
