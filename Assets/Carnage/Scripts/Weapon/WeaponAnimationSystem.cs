using Rival;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateBefore(typeof(TransformSystemGroup))]
public partial class WeaponAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float elapsedTime = (float)Time.ElapsedTime;

        // Weapon Bob & Recoil
        Entities
            .ForEach((
                Entity entity,
                ref FirstPersonCharacterComponent character,
                in KinematicCharacterBody characterBody,
                in ActiveWeapon activeWeapon) =>
            {

                    bool isAiming = false;
                    float characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;
                    LocalToWorld cameraLocalToWorld = GetComponent<LocalToWorld>(character.CharacterViewEntity);

                    if (activeWeapon.WeaponEntity != Entity.Null)
                    {
                        float characterVelocityRatio = math.length(characterBody.RelativeVelocity) / characterMaxSpeed;

                        // Weapon bob
                        {
                            float3 targetBobPos = default;
                            float3 targetBobRot = default;
                            if (characterBody.IsGrounded)
                            {
                                float bobSpeedMultiplier = isAiming ? character.WeaponBobAimRatio : 1f;
                                float hBob = math.sin(elapsedTime * character.WeaponBobFrequency) * character.WeaponBobHAmount * bobSpeedMultiplier * characterVelocityRatio;
                                float vBob = ((math.sin(elapsedTime * character.WeaponBobFrequency * 2f) * 0.5f) + 0.5f) * character.WeaponBobVAmount * bobSpeedMultiplier * characterVelocityRatio;
                                float tBob = math.sin(elapsedTime * character.WeaponBobFrequency) * character.WeaponBobTAmount * bobSpeedMultiplier * characterVelocityRatio;
                                targetBobPos = new float3(hBob, vBob, 0f);
                                targetBobRot = new float3(0f, 0f, tBob);
                            }
                            character.WeaponLocalPosBob = math.lerp(character.WeaponLocalPosBob, targetBobPos, math.saturate(character.WeaponBobSharpness * deltaTime));
                        }

                        // Weapon recoil
                        {
                            // go towards recoil
                            if (character.WeaponLocalPosRecoil.z >= character.RecoilVector.z * 0.99f)
                            {
                                character.WeaponLocalPosRecoil = math.lerp(character.WeaponLocalPosRecoil, character.RecoilVector, math.saturate(character.RecoilSharpness * deltaTime));
                            }
                            // go towards restitution
                            else
                            {
                                character.WeaponLocalPosRecoil = math.lerp(character.WeaponLocalPosRecoil, float3.zero, math.saturate(character.RecoilRestitutionSharpness * deltaTime));
                                character.RecoilVector = character.WeaponLocalPosRecoil;
                            }

                            // FOV go towards recoil
                            if (character.CurrentRecoilFOVKick <= character.TargetRecoilFOVKick * 0.99f)
                            {
                                character.CurrentRecoilFOVKick = math.lerp(character.CurrentRecoilFOVKick, character.TargetRecoilFOVKick, math.saturate(character.RecoilFOVKickSharpness * deltaTime));
                            }
                            // FOV go towards restitution
                            else
                            {
                                character.CurrentRecoilFOVKick = math.lerp(character.CurrentRecoilFOVKick, 0f, math.saturate(character.RecoilFOVKickRestitutionSharpness * deltaTime));
                                character.TargetRecoilFOVKick = character.CurrentRecoilFOVKick;
                            }
                        }

                        // Final weapon pose
                        float3 targetWeaponLocalPosition = character.WeaponLocalPosBob + character.WeaponLocalPosRecoil;
                        SetComponent(activeWeapon.WeaponEntity, new Translation { Value = targetWeaponLocalPosition });
                    }
                
            }).Schedule();
    }
}
