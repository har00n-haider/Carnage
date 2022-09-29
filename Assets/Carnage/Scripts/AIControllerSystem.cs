using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

public partial class AIControllerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;

        NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.TempJob);

        Entities
            .WithDisposeOnCompletion(distanceHits) // Dispose the list when the job is done
            .ForEach((ref ThirdPersonCharacterInputs characterInputs, in AIController aiController, in ThirdPersonCharacterComponent character, in Translation translation) =>
            {
                UnityEngine.Debug.Log("Running AI system");

                // Clear our detected hits list between each use
                distanceHits.Clear();

                // Create a hit collector for the detection hits
                AllHitsCollector<DistanceHit> hitsCollector = new AllHitsCollector<DistanceHit>(aiController.DetectionDistance, ref distanceHits);

                // Detect hits that are within the detection range of the AI character
                PointDistanceInput distInput = new PointDistanceInput
                {
                    Position = translation.Value,
                    MaxDistance = aiController.DetectionDistance,
                    Filter = new CollisionFilter { BelongsTo = CollisionFilter.Default.BelongsTo, CollidesWith = aiController.DetectionFilter.Value },
                };
                physicsWorld.CalculateDistance(distInput, ref hitsCollector);

                // Iterate on all detected hits to try to find a human-controlled character...
                Entity selectedTarget = Entity.Null;
                for (int i = 0; i < hitsCollector.NumHits; i++)
                {
                    Entity hitEntity = distanceHits[i].Entity;

                    // If it has a character component but no AIController component, that means it's a human player character
                    if (HasComponent<ThirdPersonCharacterComponent>(hitEntity) && !HasComponent<AIController>(hitEntity))
                    {
                        selectedTarget = hitEntity;
                        break; // early out
                    }
                }

                // In the character inputs component, set a movement vector that will make the ai character move towards the selected target
                if (selectedTarget != Entity.Null)
                {
                    characterInputs.MoveVector = math.normalizesafe((GetComponent<Translation>(selectedTarget).Value - translation.Value));
                }
                else
                {
                    UnityEngine.Debug.Log("Not moving");
                    characterInputs.MoveVector = float3.zero;
                }
            }).Schedule();
    }
}