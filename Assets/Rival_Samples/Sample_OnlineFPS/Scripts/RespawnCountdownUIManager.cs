using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Rival.Samples.OnlineFPS
{
    public class RespawnCountdownUIManager : MonoBehaviour
    {
        [HideInInspector]
        public float CountdownTime;

        public Text CountdownText;

        void Update()
        {
            CountdownTime -= Time.deltaTime;
            CountdownText.text = "Respawning in... " + Mathf.CeilToInt(CountdownTime);

            if (CountdownTime <= 0f)
            {
                Destroy(this.gameObject);
            }
        }
    }
}