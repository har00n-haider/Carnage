using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class WeaponAssignmentSystem : SystemBase
{
    public WeaponCommandBufferSystem WeaponSimulationCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        WeaponSimulationCommandBufferSystem = World.GetOrCreateSystem<WeaponCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = WeaponSimulationCommandBufferSystem.CreateCommandBuffer();
        ComponentDataFromEntity<Parent> parentFromEntity = GetComponentDataFromEntity<Parent>(true);
        ComponentDataFromEntity<LocalToParent> localToParentFromEntity = GetComponentDataFromEntity<LocalToParent>(true);
        BufferFromEntity<LinkedEntityGroup> linkedEntityBufferFromEntity = GetBufferFromEntity<LinkedEntityGroup>(false);

        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity entity, ref ActiveWeapon activeWeapon) =>
            {
                // Handle assigning new active weapon
                if (activeWeapon.WeaponEntity != activeWeapon.PreviousWeaponEntity)
                {
                    Weapon weapon = GetComponent<Weapon>(activeWeapon.WeaponEntity);
                    weapon.OwnerEntity = entity;
                    // For characters, make View our shoot raycast start point
                    if(HasComponent<FirstPersonCharacterComponent>(entity))
                    {
                        FirstPersonCharacterComponent character = GetComponent<FirstPersonCharacterComponent>(entity);
                        weapon.ShootOriginOverride = character.ViewEntity;
                    }
                    SetComponent(activeWeapon.WeaponEntity, weapon);

                    if (HasComponent<FirstPersonCharacterComponent>(entity))
                    {
                        FirstPersonCharacterComponent onlineFPSCharacter = GetComponent<FirstPersonCharacterComponent>(entity);
                        CarnageUtilities.SetParent(
                            commandBuffer,
                            parentFromEntity,
                            localToParentFromEntity,
                            onlineFPSCharacter.WeaponSocketEntity,
                            activeWeapon.WeaponEntity,
                            default,
                            quaternion.identity);

                        DynamicBuffer<LinkedEntityGroup> linkedEntityBuffer = linkedEntityBufferFromEntity[entity];
                        linkedEntityBuffer.Add(new LinkedEntityGroup { Value = activeWeapon.WeaponEntity });
                    }

                    activeWeapon.PreviousWeaponEntity = activeWeapon.WeaponEntity;
                }
            }).Run();

        WeaponSimulationCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
