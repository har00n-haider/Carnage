using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Rival;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace Rival.Samples.Platformer
{
    public enum CharacterState
    {
        DisabledNoCollisions,
        DisabledWithCollisions,
        GroundMove,
        Crouched,
        AirMove,
        WallRun,
        Rolling,
        Sliding,
        LedgeGrab,
        LedgeStandingUp,
        Dashing,
        Swimming,
        Climbing,
        FlyingNoCollisions,
        RopeSwing,
    }

    public interface IPlatformerCharacterState
    {
        void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor processor);
        void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor processor);
        void OnStateUpdate(ref PlatformerCharacterProcessor processor);
    }

    [Serializable]
    public struct PlatformerCharacterComponent : IComponentData
    {
        [Header("References")]
        public Entity DefaultCameraTargetEntity;
        public Entity ClimbingCameraTargetEntity;
        public Entity SwimmingCameraTargetEntity;
        public Entity CrouchingCameraTargetEntity;
        public Entity MeshRootEntity;
        public Entity RopePrefabEntity;
        public Entity RollballMeshEntity;
        public Entity LedgeDetectionPointEntity;
        public Entity SwimmingDetectionPointEntity;

        [Header("Ground movement")]
        public float GroundRunMaxSpeed;
        public float GroundSprintMaxSpeed;
        public float GroundedMovementSharpness;
        public float GroundedRotationSharpness;

        [Header("Crouching")]
        public float CrouchedMaxSpeed;
        public float CrouchedMovementSharpness;
        public float CrouchedRotationSharpness;

        [Header("Sliding")]
        public float SlidingDrag;
        public float SlidingSnapDistance;
        public float SlidingTransitionSharpness;
        public float SlidingMaxDotRatio;

        [Header("Air movement")]
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float AirRotationSharpness;

        [Header("Rolling")]
        public float RollingAcceleration;

        [Header("Wall run")]
        public float WallRunAcceleration;
        public float WallRunMaxSpeed;
        public float WallRunDrag;
        public float WallRunGravityFactor;
        public float WallRunJumpRatioFromCharacterUp;
        public float WallRunDetectionDistance;

        [Header("Flying")]
        public float FlyingMaxSpeed;
        public float FlyingMovementSharpness;
        public float FlyingRotationSharpness;

        [Header("Jumping")]
        public float GroundJumpSpeed;
        public float AirJumpSpeed;
        public float WallRunJumpSpeed;
        public float JumpHeldAcceleration;
        public float MaxHeldJumpTime;
        public byte MaxUngroundedJumps;
        public float JumpAfterUngroundedGraceTime;
        public float JumpBeforeGroundedGraceTime;

        [Header("Ledge Detection")]
        public float LedgeMoveSpeed;
        public float LedgeRotationSharpness;
        public float LedgeSurfaceProbingHeight;
        public float LedgeSurfaceObstructionProbingHeight;
        public float LedgeSideProbingLength;

        [Header("Slam")]
        public float SlamFirstPhaseDuration;
        public float SlamSecondPhaseSpeed;
        public float SlamThirdPhaseDuration;

        [Header("Dashing")]
        public float DashDuration;
        public float DashSpeed;

        [Header("Swimming")]
        public float SwimmingAcceleration;
        public float SwimmingMaxSpeed;
        public float SwimmingDrag;
        public float SwimmingRotationSharpness;
        public float SwimmingStandUpDistanceFromSurface;
        public float WaterDetectionDistance;
        public float SwimmingJumpSpeed;
        public float SwimmingSurfaceDiveThreshold;

        [Header("RopeSwing")]
        public float RopeSwingAcceleration;
        public float RopeSwingMaxSpeed;
        public float RopeSwingDrag;
        public float RopeSwingRotationSharpness;
        public float RopeLength;
        public float3 LocalRopeAnchorPoint;

        [Header("Climbing")]
        public float ClimbingDistanceFromSurface;
        public float ClimbingSpeed;
        public float ClimbingMovementSharpness;
        public float ClimbingRotationSharpness;

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
        public float MaxStableDistanceFromLedge;
        public CustomPhysicsBodyTags StickySurfaceTag;
        public CustomPhysicsBodyTags ClimbableTag;
        public PhysicsCategoryTags WaterPhysicsCategory;
        public PhysicsCategoryTags RopeAnchorCategory;
        public float UpOrientationAdaptationSharpness;
        public CapsuleGeometryDefinition StandingGeometry;
        public CapsuleGeometryDefinition CrouchingGeometry;
        public CapsuleGeometryDefinition RollingGeometry;
        public CapsuleGeometryDefinition SlidingGeometry;
        public CapsuleGeometryDefinition ClimbingGeometry;
        public CapsuleGeometryDefinition SwimmingGeometry;

        [HideInInspector]
        public RigidTransform AccumulatedRootMotionDelta;
        [HideInInspector]
        public byte CurrentUngroundedJumps;
        [HideInInspector]
        public float HeldJumpTimeCounter;
        [HideInInspector]
        public bool RequestedJumpBeforeGrounded;
        [HideInInspector]
        public bool JumpAfterUngroundedAvailable;
        [HideInInspector]
        public bool HeldJumpValid;
        [HideInInspector]
        public float LastTimeJumpPressed;
        [HideInInspector]
        public float LastTimeWasGrounded;
        [HideInInspector]
        public bool HasDetectedMoveAgainstWall;
        [HideInInspector]
        public float3 LastKnownWallNormal;
        [HideInInspector]
        public Entity LedgeGrabBodyEntity;
        [HideInInspector]
        public float LedgeGrabBlockCounter;
        [HideInInspector]
        public quaternion PreviousCharacterRotation;
        [HideInInspector]
        public float DistanceFromWaterSurface;
        [HideInInspector]
        public float3 DirectionToWaterSurface;
        [HideInInspector]
        public bool IsSprinting;
        [HideInInspector]
        public bool IsOnStickySurface;
        [HideInInspector]
        public bool WasOnStickySurface;
        [HideInInspector]
        public bool ForceCurrentStateEnter;
    }

    [Serializable]
    public struct PlatformerCharacterInitialized : IComponentData
    {
    }

    [Serializable]
    public struct CapsuleGeometryDefinition
    {
        public float Radius;
        public float Height;
        public float3 Center;

        public CapsuleGeometry ToCapsuleGeometry()
        {
            Height = math.max(Height, (Radius + math.EPSILON) * 2f);
            float halfHeight = Height * 0.5f;

            return new CapsuleGeometry
            {
                Radius = Radius,
                Vertex0 = Center + (-math.up() * (halfHeight - Radius)),
                Vertex1 = Center + (math.up() * (halfHeight - Radius)),
            };
        }
    }
}