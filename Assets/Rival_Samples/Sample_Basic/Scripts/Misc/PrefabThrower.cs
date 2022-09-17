using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct PrefabThrower : IComponentData
    {
        public Entity PrefabEntity;
        public float3 InitialEulerAngles;
        public float ThrowForce;
    }
}