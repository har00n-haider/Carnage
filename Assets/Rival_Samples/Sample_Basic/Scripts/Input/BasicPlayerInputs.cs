using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Basic
{
    [GenerateAuthoringComponent]
    public struct BasicPlayerInputs : IComponentData
    {
        [HideInInspector]
        public Entity CameraReference;

        [HideInInspector]
        public float2 Move;
        [HideInInspector]
        public float2 Look;
        [HideInInspector]
        public float Scroll;
        [HideInInspector]
        public FixedStepButton JumpButton;
    }
}
