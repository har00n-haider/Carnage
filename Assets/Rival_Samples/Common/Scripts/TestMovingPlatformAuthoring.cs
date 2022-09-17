using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples
{
    [DisallowMultipleComponent]
    public class TestMovingPlatformAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public TestMovingPlatform MovingPlatform;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            MovingPlatform.OriginalPosition = transform.position;
            MovingPlatform.OriginalRotation = transform.rotation;

            dstManager.AddComponentData(entity, MovingPlatform);
        }
    }
}