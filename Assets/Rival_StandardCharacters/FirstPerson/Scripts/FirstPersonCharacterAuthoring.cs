using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
[UpdateAfter(typeof(EndColliderConversionSystem))]
public class FirstPersonCharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Transform CharacterViewTransform;
    public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    public FirstPersonCharacterComponent FirstPersonCharacter = FirstPersonCharacterComponent.GetDefault();

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (CharacterViewTransform == null)
        {
            UnityEngine.Debug.LogError("ERROR: the CharacterViewTransform must not be null. You must assign a 1st-level child object of the character to this field (the object that represents the camera point). Conversion will be aborted");
            return;
        }
        if (CharacterViewTransform.parent != this.transform)
        {
            UnityEngine.Debug.LogError("ERROR: the CharacterViewTransform must be a direct 1st-level child of the character authoring GameObject. Conversion will be aborted");
            return;
        }

        KinematicCharacterUtilities.HandleConversionForCharacter(dstManager, entity, gameObject, CharacterBody);

        FirstPersonCharacter.CharacterViewEntity = conversionSystem.GetPrimaryEntity(CharacterViewTransform.gameObject);

        dstManager.AddComponentData(entity, FirstPersonCharacter);
        dstManager.AddComponentData(entity, new FirstPersonCharacterInputs());
    }
}
