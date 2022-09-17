using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct DisabledNoCollisionsState : IPlatformerCharacterState
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
            p.DetectGlobalTransitions();
        }
    }
}