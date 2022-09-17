using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct CustomGravity : IComponentData
    {
        public float GravityMultiplier;

        [HideInInspector]
        public float3 Gravity;
        [HideInInspector]
        public bool TouchedByNonGlobalGravity;
        [HideInInspector]
        public Entity CurrentZoneEntity;
        [HideInInspector]
        public Entity LastZoneEntity;
    }
}
