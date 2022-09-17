using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.Physics;
using Unity.NetCode;

namespace Rival.Samples.OnlineFPS
{
    public struct OnlineFPSCharacterProcessor : IKinematicCharacterProcessor
    {
        public float DeltaTime;
        public CollisionWorld CollisionWorld;

        public ComponentDataFromEntity<StoredKinematicCharacterBodyProperties> StoredKinematicCharacterBodyPropertiesFromEntity;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
        public ComponentDataFromEntity<TrackedTransform> TrackedTransformFromEntity;

        public NativeList<int> TmpRigidbodyIndexesProcessed;
        public NativeList<RaycastHit> TmpRaycastHits;
        public NativeList<ColliderCastHit> TmpColliderCastHits;
        public NativeList<DistanceHit> TmpDistanceHits;

        public Entity Entity;
        public float3 Translation;
        public quaternion Rotation;
        public float3 GroundingUp;
        public PhysicsCollider PhysicsCollider;
        public KinematicCharacterBody CharacterBody;
        public OnlineFPSCharacterComponent OnlineFPSCharacter;
        public OnlineFPSCharacterInputs CharacterIputs;

        public DynamicBuffer<KinematicCharacterHit> CharacterHitsBuffer;
        public DynamicBuffer<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBuffer;
        public DynamicBuffer<KinematicVelocityProjectionHit> VelocityProjectionHitsBuffer;
        public DynamicBuffer<StatefulKinematicCharacterHit> StatefulCharacterHitsBuffer;

        #region Processor Getters
        public CollisionWorld GetCollisionWorld => CollisionWorld;
        public ComponentDataFromEntity<StoredKinematicCharacterBodyProperties> GetStoredCharacterBodyPropertiesFromEntity => StoredKinematicCharacterBodyPropertiesFromEntity;
        public ComponentDataFromEntity<PhysicsMass> GetPhysicsMassFromEntity => PhysicsMassFromEntity;
        public ComponentDataFromEntity<PhysicsVelocity> GetPhysicsVelocityFromEntity => PhysicsVelocityFromEntity;
        public ComponentDataFromEntity<TrackedTransform> GetTrackedTransformFromEntity => TrackedTransformFromEntity;
        public NativeList<int> GetTmpRigidbodyIndexesProcessed => TmpRigidbodyIndexesProcessed;
        public NativeList<RaycastHit> GetTmpRaycastHits => TmpRaycastHits;
        public NativeList<ColliderCastHit> GetTmpColliderCastHits => TmpColliderCastHits;
        public NativeList<DistanceHit> GetTmpDistanceHits => TmpDistanceHits;
        #endregion

        #region Processor Callbacks
        public bool CanCollideWithHit(in BasicHit hit)
        {
            return KinematicCharacterUtilities.DefaultMethods.CanCollideWithHit(in hit, in StoredKinematicCharacterBodyPropertiesFromEntity);
        }

        public bool IsGroundedOnHit(in BasicHit hit, int groundingEvaluationType)
        {
            return KinematicCharacterUtilities.DefaultMethods.IsGroundedOnHit(
                ref this,
                in hit,
                in CharacterBody,
                in PhysicsCollider,
                Entity,
                GroundingUp,
                groundingEvaluationType,
                OnlineFPSCharacter.StepHandling,
                OnlineFPSCharacter.MaxStepHeight,
                OnlineFPSCharacter.ExtraStepChecksDistance);
        }

        public void OnMovementHit(
                ref KinematicCharacterHit hit,
                ref float3 remainingMovementDirection,
                ref float remainingMovementLength,
                float3 originalVelocityDirection,
                float hitDistance)
        {
            KinematicCharacterUtilities.DefaultMethods.OnMovementHit(
                ref this,
                ref hit,
                ref CharacterBody,
                ref VelocityProjectionHitsBuffer,
                ref Translation,
                ref remainingMovementDirection,
                ref remainingMovementLength,
                in PhysicsCollider,
                Entity,
                Rotation,
                GroundingUp,
                originalVelocityDirection,
                hitDistance,
                OnlineFPSCharacter.StepHandling,
                OnlineFPSCharacter.MaxStepHeight);
        }

        public void OverrideDynamicHitMasses(
            ref PhysicsMass characterMass,
            ref PhysicsMass otherMass,
            Entity characterEntity,
            Entity otherEntity,
            int otherRigidbodyIndex)
        {
        }

        public void ProjectVelocityOnHits(
            ref float3 velocity,
            ref bool characterIsGrounded,
            ref BasicHit characterGroundHit,
            in DynamicBuffer<KinematicVelocityProjectionHit> hits,
            float3 originalVelocityDirection)
        {
            // The last hit in the "hits" buffer is the latest hit. The rest of the hits are all hits so far in the movement iterations
            KinematicCharacterUtilities.DefaultMethods.ProjectVelocityOnHits(
                ref velocity,
                ref characterIsGrounded,
                ref characterGroundHit,
                in hits,
                originalVelocityDirection,
                GroundingUp,
                OnlineFPSCharacter.ConstrainVelocityToGroundPlane);
        }
        #endregion

