using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GhostComponent(OwnerPredictedSendType = GhostSendType.All, PrefabType = GhostPrefabType.All, SendDataForChildEntity = true)]
    public struct Railgun : IComponentData
    {
        public float FireRate;
        public float Damage;
        public float Range;
        public int HitSparksCount;
        public float Recoil;
        public float RecoilFOVKick;

        [HideInInspector]
        public Entity LazerPrefab;
        [HideInInspector]
        public Entity HitSparkPrefab;

        [GhostField]
        [HideInInspector]
        public float _firingTimer;
        [HideInInspector]
        public Entity _muzzleEntity;
        [HideInInspector]
        public uint _lastTickShot;
    }
}