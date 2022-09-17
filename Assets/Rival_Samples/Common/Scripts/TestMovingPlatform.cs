using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples
{
    [Serializable]
    public struct TestMovingPlatform : IComponentData
    {
        public float3 TranslationAxis;
        public float TranslationAmplitude;
        public float TranslationSpeed;
        public float3 RotationAxis;
        public float RotationSpeed;
        public float3 OscillationAxis;
        public float OscillationAmplitude;
        public float OscillationSpeed;

        [NonSerialized]
        public float3 OriginalPosition;
        [NonSerialized]
        public quaternion OriginalRotation;
    }
}