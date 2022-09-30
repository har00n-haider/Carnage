using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


[Serializable]
public struct ScaleFade : IComponentData
{
    public float Duration;
    public bool3 Axis;

    [HideInInspector]
    public float StartTime;
    [HideInInspector]
    public float3 StartScale;
    [HideInInspector]
    public bool Initialized;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class ScaleFadeSystem : SystemBase
{
    public EndSimulationEntityCommandBufferSystem EndSimulationCommandBufferSystem;

    public JobHandle OutputDependency;

    protected override void OnCreate()
    {
        base.OnCreate();

        EndSimulationCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float elapsedTime = (float)Time.ElapsedTime;
        EntityCommandBuffer commandBuffer = EndSimulationCommandBufferSystem.CreateCommandBuffer();

        Dependency = Entities
            .ForEach((Entity entity, ref NonUniformScale nonUniformScale, ref ScaleFade scaleFade, in Rotation rotation) =>
        {
            if (!scaleFade.Initialized)
            {
                scaleFade.Initialized = true;
                scaleFade.StartTime = elapsedTime;
                scaleFade.StartScale = nonUniformScale.Value;
            }

            float normDuration = (elapsedTime - scaleFade.StartTime) / scaleFade.Duration;
            if (normDuration > 1f)
            {
                commandBuffer.DestroyEntity(entity);
            }
            else
            {
                float normInvClampedDuration = 1f - math.clamp(normDuration, 0f, 1f);
                float3 newScale = scaleFade.StartScale * normInvClampedDuration;
                if (scaleFade.Axis.x)
                {
                    nonUniformScale.Value.x = newScale.x;
                }
                if (scaleFade.Axis.y)
                {
                    nonUniformScale.Value.y = newScale.y;
                }
                if (scaleFade.Axis.z)
                {
                    nonUniformScale.Value.z = newScale.z;
                }
            }
        }).Schedule(Dependency);

        OutputDependency = Dependency;

        EndSimulationCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
