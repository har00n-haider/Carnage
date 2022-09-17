using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct FirstPersonPlayer : IComponentData
{
    public Entity ControlledCharacter;
    public float RotationSpeed;

    [NonSerialized]
    public uint LastInputsProcessingTick;
}
