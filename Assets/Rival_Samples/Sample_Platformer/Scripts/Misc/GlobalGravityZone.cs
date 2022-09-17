using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct GlobalGravityZone : IComponentData
    {
        public float3 Gravity;
    }
}
