using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Basic
{
    [Serializable]
    public struct BasicCharacterInputs : IComponentData
    {
        public float3 WorldMoveVector;
        public float3 TargetLookDirection;
        public bool JumpRequested;
    }
}