using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [System.Serializable]
    [GenerateAuthoringComponent]
    public struct BasicSceneInitialization : IComponentData
    {
        public float FixedRate;
        public Entity CharacterSpawnPointEntity;
        public Entity GameCameraPrefabEntity;
        public Entity KinematicCharacterPrefabEntity;

        [HideInInspector]
        public Entity ActiveCameraEntity;
        [HideInInspector]
        public Entity ActiveCharacterEntity;
    }

    [System.Serializable]
    public struct CharacterSpawnRequest : IComponentData
    {
        public Entity CharacterPrefabEntity;
    }

    [System.Serializable]
    public struct Initialized : IComponentData
    {
    }
}

