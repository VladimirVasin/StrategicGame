using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCinematicVisualController
    {
        private const int NightMaskWidth = 128;
        private const int NightMaskHeight = 72;
        private const float NightMaskUpdateInterval = 0.16f;
        private const float NightMaskViewMoveThreshold = 0.35f;
        private const float NightMaskMaxAlpha = 1f;
        private const float NightMaskLightCutoutBoost = 1.85f;
        private const float NightMaskStableCoreRadius = 1.45f;
        private const float NightMaskPerfLogInterval = 6f;
        private const float NightMaskPerfLogThresholdMs = 3f;

        private SpriteRenderer nightDarknessRenderer;
        private Texture2D nightDarknessTexture;
        private Sprite nightDarknessSprite;
        private Color[] nightDarknessPixels;
        private readonly List<NightMaskLightSample> nightMaskLightSamples = new();
        private Rect lastNightDarknessView;
        private float nightDarknessTimer;
        private float lastNightDarknessAlpha = -1f;
        private float nextNightMaskPerfLogTime;

        private readonly struct NightMaskLightSample
        {
            public NightMaskLightSample(Vector3 center, float radius, float strength, float edgeFlicker)
            {
                Center = center;
                Radius = radius;
                Strength = strength;
                EdgeFlicker = edgeFlicker;
            }

            public readonly Vector3 Center;
            public readonly float Radius;
            public readonly float Strength;
            public readonly float EdgeFlicker;
        }

        private void EnsureNightDarknessMask()
        {
            if (nightDarknessRenderer != null || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            GameObject maskObject = new GameObject("Cinematic Night Darkness Mask");
            maskObject.transform.SetParent(transform, false);
            nightDarknessRenderer = maskObject.AddComponent<SpriteRenderer>();
            nightDarknessRenderer.sprite = GetNightDarknessSprite();
            nightDarknessRenderer.sortingOrder = StrategyWorldSorting.CinematicDepthOverlayOrder - 8;
            nightDarknessRenderer.color = Color.white;
            nightDarknessRenderer.enabled = false;
        }

        private void ApplyNightDarknessMask(float dt, Rect view)
        {
            EnsureNightDarknessMask();
            if (nightDarknessRenderer == null)
            {
                return;
            }

            float alpha = EvaluateNightDarknessAlpha();
            if (alpha <= 0.006f)
            {
                nightDarknessRenderer.enabled = false;
                return;
            }

            nightDarknessRenderer.transform.position = new Vector3(view.center.x, view.center.y, -0.13f);
            nightDarknessRenderer.transform.localScale = new Vector3(
                view.width / NightMaskWidth,
                view.height / NightMaskHeight,
                1f);
            nightDarknessRenderer.enabled = true;

            nightDarknessTimer -= Mathf.Max(0f, dt);
            bool viewChanged = HasNightMaskViewChanged(view);
            bool alphaChanged = Mathf.Abs(alpha - lastNightDarknessAlpha) > 0.012f;
            if (nightDarknessTimer <= 0f || viewChanged || alphaChanged)
            {
                nightDarknessTimer = viewChanged ? 0.055f : NightMaskUpdateInterval;
                UpdateNightDarknessTexture(view, alpha);
            }
        }

        private void UpdateNightDarknessTexture(Rect view, float baseAlpha)
        {
            if (nightDarknessTexture == null || nightDarknessPixels == null)
            {
                GetNightDarknessSprite();
            }

            float started = Time.realtimeSinceStartup;
            CollectNightMaskLights(view, out int emitterCount, out int handTorchCount);
            Color baseColor = new(0f, 0.006f, 0.028f, baseAlpha);
            for (int i = 0; i < nightDarknessPixels.Length; i++)
            {
                nightDarknessPixels[i] = baseColor;
            }

            int appliedLights = ApplyNightMaskLights(view, baseAlpha);
            nightDarknessTexture.SetPixels(nightDarknessPixels);
            nightDarknessTexture.Apply(false, false);
            lastNightDarknessView = view;
            lastNightDarknessAlpha = baseAlpha;
            LogNightMaskUpdatePerf(
                (Time.realtimeSinceStartup - started) * 1000f,
                appliedLights,
                emitterCount,
                handTorchCount);
        }

        private void CollectNightMaskLights(Rect view, out int emitterCount, out int handTorchCount)
        {
            nightMaskLightSamples.Clear();
            emitterCount = 0;
            handTorchCount = 0;
            for (int i = 0; i < nightMaskEmitters.Count; i++)
            {
                StrategyCinematicLightEmitter emitter = nightMaskEmitters[i];
                if (emitter == null
                    || !emitter.TryGetNightMaskLight(
                        out Vector3 center,
                        out float radius,
                        out float strength,
                        out float edgeFlicker))
                {
                    continue;
                }

                if (!CircleIntersectsRect(center, radius, view))
                {
                    continue;
                }

                emitterCount++;
                nightMaskLightSamples.Add(new NightMaskLightSample(center, radius, strength, edgeFlicker));
            }

            for (int i = 0; i < StrategyResidentAgent.ActiveNightTorchLightCount; i++)
            {
                if (!StrategyResidentAgent.TryGetActiveNightTorchLight(
                        i,
                        out Vector3 center,
                        out float radius,
                        out float strength,
                        out float edgeFlicker))
                {
                    continue;
                }

                if (!CircleIntersectsRect(center, radius, view))
                {
                    continue;
                }

                handTorchCount++;
                nightMaskLightSamples.Add(new NightMaskLightSample(center, radius, strength, edgeFlicker));
            }
        }

        private int ApplyNightMaskLights(Rect view, float baseAlpha)
        {
            if (nightMaskLightSamples.Count == 0 || view.width <= 0.001f || view.height <= 0.001f)
            {
                return 0;
            }

            float invWidth = NightMaskWidth / view.width;
            float invHeight = NightMaskHeight / view.height;
            float worldXStep = view.width / NightMaskWidth;
            float worldYStep = view.height / NightMaskHeight;
            float worldXStart = view.xMin + worldXStep * 0.5f;
            float worldYStart = view.yMin + worldYStep * 0.5f;
            int applied = 0;
            for (int i = 0; i < nightMaskLightSamples.Count; i++)
            {
                NightMaskLightSample light = nightMaskLightSamples[i];
                int minX = Mathf.Clamp(Mathf.FloorToInt((light.Center.x - light.Radius - view.xMin) * invWidth), 0, NightMaskWidth - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt((light.Center.x + light.Radius - view.xMin) * invWidth), 0, NightMaskWidth - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt((light.Center.y - light.Radius - view.yMin) * invHeight), 0, NightMaskHeight - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt((light.Center.y + light.Radius - view.yMin) * invHeight), 0, NightMaskHeight - 1);
                bool touched = false;
                for (int y = minY; y <= maxY; y++)
                {
                    float worldY = worldYStart + y * worldYStep;
                    int row = y * NightMaskWidth;
                    for (int x = minX; x <= maxX; x++)
                    {
                        float worldX = worldXStart + x * worldXStep;
                        float contribution = EvaluateNightMaskLightContribution(
                            worldX,
                            worldY,
                            light.Center,
                            light.Radius,
                            light.Strength,
                            light.EdgeFlicker);
                        if (contribution <= 0f)
                        {
                            continue;
                        }

                        int index = row + x;
                        float alpha = baseAlpha * (1f - Mathf.Clamp01(contribution * NightMaskLightCutoutBoost));
                        if (alpha >= nightDarknessPixels[index].a)
                        {
                            continue;
                        }

                        Color pixel = nightDarknessPixels[index];
                        pixel.a = alpha;
                        nightDarknessPixels[index] = pixel;
                        touched = true;
                    }
                }

                if (touched)
                {
                    applied++;
                }
            }

            return applied;
        }

        private static float EvaluateNightMaskLightContribution(
            float worldX,
            float worldY,
            Vector3 center,
            float radius,
            float strength,
            float edgeFlicker)
        {
            float dx = worldX - center.x;
            float dy = worldY - center.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance >= radius)
            {
                return 0f;
            }

            float stableRadius = Mathf.Min(NightMaskStableCoreRadius, radius * 0.58f);
            if (distance <= stableRadius)
            {
                return strength;
            }

            float falloff = StrategyCinematicVisualMath.Smooth01(1f - distance / Mathf.Max(0.01f, radius));
            float edgeT = Mathf.InverseLerp(stableRadius, radius, distance);
            float flickerWeight = StrategyCinematicVisualMath.Smooth01(edgeT);
            float safeEdgeFlicker = Mathf.Clamp(edgeFlicker, 0.72f, 1.18f);
            return strength * falloff * Mathf.Lerp(1f, safeEdgeFlicker, flickerWeight);
        }

        private void LogNightMaskUpdatePerf(float durationMs, int appliedLights, int emitterCount, int handTorchCount)
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextNightMaskPerfLogTime
                || (durationMs < NightMaskPerfLogThresholdMs && handTorchCount < 8))
            {
                return;
            }

            nextNightMaskPerfLogTime = now + NightMaskPerfLogInterval;
            StrategyDebugLogger.Info(
                "CinematicVisuals",
                "NightMaskUpdated",
                StrategyDebugLogger.F("durationMs", durationMs),
                StrategyDebugLogger.F("appliedLights", appliedLights),
                StrategyDebugLogger.F("emitters", emitterCount),
                StrategyDebugLogger.F("handTorches", handTorchCount),
                StrategyDebugLogger.F("maskPixels", nightDarknessPixels != null ? nightDarknessPixels.Length : 0));
        }

        private float EvaluateNightDarknessAlpha()
        {
            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            float rain = weather != null ? weather.RainIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float weatherBoost = Mathf.Max(rain * 0.035f, storm * 0.055f);
            return Mathf.Clamp01(night * NightMaskMaxAlpha + warm * 0.055f + weatherBoost);
        }

        private bool HasNightMaskViewChanged(Rect view)
        {
            if (lastNightDarknessAlpha < 0f)
            {
                return true;
            }

            Vector2 delta = view.center - lastNightDarknessView.center;
            return delta.sqrMagnitude > NightMaskViewMoveThreshold * NightMaskViewMoveThreshold
                || Mathf.Abs(view.width - lastNightDarknessView.width) > NightMaskViewMoveThreshold
                || Mathf.Abs(view.height - lastNightDarknessView.height) > NightMaskViewMoveThreshold;
        }

        private Sprite GetNightDarknessSprite()
        {
            if (nightDarknessSprite != null)
            {
                return nightDarknessSprite;
            }

            nightDarknessTexture = new Texture2D(NightMaskWidth, NightMaskHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Cinematic Night Darkness Mask"
            };
            nightDarknessPixels = new Color[NightMaskWidth * NightMaskHeight];
            nightDarknessTexture.SetPixels(nightDarknessPixels);
            nightDarknessTexture.Apply(false, false);
            nightDarknessSprite = Sprite.Create(
                nightDarknessTexture,
                new Rect(0f, 0f, NightMaskWidth, NightMaskHeight),
                new Vector2(0.5f, 0.5f),
                1f);
            nightDarknessSprite.name = "Cinematic Night Darkness Mask Sprite";
            return nightDarknessSprite;
        }
    }
}
