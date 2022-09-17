using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rival.Samples.OnlineFPS
{
    [CreateAssetMenu]
    public class OnlineFPSGameData : ScriptableObject
    {
        public string MenuSceneName = "OnlineFPSMenu";
        public string GameSceneName = "OnlineFPS";

        public float RespawnTime = 3f;
        public GameObject NameplatePrefab;
        public GameObject RespawnCountdownPrefab;

        public static OnlineFPSGameData Load()
        {
            return Resources.Load<OnlineFPSGameData>("OnlineFPSGameData");
        }
    }
}