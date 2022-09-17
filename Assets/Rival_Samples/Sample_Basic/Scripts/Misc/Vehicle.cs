using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.Basic
{
    [Serializable]
    public struct Vehicle : IComponentData
    {
        public float MaxSpeed;
        public float Acceleration;
        public float MaxRotationSpeed;
        public float RotationAcceleration;

        public float WheelFriction;
        public float WheelRollResistance;
        public float RotationDamping;

        public float WheelSuspensionDistance;
        public float WheelSuspensionStrength;
    }

    public struct VehicleWheels : IBufferElementData
    {
        public Entity MeshEntity;
        public Entity CollisionEntity;
    }
}