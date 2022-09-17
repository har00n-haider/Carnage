using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Rival;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class FirstPersonPlayerSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        FixedUpdateTickSystem = World.GetOrCreateSystem<FixedUpdateTickSystem>();
    }

    protected override void OnUpdate()
    {
        uint fixedTick = FixedUpdateTickSystem.FixedTick;

        // Gather input
        float2 moveInput = float2.zero;
        moveInput.y += Input.GetKey(KeyCode.W) ? 1f : 0f;
        moveInput.y += Input.GetKey(KeyCode.S) ? -1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.D) ? 1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.A) ? -1f : 0f;
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        float2 lookInput = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);


        // Iterate on all Player components to apply input to their character
        Entities
            .ForEach((ref FirstPersonPlayer player) =>
            {
                if (HasComponent<FirstPersonCharacterInputs>(player.ControlledCharacter) && HasComponent<FirstPersonCharacterComponent>(player.ControlledCharacter))
                {
                    FirstPersonCharacterInputs characterInputs = GetComponent<FirstPersonCharacterInputs>(player.ControlledCharacter);
                    FirstPersonCharacterComponent character = GetComponent<FirstPersonCharacterComponent>(player.ControlledCharacter);

                    quaternion characterRotation = GetComponent<LocalToWorld>(player.ControlledCharacter).Rotation;
                    quaternion localCharacterViewRotation = GetComponent<Rotation>(character.CharacterViewEntity).Value;

                    // Look
                    characterInputs.LookYawPitchDegrees = lookInput * player.RotationSpeed;

                    // Move
                    float3 characterForward = math.mul(characterRotation, math.forward());
                    float3 characterRight = math.mul(characterRotation, math.right());
                    characterInputs.MoveVector = (moveInput.y * characterForward) + (moveInput.x * characterRight);
                    characterInputs.MoveVector = Rival.MathUtilities.ClampToMaxLength(characterInputs.MoveVector, 1f);

                    // Jump
                    // Punctual input presses need special handling when they will be used in a fixed step system.
                    // We essentially need to remember if the button was pressed at any point over the last fixed update
                    if (player.LastInputsProcessingTick == fixedTick)
                    {
                        characterInputs.JumpRequested = jumpInput || characterInputs.JumpRequested;
                    }
                    else
                    {
                        characterInputs.JumpRequested = jumpInput;
                    }
                    
                    // Sprint
                    characterInputs.Sprint = sprintInput;


                    player.LastInputsProcessingTick = fixedTick;

                    SetComponent(player.ControlledCharacter, characterInputs);
                }
            }).Schedule();
    }
}
