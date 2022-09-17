using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct OnlineFPSData : IComponentData
    {
        public Entity DeathSparkPrefab;
    }

    public struct CharacterSpawnRequest : IComponentData
    {
        public float Timer;
        public int ForNetworkConnectionId;
        public Entity ForPlayerEntity;

        public bool IsInitialized;
    }
    
    [Serializable]
    public struct LocalGameData : IComponentData
    {
        public FixedString128Bytes PlayerName;
    }

    public struct IsInitialized : ISystemStateComponentData
    {
    }

    public struct ConnectionOwnedEntity : IBufferElementData
    {
        public Entity Entity;
    }
}
