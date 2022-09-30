using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public struct Health : IComponentData
{
    public float MaxHealth;

    public float CurrentHealth;

    public void ClampToMin()
    {
        CurrentHealth = math.max(0f, CurrentHealth);
    }

    public void ClampToMinMax()
    {
        CurrentHealth = math.clamp(CurrentHealth, 0f, MaxHealth);
    }
}
