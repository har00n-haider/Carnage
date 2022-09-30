using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Railgun : IComponentData
{
    public float FireRate;
    public float Damage;
    public float Range;
    public int HitSparksCount;
    public float Recoil;
    public float RecoilFOVKick;

    [HideInInspector]
    public Entity LazerPrefab;
    [HideInInspector]
    public Entity HitSparkPrefab;

    [HideInInspector]
    public float _firingTimer;
    [HideInInspector]
    public Entity _muzzleEntity;
    [HideInInspector]
    public uint _lastTickShot;
}
