using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct PlatformerCharacterInputs : IComponentData
    {
        [HideInInspector]
        public float3 WorldMoveVector;
        [HideInInspector]
        public float3 UpDirection;
        [HideInInspector]
        public bool JumpPressed;
        [HideInInspector]
        public bool JumpHeld;
        [HideInInspector]
        public bool RollHeld;
        [HideInInspector]
        public bool SprintHeld;
        [HideInInspector]
        public bool DashPressed;
        [HideInInspector]
        public bool CrouchPressed;
        [HideInInspector]
        public bool RopePressed;
        [HideInInspector]
        public bool ClimbPressed;
        [HideInInspector]
        public bool FlyNoCollisionsPressed;
    }
}