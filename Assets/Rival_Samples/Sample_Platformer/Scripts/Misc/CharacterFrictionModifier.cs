using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct CharacterFrictionModifier : IComponentData
    {
        public float Friction;
    }
}
