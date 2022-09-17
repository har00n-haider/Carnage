using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct SelfDestructAfterTime : IComponentData
    {
        public float LifeTime;

        [HideInInspector]
        public float _timeSinceAlive;
    }
}