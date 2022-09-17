using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct DisabledState : IPlatformerCharacterState
    {
        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.SetCollisionDetectionActive(false);
            p.CharacterBody.Unground();
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.SetCollisionDetectionActive(true);
        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            p.DetectGlobalTransitions();
        }
    }
}