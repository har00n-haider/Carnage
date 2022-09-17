using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Authoring;
using System.Collections.Generic;

namespace Rival.Samples.Basic
{
    [DisallowMultipleComponent]
    public class VehicleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Vehicle Vehicle;
        public List<GameObject> Wheels;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, Vehicle);

            dstManager.AddBuffer<VehicleWheels>(entity);
            DynamicBuffer<VehicleWheels> wheelsBuffer = dstManager.GetBuffer<VehicleWheels>(entity);
            foreach (var wheelGO in Wheels)
            {
                wheelsBuffer.Add(new VehicleWheels { 
                    MeshEntity = conversionSystem.GetPrimaryEntity(wheelGO.GetComponentInChildren<MeshRenderer>().gameObject),
                    CollisionEntity = conversionSystem.GetPrimaryEntity(wheelGO.GetComponentInChildren<PhysicsShapeAuthoring>().gameObject),
                });
            }
        }
    }
}