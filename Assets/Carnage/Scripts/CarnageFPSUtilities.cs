using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;


public static class CarnageFPSUtilities
{

    public static bool GetGamePrefabOfType<T>(EntityManager entityManager, Entity gameCollectionEntity, out Entity prefabEntity) where T : struct
    {
        prefabEntity = default;

        DynamicBuffer<GamePrefabsReference> prefabs = entityManager.GetBuffer<GamePrefabsReference>(gameCollectionEntity);
        for (int i = 0; i < prefabs.Length; ++i)
        {
            if (entityManager.HasComponent<T>(prefabs[i].Value))
            {
                prefabEntity = prefabs[i].Value;
                return true;
            }
        }

        return false;
    }

    public static void SetShadowModeInHierarchy(EntityManager entityManager, EntityCommandBuffer commandBuffer, Entity onEntity, BufferFromEntity<Child> childBufferFromEntity, UnityEngine.Rendering.ShadowCastingMode mode)
    {
        if (entityManager.HasComponent<RenderMesh>(onEntity))
        {
            RenderMesh renderMesh = entityManager.GetSharedComponentData<RenderMesh>(onEntity);
            renderMesh.castShadows = mode;
            commandBuffer.SetSharedComponent<RenderMesh>(onEntity, renderMesh);
        }

        if (childBufferFromEntity.HasComponent(onEntity))
        {
            DynamicBuffer<Child> childBuffer = childBufferFromEntity[onEntity];
            for (int i = 0; i < childBuffer.Length; i++)
            {
                SetShadowModeInHierarchy(entityManager, commandBuffer, childBuffer[i].Value, childBufferFromEntity, mode);
            }
        }
    }

    public static T GetOrCreateSingleton<T>(World inWorld) where T : struct, IComponentData
    {
        if (inWorld.Systems.Count > 0)
        {
            ComponentSystemBase anySystem = inWorld.Systems[0];
            if (!anySystem.HasSingleton<T>())
            {
                inWorld.EntityManager.CreateEntity(typeof(T));
            }

            return anySystem.GetSingleton<T>();
        }

        return default;
    }

    public static void SetParent(
        EntityCommandBuffer commandBuffer,
        ComponentDataFromEntity<Parent> parentFromEntity,
        ComponentDataFromEntity<LocalToParent> localToParentFromEntity,
        Entity parent, 
        Entity child, 
        float3 localTranslation, 
        quaternion localRotation)
    {
        commandBuffer.SetComponent(child, new Translation { Value = localTranslation });
        commandBuffer.SetComponent(child, new Rotation { Value = localRotation });

        // Add Parent
        if (!parentFromEntity.HasComponent(child))
        {
            commandBuffer.AddComponent(child, new Parent { Value = parent });
        }
        else
        {
            commandBuffer.SetComponent(child, new Parent { Value = parent });
        }

        // Add LocalToParent
        if (!localToParentFromEntity.HasComponent(child))
        {
            commandBuffer.AddComponent(child, new LocalToParent());
        }
    }
}
