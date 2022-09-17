using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples
{
    [GenerateAuthoringComponent]
    public struct BouncySurface : IComponentData
    {
        public float BounceEnergyMultiplier;
    }
}
