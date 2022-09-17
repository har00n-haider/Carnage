using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    public struct OrbitCameraState : IComponentData
    {

    }

    [Serializable]
    public struct OrbitCamera : IComponentData
    {
        [HideInInspector]
        public Entity FollowedEntity;
        [HideInInspector]
        public Entity PreviousFollowedEntity;
        [HideInInspector]
        public Entity CharacterEntity;

        [Header("Position")]
        public float FollowTargetTransitionDelay;

        [Header("Rotation")]
        public float RotationSpeed;
        public float MaxVAngle;
        public float MinVAngle;
        public bool RotateWithCharacterParent;
        public float CameraUpTransitionsTime;

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
        public float FollowTargetTransitionTime;
        [HideInInspector]
        public float FollowTargetTransitionTotalTime;
        [HideInInspector]
        public float3 TargetUpTransitionFromUp;
        [HideInInspector]
        public float TargetUpTransitionTime;
        [HideInInspector]
        public float TargetUpTransitionTotalTime;
        [HideInInspector]
        public float CurrentDistanceFromMovement;
        [HideInInspector]
        public float CurrentDistanceFromObstruction;
        [HideInInspector]
        public float PitchAngle;
        [HideInInspector]
        public float3 PlanarForward;
        [HideInInspector]
        public Entity PreviousParentEntity;
        [HideInInspector]
        public quaternion PreviousParentRotation;
        [HideInInspector]
        public float3 FollowedTranslation;
        [HideInInspector]
        public float3 PreviousTargetUp;

        public static OrbitCamera GetDefault()
        {
            OrbitCamera c = new OrbitCamera
            {
                FollowedEntity = default,

                RotationSpeed = 1f,
                MaxVAngle = 90f,
                MinVAngle = -90f,
                RotateWithCharacterParent = false,
                CameraUpTransitionsTime = 1f,

                TargetDistance = 5f,
                MinDistance = 0f,
                MaxDistance = 10f,
                DistanceMovementSpeed = 10f,
                DistanceMovementSharpness = 20f,

                ObstructionRadius = 0f,
                ObstructionInnerSmoothingSharpness = float.MaxValue,
                ObstructionOuterSmoothingSharpness = 5f,

                CurrentDistanceFromObstruction = 0f,
            };
            return c;
        }

        public void TransitionToFollowedEntity(Entity newTarget, float overTime)
        {
            PreviousFollowedEntity = FollowedEntity;
            FollowedEntity = newTarget;

            if (FollowTargetTransitionTime < FollowTargetTransitionTotalTime)
            {
                FollowTargetTransitionTime = FollowTargetTransitionTotalTime - FollowTargetTransitionTime;
            }
            else
            {
                FollowTargetTransitionTime = 0f;
            }

            FollowTargetTransitionTotalTime = overTime;
        }

        public void BeginSmoothUpTransition(float3 fromUp, float overTime)
        {
            TargetUpTransitionFromUp = fromUp;

            TargetUpTransitionTotalTime = overTime;
            TargetUpTransitionTime = 0f;
        }
    }
}