using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using Unity.NetCode.Editor;
#endif

namespace Rival.Samples
{
    public enum WorldSystemsConfig
    {
        Default,
        Basic,
        Platformer,
        StressTest,
        OnlineFPS,
    }

    public class SamplesBootstrap : ClientServerBootstrap
    {
        public const string _kBasicAssemblyName = "Samples.Basic";
        public const string _kPlatformerAssemblyName = "Samples.Platformer";
        public const string _kOnlineFPSAssemblyName = "Samples.OnlineFPS";
        public const string _kStressTestAssemblyName = "Samples.StressTest";

        public override bool Initialize(string defaultWorldName)
        {
            // Detect config based on starting scene
            string startSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            WorldSystemsConfig worldConfig = WorldSystemsConfig.Default;
            switch (startSceneName)
            {
                case "Basic":
                    worldConfig = WorldSystemsConfig.Basic;
                    break;
                case "OnlineFPS":
                case "OnlineFPSMenu":
                    worldConfig = WorldSystemsConfig.OnlineFPS;
                    break;
                case "Platformer":
                    worldConfig = WorldSystemsConfig.Platformer;
                    break;
                case "StressTest":
                    worldConfig = WorldSystemsConfig.StressTest;
                    break;
            }

            World world = new World(defaultWorldName, WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default).ToList();
            switch (worldConfig)
            {
                case WorldSystemsConfig.Basic:
                    FilterOutSystemsOfAssembly(systems, new List<string> { _kPlatformerAssemblyName, _kOnlineFPSAssemblyName, _kStressTestAssemblyName });
                    break;
                case WorldSystemsConfig.Platformer:
                    FilterOutSystemsOfAssembly(systems, new List<string> { _kBasicAssemblyName, _kOnlineFPSAssemblyName, _kStressTestAssemblyName });
                    break;
                case WorldSystemsConfig.StressTest:
                    FilterOutSystemsOfAssembly(systems, new List<string> { _kBasicAssemblyName, _kPlatformerAssemblyName, _kOnlineFPSAssemblyName });
                    break;
                case WorldSystemsConfig.OnlineFPS:
                    FilterOutSystemsOfAssembly(systems, new List<string> { _kBasicAssemblyName, _kPlatformerAssemblyName, _kStressTestAssemblyName });
                    break;
                case WorldSystemsConfig.Default:
                default:
                    FilterOutSystemsOfAssembly(systems, new List<string> { _kBasicAssemblyName, _kPlatformerAssemblyName, _kOnlineFPSAssemblyName, _kStressTestAssemblyName });
                    break;
            }
            GenerateSystemLists(systems);

            switch (worldConfig)
            {
                case WorldSystemsConfig.OnlineFPS:
                    DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, ExplicitDefaultWorldSystems);
                    break;
                case WorldSystemsConfig.Default:
                case WorldSystemsConfig.Basic:
                case WorldSystemsConfig.Platformer:
                case WorldSystemsConfig.StressTest:
                default:
                    DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, DefaultWorldSystems);
                    break;
            }

#if !UNITY_DOTSRUNTIME
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
#endif

            return true;
        }

        public static void FilterOutSystemsOfAssembly(List<Type> systemTypes, List<string> assemblyNames)
        {
            for (int i = systemTypes.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < assemblyNames.Count; j++)
                {
                    if (systemTypes[i].Assembly.GetName().Name.Contains(assemblyNames[j]))
                    {
                        systemTypes.RemoveAt(i);
                    }
                }
            }
        }
    }
}