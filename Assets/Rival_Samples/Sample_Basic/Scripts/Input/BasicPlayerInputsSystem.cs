using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class BasicPlayerInputsSystem : SystemBase
    {
        public BasicInputActions InputActions;

        private FixedStepTimeSystem _fixedStepTickCounterSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            _fixedStepTickCounterSystem = World.GetOrCreateSystem<FixedStepTimeSystem>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            // Create the input user
            InputActions = new BasicInputActions();
            InputActions.Enable();
            InputActions.DefaultMap.Enable();
        }

        protected override void OnUpdate()
        {
            uint fixedTick = _fixedStepTickCounterSystem.Tick;

            // finish updating inputs
            BasicInputActions.DefaultMapActions defaultActionsMap = InputActions.DefaultMap;

            float2 moveInput = Vector2.ClampMagnitude(defaultActionsMap.Move.ReadValue<Vector2>(), 1f);
            float2 lookInput = defaultActionsMap.LookDelta.ReadValue<Vector2>();
            if(math.lengthsq(defaultActionsMap.LookConst.ReadValue<Vector2>()) > math.lengthsq(defaultActionsMap.LookDelta.ReadValue<Vector2>()))
            {
                lookInput = defaultActionsMap.LookConst.ReadValue<Vector2>() * Time.DeltaTime;
            }
            float scrollInput = defaultActionsMap.Scroll.ReadValue<float>();
            float jumpInput = defaultActionsMap.Jump.ReadValue<float>();

            // Write inputs to all entities that have the component
            Dependency = Entities.ForEach((ref BasicPlayerInputs inputs) =>
            {
                inputs.Move = moveInput;
                inputs.Look = lookInput;
                inputs.Scroll = scrollInput;

                inputs.JumpButton.UpdateWithValue(jumpInput, fixedTick);

            }).Schedule(Dependency);
        }
    }
}