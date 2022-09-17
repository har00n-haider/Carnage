using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.Physics;

namespace Rival.Samples.StressTest
{
    public struct StressTestCharacterProcessor : IKinematicCharacterProcessor
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
        public StressTestCharacterComponent StressTestCharacter;
        public StressTestCharacterInputs StressTestCharacterInputs;

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
                StressTestCharacter.StepHandling,
                StressTestCharacter.MaxStepHeight,
                StressTestCharacter.ExtraStepChecksDistance);
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
                StressTestCharacter.StepHandling,
                StressTestCharacter.MaxStepHeight);
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
                StressTestCharacter.ConstrainVelocityToGroundPlane);
        }
        #endregion

        public void OnUpdate()
        {
            GroundingUp = -math.normalizesafe(StressTestCharacter.Gravity);

            KinematicCharacterUtilities.InitializationUpdate(ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer);
            KinematicCharacterUtilities.ParentMovementUpdate(ref this, ref Translation, ref CharacterBody, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp, CharacterBody.WasGroundedBeforeCharacterUpdate); // safe to remove if not needed
            Rotation = math.mul(Rotation, CharacterBody.RotationFromParent);
            KinematicCharacterUtilities.GroundingUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, in PhysicsCollider, Entity, Rotation, GroundingUp);

            HandleCharacterControl();

            // Prevent grounding from future slope change
            if (CharacterBody.IsGrounded && (StressTestCharacter.PreventGroundingWhenMovingTowardsNoGrounding || StressTestCharacter.HasMaxDownwardSlopeChangeAngle))
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
                    StressTestCharacter.StepHandling,
                    StressTestCharacter.MaxStepHeight,
                    out bool isMovingTowardsNoGrounding,
                    out bool foundSlopeHit,
                    out float futureSlopeChangeAnglesRadians,
                    out RaycastHit futureSlopeHit);
                if ((StressTestCharacter.PreventGroundingWhenMovingTowardsNoGrounding && isMovingTowardsNoGrounding) ||
                    (StressTestCharacter.HasMaxDownwardSlopeChangeAngle && foundSlopeHit && math.degrees(futureSlopeChangeAnglesRadians) < -StressTestCharacter.MaxDownwardSlopeChangeAngle))
                {
                    CharacterBody.IsGrounded = false;
                }
            }

            if (CharacterBody.IsGrounded && CharacterBody.SimulateDynamicBody)
            {
                KinematicCharacterUtilities.DefaultMethods.UpdateGroundPushing(ref this, ref CharacterDeferredImpulsesBuffer, ref CharacterBody, DeltaTime, Entity, StressTestCharacter.Gravity, Translation, Rotation, 1f); // safe to remove if not needed
            }

            KinematicCharacterUtilities.MovementAndDecollisionsUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp);
            KinematicCharacterUtilities.DefaultMethods.MovingPlatformDetection(ref TrackedTransformFromEntity, ref StoredKinematicCharacterBodyPropertiesFromEntity, ref CharacterBody); // safe to remove if not needed
            KinematicCharacterUtilities.ParentMomentumUpdate(ref TrackedTransformFromEntity, ref CharacterBody, in Translation, DeltaTime, GroundingUp); // safe to remove if not needed

            if (StressTestCharacter.ProcessStatefulCharacterHits)
            {
                KinematicCharacterUtilities.ProcessStatefulCharacterHits(ref StatefulCharacterHitsBuffer, in CharacterHitsBuffer); // safe to remove if not needed
            }
        }

        public void HandleCharacterControl()
        {
            if (CharacterBody.IsGrounded)
            {
                // Move on ground
                float3 targetVelocity = StressTestCharacterInputs.WorldMoveVector * StressTestCharacter.GroundMaxSpeed;
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref CharacterBody.RelativeVelocity, targetVelocity, StressTestCharacter.GroundedMovementSharpness, DeltaTime, GroundingUp, CharacterBody.GroundHit.Normal);

                // Jump
                if (StressTestCharacterInputs.JumpRequested)
                {
                    CharacterControlUtilities.StandardJump(ref CharacterBody, GroundingUp * StressTestCharacter.JumpSpeed, true, GroundingUp);
                }
            }
            else
            {
                // Move in air
                float3 airAcceleration = StressTestCharacterInputs.WorldMoveVector * StressTestCharacter.AirAcceleration;
                CharacterControlUtilities.StandardAirMove(ref CharacterBody.RelativeVelocity, airAcceleration, StressTestCharacter.AirMaxSpeed, GroundingUp, DeltaTime, false);

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(ref CharacterBody.RelativeVelocity, StressTestCharacter.Gravity, DeltaTime);
            }

            // Rotation (towards move direction)
            if (math.lengthsq(StressTestCharacterInputs.WorldMoveVector) > 0f)
            {
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref Rotation, DeltaTime, math.normalizesafe(StressTestCharacterInputs.WorldMoveVector), GroundingUp, StressTestCharacter.RotationSharpness);
            }

            // Reset jump request
            StressTestCharacterInputs.JumpRequested = false;
        }
    }
}
