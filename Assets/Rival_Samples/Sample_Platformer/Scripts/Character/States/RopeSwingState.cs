using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Rival.Samples.Platformer
{
    public struct RopeSwingState : IPlatformerCharacterState
    {
        public float3 AnchorPoint;

        public void OnStateEnter(CharacterState previousState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.EvaluateGrounding = false;

            // Spawn rope
            Entity ropeInstanceEntity = p.CommandBuffer.Instantiate(p.IndexInChunk, p.PlatformerCharacter.RopePrefabEntity);
            p.CommandBuffer.AddComponent(p.IndexInChunk, ropeInstanceEntity, new CharacterRope { OwningCharacterEntity = p.Entity });
        }

        public void OnStateExit(CharacterState nextState, ref PlatformerCharacterProcessor p)
        {
            p.CharacterBody.EvaluateGrounding = true;
        }

        public void OnStateUpdate(ref PlatformerCharacterProcessor p)
        {
            p.CharacterGroundingAndParentMovementUpdate();

            HandleCharacterControl(ref p);

            p.CharacterMovementAndFinalizationUpdate(false);

            if (!DetectTransitions(ref p))
            {
                p.DetectGlobalTransitions();
            }
        }

        public void HandleCharacterControl(ref PlatformerCharacterProcessor p)
        {
            // Move
            float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(p.CharacterInputs.WorldMoveVector, p.GroundingUp)) * math.length(p.CharacterInputs.WorldMoveVector);
            float3 acceleration = moveVectorOnPlane * p.PlatformerCharacter.RopeSwingAcceleration;
            CharacterControlUtilities.StandardAirMove(ref p.CharacterBody.RelativeVelocity, acceleration, p.PlatformerCharacter.RopeSwingMaxSpeed, p.GroundingUp, p.DeltaTime, false);

            // Gravity
            CharacterControlUtilities.AccelerateVelocity(ref p.CharacterBody.RelativeVelocity, p.CustomGravity.Gravity, p.DeltaTime);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref p.CharacterBody.RelativeVelocity, p.DeltaTime, p.PlatformerCharacter.RopeSwingDrag);

            // Rope constraint
            RigidTransform characterTransform = new RigidTransform(p.Rotation, p.Translation);
            ConstrainToRope(ref p.Translation, ref p.CharacterBody.RelativeVelocity, p.PlatformerCharacter.RopeLength, AnchorPoint, math.transform(characterTransform, p.PlatformerCharacter.LocalRopeAnchorPoint));

            // Orientation
            p.OrientCharacterOnPlaneTowardsMoveInput(p.PlatformerCharacter.AirRotationSharpness);
            p.OrientCharacterUpTowardsDirection(math.normalizesafe(AnchorPoint - p.Translation), p.PlatformerCharacter.UpOrientationAdaptationSharpness);
        }

        public bool DetectTransitions(ref PlatformerCharacterProcessor p)
        {
            if (p.CharacterInputs.JumpPressed || p.CharacterInputs.DashPressed)
            {
                p.TransitionToState(CharacterState.AirMove);
                return true;
            }

            return false;
        }

        public static bool DetectRopePoints(ref PlatformerCharacterProcessor p, out float3 point)
        {
            point = default;

            RigidTransform characterTransform = new RigidTransform(p.Rotation, p.Translation);
            float3 ropeDetectionPoint = math.transform(characterTransform, p.PlatformerCharacter.LocalRopeAnchorPoint);

            CollisionFilter ropeAnchorDetectionFilter = CollisionFilter.Default;
            ropeAnchorDetectionFilter.CollidesWith = p.PlatformerCharacter.RopeAnchorCategory.Value;

            PointDistanceInput pointInput = new PointDistanceInput
            {
                Filter = ropeAnchorDetectionFilter,
                MaxDistance = p.PlatformerCharacter.RopeLength,
                Position = ropeDetectionPoint,
            };

            if (p.CollisionWorld.CalculateDistance(pointInput, out DistanceHit closestHit))
            {
                point = closestHit.Position;
                return true;
            }

            return false;
        }

        public static void ConstrainToRope(
            ref float3 translation,
            ref float3 velocity,
            float ropeLength,
            float3 ropeAnchorPoint,
            float3 ropeAnchorPointOnCharacter)
        {
            float3 characterToRopeVector = ropeAnchorPoint - ropeAnchorPointOnCharacter;
            float3 ropeNormal = math.normalizesafe(characterToRopeVector);

            if (math.length(characterToRopeVector) >= ropeLength)
            {
                float3 targetAnchorPointOnCharacter = ropeAnchorPoint - MathUtilities.ClampToMaxLength(characterToRopeVector, ropeLength);
                translation += (targetAnchorPointOnCharacter - ropeAnchorPointOnCharacter);

                if (math.dot(velocity, ropeNormal) < 0f)
                {
                    velocity = MathUtilities.ProjectOnPlane(velocity, ropeNormal);
                }
            }
        }
    }
}