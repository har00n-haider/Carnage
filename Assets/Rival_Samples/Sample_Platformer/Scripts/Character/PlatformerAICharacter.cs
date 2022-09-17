using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct PlatformerAICharacter : IComponentData
{
    public float MovementPeriod;
    public float3 MovementDirection;
}
