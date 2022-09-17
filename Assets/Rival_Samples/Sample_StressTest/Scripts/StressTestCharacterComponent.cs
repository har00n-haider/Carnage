using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.StressTest
{
    [Serializable]
    public struct StressTestCharacterComponent : IComponentData
    {
        public float RotationSharpness;
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float JumpSpeed;
        public float3 Gravity;

        [Header("Step Handling")]
        [HideInInspector]
        public bool StepHandling;
        public float MaxStepHeight;
        public float ExtraStepChecksDistance;

        [Header("Slope Changes")]
        [HideInInspector]
        public bool PreventGroundingWhenMovingTowardsNoGrounding;
        [HideInInspector]
        public bool HasMaxDownwardSlopeChangeAngle;
        [Range(0f, 180f)]
        public float MaxDownwardSlopeChangeAngle;

        [Header("Misc")]
        public bool ConstrainVelocityToGroundPlane;

        [HideInInspector]
        public bool ProcessStatefulCharacterHits;
    }

    [Serializable]
    public struct StressTestCharacterInputs : IComponentData
    {
        public float3 WorldMoveVector;
        public bool JumpRequested;
    }
}
