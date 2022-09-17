using System.Collections.Generic;
using Rival.Samples.Platformer;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    [DisallowMultipleComponent]
    public class PlatformerCharacterAuthoring : MonoBehaviour
    {
        public GameObject MeshPrefab;

        public GameObject DefaultCameraTarget;
        public GameObject SwimmingCameraTarget;
        public GameObject ClimbingCameraTarget;
        public GameObject CrouchingCameraTarget;
        public GameObject MeshRoot;
        public GameObject RollballMesh;
        public GameObject RopePrefab;
        public GameObject LedgeDetectionPoint;
        public GameObject SwimmingDetectionPoint;
        public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
        public PlatformerCharacterComponent PlatformerCharacter;

        [Header("Debug")]
        public bool DebugStandingGeometry;
        public bool DebugCrouchingGeometry;
        public bool DebugRollingGeometry;
        public bool DebugSlidingGeometry;
        public bool DebugClimbingGeometry;
        public bool DebugSwimmingGeometry;

        private void OnDrawGizmosSelected()
        {
            if (DebugStandingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.StandingGeometry);
            }
            if (DebugCrouchingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.CrouchingGeometry);
            }
            if (DebugRollingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.RollingGeometry);
            }
            if (DebugSlidingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.SlidingGeometry);
            }
            if (DebugClimbingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.ClimbingGeometry);
            }
            if (DebugSwimmingGeometry)
            {
                Gizmos.color = Color.cyan;
                DrawCapsuleGizmo(PlatformerCharacter.SwimmingGeometry);
            }
        }

        private void DrawCapsuleGizmo(CapsuleGeometryDefinition capsuleGeo)
        {
            RigidTransform characterTransform = new RigidTransform(transform.rotation, transform.position);
            float3 characterUp = transform.up;
            float3 characterFwd = transform.forward;
            float3 characterRight = transform.right;
            float3 capsuleCenter = math.transform(characterTransform, capsuleGeo.Center);
            float halfHeight = capsuleGeo.Height * 0.5f;

            float3 bottomHemiCenter = capsuleCenter - (characterUp * (halfHeight - capsuleGeo.Radius));
            float3 topHemiCenter = capsuleCenter + (characterUp * (halfHeight - capsuleGeo.Radius));

            Gizmos.DrawWireSphere(bottomHemiCenter, capsuleGeo.Radius);
            Gizmos.DrawWireSphere(topHemiCenter, capsuleGeo.Radius);

            Gizmos.DrawLine(bottomHemiCenter + (characterFwd * capsuleGeo.Radius), topHemiCenter + (characterFwd * capsuleGeo.Radius));
            Gizmos.DrawLine(bottomHemiCenter - (characterFwd * capsuleGeo.Radius), topHemiCenter - (characterFwd * capsuleGeo.Radius));
            Gizmos.DrawLine(bottomHemiCenter + (characterRight * capsuleGeo.Radius), topHemiCenter + (characterRight * capsuleGeo.Radius));
            Gizmos.DrawLine(bottomHemiCenter - (characterRight * capsuleGeo.Radius), topHemiCenter - (characterRight * capsuleGeo.Radius));
        }
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    class PlatformerCharacterPrefabDeclarationSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlatformerCharacterAuthoring authoring) =>
            {
                DeclareReferencedPrefab(authoring.RopePrefab);
            });
        }
    }

    [UpdateAfter(typeof(EndColliderConversionSystem))]
    public class PlatformerCharacterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlatformerCharacterAuthoring authoring) =>
            {
                Entity entity = GetPrimaryEntity(authoring.gameObject);

                KinematicCharacterUtilities.HandleConversionForCharacter(DstEntityManager, entity, authoring.gameObject, authoring.CharacterBody);

                authoring.PlatformerCharacter.DefaultCameraTargetEntity = GetPrimaryEntity(authoring.DefaultCameraTarget);
                authoring.PlatformerCharacter.SwimmingCameraTargetEntity = GetPrimaryEntity(authoring.SwimmingCameraTarget);
                authoring.PlatformerCharacter.ClimbingCameraTargetEntity = GetPrimaryEntity(authoring.ClimbingCameraTarget);
                authoring.PlatformerCharacter.CrouchingCameraTargetEntity = GetPrimaryEntity(authoring.CrouchingCameraTarget);
                authoring.PlatformerCharacter.MeshRootEntity = GetPrimaryEntity(authoring.MeshRoot);
                authoring.PlatformerCharacter.RopePrefabEntity = GetPrimaryEntity(authoring.RopePrefab);
                authoring.PlatformerCharacter.RollballMeshEntity = GetPrimaryEntity(authoring.RollballMesh);
                authoring.PlatformerCharacter.LedgeDetectionPointEntity = GetPrimaryEntity(authoring.LedgeDetectionPoint);
                authoring.PlatformerCharacter.SwimmingDetectionPointEntity = GetPrimaryEntity(authoring.SwimmingDetectionPoint);

                DstEntityManager.AddComponentData(entity, authoring.PlatformerCharacter);
                DstEntityManager.AddComponentData(entity, new PlatformerCharacterStateMachine());
                DstEntityManager.AddComponentData(entity, new PlatformerCharacterInputs());

                DstEntityManager.AddComponentObject(entity, new PlatformerCharacterHybridData { MeshPrefab = authoring.MeshPrefab });

                DeclareLinkedEntityGroup(authoring.MeshRoot);
                DeclareLinkedEntityGroup(authoring.RollballMesh);
            });
        }
    }
}