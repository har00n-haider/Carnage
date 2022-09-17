using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival.Samples
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct MainEntityCamera : IComponentData
    {
        public float FoV;
    }
}