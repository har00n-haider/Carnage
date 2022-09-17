using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

namespace Rival.Samples.Basic
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BasicPlayerInputsSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class BasicPlayerInputsToCharacterInputsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency = Entities.ForEach((ref BasicPlayerInputs inputs, ref BasicCharacterInputs characterInputs) =>
            {
                if (HasComponent<OrbitCamera>(inputs.CameraReference))
                {
                    OrbitCamera orbitCamera = GetComponent<OrbitCamera>(inputs.CameraReference);
                    if (orbitCamera.FollowedEntity != Entity.Null)
                    {
                        quaternion cameraRotation = GetComponent<Rotation>(inputs.CameraReference).Value;
                        quaternion cameraFollowedEntityRotation = GetComponent<LocalToWorld>(orbitCamera.FollowedEntity).Rotation;

                        float3 cameraFwd = Rival.MathUtilities.GetForwardFromRotation(cameraRotation);
                        float3 cameraRight = Rival.MathUtilities.GetRightFromRotation(cameraRotation);
                        float3 cameraFollowedEntityUp = Rival.MathUtilities.GetUpFromRotation(cameraFollowedEntityRotation);
                        float3 cameraForwardOnPlane = math.normalizesafe(Rival.MathUtilities.ProjectOnPlane(cameraFwd, cameraFollowedEntityUp));

                        characterInputs.WorldMoveVector = (cameraRight * inputs.Move.x) + (cameraForwardOnPlane * inputs.Move.y);
                        characterInputs.TargetLookDirection = cameraForwardOnPlane;

                        characterInputs.JumpRequested = inputs.JumpButton.WasPressed;
                    }
                }
            }).ScheduleParallel(Dependency);
        }
    }
}