using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System;

namespace Rival.Samples
{
    public class SamplesMenuManager : MonoBehaviour
    {
        [Header("Components")]
        public Canvas MainCanvas;
        public Text AvgFPS;
        public Text WorstFPS;
        public Text BestFPS;

        [Header("Misc")]
        public float FPSPollRate = 1f;

        private FramerateCalculator _framerateCalculator = default;
        private float _lastTimePolledFPS = float.MinValue;
        private bool _hasVSync = false;

        void Start()
        {
            _framerateCalculator.Initialize();
            UpdateRenderSettings();
        }

        void Update()
        {
            // show hide
            if (Input.GetKeyDown(KeyCode.F1))
            {
                MainCanvas.gameObject.SetActive(!MainCanvas.gameObject.activeSelf);
            }

            if(Input.GetKeyDown(KeyCode.F3))
            {
                _hasVSync = !_hasVSync;
                UpdateRenderSettings();
            }

            // FPS
            _framerateCalculator.Update();
            if (Time.time >= _lastTimePolledFPS + FPSPollRate)
            {
                _framerateCalculator.PollFramerate(out string avg, out string worst, out string best);
                AvgFPS.text = avg;
                WorstFPS.text = worst;
                BestFPS.text = best;

                _lastTimePolledFPS = Time.time;
            }
        }

        private void UpdateRenderSettings()
        {
            QualitySettings.vSyncCount = _hasVSync ? 1 : 0;
        }
    }
}