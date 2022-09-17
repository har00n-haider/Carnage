using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GhostComponent(OwnerPredictedSendType = GhostSendType.All, PrefabType = GhostPrefabType.All, SendDataForChildEntity = false)]
    public struct Health : IComponentData
    {
        public float MaxHealth;

        [GhostField]
        public float CurrentHealth;

        public void ClampToMin()
        {
            CurrentHealth = math.max(0f, CurrentHealth);
        }

        public void ClampToMinMax()
        {
            CurrentHealth = math.clamp(CurrentHealth, 0f, MaxHealth);
        }
    }
}