using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


namespace Rival.Samples.OnlineFPS
{
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateBefore(typeof(PredictedPhysicsSystemGroup))]
    public partial class OnlineFPSPlayerControlSystem : SystemBase
    {
        public GhostPredictionSystemGroup GhostPredictionSystemGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            bool isClient = World.GetExistingSystem<ClientSimulationSystemGroup>() != null;
            float deltaTime = Time.DeltaTime;
            uint tick = GhostPredictionSystemGroup.PredictingTick;

            // Iterate on all Player components to apply input to their character
            Entities
                .ForEach((ref OnlineFPSPlayer player, in DynamicBuffer<OnlineFPSPlayerCommands> playerCommandsBuffer, in PredictedGhostComponent predictedGhost) =>
                {
                    if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
                        return;

                    if (playerCommandsBuffer.GetDataAtTick(tick, out OnlineFPSPlayerCommands playerCommands))
                    {
                        // Character control
                        if (HasComponent<OnlineFPSCharacterInputs>(player.ControlledEntity))
                        {
                            OnlineFPSCharacterInputs characterInputs = GetComponent<OnlineFPSCharacterInputs>(player.ControlledEntity);
                            quaternion characterRotation = GetComponent<Rotation>(player.ControlledEntity).Value;
                            float3 characterForward = math.mul(characterRotation, math.forward());
                            float3 characterRight = math.mul(characterRotation, math.right());

                            // Look
                            characterInputs.LookYawPitchDegrees = playerCommands.LookInput * player.LookRotationSpeed;

                            // Move
                            characterInputs.MoveVector = (playerCommands.MoveInput.y * characterForward) + (playerCommands.MoveInput.x * characterRight);
                            characterInputs.MoveVector = Rival.MathUtilities.ClampToMaxLength(characterInputs.MoveVector, 1f);

                            // Jump
                            characterInputs.JumpRequested = playerCommands.JumpRequested;

                            SetComponent(player.ControlledEntity, characterInputs);
                        }

                        // Aiming (zoom)
                        if (HasComponent<OnlineFPSCharacterComponent>(player.ControlledEntity))
                        {
                            OnlineFPSCharacterComponent character = GetComponent<OnlineFPSCharacterComponent>(player.ControlledEntity);
                            if (HasComponent<MainEntityCamera>(character.ViewEntity))
                            {
                                MainEntityCamera cam = GetComponent<MainEntityCamera>(character.ViewEntity);

                                float targetFOV = playerCommands.AimHeld ? character.AimFOV : character.DefaultFOV;
                                cam.FoV = math.lerp(cam.FoV, targetFOV + character.CurrentRecoilFOVKick, math.saturate(character.AimFOVSharpness * deltaTime));
                                SetComponent(character.ViewEntity, cam);
                            }
                        }

                        // Shooting
                        if (HasComponent<ActiveWeapon>(player.ControlledEntity))
                        {
                            ActiveWeapon activeWeapon = GetComponent<ActiveWeapon>(player.ControlledEntity);
                            if (HasComponent<Weapon>(activeWeapon.WeaponEntity))
                            {
                                Weapon weapon = GetComponent<Weapon>(activeWeapon.WeaponEntity);
                                weapon.ShootRequested = playerCommands.ShootRequested;
                                SetComponent<Weapon>(activeWeapon.WeaponEntity, weapon);
                            }
                        }
                    }
                }).Schedule();
        }
    }
}