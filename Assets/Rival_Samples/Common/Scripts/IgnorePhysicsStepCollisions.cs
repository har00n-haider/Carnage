using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Rival.Samples
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct IgnorePhysicsStepCollisions : IComponentData
    { }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public partial class IgnorePhysicsStepCollisionsSystem : SystemBase
    {
        private EntityQuery _ignoreCollisionsQuery;
        private StepPhysicsWorld _stepPhysicsWorld;

        protected override void OnCreate()
        {
            _stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

            _ignoreCollisionsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(IgnorePhysicsStepCollisions) }
            });

            RequireForUpdate(_ignoreCollisionsQuery);
        }

        protected override void OnUpdate()
        {
            if (_stepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
            {
                return;
            }

            SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                inDeps.Complete();

                return new IgnorePhysicsStepCollisionsJob
                {
                    IgnorePhysicsStepCollisionsFromEntity = GetComponentDataFromEntity<IgnorePhysicsStepCollisions>(true),
                }.Schedule(simulation, ref world, Dependency);
            };
            _stepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);
        }

        [BurstCompile]
        struct IgnorePhysicsStepCollisionsJob : IBodyPairsJob
        {
            [ReadOnly]
            public ComponentDataFromEntity<IgnorePhysicsStepCollisions> IgnorePhysicsStepCollisionsFromEntity;

            public unsafe void Execute(ref ModifiableBodyPair pair)
            {
                if (IgnorePhysicsStepCollisionsFromEntity.HasComponent(pair.EntityA) || IgnorePhysicsStepCollisionsFromEntity.HasComponent(pair.EntityB))
                {
                    pair.Disable();
                }
            }
        }
    }
}