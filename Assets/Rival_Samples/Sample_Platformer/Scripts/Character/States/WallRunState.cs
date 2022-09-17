using Unity.Mathematics;
using Unity.Physics;

namespace Rival.Samples.Platformer
{
    public struct WallRunState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {

        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {

        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            p.CharacterGroundingAndParentMovementUpdate();

            HandleCharacterControl(ref p);

            p.CharacterMovementAndFinalizationUpdate(true);

            if (!DetectTransitions(ref p))
            {
                p.DetectGlobalTransitions();
            }
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            // Detect if still moving against ungrounded surface
            if (p.DetectUngroundedHits(-p.PlatformerCharacter.LastKnownWallNormal * p.PlatformerCharacter.WallRunDetectionDistance, out ColliderCastHit detectedHit))
            {
                p.PlatformerCharacter.HasDetectedMoveAgainstWall = true;
                p.PlatformerCharacter.LastKnownWallNormal = detectedHit.SurfaceNormal;
            }
            else
            {
                p.PlatformerCharacter.LastKnownWallNormal = default;
            }

            if (p.PlatformerCharacter.HasDetectedMoveAgainstWall)
            {
                float3 constrainedMoveDirection = math.normalizesafe(math.cross(p.PlatformerCharacter.LastKnownWallNormal, p.GroundingUp));

                float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
                float3 acceleration = moveVectorOnPlane * p.PlatformerCharacter.WallRunAcceleration;
                acceleration = math.projectsafe(acceleration, constrainedMoveDirection);
                CharacterControlUtilities.StandardAirMove(ref p.CharacterBody.RelativeVelocity, acceleration, p.PlatformerCharacter.WallRunMaxSpeed, p.GroundingUp, p.DeltaTime, false);

                // Jumping
                if (p.PlatformerCharacter.HasDetectedMoveAgainstWall &&
                    p.CharacterInputs.JumpPressed)
                {
                    float3 jumpDirection = math.normalizesafe(math.lerp(p.GroundingUp, p.PlatformerCharacter.LastKnownWallNormal, p.PlatformerCharacter.WallRunJumpRatioFromCharacterUp));
                    CharacterControlUtilities.StandardJump(ref p.CharacterBody, jumpDirection * p.PlatformerCharacter.WallRunJumpSpeed, true, jumpDirection);
                }
                if (p.PlatformerCharacter.HeldJumpValid)
                {
                    p.CharacterBody.RelativeVelocity += p.GroundingUp * p.PlatformerCharacter.JumpHeldAcceleration * p.DeltaTime;
                }
            }

            // Gravity
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, (p.CustomGravity.Gravity * p.PlatformerCharacter.WallRunGravityFactor), p.DeltaTime);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref p.CharacterBody.RelativeVelocity, p.DeltaTime, p.PlatformerCharacter.WallRunDrag);

            // Orientation
            if (p.PlatformerCharacter.HasDetectedMoveAgainstWall)
            {
                float3 rotationDirection = math.normalizesafe(math.cross(p.PlatformerCharacter.LastKnownWallNormal, p.GroundingUp));
                if(math.dot(rotationDirection, p.CharacterBody.RelativeVelocity) < 0f)
                {
                    rotationDirection *= -1f;
                }
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref p.Rotation, p.DeltaTime, rotationDirection, p.GroundingUp, p.PlatformerCharacter.GroundedRotationSharpness);
            }
            p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.CharacterInputs.RollHeld)
            {
                p.TransitionToState(CharacterState.Rolling);
                return true;
            }
            
            if (p.CharacterInputs.DashPressed)
            {
                p.TransitionToState(CharacterState.Dashing);
                return true;
            }
            
            if (p.CharacterBody.IsGrounded)
            {
                p.TransitionToState(CharacterState.GroundMove);
                return true;
            }

            if (!p.PlatformerCharacter.HasDetectedMoveAgainstWall)
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            return false;
        }
    }
}