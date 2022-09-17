using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)] // update in variable update because the camera can use gravity to adjust its up direction
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class GravityZonesSystem : SystemBase
    {
        public static void TryAddGravityToEntity(
            bool isGlobalZone,
            float3 gravityToApply,
            ref CustomGravity customGravity)
        {
            if (!isGlobalZone)
            {
                customGravity.TouchedByNonGlobalGravity = true;
            }

            if (!isGlobalZone || !customGravity.TouchedByNonGlobalGravity)
            {
            }
        }

        protected override unsafe void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            ComponentDataFromEntity<CustomGravity> customGravityFromEntity = GetComponentDataFromEntity<CustomGravity>(false);
            ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>(true);

            // Reset gravities
            Dependency = Entities
                .ForEach((Entity entity, ref CustomGravity customGravity) =>
                {
                    customGravity.LastZoneEntity = customGravity.CurrentZoneEntity;
                    customGravity.TouchedByNonGlobalGravity = false;
                }).Schedule(Dependency);

            // Spherical gravity zone
            Dependency = Entities
                .WithReadOnly(localToWorldFromEntity)
                .ForEach((Entity entity, in SphericalGravityZone sphericalGravityZone, in PhysicsCollider physicsCollider, in DynamicBuffer<StatefulTriggerEvent> triggerEventsBuffer) =>
                {
                    if (triggerEventsBuffer.Length > 0)
                    {
                        SphereCollider* sphereCollider = ((SphereCollider*)physicsCollider.ColliderPtr);
                        SphereGeometry sphereGeometry = sphereCollider->Geometry;

                        for (int i = 0; i < triggerEventsBuffer.Length; i++)
                        {
                            StatefulTriggerEvent triggerEvent = triggerEventsBuffer[i];
                            if (triggerEvent.State == EventOverlapState.Stay)
                            {
                                Entity otherEntity = triggerEvent.GetOtherEntity(entity);

                                float3 fromOtherToSelfVector = localToWorldFromEntity[entity].Position - localToWorldFromEntity[otherEntity].Position;
                                float distanceRatio = math.clamp(math.length(fromOtherToSelfVector) / sphereGeometry.Radius, 0.01f, 0.99f);
                                float3 gravityToApply = ((1f - distanceRatio) * (math.normalizesafe(fromOtherToSelfVector) * sphericalGravityZone.GravityStrengthAtCenter));

                                if (customGravityFromEntity.HasComponent(otherEntity))
                                {
                                    CustomGravity customGravity = customGravityFromEntity[otherEntity];
                                    customGravity.Gravity = gravityToApply * customGravity.GravityMultiplier;
                                    customGravity.TouchedByNonGlobalGravity = true; 
                                    customGravity.CurrentZoneEntity = entity;
                                    customGravityFromEntity[otherEntity] = customGravity;
                                }
                            }
                        }
                    }
                }).Schedule(Dependency);

            // Global gravity
            if (HasSingleton<GlobalGravityZone>())
            {
                float3 globalGravity = GetSingleton<GlobalGravityZone>().Gravity;
                Dependency = Entities
                    .ForEach((Entity entity, ref CustomGravity customGravity) =>
                    {
                        if (!customGravity.TouchedByNonGlobalGravity)
                        {
                            customGravity.Gravity = globalGravity * customGravity.GravityMultiplier;
                            customGravity.CurrentZoneEntity = Entity.Null;
                        }
                    }).Schedule(Dependency);
            }

            // Apply gravity to physics bodies
            Dependency = Entities
                .ForEach((Entity entity, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in CustomGravity customGravity) =>
                {
                    if (physicsMass.InverseMass > 0f)
                    {
                        CharacterControlUtilities.AccelerateVelocity(ref physicsVelocity.Linear, customGravity.Gravity, deltaTime);
                    }
                }).Schedule(Dependency);
        }
    }
}
