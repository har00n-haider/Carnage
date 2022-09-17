using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct WindZone : IComponentData
    {
        public float3 WindForce;
    }
}