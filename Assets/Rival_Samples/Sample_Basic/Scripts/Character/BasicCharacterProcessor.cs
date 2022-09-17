using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace Rival.Samples.Basic
{
    public struct BasicCharacterProcessor : IKinematicCharacterProcessor
    {
        public float DeltaTime;
        public CollisionWorld CollisionWorld;

        public ComponentDataFromEntity<StoredKinematicCharacterBodyProperties> StoredKinematicCharacterBodyPropertiesFromEntity;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
        public ComponentDataFromEntity<TrackedTransform> TrackedTransformFromEntity;
        public ComponentDataFromEntity<BouncySurface> BouncySurfaceFromEntity;

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
        public BasicCharacterComponent BasicCharacter;
        public BasicCharacterInputs BasicCharacterInputs;

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
            if (!KinematicCharacterUtilities.DefaultMethods.CanCollideWithHit(in hit, in StoredKinematicCharacterBodyPropertiesFromEntity))
            {
                return false;
            }

            if (BasicCharacter.IgnoredPhysicsBodyTags.Value > CustomPhysicsBodyTags.Nothing.Value)
            {
                if ((CollisionWorld.Bodies[hit.RigidBodyIndex].CustomTags & BasicCharacter.IgnoredPhysicsBodyTags.Value) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsGroundedOnHit(in BasicHit hit, int groundingEvaluationType)
        {
            // Ignore grounding tag
            if (BasicCharacter.UngroundablePhysicsBodyTags.Value > CustomPhysicsBodyTags.Nothing.Value)
            {
                if ((CollisionWorld.Bodies[hit.RigidBodyIndex].CustomTags & BasicCharacter.UngroundablePhysicsBodyTags.Value) > 0)
                {
                    return false;
                }
            }

            // Prevent step handling on certain bodies
            bool useStepHandling = BasicCharacter.StepHandling;
            if (useStepHandling)
            {
                if (BasicCharacter.IgnoreStepHandlingTags.Value > CustomPhysicsBodyTags.Nothing.Value)
                {
                    if ((CollisionWorld.Bodies[hit.RigidBodyIndex].CustomTags & BasicCharacter.IgnoreStepHandlingTags.Value) > 0)
                    {
                        useStepHandling = false;
                    }
                }
            }

            return KinematicCharacterUtilities.DefaultMethods.IsGroundedOnHit(
                ref this,
                in hit,
                in CharacterBody,
                in PhysicsCollider,
                Entity,
                GroundingUp,
                groundingEvaluationType,
                useStepHandling,
                BasicCharacter.MaxStepHeight,
                BasicCharacter.ExtraStepChecksDistance);
        }

        public void OnMovementHit(
                ref KinematicCharacterHit hit,
                ref float3 remainingMovementDirection,
                ref float remainingMovementLength,
                float3 originalVelocityDirection,
                float hitDistance)
        {
            // Prevent step handling on certain bodies
            bool useStepHandling = BasicCharacter.StepHandling;
            if (useStepHandling)
            {
                if (BasicCharacter.IgnoreStepHandlingTags.Value > CustomPhysicsBodyTags.Nothing.Value)
                {
                    if ((CollisionWorld.Bodies[hit.RigidBodyIndex].CustomTags & BasicCharacter.IgnoreStepHandlingTags.Value) > 0)
                    {
                        useStepHandling = false;
                    }
                }
            }

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
                useStepHandling,
                BasicCharacter.MaxStepHeight);
        }

        public void OverrideDynamicHitMasses(
            ref PhysicsMass characterMass,
            ref PhysicsMass otherMass,
            Entity characterEntity,
            Entity otherEntity,
            int otherRigidbodyIndex)
        {
            if (BasicCharacter.ZeroMassAgainstCharacterPhysicsBodyTags.Value > CustomPhysicsBodyTags.Nothing.Value)
            {
                if ((CollisionWorld.Bodies[otherRigidbodyIndex].CustomTags & BasicCharacter.ZeroMassAgainstCharacterPhysicsBodyTags.Value) > 0)
                {
                    characterMass.InverseMass = 0f;
                    characterMass.InverseInertia = new float3(0f);
                    otherMass.InverseMass = 1f;
                    otherMass.InverseInertia = new float3(1f);
                }
            }
            if (BasicCharacter.InfiniteMassAgainstCharacterPhysicsBodyTags.Value > CustomPhysicsBodyTags.Nothing.Value)
            {
                if ((CollisionWorld.Bodies[otherRigidbodyIndex].CustomTags & BasicCharacter.InfiniteMassAgainstCharacterPhysicsBodyTags.Value) > 0)
                {
                    characterMass.InverseMass = 1f;
                    characterMass.InverseInertia = new float3(1f);
                    otherMass.InverseMass = 0f;
                    otherMass.InverseInertia = new float3(0f);
                }
            }
        }

        public void ProjectVelocityOnHits(
            ref float3 velocity,
            ref bool characterIsGrounded,
            ref BasicHit characterGroundHit,
            in DynamicBuffer<KinematicVelocityProjectionHit> hits,
            float3 originalVelocityDirection)
        {
            var latestHit = hits[hits.Length - 1];
            if (BasicCharacter.HandleBouncySurfaces && BouncySurfaceFromEntity.HasComponent(latestHit.Entity))
            {
                BouncySurface bouncySurface = BouncySurfaceFromEntity[latestHit.Entity];
                velocity = math.reflect(velocity, latestHit.Normal);
                velocity *= bouncySurface.BounceEnergyMultiplier;
            }
            else
            {
                KinematicCharacterUtilities.DefaultMethods.ProjectVelocityOnHits(
                    ref velocity,
                    ref characterIsGrounded,
                    ref characterGroundHit,
                    in hits,
                    originalVelocityDirection,
                    GroundingUp,
                    BasicCharacter.ConstrainVelocityToGroundPlane);
            }
        }
        #endregion

        public void OnUpdate()
        {
            GroundingUp = -math.normalizesafe(BasicCharacter.Gravity);

            KinematicCharacterUtilities.InitializationUpdate(ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer);
            KinematicCharacterUtilities.ParentMovementUpdate(ref this, ref Translation, ref CharacterBody, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp, CharacterBody.WasGroundedBeforeCharacterUpdate); // safe to remove if not needed
            Rotation = math.mul(Rotation, CharacterBody.RotationFromParent);
            KinematicCharacterUtilities.GroundingUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, in PhysicsCollider, Entity, Rotation, GroundingUp);

            HandleCharacterControl();

            // Prevent grounding from future slope change
            if (CharacterBody.IsGrounded && (BasicCharacter.PreventGroundingWhenMovingTowardsNoGrounding || BasicCharacter.HasMaxDownwardSlopeChangeAngle))
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
                    BasicCharacter.StepHandling,
                    BasicCharacter.MaxStepHeight,
                    out bool isMovingTowardsNoGrounding,
                    out bool foundSlopeHit,
                    out float futureSlopeChangeAnglesRadians,
                    out RaycastHit futureSlopeHit);
                if ((BasicCharacter.PreventGroundingWhenMovingTowardsNoGrounding && isMovingTowardsNoGrounding) ||
                    (BasicCharacter.HasMaxDownwardSlopeChangeAngle && foundSlopeHit && math.degrees(futureSlopeChangeAnglesRadians) < -BasicCharacter.MaxDownwardSlopeChangeAngle))
                {
                    CharacterBody.IsGrounded = false;
                }
            }

            if (CharacterBody.IsGrounded && CharacterBody.SimulateDynamicBody && BasicCharacter.PushGroundBodies)
            {
                KinematicCharacterUtilities.DefaultMethods.UpdateGroundPushing(ref this, ref CharacterDeferredImpulsesBuffer, ref CharacterBody, DeltaTime, Entity, BasicCharacter.Gravity, Translation, Rotation, 1f); // safe to remove if not needed
            }

            KinematicCharacterUtilities.MovementAndDecollisionsUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp);
            KinematicCharacterUtilities.DefaultMethods.MovingPlatformDetection(ref TrackedTransformFromEntity, ref StoredKinematicCharacterBodyPropertiesFromEntity, ref CharacterBody); // safe to remove if not needed
            KinematicCharacterUtilities.ParentMomentumUpdate(ref TrackedTransformFromEntity, ref CharacterBody, in Translation, DeltaTime, GroundingUp); // safe to remove if not needed
            KinematicCharacterUtilities.ProcessStatefulCharacterHits(ref StatefulCharacterHitsBuffer, in CharacterHitsBuffer); // safe to remove if not needed
        }

        public void HandleCharacterControl()
        {
            if (CharacterBody.IsGrounded)
            {
                // Move on ground
                float3 targetVelocity = BasicCharacterInputs.WorldMoveVector * BasicCharacter.GroundMaxSpeed;
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref CharacterBody.RelativeVelocity, targetVelocity, BasicCharacter.GroundedMovementSharpness, DeltaTime, GroundingUp, CharacterBody.GroundHit.Normal);

                // Jump
                if (BasicCharacterInputs.JumpRequested)
                {
                    CharacterControlUtilities.StandardJump(ref CharacterBody, GroundingUp * BasicCharacter.JumpSpeed, true, GroundingUp);
                }

                // Reset air jumps when grounded
                BasicCharacter.CurrentJumpsInAir = 0;
            }
            else
            {
                // Move in air
                float3 airAcceleration = BasicCharacterInputs.WorldMoveVector * BasicCharacter.AirAcceleration;

                // Prevent Air Climbing Slopes
                if (BasicCharacter.PreventAirClimbingSlopes)
                {
                    float3 velocityAfterAcceleration = CharacterBody.RelativeVelocity + (airAcceleration * DeltaTime);
                    float3 movementFromVelocity = velocityAfterAcceleration * DeltaTime;
                    if(math.lengthsq(movementFromVelocity) > 0f)
                    {
                        if (KinematicCharacterUtilities.CastColliderClosestCollisions(
                            ref this,
                            in PhysicsCollider,
                            Entity,
                            Translation,
                            Rotation,
                            math.normalizesafe(movementFromVelocity),
                            math.length(movementFromVelocity),
                            true,
                            CharacterBody.ShouldIgnoreDynamicBodies(),
                            out ColliderCastHit hit,
                            out float hitDistance))
                        {
                            if (!IsGroundedOnHit(new BasicHit(hit), 0))
                            {
                                // Kill acceleration
                                airAcceleration = float3.zero;
                            }
                        }
                    }
                }

                CharacterControlUtilities.StandardAirMove(ref CharacterBody.RelativeVelocity, airAcceleration, BasicCharacter.AirMaxSpeed, GroundingUp, DeltaTime, false);

                // Air Jumps
                if (BasicCharacterInputs.JumpRequested && BasicCharacter.CurrentJumpsInAir < BasicCharacter.MaxJumpsInAir)
                {
                    CharacterControlUtilities.StandardJump(ref CharacterBody, GroundingUp * BasicCharacter.JumpSpeed, true, GroundingUp);
                    BasicCharacter.CurrentJumpsInAir++;
                }

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(ref CharacterBody.RelativeVelocity, BasicCharacter.Gravity, DeltaTime);

                // Drag
                CharacterControlUtilities.ApplyDragToVelocity(ref CharacterBody.RelativeVelocity, DeltaTime, BasicCharacter.AirDrag);
            }

            // Rotation 
            if (BasicCharacter.RotationMode == CharacterRotationMode.TowardsCameraForward)
            {
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref Rotation, DeltaTime, BasicCharacterInputs.TargetLookDirection, GroundingUp, BasicCharacter.RotationSharpness);
            }
            else if (BasicCharacter.RotationMode == CharacterRotationMode.TowardsMoveVector)
            {
                if (math.lengthsq(BasicCharacterInputs.WorldMoveVector) > 0f)
                {
                    CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref Rotation, DeltaTime, BasicCharacterInputs.WorldMoveVector, GroundingUp, BasicCharacter.RotationSharpness);
                }
            }

            // Reset jump request
            BasicCharacterInputs.JumpRequested = false;
        }
    }
}
