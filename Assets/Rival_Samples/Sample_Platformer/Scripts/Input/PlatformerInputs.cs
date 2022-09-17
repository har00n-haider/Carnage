using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [GenerateAuthoringComponent]
    public struct PlatformerInputs : IComponentData
    {
        [HideInInspector]
        public Entity CameraReference;

        [HideInInspector]
        public float2 Move;
        [HideInInspector]
        public float2 Look;

        [HideInInspector]
        public float CameraZoom;

        [HideInInspector]
        public FixedStepButton JumpButton;
        [HideInInspector]
        public FixedStepButton RollButton;
        [HideInInspector]
        public FixedStepButton SprintButton;
        [HideInInspector]
        public FixedStepButton DashButton;
        [HideInInspector]
        public FixedStepButton CrouchButton;
        [HideInInspector]
        public FixedStepButton RopeButton;
        [HideInInspector]
        public FixedStepButton ClimbButton;
        [HideInInspector]
        public FixedStepButton FlyNoCollisionsButton;
    }
}
