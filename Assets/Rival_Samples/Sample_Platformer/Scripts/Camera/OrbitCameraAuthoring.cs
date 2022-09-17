using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [DisallowMultipleComponent]
    public class OrbitCameraAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public OrbitCamera OrbitCamera = OrbitCamera.GetDefault();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, "OrbitCamera");
#endif

            OrbitCamera.CurrentDistanceFromMovement = OrbitCamera.TargetDistance;
            OrbitCamera.CurrentDistanceFromObstruction = OrbitCamera.TargetDistance;
            OrbitCamera.PlanarForward = math.forward();
            OrbitCamera.PreviousParentRotation = quaternion.identity;

            dstManager.AddComponentData(entity, OrbitCamera);
            dstManager.AddComponentData(entity, new PlatformerInputs());
            dstManager.AddBuffer<IgnoredEntityBufferElement>(entity);
        }
    }
}