using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct FlyingNoCollisionsState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.SetCollisionResponse(CollisionResponsePolicy.None);
            p.CharacterBody.SetCollisionDetectionActive(false);
            p.CharacterBody.Unground();
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.SetCollisionResponse(CollisionResponsePolicy.RaiseTriggerEvents);
            p.CharacterBody.SetCollisionDetectionActive(true);
        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            HandleCharacterControl(ref p);

            p.DetectGlobalTransitions();
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            // Movement
            float verticalInput = (p.CharacterInputs.JumpHeld ? 1f : 0f) + (p.CharacterInputs.RollHeld ? -1f : 0f);
            float3 targetMoveVector = MathUtilities.ClampToMaxLength(p.CharacterInputs.WorldMoveVector + (p.CharacterInputs.UpDirection * verticalInput), 1f);
            float3 targetVelocity = targetMoveVector * p.PlatformerCharacter.FlyingMaxSpeed;
            CharacterControlUtilities.InterpolateVelocityTowardsTarget(ref p.CharacterBody.RelativeVelocity, targetVelocity, p.DeltaTime, p.PlatformerCharacter.FlyingMovementSharpness);
            p.Translation += p.CharacterBody.RelativeVelocity * p.DeltaTime;

            // Orientation
            p.Rotation = quaternion.identity;
        }
    }
}