using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct GroundMoveState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {

        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.PlatformerCharacter.IsOnStickySurface = false;
            p.PlatformerCharacter.IsSprinting = false;
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
            p.GroundingUp = MathUtilities.GetUpFromRotation(p.Rotation);

            if (p.CharacterBody.IsGrounded)
            {
                p.PlatformerCharacter.IsSprinting = p.CharacterInputs.SprintHeld;

                // Move on ground
                float chosenMaxSpeed = p.PlatformerCharacter.IsSprinting ? p.PlatformerCharacter.GroundSprintMaxSpeed : p.PlatformerCharacter.GroundRunMaxSpeed;
                float chosenSharpness = p.PlatformerCharacter.GroundedMovementSharpness;
                if (p.CharacterFrictionModifierFromEntity.HasComponent(p.CharacterBody.GroundHit.Entity))
                {
                    chosenSharpness *= p.CharacterFrictionModifierFromEntity[p.CharacterBody.GroundHit.Entity].Friction;
                }
                float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
                float3 targetVelocity = moveVectorOnPlane * chosenMaxSpeed;
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref p.CharacterBody.RelativeVelocity, targetVelocity, chosenSharpness, p.DeltaTime, p.GroundingUp, p.CharacterBody.GroundHit.Normal);

                // Jumping
                p.PlatformerCharacter.CurrentUngroundedJumps = 0;
                p.PlatformerCharacter.JumpAfterUngroundedAvailable = true;
                bool canJumpForBeforeGroundedGraceTime = p.ElapsedTime < p.PlatformerCharacter.LastTimeJumpPressed + p.PlatformerCharacter.JumpBeforeGroundedGraceTime;
                if (p.CharacterInputs.JumpPressed || (p.PlatformerCharacter.RequestedJumpBeforeGrounded && canJumpForBeforeGroundedGraceTime))
                {
                    CharacterControlUtilities.StandardJump(ref p.CharacterBody, p.GroundingUp * p.PlatformerCharacter.GroundJumpSpeed, true, p.GroundingUp);
                    p.PlatformerCharacter.HeldJumpValid = true;
                    p.PlatformerCharacter.JumpAfterUngroundedAvailable = false;
                }
                p.PlatformerCharacter.RequestedJumpBeforeGrounded = false;

                p.PlatformerCharacter.IsOnStickySurface = false;
                p.OrientCharacterOnPlaneTowardsMoveInput(p.PlatformerCharacter.GroundedRotationSharpness);
                if ((p.CollisionWorld.Bodies[p.CharacterBody.GroundHit.RigidBodyIndex].CustomTags & p.PlatformerCharacter.StickySurfaceTag.Value) > 0)
                {
                    p.PlatformerCharacter.IsOnStickySurface = true;
                    p.OrientCharacterUpTowardsDirection(p.CharacterBody.GroundHit.Normal, p.PlatformerCharacter.UpOrientationAdaptationSharpness);
                }
                else if (math.dot(p.GroundingUp, math.up()) < 1f)
                {
                    p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
                }

                p.GroundingUp = MathUtilities.GetUpFromRotation(p.Rotation);
            }
            else
            {
                // This is required to allow one frame of air movement when something wants to unground us, like jump pads
                CharacterControlUtilities.StandardAirMove(ref p.CharacterBody.RelativeVelocity, float3.zero, p.PlatformerCharacter.AirMaxSpeed, p.GroundingUp, p.DeltaTime, false);
            }
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (SlidingState.ShouldBeSliding(ref p))
            {
                p.TransitionToState(CharacterState.Sliding);
                return true;
            }

            if(p.CharacterInputs.CrouchPressed)
            {
                p.TransitionToState(CharacterState.Crouched);
                return true;
            }

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

            if (!p.CharacterBody.IsGrounded)
            {
                p.TransitionToState(CharacterState.AirMove);
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