using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCinematicVisualController
    {
        private float lightningTimer;
        private float lightningBurstTimer;
        private float lightningFlashDecaySpeed = 2.4f;
        private float lightningFlashAlphaScale = 0.52f;
        private float flashIntensity;
        private int lightningBurstFlashes;
        private bool lightningWeatherActive;

        private void ResetLightningScheduler()
        {
            flashIntensity = 0f;
            lightningBurstFlashes = 0;
            lightningBurstTimer = 0f;
            lightningWeatherActive = false;
            lightningTimer = Random.Range(12f, 32f);
            lightningFlashAlphaScale = 0.52f;
        }

        private void UpdateLightning(float dt, Rect view)
        {
            float rain = weather != null ? weather.RainIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float activity = GetLightningActivity(rain, storm);
            bool activeWeather = activity > 0.015f;

            if (activeWeather && !lightningWeatherActive)
            {
                lightningWeatherActive = true;
                ScheduleNextLightning(activity, true);
            }
            else if (!activeWeather)
            {
                lightningWeatherActive = false;
                lightningBurstFlashes = 0;
            }

            if (activeWeather)
            {
                UpdateLightningTimers(dt, rain, storm, activity);
            }

            flashIntensity = Mathf.MoveTowards(flashIntensity, 0f, dt * lightningFlashDecaySpeed);
            Color color = new(0.70f, 0.84f, 1f, flashIntensity * lightningFlashAlphaScale);
            ApplyScreenRenderer(flashRenderer, view, color);
        }

        private void UpdateLightningTimers(float dt, float rain, float storm, float activity)
        {
            lightningTimer -= dt;
            if (lightningTimer <= 0f)
            {
                TriggerLightningFlash(rain, storm, activity, true);
                ScheduleNextLightning(activity, false);
            }

            if (lightningBurstFlashes <= 0)
            {
                return;
            }

            lightningBurstTimer -= dt;
            if (lightningBurstTimer > 0f)
            {
                return;
            }

            lightningBurstFlashes--;
            lightningBurstTimer = Random.Range(0.05f, Mathf.Lerp(0.22f, 0.10f, activity));
            TriggerLightningFlash(rain, storm, activity, false);
        }

        private void TriggerLightningFlash(float rain, float storm, float activity, bool primary)
        {
            float baseStrength = Random.Range(0.10f, 0.34f) + activity * Random.Range(0.20f, 0.52f);
            if (!primary)
            {
                baseStrength *= Random.Range(0.32f, 0.76f);
            }

            flashIntensity = Mathf.Max(flashIntensity, Mathf.Clamp01(baseStrength));
            lightningFlashDecaySpeed = Random.Range(2.1f, 7.5f);
            lightningFlashAlphaScale = Random.Range(0.48f, 0.60f);

            if (!primary)
            {
                return;
            }

            float burstChance = Mathf.Lerp(0.14f, 0.68f, activity);
            lightningBurstFlashes = Random.value < burstChance ? Random.Range(1, 4) : 0;
            lightningBurstTimer = Random.Range(0.04f, 0.24f);

            StrategyDebugLogger.Info(
                "CinematicVisuals",
                "LightningFlash",
                StrategyDebugLogger.F("rain", rain),
                StrategyDebugLogger.F("storm", storm),
                StrategyDebugLogger.F("activity", activity),
                StrategyDebugLogger.F("burstFlashes", lightningBurstFlashes));
        }

        private void ScheduleNextLightning(float activity, bool enteringWeather)
        {
            float minDelay = Mathf.Lerp(54f, 3.8f, activity);
            float maxDelay = Mathf.Lerp(120f, 17f, activity);
            float roll = Random.value;
            float delay;

            if (roll < 0.04f + activity * 0.24f)
            {
                delay = Random.Range(1.4f, Mathf.Lerp(18f, 4.6f, activity));
            }
            else if (roll > 0.78f - activity * 0.18f)
            {
                delay = Random.Range(maxDelay, maxDelay * Random.Range(1.20f, 2.35f));
            }
            else
            {
                delay = Random.Range(minDelay, maxDelay);
            }

            if (enteringWeather)
            {
                delay *= Random.Range(0.25f, 0.80f);
            }

            lightningTimer = Mathf.Max(0.65f, delay);
        }

        private static float GetLightningActivity(float rain, float storm)
        {
            float rainActivity = Mathf.Pow(Mathf.Clamp01((rain - 0.28f) / 0.72f), 1.6f) * 0.35f;
            return Mathf.Clamp01(Mathf.Max(rainActivity, storm));
        }
    }
}