        public void OnUpdate()
        {
            GroundingUp = -math.normalizesafe(OnlineFPSCharacter.Gravity);

            KinematicCharacterUtilities.InitializationUpdate(ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer);
            KinematicCharacterUtilities.ParentMovementUpdate(ref this, ref Translation, ref CharacterBody, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp, CharacterBody.WasGroundedBeforeCharacterUpdate);
            Rotation = math.mul(Rotation, CharacterBody.RotationFromParent);
            KinematicCharacterUtilities.GroundingUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, in PhysicsCollider, Entity, Rotation, GroundingUp);

            HandleCharacterControl();

            // Prevent grounding from future slope change
            if (CharacterBody.IsGrounded)
            {
                KinematicCharacterUtilities.DefaultMethods.DetectFutureSlopeChange(
                    ref this,
                    in CharacterBody.GroundHit,
                    in CharacterBody,
                    in PhysicsCollider,
                    Entity,
                    CharacterBody.RelativeVelocity,
                    GroundingUp,
                    0.05f, // verticalOffset
                    0.05f, // downDetectionDepth
                    DeltaTime, // deltaTimeIntoFuture
                    0.25f, // secondaryNoGroundingCheckDistance
                    OnlineFPSCharacter.StepHandling,
                    OnlineFPSCharacter.MaxStepHeight,
                    out bool isMovingTowardsNoGrounding,
                    out bool foundSlopeHit,
                    out float futureSlopeChangeAnglesRadians,
                    out RaycastHit futureSlopeHit);
                if (isMovingTowardsNoGrounding)
                {
                    CharacterBody.IsGrounded = false;
                }
            }

            KinematicCharacterUtilities.MovementAndDecollisionsUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp);
            KinematicCharacterUtilities.DefaultMethods.MovingPlatformDetection(ref TrackedTransformFromEntity, ref StoredKinematicCharacterBodyPropertiesFromEntity, ref CharacterBody);
            KinematicCharacterUtilities.ParentMomentumUpdate(ref TrackedTransformFromEntity, ref CharacterBody, in Translation, DeltaTime, GroundingUp); 
        }

        public void HandleCharacterControl()
        {
            if (CharacterBody.IsGrounded)
            {
                // Move on ground
                float3 targetVelocity = CharacterIputs.MoveVector * OnlineFPSCharacter.GroundMaxSpeed;
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref CharacterBody.RelativeVelocity, targetVelocity, OnlineFPSCharacter.GroundedMovementSharpness, DeltaTime, GroundingUp, CharacterBody.GroundHit.Normal);

                // Jump
                if (CharacterIputs.JumpRequested)
                {
                    CharacterControlUtilities.StandardJump(ref CharacterBody, GroundingUp * OnlineFPSCharacter.JumpSpeed, true, GroundingUp);
                }
            }
            else
            {
                // Move in air
                float3 airAcceleration = CharacterIputs.MoveVector * OnlineFPSCharacter.AirAcceleration;
                CharacterControlUtilities.StandardAirMove(ref CharacterBody.RelativeVelocity, airAcceleration, OnlineFPSCharacter.AirMaxSpeed, GroundingUp, DeltaTime, false);

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(ref CharacterBody.RelativeVelocity, OnlineFPSCharacter.Gravity, DeltaTime);

                // Drag
                CharacterControlUtilities.ApplyDragToVelocity(ref CharacterBody.RelativeVelocity, DeltaTime, OnlineFPSCharacter.AirDrag);
            }
        }
    }

    public static class OnlineFPSCharacterUtilities
    {
        public static void ComputeFinalRotationsFromRotationDelta(
            ref quaternion characterRotation,
            ref float viewPitchDegrees,
            float2 yawPitchDeltaDegrees,
            float viewRollDegrees,
            float minPitchDegrees,
            float maxPitchDegrees,
            out quaternion localCharacterViewRotation,
            out float canceledPitchDegrees)
        {
            // Yaw
            quaternion yawRotation = quaternion.Euler(math.up() * math.radians(yawPitchDeltaDegrees.x));
            characterRotation = math.mul(characterRotation, yawRotation);

            // Pitch
            viewPitchDegrees += yawPitchDeltaDegrees.y;
            float viewPitchAngleDegreesBeforeClamp = viewPitchDegrees;
            viewPitchDegrees = math.clamp(viewPitchDegrees, minPitchDegrees, maxPitchDegrees);
            canceledPitchDegrees = yawPitchDeltaDegrees.y - (viewPitchAngleDegreesBeforeClamp - viewPitchDegrees);

            localCharacterViewRotation = CalculateLocalViewRotation(viewPitchDegrees, viewRollDegrees);
        }

        public static quaternion CalculateLocalViewRotation(float viewPitchDegrees, float viewRollDegrees)
        {
            // Pitch
            quaternion viewLocalRotation = quaternion.AxisAngle(-math.right(), math.radians(viewPitchDegrees));

            // Roll
            viewLocalRotation = math.mul(viewLocalRotation, quaternion.AxisAngle(math.forward(), math.radians(viewRollDegrees)));

            return viewLocalRotation;
        }

        public static quaternion GetCurrentWorldViewRotation(quaternion characterRotation, quaternion localCharacterViewRotation)
        {
            return math.mul(characterRotation, localCharacterViewRotation);
        }

    }
}
