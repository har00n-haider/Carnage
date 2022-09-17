using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct CrouchedState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.CrouchingGeometry.ToCapsuleGeometry());
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.PlatformerCharacter.IsOnStickySurface = false;
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());
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
            // Move on ground
            float chosenMaxSpeed = p.PlatformerCharacter.CrouchedMaxSpeed;
            float chosenSharpness = p.PlatformerCharacter.CrouchedMovementSharpness;
            if (p.CharacterFrictionModifierFromEntity.HasComponent(p.CharacterBody.GroundHit.Entity))
            {
                chosenSharpness *= p.CharacterFrictionModifierFromEntity[p.CharacterBody.GroundHit.Entity].Friction;
            }
            float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
            float3 targetVelocity = moveVectorOnPlane * chosenMaxSpeed;
            CharacterControlUtilities.StandardGroundMove_Interpolated(ref p.CharacterBody.RelativeVelocity, targetVelocity, chosenSharpness, p.DeltaTime, p.GroundingUp, p.CharacterBody.GroundHit.Normal);

            p.PlatformerCharacter.IsOnStickySurface = false;
            p.OrientCharacterOnPlaneTowardsMoveInput(p.PlatformerCharacter.CrouchedRotationSharpness);
            if ((p.CollisionWorld.Bodies[p.CharacterBody.GroundHit.RigidBodyIndex].CustomTags & p.PlatformerCharacter.StickySurfaceTag.Value) > 0)
            {
                p.PlatformerCharacter.IsOnStickySurface = true;
                p.OrientCharacterUpTowardsDirection(p.CharacterBody.GroundHit.Normal, p.PlatformerCharacter.UpOrientationAdaptationSharpness);
            }
            else if (math.dot(p.GroundingUp, math.up()) < 1f)
            {
                p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
            }
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.CharacterInputs.CrouchPressed)
            {
                if(p.CanStandUp())
                {
                    if (p.CharacterBody.IsGrounded)
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
            }

            if (SlidingState.ShouldBeSliding(ref p))
            {
                p.TransitionToState(CharacterState.Sliding);
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

            return false;
        }
    }
}