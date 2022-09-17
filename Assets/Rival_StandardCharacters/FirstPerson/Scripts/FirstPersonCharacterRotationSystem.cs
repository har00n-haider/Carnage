using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FirstPersonPlayerSystem))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class FirstPersonCharacterRotationSystem : SystemBase
{
    public FixedStepSimulationSystemGroup FixedStepSimulationSystemGroup;
    public EntityQuery CharacterQuery;

    protected override void OnCreate()
    {
        FixedStepSimulationSystemGroup = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();

        CharacterQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(FirstPersonCharacterComponent),
                    typeof(FirstPersonCharacterInputs),
                }),
        });

        RequireForUpdate(CharacterQuery);
    }

    protected unsafe override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float fixedDeltaTime = FixedStepSimulationSystemGroup.RateManager.Timestep;

        Entities.ForEach((
            Entity entity,
            ref CharacterInterpolation characterInterpolation,
            ref FirstPersonCharacterComponent character,
            in FirstPersonCharacterInputs characterInputs,
            in KinematicCharacterBody characterBody) =>
        {
            Rotation characterRotation = GetComponent<Rotation>(entity);
            Rotation localViewRotation = GetComponent<Rotation>(character.CharacterViewEntity);

            // Compute character & view rotations from rotation input
            FirstPersonCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                ref characterRotation.Value,
                ref character.ViewPitchDegrees,
                characterInputs.LookYawPitchDegrees,
                0f,
                character.MinVAngle,
                character.MaxVAngle,
                out float canceledPitchDegrees,
                out localViewRotation.Value);

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref characterRotation.Value, in characterBody, fixedDeltaTime, deltaTime);

            // Apply character & view rotations
            SetComponent(entity, characterRotation);
            SetComponent(character.CharacterViewEntity, localViewRotation);
        }).Schedule();
    }
}