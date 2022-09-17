using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class PlatformerCharacterHybridSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Create
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<PlatformerCharacterHybridLink>()
                .ForEach((Entity entity, ref PlatformerCharacterAnimation characterAnimation, in PlatformerCharacterHybridData hybridData) =>
                {
                    GameObject tmpObject = GameObject.Instantiate(hybridData.MeshPrefab);
                    Animator animator = tmpObject.GetComponent<Animator>();

                    EntityManager.AddComponentObject(entity, new PlatformerCharacterHybridLink
                    {
                        Object = tmpObject,
                        Animator = animator,
                    });

                    // Find the clipIndex param
                    for (int i = 0; i < animator.parameters.Length; i++)
                    {
                        if (animator.parameters[i].name == "ClipIndex")
                        {
                            characterAnimation.ClipIndexParameterHash = animator.parameters[i].nameHash;
                            break;
                        }
                    }

                }).Run();

            // Update Transform & Animation
            Entities
                .WithoutBurst()
                .ForEach((
                    Entity entity, 
                    ref PlatformerCharacterAnimation characterAnimation, 
                    in KinematicCharacterBody characterBody,
                    in Rotation characterRotation,
                    in PlatformerCharacterComponent characterComponent, 
                    in PlatformerCharacterStateMachine characterStateMachine,
                    in PlatformerInputs inputs,
                    in PlatformerCharacterHybridLink hybridLink) =>
                {
                    if (hybridLink.Object)
                    {
                        // Transform
                        LocalToWorld meshRootLTW = GetComponent<LocalToWorld>(characterComponent.MeshRootEntity);
                        hybridLink.Object.transform.position = meshRootLTW.Position;
                        hybridLink.Object.transform.rotation = meshRootLTW.Rotation;

                        // Animation
                        if (hybridLink.Animator)
                        {
                            PlatformerCharacterAnimationHandler.UpdateAnimation(
                                hybridLink.Animator,
                                ref characterAnimation,
                                in characterBody,
                                in characterComponent,
                                in characterStateMachine,
                                in inputs,
                                in characterRotation);
                        }

                        // Mesh enabling
                        if(characterStateMachine.CurrentCharacterState == CharacterState.Rolling)
                        {
                            if(hybridLink.Object.activeSelf)
                            {
                                hybridLink.Object.SetActive(false);
                            }
                        }
                        else
                        {
                            if (!hybridLink.Object.activeSelf)
                            {
                                hybridLink.Object.SetActive(true);
                            }
                        }
                    }
                }).Run();

            // Destroy
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<PlatformerCharacterHybridData>()
                .ForEach((Entity entity, in PlatformerCharacterHybridLink hybridLink) =>
                {
                    GameObject.Destroy(hybridLink.Object);
                    EntityManager.RemoveComponent<PlatformerCharacterHybridLink>(entity);
                }).Run();
        }
    }
}