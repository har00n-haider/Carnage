using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct PlatformerCharacterStateMachine : IComponentData
    {
        [HideInInspector]
        public CharacterState CurrentCharacterState;
        [HideInInspector]
        public CharacterState PreviousCharacterState;

        [HideInInspector]
        public DisabledNoCollisionsState DisabledNoCollisionsState;
        [HideInInspector]
        public DisabledState DisabledWithCollisionsState;
        [HideInInspector]
        public GroundMoveState GroundMoveState;
        [HideInInspector]
        public CrouchedState CrouchedState;
        [HideInInspector]
        public AirMoveState AirMoveState;
        [HideInInspector]
        public WallRunState WallRunState;
        [HideInInspector]
        public RollingState RollingState;
        [HideInInspector]
        public ClimbingState ClimbingState;
        [HideInInspector]
        public DashingState DashingState;
        [HideInInspector]
        public SlidingState SlidingState;
        [HideInInspector]
        public SwimmingState SwimmingState;
        [HideInInspector]
        public LedgeGrabState LedgeGrabState;
        [HideInInspector]
        public LedgeStandingUpState LedgeStandingUpState;
        [HideInInspector]
        public FlyingNoCollisionsState FlyingNoCollisionsState;
        [HideInInspector]
        public RopeSwingState RopeSwingState;

        public void OnStateEnter(CharacterState state, CharacterState previousState, ref PlatformerCharacterProcessor processor)
        {
            switch (state)
            {
                case CharacterState.DisabledNoCollisions:
                    DisabledNoCollisionsState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.DisabledWithCollisions:
                    DisabledWithCollisionsState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.GroundMove:
                    GroundMoveState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Crouched:
                    CrouchedState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.AirMove:
                    AirMoveState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.WallRun:
                    WallRunState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Rolling:
                    RollingState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Sliding:
                    SlidingState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.LedgeGrab:
                    LedgeGrabState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.LedgeStandingUp:
                    LedgeStandingUpState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Dashing:
                    DashingState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Swimming:
                    SwimmingState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.Climbing:
                    ClimbingState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.FlyingNoCollisions:
                    FlyingNoCollisionsState.OnStateEnter(previousState, ref processor);
                    break;
                case CharacterState.RopeSwing:
                    RopeSwingState.OnStateEnter(previousState, ref processor);
                    break;
            }
        }

        public void OnStateExit(CharacterState state, CharacterState newState, ref PlatformerCharacterProcessor processor)
        {
            switch (state)
            {
                case CharacterState.DisabledNoCollisions:
                    DisabledNoCollisionsState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.DisabledWithCollisions:
                    DisabledWithCollisionsState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.GroundMove:
                    GroundMoveState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Crouched:
                    CrouchedState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.AirMove:
                    AirMoveState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.WallRun:
                    WallRunState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Rolling:
                    RollingState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Sliding:
                    SlidingState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.LedgeGrab:
                    LedgeGrabState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.LedgeStandingUp:
                    LedgeStandingUpState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Dashing:
                    DashingState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Swimming:
                    SwimmingState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.Climbing:
                    ClimbingState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.FlyingNoCollisions:
                    FlyingNoCollisionsState.OnStateExit(newState, ref processor);
                    break;
                case CharacterState.RopeSwing:
                    RopeSwingState.OnStateExit(newState, ref processor);
                    break;
            }
        }

        public void OnStateUpdate(CharacterState state, ref PlatformerCharacterProcessor processor)
        {
            switch (state)
            {
                case CharacterState.DisabledNoCollisions:
                    DisabledNoCollisionsState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.DisabledWithCollisions:
                    DisabledWithCollisionsState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.GroundMove:
                    GroundMoveState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Crouched:
                    CrouchedState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.AirMove:
                    AirMoveState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.WallRun:
                    WallRunState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Rolling:
                    RollingState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Sliding:
                    SlidingState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.LedgeGrab:
                    LedgeGrabState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.LedgeStandingUp:
                    LedgeStandingUpState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Dashing:
                    DashingState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Swimming:
                    SwimmingState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.Climbing:
                    ClimbingState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.FlyingNoCollisions:
                    FlyingNoCollisionsState.OnStateUpdate(ref processor);
                    break;
                case CharacterState.RopeSwing:
                    RopeSwingState.OnStateUpdate(ref processor);
                    break;
            }
        }
    }
}
