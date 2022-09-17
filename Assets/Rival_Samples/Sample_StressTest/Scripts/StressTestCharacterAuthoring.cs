using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;

namespace Rival.Samples.StressTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsShapeAuthoring))]
    public class StressTestCharacterAuthoring : MonoBehaviour
    {
        public StressTestCharacterComponent StressTestCharacter;
        public AuthoringKinematicCharacterBody CharacterProperties = AuthoringKinematicCharacterBody.GetDefault();
    }

    [UpdateAfter(typeof(EndColliderConversionSystem))]
    public class StressTestCharacterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((StressTestCharacterAuthoring authoring) =>
            {
                Entity entity = GetPrimaryEntity(authoring.gameObject);

                KinematicCharacterUtilities.HandleConversionForCharacter(DstEntityManager, entity, authoring.gameObject, authoring.CharacterProperties);

                DstEntityManager.AddComponentData(entity, authoring.StressTestCharacter);
                DstEntityManager.AddComponentData(entity, new StressTestCharacterInputs());
            });
        }
    }
}
