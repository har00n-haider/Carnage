using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    [GhostComponent(OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct OnlineFPSPlayerCommands : ICommandData
    {
        [GhostField]
        public uint Tick { get; set; }

        [GhostField]
        public float2 MoveInput;
        [GhostField]
        public float2 LookInput;
        [GhostField]
        public bool JumpRequested;
        [GhostField]
        public bool ShootRequested;
        [GhostField]
        public bool AimHeld;
    }
}