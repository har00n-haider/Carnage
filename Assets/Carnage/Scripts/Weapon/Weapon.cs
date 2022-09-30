using Rival;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;


[Serializable]
public struct Weapon : IComponentData
{
    public Entity OwnerEntity;
    public Entity ShootOriginOverride;
    public bool ShootRequested;
}

[Serializable]
public struct ActiveWeapon : IComponentData
{
    public Entity WeaponEntity;
    public Entity PreviousWeaponEntity;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(KinematicCharacterUpdateGroup))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public class WeaponUpdateGroup : ComponentSystemGroup
{ }
