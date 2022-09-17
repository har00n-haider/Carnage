using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct RollingState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.RollingGeometry.ToCapsuleGeometry());
            p.CharacterBody.EvaluateGrounding = false;
            p.CharacterBody.Unground();

            PlatformerUtilities.SetEntityHierarchyEnabledParallel(true, p.PlatformerCharacter.RollballMeshEntity, p.CommandBuffer, p.IndexInChunk, p.LinkedEntityGroupFromEntity);
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.SetCapsuleGeometry(p.PlatformerCharacter.StandingGeometry.ToCapsuleGeometry());
            p.CharacterBody.EvaluateGrounding = true;

            PlatformerUtilities.SetEntityHierarchyEnabledParallel(false, p.PlatformerCharacter.RollballMeshEntity, p.CommandBuffer, p.IndexInChunk, p.LinkedEntityGroupFromEntity);
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
            // Movement
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, p.CharacterInputs.WorldMoveVector * p.PlatformerCharacter.RollingAcceleration, p.DeltaTime);
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, p.CustomGravity.Gravity, p.DeltaTime);

            // Orientation
            p.OrientCharacterUpTowardsDirection(-math.normalizesafe(p.CustomGravity.Gravity), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (!p.CharacterInputs.RollHeld && p.CanStandUp())
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            return false;
        }
    }
}