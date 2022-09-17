using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct MapIsLoaded : IComponentData
    { 
    }
}