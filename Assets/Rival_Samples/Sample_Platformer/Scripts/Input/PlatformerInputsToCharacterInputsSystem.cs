using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

namespace Rival.Samples.Platformer
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(PlatformerInputsSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class PlatformerInputsToCharacterInputsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency = Entities.ForEach((Entity entity, ref PlatformerInputs inputs, ref PlatformerCharacterInputs characterInputs, in CustomGravity gravity, in PlatformerCharacterComponent platformerCharacter, in PlatformerCharacterStateMachine characterStateMachine) =>
            {
                if (HasComponent<OrbitCamera>(inputs.CameraReference))
                {
                    OrbitCamera orbitCamera = GetComponent<OrbitCamera>(inputs.CameraReference);
                    if (orbitCamera.FollowedEntity != Entity.Null)
                    {
                        quaternion cameraRotation = GetComponent<Rotation>(inputs.CameraReference).Value;
                        quaternion cameraFollowedEntityRotation = GetComponent<LocalToWorld>(orbitCamera.FollowedEntity).Rotation;

                        float3 cameraFwd = Rival.MathUtilities.GetForwardFromRotation(cameraRotation);
                        float3 cameraRight = Rival.MathUtilities.GetRightFromRotation(cameraRotation);
                        float3 cameraUp = Rival.MathUtilities.GetUpFromRotation(cameraRotation);

                        // Determine movement vector based on character state
                        CharacterState state = characterStateMachine.CurrentCharacterState;
                        if (state == CharacterState.Climbing)
                        {
                            // Only use input if the camera is pointing towards the normal
                            if (math.dot(characterStateMachine.ClimbingState.LastKnownClimbNormal, cameraFwd) < -0.05f)
                            {
                                characterInputs.WorldMoveVector = (cameraRight * inputs.Move.x) + (cameraUp * inputs.Move.y);
                            }
                            else
                            {
                                characterInputs.WorldMoveVector = (cameraRight * inputs.Move.x) + (cameraFwd * inputs.Move.y);
                            }
                        }
                        else if (state == CharacterState.Swimming)
                        {
                            characterInputs.WorldMoveVector = (cameraRight * inputs.Move.x) + (cameraFwd * inputs.Move.y);
                            if (inputs.JumpButton.IsHeld)
                            {
                                characterInputs.WorldMoveVector += cameraUp;
                            }
                            if (inputs.RollButton.IsHeld)
                            {
                                characterInputs.WorldMoveVector -= cameraUp;
                            }
                            characterInputs.WorldMoveVector = MathUtilities.ClampToMaxLength(characterInputs.WorldMoveVector, 1f);
                        }
                        else
                        {
                            characterInputs.WorldMoveVector = (cameraRight * inputs.Move.x) + (cameraFwd * inputs.Move.y);
                        }

                        characterInputs.UpDirection = cameraUp;

                        characterInputs.JumpPressed = inputs.JumpButton.WasPressed;
                        characterInputs.DashPressed = inputs.DashButton.WasPressed;
                        characterInputs.CrouchPressed = inputs.CrouchButton.WasPressed;
                        characterInputs.RopePressed = inputs.RopeButton.WasPressed;
                        characterInputs.ClimbPressed = inputs.ClimbButton.WasPressed;
                        characterInputs.FlyNoCollisionsPressed = inputs.FlyNoCollisionsButton.WasPressed;

                        characterInputs.JumpHeld = inputs.JumpButton.IsHeld;
                        characterInputs.RollHeld = inputs.RollButton.IsHeld;
                        characterInputs.SprintHeld = inputs.SprintButton.IsHeld;
                    }
                }
            }).ScheduleParallel(Dependency);
        }
    }
}