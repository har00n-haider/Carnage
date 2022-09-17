using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics.Systems;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct Weapon : IComponentData
    {
        public Entity OwnerEntity;
        public Entity ShootOriginOverride;
        public bool ShootRequested;
    }

    [Serializable]
    [GhostComponent(OwnerPredictedSendType = GhostSendType.All, PrefabType = GhostPrefabType.All, SendDataForChildEntity = false)]
    public struct ActiveWeapon : IComponentData
    {
        [GhostField()]
        public Entity WeaponEntity;
        public Entity PreviousWeaponEntity;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(KinematicCharacterUpdateGroup))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class WeaponUpdateGroup : ComponentSystemGroup
    { }
}