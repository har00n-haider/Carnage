using System;
using Unity.Entities;
using Unity.Collections;

public partial class CommonGameSystem : SystemBase
{

    private Entity _railgunPrefabEntity;

    private EntityQuery characterQuery;

    protected override void OnStartRunning()
    {
        base.OnCreate();
        characterQuery = GetEntityQuery(typeof(MainCharacter));

        // Get prefab
        if (_railgunPrefabEntity == Entity.Null)
        {
            Entity ghostPrefabsReference = GetSingletonEntity<GamePrefabsReference>();
            CarnageFPSUtilities.GetGamePrefabOfType<Railgun>(EntityManager, ghostPrefabsReference, out _railgunPrefabEntity);
        }

        Entity railgunPrefabEntity = _railgunPrefabEntity;


        using (NativeArray<Entity> characterEntities = characterQuery.ToEntityArray(Allocator.TempJob))
        {

            foreach (var characterEntity in characterEntities)
            {


                // Spawn weapon and set as activeWeapon
                Entity weaponInstance = EntityManager.Instantiate(railgunPrefabEntity);
                ActiveWeapon activeWeapon = EntityManager.GetComponentData<ActiveWeapon>(characterEntity);
                activeWeapon.WeaponEntity = weaponInstance;
                EntityManager.SetComponentData(characterEntity, activeWeapon);


            }



        }
    }

    protected override void OnUpdate()
    {

    }

}


