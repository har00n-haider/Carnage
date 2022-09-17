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
    [AlwaysSynchronizeSystem]
    [UpdateInWorld(TargetWorld.Client)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class OnlineFPSPlayerCommandsSystem : SystemBase
    {
        public FPSInputActions InputActions;
        public ClientSimulationSystemGroup ClientSimulationSystemGroup;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<NetworkIdComponent>();
            RequireSingletonForUpdate<CommandTargetComponent>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            ClientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

            // Create the input user
            InputActions = new FPSInputActions();
            InputActions.Enable();
            InputActions.DefaultMap.Enable();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<NetworkIdComponent>())
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            FPSInputActions.DefaultMapActions defaultActionsMap = InputActions.DefaultMap;

            float deltaTime = Time.DeltaTime;
            float elapsedTime = (float)Time.ElapsedTime;
            uint tick = ClientSimulationSystemGroup.ServerTick;
            int localPlayerId = GetSingleton<NetworkIdComponent>().Value;

            // Update commands
            Entities
                .WithoutBurst()
                .ForEach((
                    Entity entity,
                    ref DynamicBuffer<OnlineFPSPlayerCommands> playerCommands,
                    in OnlineFPSPlayer player,
                    in GhostOwnerComponent ghostOwner) =>
                {
                    if (ghostOwner.NetworkId == localPlayerId && HasComponent<OnlineFPSCharacterComponent>(player.ControlledEntity))
                    {
                        OnlineFPSCharacterComponent character = GetComponent<OnlineFPSCharacterComponent>(player.ControlledEntity);

                        OnlineFPSPlayerCommands newPlayerCommands = default;
                        newPlayerCommands.Tick = tick;
                        newPlayerCommands.MoveInput = Vector2.ClampMagnitude(defaultActionsMap.Move.ReadValue<Vector2>(), 1f); ;
                        newPlayerCommands.LookInput = defaultActionsMap.LookDelta.ReadValue<Vector2>();
                        if (math.lengthsq(defaultActionsMap.LookConst.ReadValue<Vector2>()) > math.lengthsq(defaultActionsMap.LookDelta.ReadValue<Vector2>()))
                        {
                            newPlayerCommands.LookInput = defaultActionsMap.LookConst.ReadValue<Vector2>() * deltaTime;
                        }
                        newPlayerCommands.JumpRequested = defaultActionsMap.Jump.ReadValue<float>() > 0.5f && defaultActionsMap.Jump.triggered;
                        newPlayerCommands.ShootRequested = defaultActionsMap.Shoot.ReadValue<float>() > 0.5f && defaultActionsMap.Shoot.triggered;
                        newPlayerCommands.AimHeld = defaultActionsMap.Aim.ReadValue<float>() > 0.5f;

                        // Merge same-tick commands (special input handling for fixed timestep simulation)
                        if (playerCommands.GetDataAtTick(tick, out OnlineFPSPlayerCommands sameTickPreviousCommands))
                        {
                            if (tick == sameTickPreviousCommands.Tick)
                            {
                                newPlayerCommands.LookInput += sameTickPreviousCommands.LookInput;
                                if (sameTickPreviousCommands.JumpRequested)
                                {
                                    newPlayerCommands.JumpRequested = true;
                                }
                                if (sameTickPreviousCommands.ShootRequested)
                                {
                                    newPlayerCommands.ShootRequested = true;
                                }
                            }
                        }

#if ONLINE_FPS_BOT
                        newPlayerCommands.MoveInput = math.sin(elapsedTime * 2f);
                        newPlayerCommands.LookInput.x = math.sin(elapsedTime * 1f);
                        newPlayerCommands.LookInput.y = math.sin(elapsedTime * 3f);
#endif

                        playerCommands.AddCommandData(newPlayerCommands);
                    }
                }).Run();
        }
    }
}