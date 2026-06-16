using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCinematicVisualMath
    {
        public static float NightFactor(float phase)
        {
            if (phase < 0.18f)
            {
                return 1f - Smooth01(phase / 0.18f);
            }

            if (phase >= 0.78f)
            {
                return Smooth01((phase - 0.78f) / 0.22f);
            }

            return 0f;
        }

        public static float WarmFactor(float phase)
        {
            float dawn = Pulse01(phase, 0.08f, 0.34f);
            float dusk = Pulse01(phase, 0.58f, 0.86f);
            return Mathf.Clamp01(dawn * 0.58f + dusk * 0.82f);
        }

        public static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static float Pulse01(float value, float start, float end)
        {
            if (value <= start || value >= end)
            {
                return 0f;
            }

            float t = Mathf.InverseLerp(start, end, value);
            return Mathf.Sin(t * Mathf.PI);
        }
    }
}
