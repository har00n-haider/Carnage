using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Rival;

namespace Rival.Samples
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public partial class TestMovingPlatformSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            if (deltaTime > 0f)
            {
                float invDeltaTime = 1f / deltaTime;
                float time = (float)World.Time.ElapsedTime;
                PhysicsWorld physicsWorld = BuildPhysicsWorldSystem.PhysicsWorld;

                Dependency = Entities
                    .ForEach((Entity entity, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in Translation translation, in Rotation rotation, in TestMovingPlatform movingPlatform) =>
                {
                    float3 targetPos = movingPlatform.OriginalPosition + (math.normalizesafe(movingPlatform.TranslationAxis) * math.sin(time * movingPlatform.TranslationSpeed) * movingPlatform.TranslationAmplitude);

                    quaternion rotationFromRotation = quaternion.Euler(math.normalizesafe(movingPlatform.RotationAxis) * movingPlatform.RotationSpeed * time);
                    quaternion rotationFromOscillation = quaternion.Euler(math.normalizesafe(movingPlatform.OscillationAxis) * (math.sin(time * movingPlatform.OscillationSpeed) * movingPlatform.OscillationAmplitude));
                    quaternion totalRotation = math.mul(rotationFromRotation, rotationFromOscillation);
                    quaternion targetRot = math.mul(totalRotation, movingPlatform.OriginalRotation);

                    RigidTransform targetTransform = new RigidTransform(targetRot, targetPos);

                    physicsVelocity = PhysicsVelocity.CalculateVelocityToTarget(in physicsMass, in translation, in rotation, in targetTransform, invDeltaTime);

                }).ScheduleParallel(Dependency);
            }
        }
    }
}