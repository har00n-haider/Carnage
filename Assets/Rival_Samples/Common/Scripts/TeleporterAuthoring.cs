using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples
{
    [DisallowMultipleComponent]
    public class TeleporterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public GameObject Destination;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Entity destinationEntity = conversionSystem.GetPrimaryEntity(Destination);

            dstManager.AddComponentData(entity, new Teleporter { DestinationEntity = destinationEntity });
        }
    }
}