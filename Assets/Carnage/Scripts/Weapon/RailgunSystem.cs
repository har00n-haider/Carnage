using Rival;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;


[UpdateInGroup(typeof(WeaponUpdateGroup))]
public partial class RailgunSystem : SystemBase
{
    public BuildPhysicsWorld BuildPhysicsWorld;
    public WeaponCommandBufferSystem WeaponCommandBufferSystem;

    private Unity.Mathematics.Random _random;

    protected override void OnCreate()
    {
        base.OnCreate();

        BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        WeaponCommandBufferSystem = World.GetOrCreateSystem<WeaponCommandBufferSystem>();

        _random = Unity.Mathematics.Random.CreateFromIndex((uint)0);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float elapsedTime = (float)Time.ElapsedTime;

        EntityCommandBuffer commandBuffer = WeaponCommandBufferSystem.CreateCommandBuffer();
        CollisionWorld collisionWorld = BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
        NativeList<RaycastHit> tmpRaycastHits = new NativeList<RaycastHit>(Allocator.TempJob);

        Unity.Mathematics.Random sparksRandom = _random;

        Dependency = Entities
            .WithReadOnly(collisionWorld)
            .WithDisposeOnCompletion(tmpRaycastHits)
            .ForEach((
                Entity entity,
                ref Weapon weapon,
                ref Railgun railgun) =>
            {

                railgun._firingTimer -= deltaTime;

                if (weapon.ShootRequested && railgun._firingTimer <= 0f)
                {
                    LocalToWorld muzzleLocalToWorld = GetComponent<LocalToWorld>(railgun._muzzleEntity);

                    // Select shoot raycast origin point
                    LocalToWorld raycastOriginEntityLocalToWorld = muzzleLocalToWorld;
                    if (HasComponent<LocalToWorld>(weapon.ShootOriginOverride))
                    {
                        raycastOriginEntityLocalToWorld = GetComponent<LocalToWorld>(weapon.ShootOriginOverride);
                    }

                    float3 shotLazerPosition = muzzleLocalToWorld.Position;
                    float3 shotHitPosition = shotLazerPosition + (muzzleLocalToWorld.Forward * railgun.Range);
                    bool shotHasHit = false;

                    tmpRaycastHits.Clear();
                    RaycastInput rayInput = new RaycastInput
                    {
                        Filter = CollisionFilter.Default,
                        Start = raycastOriginEntityLocalToWorld.Position,
                        End = raycastOriginEntityLocalToWorld.Position + (raycastOriginEntityLocalToWorld.Forward * railgun.Range),
                    };
                    if (collisionWorld.CastRay(rayInput, ref tmpRaycastHits))
                    {
                        // Select hit
                        RaycastHit closestValidHit = default;
                        closestValidHit.Fraction = float.MaxValue;
                        for (int i = 0; i < tmpRaycastHits.Length; i++)
                        {
                            RaycastHit tmpHit = tmpRaycastHits[i];

                            if (tmpHit.Entity != weapon.OwnerEntity)
                            {
                                if (tmpHit.Fraction < closestValidHit.Fraction)
                                {
                                    closestValidHit = tmpHit;
                                }
                            }
                        }

                        if (closestValidHit.Entity != Entity.Null)
                        {

                            if (HasComponent<Health>(closestValidHit.Entity))
                            {
                                Health health = GetComponent<Health>(closestValidHit.Entity);
                                health.CurrentHealth -= railgun.Damage;
                                SetComponent(closestValidHit.Entity, health);
                            }


                            shotHasHit = true;
                            shotHitPosition = closestValidHit.Position;
                        }
                    }

                    quaternion shotLazerRotation = quaternion.LookRotationSafe(math.normalizesafe(shotHitPosition - muzzleLocalToWorld.Position), muzzleLocalToWorld.Up);

                    // Shot visuals

                    // Spawn hit vfx
                    if (shotHasHit && railgun.HitSparkPrefab != Entity.Null)
                    {
                        for (int s = 0; s < railgun.HitSparksCount; s++)
                        {
                            Entity hitVfxEntity = commandBuffer.Instantiate(railgun.HitSparkPrefab);
                            commandBuffer.SetComponent(hitVfxEntity, new Translation { Value = shotHitPosition });
                            commandBuffer.SetComponent(hitVfxEntity, new Rotation { Value = sparksRandom.NextQuaternionRotation() });
                        }
                    }

                    // Spawn shoot vfx
                    if (railgun.LazerPrefab != Entity.Null)
                    {
                        Entity shootVfxEntity = commandBuffer.Instantiate(railgun.LazerPrefab);
                        commandBuffer.SetComponent(shootVfxEntity, new Translation { Value = shotLazerPosition });
                        commandBuffer.SetComponent(shootVfxEntity, new Rotation { Value = shotLazerRotation });

                        NonUniformScale shootVfxScale = GetComponent<NonUniformScale>(railgun.LazerPrefab);
                        shootVfxScale.Value.z = math.distance(shotLazerPosition, shotHitPosition);
                        commandBuffer.SetComponent(shootVfxEntity, shootVfxScale);
                    }

                    // Handle recoil
                    if (HasComponent<FirstPersonCharacterComponent>(weapon.OwnerEntity))
                    {
                        FirstPersonCharacterComponent owningCharacter = GetComponent<FirstPersonCharacterComponent>(weapon.OwnerEntity);

                        owningCharacter.RecoilVector += -math.forward() * railgun.Recoil;
                        owningCharacter.RecoilVector = MathUtilities.ClampToMaxLength(owningCharacter.RecoilVector, owningCharacter.RecoilMaxDistance);

                        owningCharacter.TargetRecoilFOVKick += railgun.RecoilFOVKick;
                        owningCharacter.TargetRecoilFOVKick = math.clamp(owningCharacter.TargetRecoilFOVKick, 0f, owningCharacter.RecoilMaxFOVKick);

                        SetComponent(weapon.OwnerEntity, owningCharacter);
                    }

                    railgun._firingTimer = 1f / railgun.FireRate;
                }

            }).Schedule(Dependency);

        WeaponCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
