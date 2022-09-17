using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [Serializable]
    public class PlatformerCharacterHybridData : IComponentData
    {
        public GameObject MeshPrefab;
    }

    [Serializable]
    public class PlatformerCharacterHybridLink : ISystemStateComponentData
    {
        public GameObject Object;
        public Animator Animator;
    }
}