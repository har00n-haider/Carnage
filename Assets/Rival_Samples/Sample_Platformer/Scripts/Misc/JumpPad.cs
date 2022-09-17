using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Platformer
{
    [GenerateAuthoringComponent]
    [Serializable]
    public struct JumpPad : IComponentData
    {
        public float JumpPower;
        public float UngroundingDotThreshold;
    }
}