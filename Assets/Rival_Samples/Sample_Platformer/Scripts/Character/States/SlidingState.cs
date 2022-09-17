using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct SlidingState : IPlatformerCharacterState
    {
        private bool _preventInAir;
        private bool _detectedGrounded;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.SlidingGeometry.ToCapsuleGeometry());
            p.CharacterBody.SnapToGround = false;
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());
            p.CharacterBody.SnapToGround = true;

            p.RotationFromEntity[p.PlatformerCharacter.MeshRootEntity] = new Rotation { Value = quaternion.identity };
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
            // Stick to ground
            _preventInAir = false;
            _detectedGrounded = true;
            KinematicCharacterUtilities.GroundDetection(
                ref p,
                in p.CharacterBody,
                in p.PhysicsCollider,
                p.Entity,
                p.Translation,
                p.Rotation,
                p.GroundingUp,
                p.PlatformerCharacter.SlidingSnapDistance,
                out bool isGrounded,
                out BasicHit groundHit,
                out float distanceToGround);
            if(groundHit.Entity != Entity.Null)
            {
                _preventInAir = true;

                _detectedGrounded = isGrounded;

                // Orient mesh to ground
                quaternion targetRotation = quaternion.LookRotationSafe(-math.normalizesafe(MathUtilities.ProjectOnPlane(-p.GroundingUp, groundHit.Normal)), p.GroundingUp);
                targetRotation = math.slerp(math.mul(p.Rotation, p.RotationFromEntity[p.PlatformerCharacter.MeshRootEntity].Value), targetRotation, MathUtilities.GetSharpnessInterpolant(p.PlatformerCharacter.SlidingTransitionSharpness, p.DeltaTime));

                p.RotationFromEntity[p.PlatformerCharacter.MeshRootEntity] = new Rotation { Value = math.mul(math.inverse(p.Rotation), targetRotation) };
            }

            // Velocity
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, p.CustomGravity.Gravity, p.DeltaTime);
            p.CharacterBody.RelativeVelocity = MathUtilities.ProjectOnPlane(p.CharacterBody.RelativeVelocity, p.CharacterBody.GroundHit.Normal);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref p.CharacterBody.RelativeVelocity, p.DeltaTime, p.PlatformerCharacter.SlidingDrag);

            // Orientation
            p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.CharacterInputs.RollHeld)
            {
                p.TransitionToState(CharacterState.Rolling);
                return true;
            }

            if (_detectedGrounded || p.CharacterBody.IsGrounded)
            {
                p.TransitionToState(CharacterState.GroundMove);
                return true;
            }

            if (!_preventInAir && !p.CharacterBody.IsGrounded && p.CharacterBody.GroundHit.Entity == Entity.Null)
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            return false;
        }

        public static bool ShouldBeSliding(ref PlatformerCharacterProcessor p)
        {
            // TODO: disabled until further improvements
            return false;

            //return !p.CharacterBody.IsGrounded &&
            //    p.CharacterBody.GroundHit.Entity != Entity.Null &&
            //    math.dot(p.CharacterUp, p.CharacterBody.GroundHit.Normal) > p.PlatformerCharacter.SlidingMaxDotRatio &&
            //    math.dot(p.CharacterBody.RelativeVelocity, p.CharacterBody.GroundHit.Normal) < 0f;
        }
    }
}