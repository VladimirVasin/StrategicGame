using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyDayNightCycleController : MonoBehaviour
    {
        private const float CycleSeconds = 220f;
        private const float LogPhaseStep = 0.25f;

        private static Sprite overlaySprite;

        private CityMapController map;
        private Camera strategyCamera;
        private SpriteRenderer overlayRenderer;
        private Color baseCameraColor = new(0.09f, 0.12f, 0.14f);
        private int loggedPhaseIndex = -1;

        public float DayPhase => Mathf.Repeat(Time.timeSinceLevelLoad / CycleSeconds, 1f);
        public static float DayLengthSeconds => CycleSeconds;
        public static int CurrentDayIndex => Mathf.FloorToInt(Time.timeSinceLevelLoad / CycleSeconds);
        public static float CurrentDayPhase => Mathf.Repeat(Time.timeSinceLevelLoad / CycleSeconds, 1f);
        public static bool IsHouseholdOutdoorWorkTime
        {
            get
            {
                float phase = CurrentDayPhase;
                return phase >= 0.18f && phase < 0.78f;
            }
        }

        public static float ShadowOpacityMultiplier { get; private set; } = 1f;
        public static float ShadowLengthMultiplier { get; private set; } = 1f;

        public void Configure(CityMapController mapController, Camera camera)
        {
            map = mapController;
            strategyCamera = camera;
            if (strategyCamera != null)
            {
                baseCameraColor = strategyCamera.backgroundColor;
            }

            EnsureOverlayRenderer();
            ResizeOverlay();
            ApplyVisuals(true);
            StrategyDebugLogger.Info(
                "DayNight",
                "Configured",
                StrategyDebugLogger.F("cycleSeconds", CycleSeconds),
                StrategyDebugLogger.F("sortingOrder", StrategyWorldSorting.DayNightOverlayOrder));
        }

        private void Update()
        {
            if (map == null || overlayRenderer == null)
            {
                return;
            }

            ResizeOverlay();
            ApplyVisuals(false);
        }

        private void EnsureOverlayRenderer()
        {
            if (overlayRenderer != null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("Day Night Overlay");
            overlayObject.transform.SetParent(transform, false);
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = GetOverlaySprite();
            overlayRenderer.sortingOrder = StrategyWorldSorting.DayNightOverlayOrder;
            overlayRenderer.color = Color.clear;
        }

        private void ResizeOverlay()
        {
            if (map == null || overlayRenderer == null)
            {
                return;
            }

            Bounds bounds = map.WorldBounds;
            overlayRenderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.2f);
            overlayRenderer.transform.localScale = new Vector3(
                Mathf.Max(1f, bounds.size.x),
                Mathf.Max(1f, bounds.size.y),
                1f);
        }

        private void ApplyVisuals(bool forceLog)
        {
            float phase = DayPhase;
            Color overlayColor = EvaluateOverlayColor(phase);
            overlayRenderer.color = overlayColor;
            ShadowOpacityMultiplier = EvaluateShadowOpacity(phase);
            ShadowLengthMultiplier = EvaluateShadowLength(phase);

            if (strategyCamera != null)
            {
                strategyCamera.backgroundColor = EvaluateCameraColor(phase);
            }

            int phaseIndex = Mathf.FloorToInt(phase / LogPhaseStep);
            if (forceLog || phaseIndex != loggedPhaseIndex)
            {
                loggedPhaseIndex = phaseIndex;
                StrategyDebugLogger.Info(
                    "DayNight",
                    "PhaseChanged",
                    StrategyDebugLogger.F("phase", GetPhaseName(phase)),
                    StrategyDebugLogger.F("phaseValue", phase),
                    StrategyDebugLogger.F("overlayAlpha", overlayColor.a));
            }
        }

        private static Color EvaluateOverlayColor(float phase)
        {
            Color dawn = new Color(0.72f, 0.40f, 0.16f, 0.10f);
            Color day = new Color(0.08f, 0.10f, 0.06f, 0.02f);
            Color dusk = new Color(0.78f, 0.32f, 0.12f, 0.16f);
            Color night = new Color(0.02f, 0.07f, 0.18f, 0.36f);

            if (phase < 0.18f)
            {
                return Color.Lerp(night, dawn, Smooth01(phase / 0.18f));
            }

            if (phase < 0.32f)
            {
                return Color.Lerp(dawn, day, Smooth01((phase - 0.18f) / 0.14f));
            }

            if (phase < 0.62f)
            {
                return day;
            }

            if (phase < 0.78f)
            {
                return Color.Lerp(day, dusk, Smooth01((phase - 0.62f) / 0.16f));
            }

            return Color.Lerp(dusk, night, Smooth01((phase - 0.78f) / 0.22f));
        }

        private static float EvaluateShadowOpacity(float phase)
        {
            if (phase < 0.18f)
            {
                return Mathf.Lerp(0.38f, 0.74f, Smooth01(phase / 0.18f));
            }

            if (phase < 0.32f)
            {
                return Mathf.Lerp(0.74f, 1f, Smooth01((phase - 0.18f) / 0.14f));
            }

            if (phase < 0.62f)
            {
                return 1f;
            }

            if (phase < 0.78f)
            {
                return Mathf.Lerp(1f, 0.72f, Smooth01((phase - 0.62f) / 0.16f));
            }

            return Mathf.Lerp(0.72f, 0.38f, Smooth01((phase - 0.78f) / 0.22f));
        }

        private static float EvaluateShadowLength(float phase)
        {
            if (phase < 0.18f)
            {
                return Mathf.Lerp(0.78f, 1.36f, Smooth01(phase / 0.18f));
            }

            if (phase < 0.32f)
            {
                return Mathf.Lerp(1.36f, 0.92f, Smooth01((phase - 0.18f) / 0.14f));
            }

            if (phase < 0.62f)
            {
                return 0.92f;
            }

            if (phase < 0.78f)
            {
                return Mathf.Lerp(0.92f, 1.42f, Smooth01((phase - 0.62f) / 0.16f));
            }

            return Mathf.Lerp(1.42f, 0.78f, Smooth01((phase - 0.78f) / 0.22f));
        }

        private Color EvaluateCameraColor(float phase)
        {
            Color nightCamera = new Color(0.025f, 0.035f, 0.065f);
            Color duskCamera = new Color(0.10f, 0.08f, 0.075f);
            Color dawnCamera = new Color(0.10f, 0.09f, 0.075f);

            if (phase < 0.18f)
            {
                return Color.Lerp(nightCamera, dawnCamera, Smooth01(phase / 0.18f));
            }

            if (phase < 0.32f)
            {
                return Color.Lerp(dawnCamera, baseCameraColor, Smooth01((phase - 0.18f) / 0.14f));
            }

            if (phase < 0.62f)
            {
                return baseCameraColor;
            }

            if (phase < 0.78f)
            {
                return Color.Lerp(baseCameraColor, duskCamera, Smooth01((phase - 0.62f) / 0.16f));
            }

            return Color.Lerp(duskCamera, nightCamera, Smooth01((phase - 0.78f) / 0.22f));
        }

        private static string GetPhaseName(float phase)
        {
            if (phase < 0.18f)
            {
                return "Dawn";
            }

            if (phase < 0.62f)
            {
                return "Day";
            }

            if (phase < 0.78f)
            {
                return "Dusk";
            }

            return "Night";
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static Sprite GetOverlaySprite()
        {
            if (overlaySprite != null)
            {
                return overlaySprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Day Night Overlay Pixel"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            overlaySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            overlaySprite.name = "Day Night Overlay Sprite";
            return overlaySprite;
        }
    }
}
