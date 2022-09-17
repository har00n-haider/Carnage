using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Basic
{
    public partial class PrefabThrowerSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem _commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                EntityCommandBuffer commandBuffer = _commandBufferSystem.CreateCommandBuffer();

                Dependency = Entities.ForEach((ref PrefabThrower prefabThrower, in LocalToWorld localToWorld) =>
                {
                    Entity spawnedEntity = commandBuffer.Instantiate(prefabThrower.PrefabEntity);
                    commandBuffer.SetComponent(spawnedEntity, new Translation { Value = localToWorld.Position });
                    commandBuffer.SetComponent(spawnedEntity, new Rotation { Value = quaternion.Euler(prefabThrower.InitialEulerAngles) });
                    commandBuffer.SetComponent(spawnedEntity, new PhysicsVelocity { Linear = localToWorld.Forward * prefabThrower.ThrowForce });
                }).Schedule(Dependency);

                _commandBufferSystem.AddJobHandleForProducer(Dependency);
            }
        }
    }
}