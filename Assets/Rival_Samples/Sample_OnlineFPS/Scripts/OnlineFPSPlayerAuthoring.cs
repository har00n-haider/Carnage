using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GhostComponent(OwnerPredictedSendType = GhostSendType.All, PrefabType = GhostPrefabType.All, SendDataForChildEntity = false)]
    public struct OnlineFPSPlayer : IComponentData
    {
        public Entity AssociatedConnectionEntity;
        public float LookRotationSpeed;

        [GhostField()]
        public Entity ControlledEntity;
        [GhostField]
        public FixedString128Bytes PlayerName;

        public Entity PreviousControlledEntity;
    }

    [DisallowMultipleComponent]
    public class OnlineFPSPlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float LookRotationSpeed;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new OnlineFPSPlayer { LookRotationSpeed = LookRotationSpeed });
            dstManager.AddBuffer<OnlineFPSPlayerCommands>(entity);
        }
    }
}