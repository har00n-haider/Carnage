using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct GhostPrefabsReference : IBufferElementData
    {
        public Entity Value;
    }
}