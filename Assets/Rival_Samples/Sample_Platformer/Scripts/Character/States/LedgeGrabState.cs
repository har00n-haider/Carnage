using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct LedgeGrabState : IPlatformerCharacterState
    {
        private bool DetectedMustExitLedge;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.SetCollisionDetectionActive(false);

            p.CharacterBody.RelativeVelocity = float3.zero;
            p.CharacterBody.Unground();
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            if (nextState != CharacterState.LedgeStandingUp)
            {
                p.CharacterBody.SetCollisionDetectionActive(true);

                p.CharacterBody.ParentEntity = Entity.Null;
                p.PlatformerCharacter.LedgeGrabBodyEntity = Entity.Null;
            }

            p.CharacterBody.RelativeVelocity = float3.zero;
        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            p.CharacterGroundingAndParentMovementUpdate();

            HandleCharacterControl(ref p);

            p.CharacterMovementAndFinalizationUpdate(false);

            if (!DetectTransitions(ref p))
            {
                p.DetectGlobalTransitions();
            }
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            const float collisionOffset = 0.02f;

            DetectedMustExitLedge = false;
            p.CharacterBody.RelativeVelocity = float3.zero;

            LedgeDetection(
                ref p,
                p.Translation,
                p.Rotation,
                out bool ledgeIsValid,
                out ColliderCastHit surfaceHit,
                out ColliderCastHit forwardHit,
                out float3 characterTranslationAtLedgeSurface,
                out bool wouldBeGroundedOnLedgeSurfaceHit,
                out float forwardHitDistance,
                out bool isObstructedAtSurface,
                out bool isObstructedAtCurrentPosition,
                out float upOffsetToPlaceLedgeDetectionPointAtLedgeLevel);

            if(ledgeIsValid && !isObstructedAtSurface)
            {
                p.CharacterBody.ParentEntity = forwardHit.Entity;

                float3 characterForward = MathUtilities.GetForwardFromRotation(p.Rotation);
                float3 characterRight = MathUtilities.GetRightFromRotation(p.Rotation);

                // Stick to wall
                p.Translation += characterForward * (forwardHitDistance - collisionOffset);

                // Adjust to ledge height
                p.Translation += p.GroundingUp * (upOffsetToPlaceLedgeDetectionPointAtLedgeLevel - collisionOffset);

                // Adjust rotation to face current ledge wall
                quaternion targetRotation = quaternion.LookRotationSafe(math.normalizesafe(MathUtilities.ProjectOnPlane(-forwardHit.SurfaceNormal, p.GroundingUp)), p.GroundingUp);
                p.Rotation = math.slerp(p.Rotation, targetRotation, MathUtilities.GetSharpnessInterpolant(p.PlatformerCharacter.LedgeRotationSharpness, p.DeltaTime));

                if (math.lengthsq(p.CharacterInputs.WorldMoveVector) > 0f)
                {
                    // Move input
                    float3 ledgeDirection = math.normalizesafe(math.cross(surfaceHit.SurfaceNormal, forwardHit.SurfaceNormal));
                    float3 moveInputOnLedgeDirection = math.projectsafe(p.CharacterInputs.WorldMoveVector, ledgeDirection);

                    // Check for move obstructions
                    float3 targetTranslationAfterMove = p.Translation + (moveInputOnLedgeDirection * p.PlatformerCharacter.LedgeMoveSpeed * p.DeltaTime);
                    LedgeDetection(
                        ref p,
                        targetTranslationAfterMove,
                        p.Rotation,
                        out bool afterMoveLedgeIsValid,
                        out ColliderCastHit afterMoveSurfaceHit,
                        out ColliderCastHit afterMoveForwardHit,
                        out float3 afterMoveCharacterTranslationAtLedgeSurface,
                        out bool afterMoveWouldBeGroundedOnLedgeSurfaceHit,
                        out float afterMoveForwardHitDistance,
                        out bool afterMoveIsObstructedAtSurface,
                        out bool afterMoveIsObstructedAtCurrentPosition,
                        out float afterMoveUpOffsetToPlaceLedgeDetectionPointAtLedgeLevel);

                    if (afterMoveLedgeIsValid && !afterMoveIsObstructedAtSurface)
                    {
                        p.CharacterBody.RelativeVelocity = moveInputOnLedgeDirection * p.PlatformerCharacter.LedgeMoveSpeed;
                    }
                }
            }
            else
            {
                DetectedMustExitLedge = true;
            }

            // Detect letting go of ledge
            if (p.CharacterInputs.CrouchPressed || p.CharacterInputs.DashPressed)
            {
                p.PlatformerCharacter.LedgeGrabBlockCounter = 0.3f;
            }
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (IsLedgeGrabBlocked(ref p) || DetectedMustExitLedge)
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            if (p.CharacterInputs.JumpPressed)
            {
                LedgeDetection(
                    ref p,
                    p.Translation,
                    p.Rotation,
                    out bool ledgeIsValid,
                    out ColliderCastHit surfaceHit,
                    out ColliderCastHit forwardHit,
                    out float3 characterTranslationAtLedgeSurface,
                    out bool wouldBeGroundedOnLedgeSurfaceHit,
                    out float forwardHitDistance,
                    out bool isObstructedAtSurface,
                    out bool isObstructedAtCurrentPosition,
                    out float upOffsetToPlaceLedgeDetectionPointAtLedgeLevel);

                if (ledgeIsValid && !isObstructedAtSurface && wouldBeGroundedOnLedgeSurfaceHit)
                {
                    p.TransitionToState(CharacterState.LedgeStandingUp);
                    return true;
                }
            }

            return false;
        }

        public static bool IsLedgeGrabBlocked(ref PlatformerCharacterProcessor p)
        {
            return p.PlatformerCharacter.LedgeGrabBlockCounter > 0f;
        }

        public static bool CanGrabLedge(ref PlatformerCharacterProcessor p, out Entity ledgeEntity)
        {
            ledgeEntity = Entity.Null;

            if (IsLedgeGrabBlocked(ref p))
            {
                return false;
            }

            LedgeDetection(
                ref p,
                p.Translation,
                p.Rotation,
                out bool ledgeIsValid,
                out ColliderCastHit surfaceHit,
                out ColliderCastHit forwardHit,
                out float3 characterTranslationAtLedgeSurface,
                out bool wouldBeGroundedOnLedgeSurfaceHit,
                out float forwardHitDistance,
                out bool isObstructedAtSurface,
                out bool isObstructedAtCurrentPosition,
                out float upOffsetToPlaceLedgeDetectionPointAtLedgeLevel);

            // Prevent detecting valid grab if going up
            if(math.dot(p.CharacterBody.RelativeVelocity, surfaceHit.SurfaceNormal) > 0f)
            {
                ledgeIsValid = false;
            }

            if(ledgeIsValid)
            {
                ledgeEntity = surfaceHit.Entity;
            }

            return ledgeIsValid && !isObstructedAtSurface;
        }

        public static void LedgeDetection(
            ref PlatformerCharacterProcessor p,
            float3 atCharacterTranslation,
            quaternion atCharacterRotation,
            out bool ledgeIsValid,
            out ColliderCastHit surfaceHit,
            out ColliderCastHit forwardHit,
            out float3 characterTranslationAtLedgeSurface,
            out bool wouldBeGroundedOnLedgeSurfaceHit,
            out float forwardHitDistance,
            out bool isObstructedAtSurface,
            out bool isObstructedAtCurrentPosition,
            out float upOffsetToPlaceLedgeDetectionPointAtLedgeLevel)
        {
            const float ledgeProbingToleranceOffset = 0.04f;

            ledgeIsValid = false;
            surfaceHit = default;
            forwardHit = default;
            characterTranslationAtLedgeSurface = default;
            wouldBeGroundedOnLedgeSurfaceHit = false;
            forwardHitDistance = -1f;
            isObstructedAtSurface = false;
            isObstructedAtCurrentPosition = false;
            upOffsetToPlaceLedgeDetectionPointAtLedgeLevel = -1f;

            float3 currentCharacterForward = MathUtilities.GetForwardFromRotation(atCharacterRotation);
            float3 currentCharacterRight = MathUtilities.GetRightFromRotation(atCharacterRotation);
            RigidTransform currentCharacterRigidTransform = math.RigidTransform(atCharacterRotation, atCharacterTranslation);
            float3 worldSpaceLedgeDetectionPoint = math.transform(currentCharacterRigidTransform, p.TranslationFromEntity[p.PlatformerCharacter.LedgeDetectionPointEntity].Value);
            float forwardDepthOfLedgeDetectionPoint = math.length(math.projectsafe(worldSpaceLedgeDetectionPoint - atCharacterTranslation, currentCharacterForward));

            // Forward detection against the ledge wall
            bool forwardHitDetected = false;
            if (KinematicCharacterUtilities.CastColliderClosestCollisions(
                ref p,
                in p.PhysicsCollider,
                p.Entity,
                atCharacterTranslation,
                atCharacterRotation,
                currentCharacterForward,
                forwardDepthOfLedgeDetectionPoint,
                false,
                p.CharacterBody.ShouldIgnoreDynamicBodies(),
                out forwardHit,
                out forwardHitDistance))
            {
                forwardHitDetected = true;

                if (KinematicCharacterUtilities.CalculateDistanceClosestCollisions(
                    ref p,
                    in p.PhysicsCollider,
                    p.Entity,
                    atCharacterTranslation,
                    atCharacterRotation,
                    0f,
                    p.CharacterBody.ShouldIgnoreDynamicBodies(),
                    out DistanceHit closestOverlapHit))
                {
                    if (closestOverlapHit.Distance <= 0f)
                    {
                        isObstructedAtCurrentPosition = true;
                    }
                }
            }

            // Cancel rest of detection if no forward hit detected
            if (!forwardHitDetected)
            {
                return;
            }

            // Cancel rest of detection if currently obstructed
            if (isObstructedAtCurrentPosition)
            {
                return;
            }

            // Raycast downward at detectionPoint to find a surface hit
            bool surfaceRaycastHitDetected = false;
            float3 startPointOfSurfaceDetectionRaycast = worldSpaceLedgeDetectionPoint + (p.GroundingUp * p.PlatformerCharacter.LedgeSurfaceProbingHeight);
            float surfaceRaycastLength = p.PlatformerCharacter.LedgeSurfaceProbingHeight + ledgeProbingToleranceOffset;
            if (KinematicCharacterUtilities.RaycastClosestCollisions(
                ref p,
                in p.PhysicsCollider,
                p.Entity,
                startPointOfSurfaceDetectionRaycast,
                -p.GroundingUp,
                surfaceRaycastLength,
                p.CharacterBody.ShouldIgnoreDynamicBodies(),
                out RaycastHit surfaceRaycastHit,
                out float surfaceRaycastHitDistance))
            {
                if (surfaceRaycastHit.Fraction > 0f)
                {
                    surfaceRaycastHitDetected = true;
                }
            }

            // If no ray hit found, do more raycast tests on the sides
            if (!surfaceRaycastHitDetected)
            {
                float3 rightStartPointOfSurfaceDetectionRaycast = startPointOfSurfaceDetectionRaycast + (currentCharacterRight * p.PlatformerCharacter.LedgeSideProbingLength);
                if (KinematicCharacterUtilities.RaycastClosestCollisions(
                    ref p,
                    in p.PhysicsCollider,
                    p.Entity,
                    rightStartPointOfSurfaceDetectionRaycast,
                    -p.GroundingUp,
                    surfaceRaycastLength,
                p.CharacterBody.ShouldIgnoreDynamicBodies(),
                    out surfaceRaycastHit,
                    out surfaceRaycastHitDistance))
                {
                    if (surfaceRaycastHit.Fraction > 0f)
                    {
                        surfaceRaycastHitDetected = true;
                    }
                }
            }
            if (!surfaceRaycastHitDetected)
            {
                float3 leftStartPointOfSurfaceDetectionRaycast = startPointOfSurfaceDetectionRaycast - (currentCharacterRight * p.PlatformerCharacter.LedgeSideProbingLength);
                if (KinematicCharacterUtilities.RaycastClosestCollisions(
                    ref p,
                    in p.PhysicsCollider,
                    p.Entity,
                    leftStartPointOfSurfaceDetectionRaycast,
                    -p.GroundingUp,
                    surfaceRaycastLength,
                    p.CharacterBody.ShouldIgnoreDynamicBodies(),
                    out surfaceRaycastHit,
                    out surfaceRaycastHitDistance))
                {
                    if (surfaceRaycastHit.Fraction > 0f)
                    {
                        surfaceRaycastHitDetected = true;
                    }
                }
            }

            // Cancel rest of detection if no surface raycast hit detected
            if (!surfaceRaycastHitDetected)
            {
                return;
            }

            // Cancel rest of detection if surface hit is dynamic
            if (PhysicsUtilities.IsBodyDynamic(in p.PhysicsMassFromEntity, surfaceRaycastHit.Entity))
            {
                return;
            }

            ledgeIsValid = true;

            upOffsetToPlaceLedgeDetectionPointAtLedgeLevel = surfaceRaycastLength - surfaceRaycastHitDistance;

            // Note: this assumes that our transform pivot is at the base of our capsule collider
            float3 startPointOfSurfaceObstructionDetectionCast = surfaceRaycastHit.Position + (p.GroundingUp * p.PlatformerCharacter.LedgeSurfaceObstructionProbingHeight);

            // Check obstructions at surface hit point
            if (KinematicCharacterUtilities.CastColliderClosestCollisions(
                ref p,
                in p.PhysicsCollider,
                p.Entity,
                startPointOfSurfaceObstructionDetectionCast,
                atCharacterRotation,
                -p.GroundingUp,
                p.PlatformerCharacter.LedgeSurfaceObstructionProbingHeight + ledgeProbingToleranceOffset,
                false,
                p.CharacterBody.ShouldIgnoreDynamicBodies(),
                out surfaceHit,
                out float closestSurfaceObstructionHitDistance))
            {
                if(surfaceHit.Fraction <= 0f)
                {
                    isObstructedAtSurface = true;
                }
            }

            // Cancel rest of detection if obstruction at surface
            if (isObstructedAtSurface)
            {
                return;
            }

            // Cancel rest of detection if found no surface hit
            if (surfaceHit.Entity == Entity.Null)
            {
                return;
            }

            characterTranslationAtLedgeSurface = startPointOfSurfaceObstructionDetectionCast + (-p.GroundingUp * closestSurfaceObstructionHitDistance);

            wouldBeGroundedOnLedgeSurfaceHit = p.IsGroundedOnHit(new BasicHit(surfaceHit), 0);
        }
    }
}