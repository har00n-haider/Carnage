using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;


//[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
//[UpdateAfter(typeof(GhostPredictionSystemGroup))]
//public class AfterGhostPredictionCommandBufferSystem : EntityCommandBufferSystem
//{ }

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
//[UpdateAfter(typeof(GhostSimulationSystemGroup))]
//[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public class WeaponCommandBufferSystem : EntityCommandBufferSystem
{ }
