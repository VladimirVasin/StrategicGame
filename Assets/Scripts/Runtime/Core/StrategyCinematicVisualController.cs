using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyCinematicVisualController : MonoBehaviour
    {
        private const int PuddleCount = 36;
        private const int ForegroundCount = 4;
        private const int MaxActivePointLights = 8;
        private const float ScanInterval = 7.0f;
        private const float LodInterval = 0.35f;
        private const float PuddleUpdateInterval = 0.18f;
        private const float ForegroundUpdateInterval = 0.14f;
        private const float CameraPadding = 1.8f;
        private const float EmitterLodPadding = 7.0f;

        private readonly List<StrategyCinematicLightEmitter> emitters = new();
        private readonly List<PuddleVisual> puddles = new();
        private readonly List<ForegroundVisual> foregrounds = new();
        private CityMapController map;
        private Camera strategyCamera;
        private StrategyDayNightCycleController dayNight;
        private StrategyWeatherController weather;
        private StrategyWindController wind;
        private Light2D globalLight;
        private SpriteRenderer depthRenderer;
        private SpriteRenderer flashRenderer;
        private float scanTimer;
        private float lodTimer;
        private float puddleTimer;
        private float foregroundTimer;
        private bool configured;

        public void Configure(
            CityMapController mapController,
            Camera camera,
            StrategyDayNightCycleController dayNightController,
            StrategyWeatherController weatherController,
            StrategyWindController windController)
        {
            map = mapController;
            strategyCamera = camera;
            dayNight = dayNightController;
            weather = weatherController;
            wind = windController;
            configured = map != null && strategyCamera != null;
            scanTimer = Random.Range(ScanInterval * 0.45f, ScanInterval);
            lodTimer = Random.Range(0f, LodInterval);
            puddleTimer = 0f;
            foregroundTimer = 0f;
            ResetLightningScheduler();

            EnsureSceneObjects();
            ScanLightEmitters();
            RefreshEmitterLods(GetCameraWorldRect());
            ApplyGlobalLighting();
            ApplyAtmosphere(0f, GetCameraWorldRect());

            StrategyDebugLogger.Info(
                "CinematicVisuals",
                "Configured",
                StrategyDebugLogger.F("puddles", PuddleCount),
                StrategyDebugLogger.F("foregrounds", ForegroundCount));
        }

        public void RefreshSceneLightingNow()
        {
            if (!configured)
            {
                return;
            }

            EnsureSceneObjects();
            ScanLightEmitters();
            Rect view = GetCameraWorldRect();
            RefreshEmitterLods(view);
            scanTimer = ScanInterval;
            lodTimer = LodInterval;
            nightDarknessTimer = 0f;
            lastNightDarknessAlpha = -1f;
            ApplyGlobalLighting();
            ApplyAtmosphere(0f, view);
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            scanTimer -= dt;
            if (scanTimer <= 0f)
            {
                scanTimer = ScanInterval;
                ScanLightEmitters();
            }

            EnsureSceneObjects();
            Rect view = GetCameraWorldRect();
            lodTimer -= dt;
            if (lodTimer <= 0f)
            {
                lodTimer = LodInterval;
                RefreshEmitterLods(view);
            }

            ApplyGlobalLighting();
            ApplyAtmosphere(dt, view);
        }

        private void EnsureSceneObjects()
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            if (globalLight == null)
            {
                globalLight = FindExistingGlobalLight();
                if (globalLight == null)
                {
                    GameObject lightObject = new GameObject("Cinematic Global 2D Light");
                    lightObject.transform.SetParent(transform, false);
                    globalLight = lightObject.AddComponent<Light2D>();
                    globalLight.blendStyleIndex = 0;
                    globalLight.lightType = Light2D.LightType.Global;
                }
            }

            if (depthRenderer == null)
            {
                depthRenderer = CreateScreenRenderer(
                    "Cinematic Depth Wash",
                    StrategyWorldSorting.CinematicDepthOverlayOrder);
            }

            if (flashRenderer == null)
            {
                flashRenderer = CreateScreenRenderer(
                    "Cinematic Lightning Flash",
                    StrategyWorldSorting.CinematicScreenFlashOrder);
            }

            EnsureNightDarknessMask();
            EnsurePuddles();
            EnsureForegrounds();
        }

        private static Light2D FindExistingGlobalLight()
        {
            Light2D[] lights = Object.FindObjectsByType<Light2D>();
            for (int i = 0; i < lights.Length; i++)
            {
                Light2D candidate = lights[i];
                if (candidate != null
                    && candidate.lightType == Light2D.LightType.Global
                    && candidate.blendStyleIndex == 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private SpriteRenderer CreateScreenRenderer(string objectName, int order)
        {
            GameObject rendererObject = new GameObject(objectName);
            rendererObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyCinematicVisualSprites.GetWhiteSprite();
            renderer.sortingOrder = order;
            renderer.color = Color.clear;
            return renderer;
        }

        private void ApplyGlobalLighting()
        {
            if (globalLight == null)
            {
                return;
            }

            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            float rain = weather != null ? weather.RainIntensity : 0f;
            float cloud = weather != null ? weather.CloudIntensity : 0f;
            float fog = weather != null ? weather.FogIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            Color color = Color.white;
            color = Color.Lerp(color, new Color(1f, 0.84f, 0.58f, 1f), warm * 0.30f);
            color = Color.Lerp(color, new Color(0.62f, 0.76f, 1f, 1f), night * 0.42f);
            color = Color.Lerp(color, new Color(0.78f, 0.86f, 0.94f, 1f), Mathf.Max(rain * 0.18f, storm * 0.24f));
            color = Color.Lerp(color, new Color(0.86f, 0.90f, 0.86f, 1f), fog * 0.24f);

            globalLight.color = color;
            globalLight.intensity = Mathf.Clamp(
                0.97f - night * 0.30f - cloud * 0.05f - rain * 0.07f - fog * 0.08f - storm * 0.08f,
                0.60f,
                1.05f);
        }

        private void ApplyAtmosphere(float dt, Rect view)
        {
            ApplyNightDarknessMask(dt, view);
            ApplyScreenRenderer(depthRenderer, view, GetDepthWashColor());
            UpdateLightning(dt, view);
            puddleTimer -= dt;
            if (puddleTimer <= 0f)
            {
                puddleTimer = PuddleUpdateInterval;
                UpdatePuddles(view);
            }

            foregroundTimer -= dt;
            if (foregroundTimer <= 0f)
            {
                foregroundTimer = ForegroundUpdateInterval;
                UpdateForegrounds(view);
            }
        }

        private void EnsurePuddles()
        {
            while (puddles.Count < PuddleCount)
            {
                GameObject puddleObject = new GameObject("Cinematic Wet Puddle");
                puddleObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = puddleObject.AddComponent<SpriteRenderer>();
                renderer.sprite = StrategyCinematicVisualSprites.GetPuddleSprite();
                renderer.sortingOrder = StrategyWorldSorting.WeatherGroundOverlayOrder + 1;
                renderer.color = Color.clear;
                puddles.Add(new PuddleVisual
                {
                    Renderer = renderer,
                    Normalized = new Vector2(Random.value, Random.value),
                    Scale = Random.Range(0.72f, 1.58f),
                    Phase = Random.Range(0f, 100f)
                });
            }
        }

        private void UpdatePuddles(Rect view)
        {
            float wet = weather != null ? weather.WetnessIntensity : 0f;
            float rain = weather != null ? weather.RainIntensity : 0f;
            bool visible = wet > 0.08f || rain > 0.04f;
            for (int i = 0; i < puddles.Count; i++)
            {
                PuddleVisual puddle = puddles[i];
                SpriteRenderer renderer = puddle.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                float x = Mathf.Lerp(view.xMin, view.xMax, puddle.Normalized.x);
                float y = Mathf.Lerp(view.yMin, view.yMax, puddle.Normalized.y);
                renderer.transform.position = new Vector3(x, y, -0.18f);
                renderer.transform.localScale = new Vector3(puddle.Scale, puddle.Scale * RandomizedPuddleSquash(i), 1f);
                float glint = Mathf.Sin(Time.unscaledTime * 1.7f + puddle.Phase) * 0.5f + 0.5f;
                float alpha = Mathf.Clamp01(wet * 0.11f + rain * 0.045f) * Mathf.Lerp(0.55f, 1.25f, glint);
                renderer.color = new Color(0.54f, 0.70f, 0.78f, Mathf.Clamp01(alpha));
            }
        }

        private float RandomizedPuddleSquash(int index)
        {
            return 0.38f + (index % 5) * 0.035f;
        }

        private void EnsureForegrounds()
        {
            while (foregrounds.Count < ForegroundCount)
            {
                GameObject foregroundObject = new GameObject("Cinematic Foreground Depth Prop");
                foregroundObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = foregroundObject.AddComponent<SpriteRenderer>();
                renderer.sprite = StrategyCinematicVisualSprites.GetBranchSprite();
                renderer.sortingOrder = StrategyWorldSorting.CinematicForegroundOverlayOrder;
                renderer.color = Color.clear;
                foregrounds.Add(new ForegroundVisual
                {
                    Renderer = renderer,
                    Anchor = foregrounds.Count,
                    Phase = Random.Range(0f, 100f)
                });
            }
        }

        private void UpdateForegrounds(Rect view)
        {
            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float fog = weather != null ? weather.FogIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float alpha = Mathf.Clamp01(0.014f + night * 0.035f + fog * 0.055f + storm * 0.060f);
            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            for (int i = 0; i < foregrounds.Count; i++)
            {
                ForegroundVisual visual = foregrounds[i];
                SpriteRenderer renderer = visual.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                Vector3 position = GetForegroundPosition(view, visual.Anchor);
                float sway = Mathf.Sin(Time.unscaledTime * 0.65f + visual.Phase) * 0.18f;
                position += new Vector3(windDirection.x * sway, windDirection.y * sway * 0.22f, 0f);
                renderer.transform.position = position;
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, GetForegroundRotation(visual.Anchor) + sway * 6f);
                renderer.transform.localScale = GetForegroundScale(visual.Anchor);
                renderer.color = new Color(0.02f, 0.03f, 0.026f, alpha);
                renderer.enabled = alpha > 0.01f;
            }
        }

        private Color GetDepthWashColor()
        {
            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float rain = weather != null ? weather.RainIntensity : 0f;
            float fog = weather != null ? weather.FogIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float alpha = Mathf.Clamp01(night * 0.026f + rain * 0.018f + fog * 0.050f + storm * 0.036f);
            Color color = Color.Lerp(new Color(0.08f, 0.12f, 0.16f, alpha), new Color(0.55f, 0.62f, 0.58f, alpha), fog);
            return color;
        }

        private void ApplyScreenRenderer(SpriteRenderer renderer, Rect view, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = new Vector3(view.center.x, view.center.y, -0.12f);
            renderer.transform.localScale = new Vector3(Mathf.Max(1f, view.width), Mathf.Max(1f, view.height), 1f);
            renderer.color = color;
            renderer.enabled = color.a > 0.001f;
        }

        private Rect GetCameraWorldRect()
        {
            if (strategyCamera != null && strategyCamera.orthographic)
            {
                Vector3 center = strategyCamera.transform.position;
                float height = strategyCamera.orthographicSize * 2f + CameraPadding * 2f;
                float width = height * Mathf.Max(0.1f, strategyCamera.aspect) + CameraPadding * 2f;
                return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            }

            Bounds bounds = map != null ? map.WorldBounds : new Bounds(Vector3.zero, new Vector3(40f, 24f, 1f));
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        private static Rect ExpandRect(Rect rect, float padding)
        {
            return new Rect(
                rect.xMin - padding,
                rect.yMin - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
        }
    }
}
