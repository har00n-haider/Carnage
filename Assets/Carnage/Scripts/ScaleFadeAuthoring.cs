using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[DisallowMultipleComponent]
public class ScaleFadeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public ScaleFade ScaleFade;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, ScaleFade);
        dstManager.AddComponent<NonUniformScale>(entity);
    }
}
