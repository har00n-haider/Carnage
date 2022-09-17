using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GhostComponent(OwnerPredictedSendType = GhostSendType.All, PrefabType = GhostPrefabType.All, SendDataForChildEntity = false)]
    public struct OnlineFPSCharacterComponent : IComponentData
    {
        [Header("Movement")]
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;

        [Header("Step Handling")]
        public bool StepHandling;
        public float MaxStepHeight;
        public float ExtraStepChecksDistance;

        [Header("Misc")]
        public bool ConstrainVelocityToGroundPlane;

        [Header("View")]
        public float DefaultFOV;
        public float AimFOV;
        public float AimFOVSharpness;
        public float TiltAmount;
        public float TiltSharpness;

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
        public Entity MeshRootEntity;
        [HideInInspector]
        public Entity ViewEntity;
        [HideInInspector]
        public Entity WeaponSocketEntity;

        [GhostField(Quantization = -1)]
        [HideInInspector]
        public float ViewPitchDegrees;
        [GhostField(Quantization = -1)]
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
    }

    [Serializable]
    public struct OnlineFPSCharacterInputs : IComponentData
    {
        public float3 MoveVector;
        public float2 LookYawPitchDegrees;
        public bool JumpRequested;
    }
}
