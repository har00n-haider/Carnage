using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Rival.Samples.OnlineFPS
{
    [GhostComponentVariation(typeof(Rival.KinematicCharacterBody), "KinematicCharacterBodyDefault")]
    public struct KinematicCharacterBody_Default
    {
        [GhostField()]
        public float3 RelativeVelocity;
        [GhostField()]
        public bool IsGrounded;
        [GhostField()]
        public Entity ParentEntity;
    }

    [GhostComponentVariation(typeof(Rival.TrackedTransform), "TrackedTransformDefault")]
    public struct TrackedTransform_Default
    {
        [GhostField()]
        public RigidTransform CurrentFixedRateTransform;
    }

    [GhostComponentVariation(typeof(Translation), "Unquantized")]
    public struct Translation_Unquantized
    {
        [GhostField(Quantization = -1)]
        public float3 Value;
    }

    [GhostComponentVariation(typeof(Rotation), "Unquantized")]
    public struct Rotation_Unquantized
    {
        [GhostField(Quantization = -1)]
        public quaternion Value;
    }
}