using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples
{
    [Serializable]
    public struct Teleporter : IComponentData
    {
        public Entity DestinationEntity;
    }
}