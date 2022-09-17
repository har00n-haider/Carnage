using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Rival
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct CameraTarget : IComponentData
    {
        public Entity CameraTargetEntity;
    }
}