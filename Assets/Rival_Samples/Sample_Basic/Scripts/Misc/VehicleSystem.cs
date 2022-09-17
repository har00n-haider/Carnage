using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    [UpdateAfter(typeof(KinematicCharacterUpdateGroup))]
    public partial class VehicleSystem : SystemBase
    {
        public BuildPhysicsWorld BuildPhysicsWorldSystem;

        public struct WheelHitCollector<T> : ICollector<T> where T : struct, IQueryResult
        {
            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction => 1f;
            public int NumHits { get; set; }

            public T ClosestHit;

            private Entity _wheelEntity;
            private Entity _vehicleEntity;
            private float _closestHitFraction;

            public void Init(Entity wheelEntity, Entity vehicleEntity)
            {
                _wheelEntity = wheelEntity;
                _vehicleEntity = vehicleEntity;
                _closestHitFraction = float.MaxValue;
            }

            public bool AddHit(T hit)
            {
                if (hit.Entity != _wheelEntity && hit.Entity != _vehicleEntity && hit.Fraction < _closestHitFraction)
                {
                    ClosestHit = hit;
                    _closestHitFraction = hit.Fraction;

                    NumHits = 1;
                    return true;
                }

                return false;
            }
        }

        protected override void OnCreate()
        {
            BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        protected unsafe override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            float fwdInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);
            float sideInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
            CollisionWorld collisionWorld = BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

            Dependency = Entities
                .WithReadOnly(collisionWorld)
                .ForEach((Entity entity, ref PhysicsVelocity physicsVelocity, in Vehicle vehicle, in Rotation rotation, in PhysicsMass physicsMass) =>
            {
                DynamicBuffer<VehicleWheels> vehicleWheelsBuffer = GetBuffer<VehicleWheels>(entity);
                Translation translation = GetComponent<Translation>(entity);
                float3 vehicleUp = MathUtilities.GetUpFromRotation(rotation.Value);
                float3 vehicleForward = MathUtilities.GetForwardFromRotation(rotation.Value);
                float3 vehicleRight = MathUtilities.GetRightFromRotation(rotation.Value);
                float wheelGroundingAmount = 0f;
                float wheelRatio = 1f / (float)vehicleWheelsBuffer.Length;

                // Wheel collision casts
                for (int i = 0; i < vehicleWheelsBuffer.Length; i++)
                {
                    VehicleWheels wheel = vehicleWheelsBuffer[i];

                    float3 wheelStartPoint = GetComponent<Translation>(wheel.CollisionEntity).Value;
                    quaternion wheelRotation = GetComponent<Rotation>(wheel.CollisionEntity).Value;

                    ColliderCastInput castInput = new ColliderCastInput(GetComponent<PhysicsCollider>(wheel.CollisionEntity).Value, wheelStartPoint, wheelStartPoint + (-vehicleUp * vehicle.WheelSuspensionDistance), wheelRotation);
                    WheelHitCollector<ColliderCastHit> collector = default;
                    collector.Init(wheel.CollisionEntity, entity);

                    float hitDistance = vehicle.WheelSuspensionDistance;
                    if (collisionWorld.CastCollider(castInput, ref collector))
                    {
                        hitDistance = collector.ClosestHit.Fraction * vehicle.WheelSuspensionDistance;

                        wheelGroundingAmount += wheelRatio;

                        // Suspension
                        float suspensionCompressedRatio = 1f - (hitDistance / vehicle.WheelSuspensionDistance);

                        // Add suspension force
                        float3 vehicleVelocityAtWheelPoint = physicsVelocity.GetLinearVelocity(physicsMass, translation, rotation, wheelStartPoint);
                        float vehicleVelocityInUpDirection = math.dot(vehicleVelocityAtWheelPoint, vehicleUp);
                        float suspensionImpulseVelocityChangeOnUpDirection = suspensionCompressedRatio * vehicle.WheelSuspensionStrength;

                        suspensionImpulseVelocityChangeOnUpDirection -= vehicleVelocityInUpDirection;
                        if (suspensionImpulseVelocityChangeOnUpDirection > 0f)
                        {
                            physicsVelocity.ApplyImpulse(in physicsMass, in translation, in rotation, vehicleUp * suspensionImpulseVelocityChangeOnUpDirection, wheelStartPoint);
                        }
                    }

                    // Place wheel mesh at goal position
                    SetComponent<Translation>(wheel.MeshEntity, new Translation { Value = -math.up() * hitDistance });
                }

                if (wheelGroundingAmount > 0f)
                {
                    float chosenAcceleration = wheelGroundingAmount * vehicle.Acceleration;

                    // Acceleration
                    float3 addedVelocityFromAcceleration = vehicleForward * fwdInput * chosenAcceleration * deltaTime;
                    float3 tmpNewVelocity = physicsVelocity.Linear + addedVelocityFromAcceleration;
                    tmpNewVelocity = MathUtilities.ClampToMaxLength(tmpNewVelocity, vehicle.MaxSpeed);
                    addedVelocityFromAcceleration = tmpNewVelocity - physicsVelocity.Linear;
                    physicsVelocity.Linear += addedVelocityFromAcceleration;

                    // Friction & Roll resistance
                    float3 upVelocity = math.projectsafe(physicsVelocity.Linear, vehicleUp);
                    float3 fwdVelocity = math.projectsafe(physicsVelocity.Linear, vehicleForward);
                    float3 lateralVelocity = math.projectsafe(physicsVelocity.Linear, vehicleRight);
                    lateralVelocity *= (1f / (1f + (vehicle.WheelFriction * deltaTime)));

                    bool movingInIntendedDirection = math.dot(fwdVelocity, vehicleForward * fwdInput) > 0f;
                    if (!movingInIntendedDirection)
                    {
                        fwdVelocity *= (1f / (1f + (vehicle.WheelRollResistance * deltaTime)));
                    }

                    physicsVelocity.Linear = upVelocity + fwdVelocity + lateralVelocity;

                    // Rotation
                    physicsVelocity.Angular.y += vehicle.RotationAcceleration * sideInput * deltaTime;
                    physicsVelocity.Angular.y = math.clamp(physicsVelocity.Angular.y, -vehicle.MaxRotationSpeed, vehicle.MaxRotationSpeed);
                    physicsVelocity.Angular.y *= (1f / (1f + (vehicle.RotationDamping * deltaTime)));
                }
            }).Schedule(Dependency);
        }
    }
}