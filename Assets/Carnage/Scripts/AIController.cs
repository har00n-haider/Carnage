using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;

[Serializable]
[GenerateAuthoringComponent]
public struct AIController : IComponentData
{
    public float DetectionDistance;
    public PhysicsCategoryTags DetectionFilter;
}