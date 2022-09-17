using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival.Samples.Platformer
{
    public static class PlatformerUtilities
    {
        public static void SetEntityHierarchyEnabled(bool enabled, Entity parent, EntityCommandBuffer commandBuffer, BufferFromEntity<LinkedEntityGroup> linkedEntityGroupFromEntity)
        {
            if (enabled)
            {
                commandBuffer.RemoveComponent<Disabled>(parent);
            }
            else
            {
                commandBuffer.AddComponent<Disabled>(parent);
            }

            if (linkedEntityGroupFromEntity.HasComponent(parent))
            {
                DynamicBuffer<LinkedEntityGroup> parentLinkedEntities = linkedEntityGroupFromEntity[parent];
                for (int i = 0; i < parentLinkedEntities.Length; i++)
                {
                    if (enabled)
                    {
                        commandBuffer.RemoveComponent<Disabled>(parentLinkedEntities[i].Value);
                    }
                    else
                    {
                        commandBuffer.AddComponent<Disabled>(parentLinkedEntities[i].Value);
                    }
                }
            }
        }

        public static void SetEntityHierarchyEnabledParallel(bool enabled, Entity parent, EntityCommandBuffer.ParallelWriter commandBuffer, int chunkIndex, BufferFromEntity<LinkedEntityGroup> linkedEntityGroupFromEntity)
        {
            if (enabled)
            {
                commandBuffer.RemoveComponent<Disabled>(chunkIndex, parent);
            }
            else
            {
                commandBuffer.AddComponent<Disabled>(chunkIndex, parent);
            }

            if (linkedEntityGroupFromEntity.HasComponent(parent))
            {
                DynamicBuffer<LinkedEntityGroup> parentLinkedEntities = linkedEntityGroupFromEntity[parent];
                for (int i = 0; i < parentLinkedEntities.Length; i++)
                {
                    if (enabled)
                    {
                        commandBuffer.RemoveComponent<Disabled>(chunkIndex, parentLinkedEntities[i].Value);
                    }
                    else
                    {
                        commandBuffer.AddComponent<Disabled>(chunkIndex, parentLinkedEntities[i].Value);
                    }
                }
            }
        }
    }
}