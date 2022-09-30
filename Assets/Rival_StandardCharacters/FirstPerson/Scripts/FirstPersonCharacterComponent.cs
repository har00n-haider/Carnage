using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics.Authoring;


[Serializable]
public struct FirstPersonCharacterComponent : IComponentData
{
    [Header("Movement")]
    public float RotationSharpness;
    public float GroundMaxSpeed;
    public float GroundedMovementSharpness;
    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;
    public float JumpSpeed;
    public int MaxAirJumps;
    public float3 Gravity;

    [Header("Step Handling")]
    public bool StepHandling;
    public float MaxStepHeight;
    public float ExtraStepChecksDistance;

    [Header("Slope Changes")]
    public bool PreventGroundingWhenMovingTowardsNoGrounding;
    public bool HasMaxDownwardSlopeChangeAngle;
    [Range(0f, 180f)]
    public float MaxDownwardSlopeChangeAngle;

    [Header("View Limits")]
    public float MinVAngle;
    public float MaxVAngle;

    [Header("Misc")]
    public bool ConstrainVelocityToGroundPlane;
    public float SprintSpeedMultiplier;

    [Header("Weapon")]
    public float WeaponBobHAmount;
    public float WeaponBobVAmount;
    public float WeaponBobTAmount;
    public float WeaponBobFrequency;
    public float WeaponBobSharpness;
    public float WeaponBobAimRatio;
    public float RecoilMaxDistance;
    public float RecoilSharpness;
    public float RecoilRestitutionSharpness;
    public float RecoilMaxFOVKick;
    public float RecoilFOVKickSharpness;
    public float RecoilFOVKickRestitutionSharpness;

    [HideInInspector]
    public float ViewPitchDegrees;

    [HideInInspector]
    public float CameraTiltAngle;

    [HideInInspector]
    public float3 RecoilVector;
    [HideInInspector]
    public float3 WeaponLocalPosBob;
    [HideInInspector]
    public float3 WeaponLocalPosRecoil;
    [HideInInspector]
    public float TargetRecoilFOVKick;
    [HideInInspector]
    public float CurrentRecoilFOVKick;
    [HideInInspector]
    public float ActiveBobMultiplier;

    public CustomPhysicsBodyTags IgnoredPhysicsTags;

    [HideInInspector]
    public Entity WeaponSocketEntity;

    [HideInInspector]
    public int CurrentAirJumps;
    [HideInInspector]
    public Entity CharacterViewEntity;
    [HideInInspector]
    public float3 GroundingUp;
    [HideInInspector]
    public Entity ViewEntity; // TODO: this has to be set up

    public static FirstPersonCharacterComponent GetDefault()
    {
        return new FirstPersonCharacterComponent
        {
            RotationSharpness = 25f,
            GroundMaxSpeed = 10f,
            GroundedMovementSharpness = 15f,
            AirAcceleration = 50f,
            AirMaxSpeed = 10f,
            AirDrag = 0f,
            JumpSpeed = 10f,
            Gravity = math.up() * -30f,

            StepHandling = false,
            MaxStepHeight = 0.5f,
            ExtraStepChecksDistance = 0.1f,

            PreventGroundingWhenMovingTowardsNoGrounding = true,
            HasMaxDownwardSlopeChangeAngle = false,
            MaxDownwardSlopeChangeAngle = 90f,

            MinVAngle = -90f,
            MaxVAngle = 90f,

            ConstrainVelocityToGroundPlane = true,

            GroundingUp = math.up(),
        };
    }
}

[Serializable]
public struct FirstPersonCharacterInputs : IComponentData
{
    public float3 MoveVector;
    public float2 LookYawPitchDegrees;
    public bool JumpRequested;
    public bool Sprint;


}
