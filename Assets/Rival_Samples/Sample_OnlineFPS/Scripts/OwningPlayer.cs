using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct OwningPlayer : IComponentData
    {
        public Entity PlayerEntity;
    }
}