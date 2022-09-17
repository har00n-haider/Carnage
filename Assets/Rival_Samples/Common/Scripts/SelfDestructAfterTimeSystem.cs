using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rival.Samples
{
    public partial class SelfDestructAfterTimeSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            EntityCommandBuffer commandBuffer = _commandBufferSystem.CreateCommandBuffer();

            Entities.ForEach((Entity entity, ref SelfDestructAfterTime selfDestructAfterTime) =>
            {
                selfDestructAfterTime._timeSinceAlive += deltaTime;

                if(selfDestructAfterTime._timeSinceAlive > selfDestructAfterTime.LifeTime)
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }).Schedule();

            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}