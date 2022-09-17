using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct ClimbingState : IPlatformerCharacterState
    {
        public float3 LastKnownClimbNormal;

        private bool _foundValidClimbSurface;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.ClimbingGeometry.ToCapsuleGeometry());

            p.CharacterBody.SetCollisionDetectionActive(false);
            p.CharacterBody.Unground();

            LastKnownClimbNormal = -MathUtilities.GetForwardFromRotation(p.Rotation);
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());

            p.CharacterBody.ParentEntity = Entity.Null;
            p.CharacterBody.SetCollisionDetectionActive(true);
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
            float3 geometryCenter = GetGeometryCenter(ref p);

            // Quad climbing surface detection raycasts
            _foundValidClimbSurface = false;
            if (ClimbingDetection(ref p, true, out float3 avgClimbingSurfaceNormal, out DistanceHit closestClimbableHit, out DistanceHit closestUnclimbableHit))
            {
                LastKnownClimbNormal = avgClimbingSurfaceNormal;
                _foundValidClimbSurface = true;
                p.CharacterBody.ParentEntity = closestClimbableHit.Entity;

                // Adjust distance of character to surface
                p.Translation += -closestClimbableHit.Distance * closestClimbableHit.SurfaceNormal;
                p.Translation += (p.PlatformerCharacter.ClimbingGeometry.Radius - p.PlatformerCharacter.ClimbingDistanceFromSurface) * -closestClimbableHit.SurfaceNormal;

                // decollide from most penetrating non-climbable hit
                if (closestUnclimbableHit.Entity != Entity.Null)
                {
                    p.Translation += -closestUnclimbableHit.Distance * closestUnclimbableHit.SurfaceNormal;
                }

                // Move
                float3 climbMoveVector = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, avgClimbingSurfaceNormal)) * math.length(p.CharacterInputs.WorldMoveVector);
                float3 targetVelocity = climbMoveVector * p.PlatformerCharacter.ClimbingSpeed;
                CharacterControlUtilities.InterpolateVelocityTowardsTarget(ref p.CharacterBody.RelativeVelocity, targetVelocity, p.DeltaTime, p.PlatformerCharacter.ClimbingMovementSharpness);
                p.CharacterBody.RelativeVelocity = MathUtilities.ProjectOnPlane(p.CharacterBody.RelativeVelocity, avgClimbingSurfaceNormal);

                // Project velocity on non-climbable obstacles
                if (p.VelocityProjectionHitsBuffer.Length > 0)
                {
                    bool tmpCharacterGrounded = false;
                    BasicHit tmpCharacterGroundHit = default;
                    p.ProjectVelocityOnHits(
                        ref p.CharacterBody.RelativeVelocity,
                        ref tmpCharacterGrounded,
                        ref tmpCharacterGroundHit,
                        in p.VelocityProjectionHitsBuffer,
                        math.normalizesafe(p.CharacterBody.RelativeVelocity));
                }

                // Rotate
                float3 targetCharacterUp = p.GroundingUp;
                if (math.lengthsq(p.CharacterInputs.WorldMoveVector) > 0f)
                {
                    targetCharacterUp = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, avgClimbingSurfaceNormal));
                }
                quaternion targetRotation = quaternion.LookRotationSafe(-avgClimbingSurfaceNormal, targetCharacterUp); 
                quaternion smoothedRotation = math.slerp(p.Rotation, targetRotation, MathUtilities.GetSharpnessInterpolant(p.PlatformerCharacter.ClimbingRotationSharpness, p.DeltaTime));
                MathUtilities.SetRotationAroundPoint(ref p.Rotation, ref p.Translation, geometryCenter, smoothedRotation);
            }
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if(!_foundValidClimbSurface || p.CharacterInputs.JumpPressed || p.CharacterInputs.DashPressed || p.CharacterInputs.ClimbPressed)
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            return false;
        }

        public static float3 GetGeometryCenter(ref PlatformerCharacterProcessor p)
        {
            RigidTransform characterTransform = new RigidTransform(p.Rotation, p.Translation);
            float3 geometryCenter = math.transform(characterTransform, math.up() * p.PlatformerCharacter.ClimbingGeometry.Height * 0.5f);
            return geometryCenter;
        }

        public static bool CanStartClimbing(ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.ClimbingGeometry.ToCapsuleGeometry());
            bool canStart = ClimbingDetection(ref p, false, out float3 avgClimbingSurfaceNormal, out DistanceHit closestClimbableHit, out DistanceHit closestUnclimbableHit);
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());

            return canStart;
        }

        public static bool ClimbingDetection(
            ref PlatformerCharacterProcessor p,
            bool addUnclimbableHitsAsVelocityProjectionHits,
            out float3 avgClimbingSurfaceNormal,
            out DistanceHit closestClimbableHit,
            out DistanceHit closestUnclimbableHit)
        {
            int climbableNormalsCounter = 0;
            avgClimbingSurfaceNormal = default;
            closestClimbableHit = default;
            closestUnclimbableHit = default;

            KinematicCharacterUtilities.CalculateDistanceAllCollisions(
                ref p,
                in p.PhysicsCollider,
                p.Entity,
                p.Translation,
                p.Rotation,
                0f,
                p.CharacterBody.ShouldIgnoreDynamicBodies(),
                out p.TmpDistanceHits);

            if (p.TmpDistanceHits.Length > 0)
            {
                closestClimbableHit.Fraction = float.MaxValue;
                closestUnclimbableHit.Fraction = float.MaxValue;

                for (int i = 0; i < p.TmpDistanceHits.Length; i++)
                {
                    DistanceHit tmpHit = p.TmpDistanceHits[i];

                    float3 faceNormal = tmpHit.SurfaceNormal;

                    // This is necessary for cases where the detected hit is the edge of a triangle/plane
                    if (PhysicsUtilities.GetHitFaceNormal(p.CollisionWorld.Bodies[tmpHit.RigidBodyIndex], tmpHit.ColliderKey, out float3 tmpFaceNormal))
                    {
                        faceNormal = tmpFaceNormal;
                    }

                    // Ignore back faces
                    if (math.dot(faceNormal, tmpHit.SurfaceNormal) > KinematicCharacterUtilities.Constants.DotProductSimilarityEpsilon)
                    {
                        bool isClimbable = false;
                        if (p.PlatformerCharacter.ClimbableTag.Value > CustomPhysicsBodyTags.Nothing.Value)
                        {
                            if ((p.CollisionWorld.Bodies[tmpHit.RigidBodyIndex].CustomTags & p.PlatformerCharacter.ClimbableTag.Value) > 0)
                            {
                                isClimbable = true;
                            }
                        }

                        // Add virtual velocityProjection hit in direction of unclimbable hit
                        if (isClimbable)
                        {
                            if (tmpHit.Fraction < closestClimbableHit.Fraction)
                            {
                                closestClimbableHit = tmpHit;
                            }

                            avgClimbingSurfaceNormal += faceNormal;
                            climbableNormalsCounter++;
                        }
                        else
                        {
                            if (tmpHit.Fraction < closestUnclimbableHit.Fraction)
                            {
                                closestUnclimbableHit = tmpHit;
                            }

                            if (addUnclimbableHitsAsVelocityProjectionHits)
                            {
                                KinematicVelocityProjectionHit velProjHit = new KinematicVelocityProjectionHit(new BasicHit(tmpHit), false);
                                p.VelocityProjectionHitsBuffer.Add(velProjHit);
                            }
                        }
                    }
                }

                if (climbableNormalsCounter > 0)
                {
                    avgClimbingSurfaceNormal = avgClimbingSurfaceNormal / climbableNormalsCounter;

                    return true;
                }

                return false;
            }

            return false;
        }
    }
}