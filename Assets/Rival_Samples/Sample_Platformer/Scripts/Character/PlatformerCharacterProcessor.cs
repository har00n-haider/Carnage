using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Authoring;

namespace Rival.Samples.Platformer
{
    public struct PlatformerCharacterProcessor : IKinematicCharacterProcessor
    {
        public int IndexInChunk;
        public float DeltaTime;
        public float ElapsedTime;
        public CollisionWorld CollisionWorld;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public ComponentDataFromEntity<StoredKinematicCharacterBodyProperties> StoredKinematicCharacterBodyPropertiesFromEntity;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
        public ComponentDataFromEntity<TrackedTransform> TrackedTransformFromEntity;
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        public ComponentDataFromEntity<Rotation> RotationFromEntity;
        public ComponentDataFromEntity<NonUniformScale> NonUniformScaleFromEntity;
        public ComponentDataFromEntity<CharacterFrictionModifier> CharacterFrictionModifierFromEntity;
        public BufferFromEntity<LinkedEntityGroup> LinkedEntityGroupFromEntity;

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
        public PlatformerCharacterComponent PlatformerCharacter;
        public PlatformerCharacterInputs CharacterInputs;
        public PlatformerCharacterStateMachine PlatformerCharacterStateMachine;
        public CustomGravity CustomGravity;

