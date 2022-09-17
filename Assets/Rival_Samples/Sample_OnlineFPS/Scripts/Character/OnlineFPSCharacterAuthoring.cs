using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;

namespace Rival.Samples.OnlineFPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsShapeAuthoring))]
    public class OnlineFPSCharacterAuthoring : MonoBehaviour
    {
        public GameObject View;
        public GameObject MeshRoot;
        public GameObject WeaponSocket;

        public OnlineFPSCharacterComponent OnlineFPSCharacter;
        public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    }

    [UpdateAfter(typeof(EndColliderConversionSystem))]
    public class BasicKinematicCharacterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((OnlineFPSCharacterAuthoring authoring) =>
            {
                Entity entity = GetPrimaryEntity(authoring.gameObject);

                KinematicCharacterUtilities.HandleConversionForCharacter(DstEntityManager, entity, authoring.gameObject, authoring.CharacterBody);

                authoring.OnlineFPSCharacter.ViewEntity = GetPrimaryEntity(authoring.View);
                authoring.OnlineFPSCharacter.MeshRootEntity = GetPrimaryEntity(authoring.MeshRoot);
                authoring.OnlineFPSCharacter.WeaponSocketEntity = GetPrimaryEntity(authoring.WeaponSocket);

                DstEntityManager.AddComponentData(entity, authoring.OnlineFPSCharacter);
                DstEntityManager.AddComponentData(entity, new OnlineFPSCharacterInputs());
                DstEntityManager.AddComponentData(entity, new ActiveWeapon());
            });
        }
    }
}
