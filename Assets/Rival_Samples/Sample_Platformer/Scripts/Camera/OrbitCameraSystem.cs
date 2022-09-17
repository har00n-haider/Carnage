using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

namespace Rival.Samples.Platformer
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
                    in PlatformerInputs inputs,
                    in DynamicBuffer<IgnoredEntityBufferElement> ignoredEntitiesBuffer) =>
                {
                    Rotation selfRotation = GetComponent<Rotation>(entity);

                    // if there is a followed entity, place the camera relatively to it
                    if (orbitCamera.FollowedEntity != Entity.Null)
                    {
                        PlatformerCharacterComponent platformerCharacter = GetComponent<PlatformerCharacterComponent>(orbitCamera.CharacterEntity);
                        PlatformerCharacterStateMachine platformerCharacterStateMachine = GetComponent<PlatformerCharacterStateMachine>(orbitCamera.CharacterEntity);
                        CustomGravity characterCustomGravity = GetComponent<CustomGravity>(orbitCamera.CharacterEntity);

                        // detect follow target transitions
                        if (platformerCharacterStateMachine.CurrentCharacterState == CharacterState.Swimming)
                        {
                            if (orbitCamera.FollowedEntity != platformerCharacter.SwimmingCameraTargetEntity)
                            {
                                orbitCamera.TransitionToFollowedEntity(platformerCharacter.SwimmingCameraTargetEntity, orbitCamera.FollowTargetTransitionDelay);
                            }
                        }
                        else if (platformerCharacterStateMachine.CurrentCharacterState == CharacterState.Climbing)
                        {
                            if (orbitCamera.FollowedEntity != platformerCharacter.ClimbingCameraTargetEntity)
                            {
                                orbitCamera.TransitionToFollowedEntity(platformerCharacter.ClimbingCameraTargetEntity, orbitCamera.FollowTargetTransitionDelay);
                            }
                        }
                        else if (platformerCharacterStateMachine.CurrentCharacterState == CharacterState.Crouched)
                        {
                            if (orbitCamera.FollowedEntity != platformerCharacter.CrouchingCameraTargetEntity)
                            {
                                orbitCamera.TransitionToFollowedEntity(platformerCharacter.CrouchingCameraTargetEntity, orbitCamera.FollowTargetTransitionDelay);
                            }
                        }
                        else
                        {
                            if (orbitCamera.FollowedEntity != platformerCharacter.DefaultCameraTargetEntity)
                            {
                                orbitCamera.TransitionToFollowedEntity(platformerCharacter.DefaultCameraTargetEntity, orbitCamera.FollowTargetTransitionDelay);
                            }
                        }

                        LocalToWorld followTargetLocalToWorld = GetComponent<LocalToWorld>(orbitCamera.FollowedEntity);

                        // detect targetUp transitions
                        if (platformerCharacter.IsOnStickySurface != platformerCharacter.WasOnStickySurface)
                        {
                            orbitCamera.BeginSmoothUpTransition(orbitCamera.PreviousTargetUp, orbitCamera.CameraUpTransitionsTime);
                        }
                        else if (characterCustomGravity.CurrentZoneEntity != characterCustomGravity.LastZoneEntity)
                        {
                            orbitCamera.BeginSmoothUpTransition(orbitCamera.PreviousTargetUp, orbitCamera.CameraUpTransitionsTime);
                        }

                        float3 targetUp = -math.normalizesafe(GetComponent<CustomGravity>(orbitCamera.CharacterEntity).Gravity);
                        if (orbitCamera.TargetUpTransitionTotalTime > 0f)
                        {
                            targetUp = math.normalizesafe(math.lerp(orbitCamera.TargetUpTransitionFromUp, targetUp, math.saturate(orbitCamera.TargetUpTransitionTime / orbitCamera.TargetUpTransitionTotalTime)));
                        }
                        orbitCamera.TargetUpTransitionTime += deltaTime;


                        // Adapt up when on sticky surfaces
                        if (platformerCharacter.IsOnStickySurface)
                        {
                            targetUp = followTargetLocalToWorld.Up;
                        }

                        // Rotation
                        {
                            selfRotation.Value = quaternion.LookRotationSafe(orbitCamera.PlanarForward, targetUp);

                            if (orbitCamera.RotateWithCharacterParent && HasComponent<KinematicCharacterBody>(orbitCamera.CharacterEntity))
                            {
                                KinematicCharacterBody characterBody = GetComponent<KinematicCharacterBody>(orbitCamera.CharacterEntity);
                                KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref selfRotation.Value, in characterBody, fixedDeltaTime, deltaTime);
                                orbitCamera.PlanarForward = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(selfRotation.Value), targetUp));
                            }

                            // Adapt to target up
                            orbitCamera.PlanarForward = math.cross(targetUp, math.cross(orbitCamera.PlanarForward, targetUp));

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
                        float desiredDistanceMovementFromInput = inputs.CameraZoom * orbitCamera.DistanceMovementSpeed * deltaTime;
                        orbitCamera.TargetDistance = math.clamp(orbitCamera.TargetDistance + desiredDistanceMovementFromInput, orbitCamera.MinDistance, orbitCamera.MaxDistance);
                        orbitCamera.CurrentDistanceFromMovement = math.lerp(orbitCamera.CurrentDistanceFromMovement, orbitCamera.TargetDistance, MathUtilities.GetSharpnessInterpolant(orbitCamera.DistanceMovementSharpness, deltaTime));

                        // Obstructions
                        if (orbitCamera.ObstructionRadius > 0f)
                        {
                            float obstructionCheckDistance = orbitCamera.CurrentDistanceFromMovement;

                            CameraObstructionHitsCollector collector = new CameraObstructionHitsCollector(in physicsWorld, ignoredEntitiesBuffer, cameraForward);
                            physicsWorld.SphereCastCustom<CameraObstructionHitsCollector>(
                                orbitCamera.FollowedTranslation,
                                orbitCamera.ObstructionRadius,
                                -cameraForward,
                                obstructionCheckDistance,
                                ref collector,
                                CollisionFilter.Default,
                                QueryInteraction.IgnoreTriggers);

                            float newObstructedDistance = obstructionCheckDistance;
                            if (collector.NumHits > 0)
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
                                        orbitCamera.FollowedTranslation,
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

                        orbitCamera.FollowedTranslation = followTargetLocalToWorld.Position;
                        if (orbitCamera.PreviousFollowedEntity != Entity.Null)
                        {
                            LocalToWorld previousFollowTargetLocalToWorld = GetComponent<LocalToWorld>(orbitCamera.PreviousFollowedEntity);
                            if (orbitCamera.FollowTargetTransitionTime < orbitCamera.FollowTargetTransitionTotalTime)
                            {
                                orbitCamera.FollowTargetTransitionTime += deltaTime;
                            }
                            else
                            {
                                orbitCamera.PreviousFollowedEntity = Entity.Null;
                            }
                            orbitCamera.FollowedTranslation = math.lerp(previousFollowTargetLocalToWorld.Position, followTargetLocalToWorld.Position, math.saturate(orbitCamera.FollowTargetTransitionTime / orbitCamera.FollowTargetTransitionTotalTime));
                        }

                        // Calculate final camera position from targetposition + rotation + distance
                        translation.Value = orbitCamera.FollowedTranslation + (-cameraForward * orbitCamera.CurrentDistanceFromObstruction);

                        orbitCamera.PreviousTargetUp = targetUp;

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