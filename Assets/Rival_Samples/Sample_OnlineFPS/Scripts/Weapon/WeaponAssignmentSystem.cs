using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Rival.Samples.OnlineFPS
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class WeaponAssignmentSystem : SystemBase
    {
        public AfterGhostSimulationCommandBufferSystem AfterGhostSimulationCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            AfterGhostSimulationCommandBufferSystem = World.GetOrCreateSystem<AfterGhostSimulationCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = AfterGhostSimulationCommandBufferSystem.CreateCommandBuffer();
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
                        if(HasComponent<OnlineFPSCharacterComponent>(entity))
                        {
                            OnlineFPSCharacterComponent character = GetComponent<OnlineFPSCharacterComponent>(entity);
                            weapon.ShootOriginOverride = character.ViewEntity;
                        }
                        SetComponent(activeWeapon.WeaponEntity, weapon);

                        if (HasComponent<OnlineFPSCharacterComponent>(entity))
                        {
                            OnlineFPSCharacterComponent onlineFPSCharacter = GetComponent<OnlineFPSCharacterComponent>(entity);
                            OnlineFPSUtilities.SetParent(
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

            AfterGhostSimulationCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}