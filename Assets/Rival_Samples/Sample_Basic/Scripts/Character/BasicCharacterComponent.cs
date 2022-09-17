using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Rival.Samples.Basic
{
    public enum CharacterRotationMode
    {
        TowardsCameraForward,
        TowardsMoveVector,
    }

    [Serializable]
    public struct BasicCharacterComponent : IComponentData
    {
        [Header("Ground movement")]
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;

        [Header("Air movement")]
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float3 Gravity;
        public bool PreventAirClimbingSlopes;

        [Header("Rotation")]
        public float RotationSharpness;
        public CharacterRotationMode RotationMode;

        [Header("Jumping")]
        public float JumpSpeed;
        public byte MaxJumpsInAir;

        [Header("Step Handling")]
        public bool StepHandling;
        public float MaxStepHeight;
        public float ExtraStepChecksDistance;

        [Header("Slope Changes")]
        public bool PreventGroundingWhenMovingTowardsNoGrounding;
        public bool HasMaxDownwardSlopeChangeAngle;
        [Range(0f, 180f)]
        public float MaxDownwardSlopeChangeAngle;

        [Header("Misc")]
        public bool ConstrainVelocityToGroundPlane;
        public bool PushGroundBodies;
        public bool HandleBouncySurfaces;
        public CustomPhysicsBodyTags IgnoredPhysicsBodyTags;
        public CustomPhysicsBodyTags UngroundablePhysicsBodyTags;
        public CustomPhysicsBodyTags ZeroMassAgainstCharacterPhysicsBodyTags;
        public CustomPhysicsBodyTags InfiniteMassAgainstCharacterPhysicsBodyTags;
        public CustomPhysicsBodyTags IgnoreStepHandlingTags;
        public CustomPhysicsBodyTags IgnoreSlopeChangesTags;

        [HideInInspector]
        public byte CurrentJumpsInAir;
        [HideInInspector]
        public Entity MeshRootEntity;

        public static BasicCharacterComponent GetDefault()
        {
            BasicCharacterComponent c = new BasicCharacterComponent
            {
                GroundMaxSpeed = 10f,
                GroundedMovementSharpness = 15f,

                AirAcceleration = 50f,
                AirMaxSpeed = 10f,
                AirDrag = 0f,
                Gravity = new float3(0f, -10f, 0f),
                PreventAirClimbingSlopes = true,

                RotationSharpness = 20f,
                RotationMode = CharacterRotationMode.TowardsMoveVector,

                JumpSpeed = 10f,
                MaxJumpsInAir = 0,

                StepHandling = true,
                MaxStepHeight = 0.5f,
                ExtraStepChecksDistance = 0.1f,

                PreventGroundingWhenMovingTowardsNoGrounding = true,
                HasMaxDownwardSlopeChangeAngle = false,
                MaxDownwardSlopeChangeAngle = 90f,

                ConstrainVelocityToGroundPlane = true,
                PushGroundBodies = true,
                HandleBouncySurfaces = true,
            };
            return c;
        }
    }
}
