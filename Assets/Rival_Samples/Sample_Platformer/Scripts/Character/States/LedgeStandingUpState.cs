using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct LedgeStandingUpState : IPlatformerCharacterState
    {
        private bool ShouldExitState;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.RelativeVelocity = default;
            p.CharacterBody.Unground();

            p.CharacterBody.SetCollisionDetectionActive(false);

            ShouldExitState = false;
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.SetCollisionDetectionActive(true);

            p.CharacterBody.ParentEntity = Entity.Null;
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
            // Let the ledge stand up animation clip drive our position with root motion
            RigidTransform characterRigidTransform = math.RigidTransform(p.Rotation, p.Translation);
            // TODO: root motion
            //float3 positionDeltaFromRootMotion = math.rotate(characterRigidTransform, d.PlatformerCharacter.AccumulatedRootMotionDelta.pos);
            //d.Translation += positionDeltaFromRootMotion;

            p.Translation = math.transform(characterRigidTransform, p.TranslationFromEntity[p.PlatformerCharacter.LedgeDetectionPointEntity].Value);
            ShouldExitState = true;
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            // TODO: root motion
            //if (d.SimpleAnimation.GetNormalizedTime(d.PlatformerCharacterAnimation.LedgeStandUpClip, ref d.SimpleAnimationClipDatas) >= 1f)
            if (ShouldExitState)
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