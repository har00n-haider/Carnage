using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using System.Collections.Generic;
using System;

public partial class GhostVariantsSystem : DefaultVariantSystemBase
{
    protected override void RegisterDefaultVariants(Dictionary<ComponentType, Type> defaultVariants)
    {
        defaultVariants.Add(typeof(Translation), typeof(TranslationDefaultVariant));
        defaultVariants.Add(typeof(Rotation), typeof(RotationDefaultVariant));
    }
}
