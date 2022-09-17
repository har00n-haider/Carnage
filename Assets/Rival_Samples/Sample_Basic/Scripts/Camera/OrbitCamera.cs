using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [Serializable]
    public struct OrbitCamera : IComponentData
    {
        [HideInInspector]
        public Entity FollowedEntity;
        [HideInInspector]
        public Entity FollowedCharacter;

        [Header("Rotation")]
        public float RotationSpeed;
        public float MaxVAngle;
        public float MinVAngle;
        public bool RotateWithCharacterParent;

        [Header("Distance")]
        public float TargetDistance;
        public float MinDistance;
        public float MaxDistance;
        public float DistanceMovementSpeed;
        public float DistanceMovementSharpness;

        [Header("Obstructions")]
        public float ObstructionRadius;
        public float ObstructionInnerSmoothingSharpness;
        public float ObstructionOuterSmoothingSharpness;
        public bool PreventFixedUpdateJitter;

        // Data in calculations
        [HideInInspector]
        public float CurrentDistanceFromMovement;
        [HideInInspector]
        public float CurrentDistanceFromObstruction;
        [HideInInspector]
        public float PitchAngle;
        [HideInInspector]
        public Entity PreviousParentEntity;
        [HideInInspector]
        public quaternion PreviousParentRotation;
        [HideInInspector]
        public float3 PlanarForward;

        public static OrbitCamera GetDefault()
        {
            OrbitCamera c = new OrbitCamera
            {
                FollowedEntity = default,

                RotationSpeed = 1f,
                MaxVAngle = 90f,
                MinVAngle = -90f,
                RotateWithCharacterParent = false,

                TargetDistance = 5f,
                MinDistance = 0f,
                MaxDistance = 10f,
                DistanceMovementSpeed = 10f,
                DistanceMovementSharpness = 20f,

                ObstructionRadius = 0f,
                ObstructionInnerSmoothingSharpness = float.MaxValue,
                ObstructionOuterSmoothingSharpness = 5f,
                PreventFixedUpdateJitter = true,

                CurrentDistanceFromObstruction = 0f,
            };
            return c;
        }
    }
}