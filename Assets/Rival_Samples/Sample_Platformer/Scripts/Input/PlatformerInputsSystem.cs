using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class PlatformerInputsSystem : SystemBase
    {
        private PlatformerInputActions.GameplayMapActions _defaultActionsMap;
        private bool _isInitialized = false;
        private FixedStepTimeSystem _fixedStepTickCounterSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _fixedStepTickCounterSystem = World.GetOrCreateSystem<FixedStepTimeSystem>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            PlatformerInputActions inputActions = new PlatformerInputActions();
            inputActions.Enable();
            inputActions.GameplayMap.Enable();
            _defaultActionsMap = inputActions.GameplayMap;
        }

        protected override void OnUpdate()
        {
            uint fixedTick = _fixedStepTickCounterSystem.Tick;

            if (!_isInitialized)
            {
                // Cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                _isInitialized = true;
            }

            float2 moveInput = Vector2.ClampMagnitude(_defaultActionsMap.Move.ReadValue<Vector2>(), 1f);
            float2 lookInput = _defaultActionsMap.LookDelta.ReadValue<Vector2>();
            if (math.lengthsq(_defaultActionsMap.LookConst.ReadValue<Vector2>()) > math.lengthsq(_defaultActionsMap.LookDelta.ReadValue<Vector2>()))
            {
                lookInput = _defaultActionsMap.LookConst.ReadValue<Vector2>() * Time.DeltaTime;
            }
            float cameraZoomInput = _defaultActionsMap.CameraZoom.ReadValue<float>();

            float jumpInput = _defaultActionsMap.Jump.ReadValue<float>();
            float rollInput = _defaultActionsMap.Roll.ReadValue<float>();
            float sprintInput = _defaultActionsMap.Sprint.ReadValue<float>();
            float dashInput = _defaultActionsMap.Dash.ReadValue<float>();
            float crouchInput = _defaultActionsMap.Crouch.ReadValue<float>();
            float ropeInput = _defaultActionsMap.Rope.ReadValue<float>();
            float climbInput = _defaultActionsMap.Climb.ReadValue<float>();
            float flyNoCollisionsInput = _defaultActionsMap.FlyNoCollisions.ReadValue<float>();

            Dependency = Entities
                .ForEach((ref PlatformerInputs inputs) =>
                {
                    inputs.Move = moveInput;
                    inputs.Look = lookInput;
                    inputs.CameraZoom = cameraZoomInput;

                    inputs.JumpButton.UpdateWithValue(jumpInput, fixedTick);
                    inputs.RollButton.UpdateWithValue(rollInput, fixedTick);
                    inputs.SprintButton.UpdateWithValue(sprintInput, fixedTick);
                    inputs.DashButton.UpdateWithValue(dashInput, fixedTick);
                    inputs.CrouchButton.UpdateWithValue(crouchInput, fixedTick);
                    inputs.RopeButton.UpdateWithValue(ropeInput, fixedTick);
                    inputs.ClimbButton.UpdateWithValue(climbInput, fixedTick);
                    inputs.FlyNoCollisionsButton.UpdateWithValue(flyNoCollisionsInput, fixedTick);

                }).ScheduleParallel(Dependency);
        }
    }
}