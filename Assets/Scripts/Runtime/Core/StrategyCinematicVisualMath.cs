using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCinematicVisualMath
    {
        private const float DawnEndPhase = 2f / 24f;
        private const float DuskStartPhase = 14f / 24f;

        public static float NightFactor(float phase)
        {
            if (phase < DawnEndPhase)
            {
                return 1f - Smooth01(phase / DawnEndPhase);
            }

            if (phase >= StrategyDayNightCycleController.NightStartPhase)
            {
                return 1f;
            }

            if (phase >= DuskStartPhase)
            {
                return Smooth01((phase - DuskStartPhase) / (StrategyDayNightCycleController.NightStartPhase - DuskStartPhase));
            }

            return 0f;
        }

        public static float WarmFactor(float phase)
        {
            float dawn = Pulse01(phase, 0.08f, 0.34f);
            float dusk = Pulse01(phase, DuskStartPhase, StrategyDayNightCycleController.NightStartPhase);
            return Mathf.Clamp01(dawn * 0.58f + dusk * 0.82f);
        }

        public static float DawnToNoonFadeOutFactor(float phase)
        {
            const float noonStartPhase = 0.25f;
            return phase < noonStartPhase
                ? 1f - Smooth01(phase / noonStartPhase)
                : 0f;
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