        public DynamicBuffer<KinematicCharacterHit> CharacterHitsBuffer;
        public DynamicBuffer<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBuffer;
        public DynamicBuffer<KinematicVelocityProjectionHit> VelocityProjectionHitsBuffer;
        public DynamicBuffer<StatefulKinematicCharacterHit> StatefulCharacterHitsBuffer;
        public DynamicBuffer<StatefulTriggerEvent> TriggerEventsBuffer;

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
                PlatformerCharacter.StepHandling,
                PlatformerCharacter.MaxStepHeight,
                PlatformerCharacter.ExtraStepChecksDistance);
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
                PlatformerCharacter.StepHandling,
                PlatformerCharacter.MaxStepHeight);
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
                PlatformerCharacter.ConstrainVelocityToGroundPlane);
        }
        #endregion

        public void OnUpdate()
        {
            // Common logic before state updates
            {
                GroundingUp = MathUtilities.GetUpFromRotation(Rotation);

                // Reset values
                PlatformerCharacter.HasDetectedMoveAgainstWall = false;

                // Jump values
                if (CharacterInputs.JumpHeld)
                {
                    PlatformerCharacter.HeldJumpTimeCounter += DeltaTime;
                    if (PlatformerCharacter.HeldJumpTimeCounter > PlatformerCharacter.MaxHeldJumpTime)
                    {
                        PlatformerCharacter.HeldJumpValid = false;
                    }
                }
                else
                {
                    PlatformerCharacter.HeldJumpValid = false;
                    PlatformerCharacter.HeldJumpTimeCounter = 0f;
                }
                if (CharacterInputs.JumpPressed)
                {
                    PlatformerCharacter.LastTimeJumpPressed = ElapsedTime;
                }

                // Other
                if (CharacterBody.IsGrounded)
                {
                    PlatformerCharacter.LastTimeWasGrounded = ElapsedTime;
                }
                if (PlatformerCharacter.LedgeGrabBlockCounter > 0f)
                {
                    PlatformerCharacter.LedgeGrabBlockCounter -= DeltaTime;
                }
                PlatformerCharacter.WasOnStickySurface = PlatformerCharacter.IsOnStickySurface;
            }

            KinematicCharacterUtilities.InitializationUpdate(ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer);

            if (PlatformerCharacter.ForceCurrentStateEnter)
            {
                PlatformerCharacterStateMachine.OnStateEnter(PlatformerCharacterStateMachine.CurrentCharacterState, PlatformerCharacterStateMachine.PreviousCharacterState, ref this);
                PlatformerCharacter.ForceCurrentStateEnter = false;
            }

            // State update
            PlatformerCharacterStateMachine.OnStateUpdate(PlatformerCharacterStateMachine.CurrentCharacterState, ref this);

            // Common logic after state updates
            {
                PlatformerCharacter.PreviousCharacterRotation = Rotation;
                PlatformerCharacter.AccumulatedRootMotionDelta = default;
            }
        }

        public void CharacterGroundingAndParentMovementUpdate()
        {
            bool shouldConstrainRotationFromParent = CharacterBody.WasGroundedBeforeCharacterUpdate;
            if(CustomGravity.CurrentZoneEntity != Entity.Null)
            {
                shouldConstrainRotationFromParent = false;
            }

            KinematicCharacterUtilities.ParentMovementUpdate(ref this, ref Translation, ref CharacterBody, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp, shouldConstrainRotationFromParent);
            Rotation = math.mul(Rotation, CharacterBody.RotationFromParent);
            KinematicCharacterUtilities.GroundingUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, in PhysicsCollider, Entity, Rotation, GroundingUp);
        }

        public void CharacterMovementAndFinalizationUpdate(bool detectMovingPlatforms)
        {
            // Prevent grounding from future slope change
            if (CharacterBody.IsGrounded && (PlatformerCharacter.PreventGroundingWhenMovingTowardsNoGrounding || PlatformerCharacter.HasMaxDownwardSlopeChangeAngle))
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
                    PlatformerCharacter.StepHandling,
                    PlatformerCharacter.MaxStepHeight,
                    out bool isMovingTowardsNoGrounding,
                    out bool foundSlopeHit,
                    out float futureSlopeChangeAnglesRadians,
                    out RaycastHit futureSlopeHit);
                if ((PlatformerCharacter.PreventGroundingWhenMovingTowardsNoGrounding && isMovingTowardsNoGrounding) ||
                    (PlatformerCharacter.HasMaxDownwardSlopeChangeAngle && foundSlopeHit && math.degrees(futureSlopeChangeAnglesRadians) < -PlatformerCharacter.MaxDownwardSlopeChangeAngle))
                {
                    CharacterBody.IsGrounded = false;
                }
            }

            if (CharacterBody.IsGrounded && CharacterBody.SimulateDynamicBody)
            {
                KinematicCharacterUtilities.DefaultMethods.UpdateGroundPushing(ref this, ref CharacterDeferredImpulsesBuffer, ref CharacterBody, DeltaTime, Entity, CustomGravity.Gravity, Translation, Rotation, 1f);
            }

            KinematicCharacterUtilities.MovementAndDecollisionsUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp);

            if (detectMovingPlatforms)
            {
                KinematicCharacterUtilities.DefaultMethods.MovingPlatformDetection(ref TrackedTransformFromEntity, ref StoredKinematicCharacterBodyPropertiesFromEntity, ref CharacterBody);
            }

            KinematicCharacterUtilities.ParentMomentumUpdate(ref TrackedTransformFromEntity, ref CharacterBody, in Translation, DeltaTime, GroundingUp);
            KinematicCharacterUtilities.ProcessStatefulCharacterHits(ref StatefulCharacterHitsBuffer, in CharacterHitsBuffer);
        }

        public void TransitionToState(CharacterState newState)
        {
            PlatformerCharacterStateMachine.PreviousCharacterState = PlatformerCharacterStateMachine.CurrentCharacterState;
            PlatformerCharacterStateMachine.CurrentCharacterState = newState;

            PlatformerCharacterStateMachine.OnStateExit(PlatformerCharacterStateMachine.PreviousCharacterState, PlatformerCharacterStateMachine.CurrentCharacterState, ref this);
            PlatformerCharacterStateMachine.OnStateEnter(PlatformerCharacterStateMachine.CurrentCharacterState, PlatformerCharacterStateMachine.PreviousCharacterState, ref this);
        }

        public bool DetectGlobalTransitions()
        {
            if (PlatformerCharacterStateMachine.CurrentCharacterState != CharacterState.Swimming && PlatformerCharacterStateMachine.CurrentCharacterState != CharacterState.FlyingNoCollisions)
            {
                if (SwimmingState.DetectWaterZones(ref this, out float3 tmpDirection, out float tmpDistance))
                {
                    if (tmpDistance < 0f)
                    {
                        TransitionToState(CharacterState.Swimming);
                        return true;
                    }
                }
            }

            if (CharacterInputs.FlyNoCollisionsPressed)
            {
                if (PlatformerCharacterStateMachine.CurrentCharacterState == CharacterState.FlyingNoCollisions)
                {
                    TransitionToState(CharacterState.AirMove);
                    return true;
                }
                else
                {
                    TransitionToState(CharacterState.FlyingNoCollisions);
                    return true;
                }
            }

            return false;
        }

        public void OrientCharacterOnPlaneTowardsMoveInput(float rotationSharpness)
        {
            float3 moveVectorOnPlane = math.normalizesafe(Rival.MathUtilities.ProjectOnPlane(CharacterInputs.WorldMoveVector, GroundingUp)) * math.length(CharacterInputs.WorldMoveVector);
            if (math.lengthsq(moveVectorOnPlane) > 0f)
            {
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref Rotation, DeltaTime, math.normalizesafe(moveVectorOnPlane), GroundingUp, rotationSharpness);
            }
        }

        public void OrientCharacterUpTowardsDirection(float3 direction, float rotationSharpness)
        {
            quaternion targetRotation = MathUtilities.CreateRotationWithUpPriority(direction, MathUtilities.GetForwardFromRotation(Rotation));
            Rotation = math.slerp(Rotation, targetRotation, MathUtilities.GetSharpnessInterpolant(rotationSharpness, DeltaTime));
        }

        public unsafe void SetCapsuleGeometry(CapsuleGeometry capsuleGeometry)
        {
            CapsuleCollider* capsuleCollider = (CapsuleCollider*)PhysicsCollider.ColliderPtr;
            capsuleCollider->Geometry = capsuleGeometry;
        }

        public unsafe void SetCollisionResponse(CollisionResponsePolicy collisionResponse)
        {
            CapsuleCollider* capsuleCollider = ((CapsuleCollider*)PhysicsCollider.ColliderPtr);
            Material mat = capsuleCollider->Material;
            mat.CollisionResponse = collisionResponse;
            capsuleCollider->Material = mat;
        }

        public bool DetectUngroundedHits(float3 detectionVector, out ColliderCastHit detectedHit)
        {
            detectedHit = default;

            // If there's a nongrounded obstruction in the direction of our acceleration, cancel acceleration
            if (KinematicCharacterUtilities.CastColliderClosestCollisions(
                ref this,
                in PhysicsCollider,
                Entity,
                Translation,
                Rotation,
                math.normalizesafe(detectionVector),
                math.length(detectionVector),
                true,
                CharacterBody.ShouldIgnoreDynamicBodies(),
                out ColliderCastHit hit,
                out float hitDistance))
            {
                if (!IsGroundedOnHit(new BasicHit(hit), 0))
                {
                    detectedHit = hit;
                    return true;
                }
            }

            return false;
        }

        public unsafe bool CanStandUp()
        {
            // Overlap test with standing geometry to see if we have space to stand
            CapsuleCollider* capsuleCollider = ((CapsuleCollider*)PhysicsCollider.ColliderPtr);

            CapsuleGeometry initialGeometry = capsuleCollider->Geometry;
            capsuleCollider->Geometry = PlatformerCharacter.StandingGeometry.ToCapsuleGeometry();

            bool isObstructed = false;
            if (KinematicCharacterUtilities.CalculateDistanceClosestCollisions(
                ref this,
                in PhysicsCollider,
                Entity,
                Translation,
                Rotation,
                0f,
                CharacterBody.ShouldIgnoreDynamicBodies(),
                out DistanceHit hit))
            {
                isObstructed = true;
            }

            capsuleCollider->Geometry = initialGeometry;

            return !isObstructed;
        }
    }

    public static class PlatformerCharacterUtilities
    {
        public static void InitializeCharacter(
            ref PlatformerCharacterComponent platformerCharacter,
            ref PlatformerCharacterStateMachine platformerCharacterStateMachine,
            ref EntityCommandBuffer commandBuffer,
            ref BufferFromEntity<Child> childBufferFromEntity,
            ref BufferFromEntity<LinkedEntityGroup> linkedEntityGroupFromEntity,
            Entity entity)
        {
            // Make sure the transform system has done a pass on it first
            if (childBufferFromEntity.HasComponent(entity))
            {
                // Initial state
                platformerCharacterStateMachine.CurrentCharacterState = CharacterState.AirMove;
                platformerCharacter.ForceCurrentStateEnter = true;
                platformerCharacter.LastTimeJumpPressed = float.MinValue;

                // Disable alternative meshes
                PlatformerUtilities.SetEntityHierarchyEnabled(false, platformerCharacter.RollballMeshEntity, commandBuffer, linkedEntityGroupFromEntity);

                commandBuffer.AddComponent<PlatformerCharacterInitialized>(entity);
            }
        }

        public static CapsuleGeometry CreateCharacterCapsuleGeometry(float radius, float height, bool centered)
        {
            height = math.max(height, radius * 2f);
            float halfHeight = height * 0.5f;

            return new CapsuleGeometry
            {
                Radius = radius,
                Vertex0 = centered ? (-math.up() * (halfHeight - radius)) : (math.up() * radius),
                Vertex1 = centered ? (math.up() * (halfHeight - radius)) : (math.up() * (height - radius)),
            };
        }

        public static bool CanBeAffectedByWindZone(CharacterState currentCharacterState)
        {
            if (currentCharacterState == CharacterState.GroundMove ||
                currentCharacterState == CharacterState.AirMove ||
                currentCharacterState == CharacterState.Crouched ||
                currentCharacterState == CharacterState.Rolling ||
                currentCharacterState == CharacterState.Sliding)
            {
                return true;
            }

            return false;
        }
    }
}