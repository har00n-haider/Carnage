using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

namespace Rival.Samples.Basic
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class OrbitCameraSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;
        public EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            EndSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected unsafe override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            float fixedDeltaTime = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>().RateManager.Timestep;
            PhysicsWorld physicsWorld = BuildPhysicsWorldSystem.PhysicsWorld;
            EntityCommandBuffer commandBuffer = EndSimulationEntityCommandBufferSystem.CreateCommandBuffer();

            // Update
            Dependency = Entities
                .WithReadOnly(physicsWorld)
                .ForEach((
                Entity entity,
                ref Translation translation,
                ref OrbitCamera orbitCamera, 
                in BasicPlayerInputs inputs,
                in DynamicBuffer<IgnoredEntityBufferElement> ignoredEntitiesBuffer) =>
            {
                // if there is a followed entity, place the camera relatively to it
                if (orbitCamera.FollowedEntity != Entity.Null)
                {
                    float3 targetUp = math.up();
                    Rotation selfRotation = GetComponent<Rotation>(entity);
                    LocalToWorld targetEntityLocalToWorld = GetComponent<LocalToWorld>(orbitCamera.FollowedEntity);

                    // Rotation
                    {
                        selfRotation.Value = quaternion.LookRotationSafe(orbitCamera.PlanarForward, targetUp);

                        if (orbitCamera.RotateWithCharacterParent && HasComponent<KinematicCharacterBody>(orbitCamera.FollowedCharacter))
                        {
                            KinematicCharacterBody characterBody = GetComponent<KinematicCharacterBody>(orbitCamera.FollowedCharacter);
                            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref selfRotation.Value, in characterBody, fixedDeltaTime, deltaTime);
                            orbitCamera.PlanarForward = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(selfRotation.Value), targetUp));
                        }

                        // Yaw
                        float yawAngleChange = inputs.Look.x * orbitCamera.RotationSpeed;
                        quaternion yawRotation = quaternion.Euler(targetUp * math.radians(yawAngleChange));
                        orbitCamera.PlanarForward = math.rotate(yawRotation, orbitCamera.PlanarForward);

                        // Pitch
                        orbitCamera.PitchAngle += -inputs.Look.y * orbitCamera.RotationSpeed;
                        orbitCamera.PitchAngle = math.clamp(orbitCamera.PitchAngle, orbitCamera.MinVAngle, orbitCamera.MaxVAngle);
                        quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(orbitCamera.PitchAngle));

                        // Final rotation
                        selfRotation.Value = quaternion.LookRotationSafe(orbitCamera.PlanarForward, targetUp);
                        selfRotation.Value = math.mul(selfRotation.Value, pitchRotation);
                    }

                    float3 cameraForward = MathUtilities.GetForwardFromRotation(selfRotation.Value);

                    // Distance input
                    float desiredDistanceMovementFromInput = inputs.Scroll * orbitCamera.DistanceMovementSpeed * deltaTime;
                    orbitCamera.TargetDistance = math.clamp(orbitCamera.TargetDistance + desiredDistanceMovementFromInput, orbitCamera.MinDistance, orbitCamera.MaxDistance);
                    orbitCamera.CurrentDistanceFromMovement = math.lerp(orbitCamera.CurrentDistanceFromMovement, orbitCamera.TargetDistance, MathUtilities.GetSharpnessInterpolant(orbitCamera.DistanceMovementSharpness, deltaTime));

                    // Obstructions
                    if (orbitCamera.ObstructionRadius > 0f)
                    {
                        float obstructionCheckDistance = orbitCamera.CurrentDistanceFromMovement;

                        CameraObstructionHitsCollector collector = new CameraObstructionHitsCollector(in physicsWorld, ignoredEntitiesBuffer, cameraForward);
                        physicsWorld.SphereCastCustom<CameraObstructionHitsCollector>(
                            targetEntityLocalToWorld.Position,
                            orbitCamera.ObstructionRadius,
                            -cameraForward,
                            obstructionCheckDistance,
                            ref collector,
                            CollisionFilter.Default,
                            QueryInteraction.IgnoreTriggers);

                        float newObstructedDistance = obstructionCheckDistance;
                        if (collector.NumHits > 0 && collector.ClosestHit.RigidBodyIndex >= 0)
                        {
                            newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;

                            // Redo cast with the interpolated body transform to prevent FixedUpdate jitter in obstruction detection
                            if (orbitCamera.PreventFixedUpdateJitter)
                            {
                                RigidBody hitBody = physicsWorld.Bodies[collector.ClosestHit.RigidBodyIndex];
                                LocalToWorld hitBodyLocalToWorld = GetComponent<LocalToWorld>(hitBody.Entity);

                                hitBody.WorldFromBody = new RigidTransform(quaternion.LookRotationSafe(hitBodyLocalToWorld.Forward, hitBodyLocalToWorld.Up), hitBodyLocalToWorld.Position);

                                collector = new CameraObstructionHitsCollector(in physicsWorld, ignoredEntitiesBuffer, cameraForward);
                                hitBody.SphereCastCustom<CameraObstructionHitsCollector>(
                                    targetEntityLocalToWorld.Position,
                                    orbitCamera.ObstructionRadius,
                                    -cameraForward,
                                    obstructionCheckDistance,
                                    ref collector,
                                    CollisionFilter.Default,
                                    QueryInteraction.IgnoreTriggers);

                                if (collector.NumHits > 0)
                                {
                                    newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;
                                }
                            }
                        }

                        // Update current distance based on obstructed distance
                        if (orbitCamera.CurrentDistanceFromObstruction < newObstructedDistance)
                        {
                            // Move outer
                            orbitCamera.CurrentDistanceFromObstruction = math.lerp(orbitCamera.CurrentDistanceFromObstruction, newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCamera.ObstructionOuterSmoothingSharpness, deltaTime));
                        }
                        else if (orbitCamera.CurrentDistanceFromObstruction > newObstructedDistance)
                        {
                            // Move inner
                            orbitCamera.CurrentDistanceFromObstruction = math.lerp(orbitCamera.CurrentDistanceFromObstruction, newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCamera.ObstructionInnerSmoothingSharpness, deltaTime));
                        }
                    }
                    else
                    {
                        orbitCamera.CurrentDistanceFromObstruction = orbitCamera.CurrentDistanceFromMovement;
                    }

                    // Calculate final camera position from targetposition + rotation + distance
                    translation.Value = targetEntityLocalToWorld.Position + (-cameraForward * orbitCamera.CurrentDistanceFromObstruction);

                    // Manually calculate the LocalToWorld since this is updating after the Transform systems, and the LtW is what rendering uses
                    LocalToWorld cameraLocalToWorld = new LocalToWorld();
                    cameraLocalToWorld.Value = new float4x4(selfRotation.Value, translation.Value);
                    SetComponent(entity, cameraLocalToWorld);
                    SetComponent<Rotation>(entity, selfRotation);
                }
            }).Schedule(Dependency);
            
            EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}