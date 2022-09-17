using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    public class HealthAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Health Health;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Health.CurrentHealth = Health.MaxHealth;

            dstManager.AddComponentData(entity, Health);
        }
    }
}
