using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Basic
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsShapeAuthoring))]
    public class BasicCharacterAuthoring : MonoBehaviour
    {
        public BasicCharacterComponent BasicCharacter = BasicCharacterComponent.GetDefault();
        public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    }
    
    [UpdateAfter(typeof(EndColliderConversionSystem))]
    public class BasicCharacterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BasicCharacterAuthoring authoring) =>
            {
                Entity entity = GetPrimaryEntity(authoring.gameObject);
    
                KinematicCharacterUtilities.HandleConversionForCharacter(DstEntityManager, entity, authoring.gameObject, authoring.CharacterBody);

                DstEntityManager.AddComponentData(entity, authoring.BasicCharacter);
                DstEntityManager.AddComponentData(entity, new BasicCharacterInputs());
            });
        }
    }
}
