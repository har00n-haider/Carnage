using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Rival.Samples
{
    public struct FixedStepButton
    {
        public bool WasPressed;
        public bool WasReleased;
        public bool IsHeld;

        private uint _lastTick;
        private float _pressThreshold;

        public void SetPressedThreshold(float value)
        {
            _pressThreshold = value;
        }

        public void UpdateWithValue(float value, uint tick)
        {
            // Clear when there is a tick change
            if(tick != _lastTick)
            {
                WasPressed = false;
                WasReleased = false;
            }

            bool wasHeld = IsHeld;
            IsHeld = value > math.max(math.EPSILON, _pressThreshold);

            if (!wasHeld && IsHeld)
            {
                WasPressed = true;
            }
            else if (wasHeld && !IsHeld)
            {
                WasReleased = true;
            }

            _lastTick = tick;
        }
    }
}