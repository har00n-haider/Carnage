using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct DashingState : IPlatformerCharacterState
    {
        private float _dashStartTime;
        private float3 _dashDirection;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            _dashStartTime = p.ElapsedTime;
            p.CharacterBody.EvaluateGrounding = false;

            float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
            if (math.lengthsq(moveVectorOnPlane) > 0f)
            {
                _dashDirection = math.normalizesafe(moveVectorOnPlane);
            }
            else
            {
                float3 meshRootForward = MathUtilities.GetForwardFromRotation(p.RotationFromEntity[p.PlatformerCharacter.MeshRootEntity].Value);
                _dashDirection = meshRootForward;

                _dashDirection = MathUtilities.GetForwardFromRotation(p.Rotation);
            }
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.EvaluateGrounding = true;
            p.CharacterBody.RelativeVelocity = float3.zero;
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
            p.CharacterBody.RelativeVelocity = _dashDirection * p.PlatformerCharacter.DashSpeed;
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.ElapsedTime > _dashStartTime + p.PlatformerCharacter.DashDuration)
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

            return false;
        }
    }
}