using Unity.Mathematics;
using Unity.Physics;
using Unity.Entities;

namespace Rival.Samples.Platformer
{
    public struct AirMoveState : IPlatformerCharacterState
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

            if(!DetectTransitions(ref p))
            {
                p.DetectGlobalTransitions();
            }
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            // Detect ungrounded walls
            float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
            float3 acceleration = moveVectorOnPlane * p.PlatformerCharacter.AirAcceleration;
            float3 displacementFromAcceleration = acceleration * p.DeltaTime * p.DeltaTime;
            if (math.lengthsq(displacementFromAcceleration) > 0f)
            {
                if (p.DetectUngroundedHits(displacementFromAcceleration, out ColliderCastHit detectedHit))
                {
                    p.PlatformerCharacter.HasDetectedMoveAgainstWall = true;
                    p.PlatformerCharacter.LastKnownWallNormal = detectedHit.SurfaceNormal;
                }
            }

            // Movement
            CharacterControlUtilities.StandardAirMove(ref p.CharacterBody.RelativeVelocity, acceleration, p.PlatformerCharacter.AirMaxSpeed, p.GroundingUp, p.DeltaTime, false);

            // Jumping
            if (p.CharacterInputs.JumpPressed)
            {
                bool canJumpForAfterUngroundedGraceTime = p.ElapsedTime < p.PlatformerCharacter.LastTimeWasGrounded + p.PlatformerCharacter.JumpAfterUngroundedGraceTime;
                if (p.PlatformerCharacter.JumpAfterUngroundedAvailable && canJumpForAfterUngroundedGraceTime)
                {
                    CharacterControlUtilities.StandardJump(ref p.CharacterBody, p.GroundingUp * p.PlatformerCharacter.GroundJumpSpeed, true, p.GroundingUp);
                    p.PlatformerCharacter.HeldJumpTimeCounter = 0f;
                }
                else if (p.PlatformerCharacter.CurrentUngroundedJumps < p.PlatformerCharacter.MaxUngroundedJumps)
                {
                    CharacterControlUtilities.StandardJump(ref p.CharacterBody, p.GroundingUp * p.PlatformerCharacter.AirJumpSpeed, true, p.GroundingUp);
                    p.PlatformerCharacter.CurrentUngroundedJumps++;
                }
                else
                {
                    p.PlatformerCharacter.RequestedJumpBeforeGrounded = true;
                }

                p.PlatformerCharacter.JumpAfterUngroundedAvailable = false;
            }
            if (p.PlatformerCharacter.HeldJumpValid)
            {
                p.CharacterBody.RelativeVelocity += p.GroundingUp * p.PlatformerCharacter.JumpHeldAcceleration * p.DeltaTime;
            }

            // Gravity
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, p.CustomGravity.Gravity, p.DeltaTime);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref p.CharacterBody.RelativeVelocity, p.DeltaTime, p.PlatformerCharacter.AirDrag);

            // Orientation
            p.OrientCharacterOnPlaneTowardsMoveInput(p.PlatformerCharacter.AirRotationSharpness);
            p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.CharacterInputs.RopePressed && RopeSwingState.DetectRopePoints(ref p, out float3 detectedRopeAnchorPoint))
            {
                p.PlatformerCharacterStateMachine.RopeSwingState.AnchorPoint = detectedRopeAnchorPoint;
                p.TransitionToState(CharacterState.RopeSwing);
                return true;
            }

            if (p.CharacterInputs.RollHeld)
            {
                p.TransitionToState(CharacterState.Rolling);
                return true;
            }

            if (SlidingState.ShouldBeSliding(ref p))
            {
                p.TransitionToState(CharacterState.Sliding);
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

            if (p.CharacterInputs.SprintHeld && p.PlatformerCharacter.HasDetectedMoveAgainstWall)
            {
                p.TransitionToState(CharacterState.WallRun);
                return true;
            }

            if (LedgeGrabState.CanGrabLedge(ref p, out Entity ledgeEntity))
            {
                p.TransitionToState(CharacterState.LedgeGrab);
                p.CharacterBody.ParentEntity = ledgeEntity;
                return true;
            }

            if (p.CharacterInputs.ClimbPressed)
            {
                if (ClimbingState.CanStartClimbing(ref p))
                {
                    p.TransitionToState(CharacterState.Climbing);
                    return true;
                }
            }

            return false;
        }
    }
}