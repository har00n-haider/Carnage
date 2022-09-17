using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Platformer
{
    [Serializable]
    public struct CharacterRope : IComponentData
    {
        public Entity OwningCharacterEntity;
    }
}