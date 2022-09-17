using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct SwimmingState : IPlatformerCharacterState
    {
        public bool HasJumpedWhileSwimming;
        public bool HasDetectedGrounding;
        public bool ShouldExitSwimming;

        private const float kDistanceFromSurfaceToAllowJumping = -0.05f;
        private const float kForcedDistanceFromSurface = 0.01f;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.SwimmingGeometry.ToCapsuleGeometry());

            p.CharacterBody.SnapToGround = false;
            p.CharacterBody.Unground();

            HasJumpedWhileSwimming = false;
            ShouldExitSwimming = false;
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());

            p.CharacterBody.SnapToGround = true;
        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            HasDetectedGrounding = false;
            p.CharacterGroundingAndParentMovementUpdate();
            HasDetectedGrounding = p.CharacterBody.IsGrounded;
            p.CharacterBody.Unground();

            HandleCharacterControl(ref p);

            p.CharacterMovementAndFinalizationUpdate(false);

            PostMovementUpdate(ref p);

            if (!DetectTransitions(ref p))
            {
                p.DetectGlobalTransitions();
            }
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            if (DetectWaterZones(ref p, out p.PlatformerCharacter.DirectionToWaterSurface, out p.PlatformerCharacter.DistanceFromWaterSurface))
            {
                // Movement
                float3 addedMoveVector = float3.zero;
                if (p.PlatformerCharacter.DistanceFromWaterSurface > p.PlatformerCharacter.SwimmingStandUpDistanceFromSurface)
                {
                    // When close to water surface, prevent moving down unless the input points strongly down
                    float dotMoveDirectionWithSurface = math.dot(math.normalizesafe(p.CharacterInputs.WorldMoveVector), p.PlatformerCharacter.DirectionToWaterSurface);
                    if (dotMoveDirectionWithSurface > p.PlatformerCharacter.SwimmingSurfaceDiveThreshold)
                    {
                        p.CharacterInputs.WorldMoveVector = MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.PlatformerCharacter.DirectionToWaterSurface);
                    }

                    // Add an automatic move towards surface
                    addedMoveVector = p.PlatformerCharacter.DirectionToWaterSurface * 0.1f;
                }
                float3 acceleration = (p.CharacterInputs.WorldMoveVector + addedMoveVector) * p.PlatformerCharacter.SwimmingAcceleration;
                CharacterControlUtilities.StandardAirMove(ref p.CharacterBody.RelativeVelocity, acceleration, p.PlatformerCharacter.SwimmingMaxSpeed, -MathUtilities.GetForwardFromRotation(p.Rotation), p.DeltaTime, true);

                // Water drag
                CharacterControlUtilities.ApplyDragToVelocity(ref p.CharacterBody.RelativeVelocity, p.DeltaTime, p.PlatformerCharacter.SwimmingDrag);

                // Orientation
                if (p.PlatformerCharacter.DistanceFromWaterSurface > p.PlatformerCharacter.SwimmingStandUpDistanceFromSurface)
                {
                    // when close to surface, orient self up
                    float3 upPlane = -math.normalizesafe(p.CustomGravity.Gravity);
                    float3 targetForward = default;
                    if (math.lengthsq(p.CharacterInputs.WorldMoveVector) > 0f)
                    {
                        targetForward = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, upPlane));
                    }
                    else
                    {
                        targetForward = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(p.Rotation), upPlane));
                        if (math.dot(p.GroundingUp, upPlane) < 0f)
                        {
                            targetForward = -targetForward;
                        }
                    }
                    quaternion targetRotation = MathUtilities.CreateRotationWithUpPriority(upPlane, targetForward);
                    targetRotation = math.slerp(p.Rotation, targetRotation, MathUtilities.GetSharpnessInterpolant(p.PlatformerCharacter.SwimmingRotationSharpness, p.DeltaTime));
                    MathUtilities.SetRotationAroundPoint(ref p.Rotation, ref p.Translation, GetGeometryCenter(ref p), targetRotation);
                }
                else
                {
                    if (math.lengthsq(p.CharacterInputs.WorldMoveVector) > 0f)
                    {
                        // Make character up face the movement direction, and character forward face gravity direction as much as it can
                        quaternion targetRotation = MathUtilities.CreateRotationWithUpPriority(math.normalizesafe(p.CharacterInputs.WorldMoveVector), math.normalizesafe(p.CustomGravity.Gravity));
                        targetRotation = math.slerp(p.Rotation, targetRotation, MathUtilities.GetSharpnessInterpolant(p.PlatformerCharacter.SwimmingRotationSharpness, p.DeltaTime));
                        MathUtilities.SetRotationAroundPoint(ref p.Rotation, ref p.Translation, GetGeometryCenter(ref p), targetRotation);
                    }
                }

                // Handle jumping out of water when close to water surface
                HasJumpedWhileSwimming = false;
                if (p.CharacterInputs.JumpPressed && p.PlatformerCharacter.DistanceFromWaterSurface > kDistanceFromSurfaceToAllowJumping)
                {
                    CharacterControlUtilities.StandardJump(ref p.CharacterBody, p.GroundingUp * p.PlatformerCharacter.SwimmingJumpSpeed, true, p.GroundingUp);
                    HasJumpedWhileSwimming = true;
                }
            }
            else
            {
                ShouldExitSwimming = true;
            }
        }

        public void PostMovementUpdate(ref PlatformerCharacterProcessor p)
        {
            bool determinedHasExitedWater = false;

            if (DetectWaterZones(ref p, out p.PlatformerCharacter.DirectionToWaterSurface, out p.PlatformerCharacter.DistanceFromWaterSurface))
            {
                // Handle snapping to water surface when trying to swim out of the water
                if (p.PlatformerCharacter.DistanceFromWaterSurface > -kForcedDistanceFromSurface)
                {
                    float currentDistanceToTargetDistance = -kForcedDistanceFromSurface - p.PlatformerCharacter.DistanceFromWaterSurface;
                    float3 translationSnappedToWaterSurface = p.Translation + (p.PlatformerCharacter.DirectionToWaterSurface * currentDistanceToTargetDistance);

                    // Only snap to water surface if we're not jumping out of the water, or if we'd be obstructed when trying to snap back (allows us to walk out of water)
                    if (HasJumpedWhileSwimming || p.CharacterBody.GroundHit.Entity != Entity.Null)
                    {
                        determinedHasExitedWater = true;
                    }
                    else
                    {
                        // Snap position bact to water surface
                        p.Translation = translationSnappedToWaterSurface;

                        // Project velocity on water surface normal
                        p.CharacterBody.RelativeVelocity = MathUtilities.ProjectOnPlane(p.CharacterBody.RelativeVelocity, p.PlatformerCharacter.DirectionToWaterSurface);
                    }
                }
            }

            ShouldExitSwimming = determinedHasExitedWater;
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if(ShouldExitSwimming || HasDetectedGrounding)
            {
                if (HasDetectedGrounding)
                {
                    p.TransitionToState(CharacterState.GroundMove);
                    return true;
                }
                else
                {
                    p.TransitionToState(CharacterState.AirMove);
                    return true;
                }
            }

            return false;
        }

        public static float3 GetGeometryCenter(ref PlatformerCharacterProcessor p)
        {
            RigidTransform characterTransform = new RigidTransform(p.Rotation, p.Translation);
            float3 geometryCenter = math.transform(characterTransform, p.PlatformerCharacter.SwimmingGeometry.Center);
            return geometryCenter;
        }

        public static bool DetectWaterZones(ref PlatformerCharacterProcessor p, out float3 directionToWaterSurface, out float waterSurfaceDistance)
        {
            directionToWaterSurface = default;
            waterSurfaceDistance = 0f;

            RigidTransform characterRigidTransform = new RigidTransform(p.Rotation, p.Translation);
            float3 swimmingDetectionPointWorldPosition = math.transform(characterRigidTransform, p.TranslationFromEntity[p.PlatformerCharacter.SwimmingDetectionPointEntity].Value);
            CollisionFilter waterDetectionFilter = new CollisionFilter
            {
                BelongsTo = p.PhysicsCollider.Value.Value.Filter.BelongsTo,
                CollidesWith = p.PlatformerCharacter.WaterPhysicsCategory.Value,
            };

            PointDistanceInput pointInput = new PointDistanceInput
            {
                Filter = waterDetectionFilter,
                MaxDistance = p.PlatformerCharacter.WaterDetectionDistance,
                Position = swimmingDetectionPointWorldPosition,
            };

            if (p.CollisionWorld.CalculateDistance(pointInput, out DistanceHit closestHit))
            {
                directionToWaterSurface = closestHit.SurfaceNormal; // always goes in the direction of decolliding from the target collider
                waterSurfaceDistance = closestHit.Distance; // positive means above surface
                return true;
            }

            return false;
        }
    }
}