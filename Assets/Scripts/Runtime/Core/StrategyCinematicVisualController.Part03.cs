using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCinematicVisualController
    {
        private const int NightMaskWidth = 128;
        private const int NightMaskHeight = 72;
        private const float NightMaskUpdateInterval = 0.16f;
        private const float NightMaskViewMoveThreshold = 0.35f;

        private SpriteRenderer nightDarknessRenderer;
        private Texture2D nightDarknessTexture;
        private Sprite nightDarknessSprite;
        private Color[] nightDarknessPixels;
        private Rect lastNightDarknessView;
        private float nightDarknessTimer;
        private float lastNightDarknessAlpha = -1f;

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

            for (int y = 0; y < NightMaskHeight; y++)
            {
                float v = (y + 0.5f) / NightMaskHeight;
                float worldY = Mathf.Lerp(view.yMin, view.yMax, v);
                for (int x = 0; x < NightMaskWidth; x++)
                {
                    float u = (x + 0.5f) / NightMaskWidth;
                    float worldX = Mathf.Lerp(view.xMin, view.xMax, u);
                    float light = EvaluateNightMaskLight(worldX, worldY);
                    float alpha = baseAlpha * (1f - Mathf.Clamp01(light * 1.08f));
                    nightDarknessPixels[y * NightMaskWidth + x] = new Color(0f, 0.006f, 0.028f, alpha);
                }
            }

            nightDarknessTexture.SetPixels(nightDarknessPixels);
            nightDarknessTexture.Apply(false, false);
            lastNightDarknessView = view;
            lastNightDarknessAlpha = baseAlpha;
        }

        private float EvaluateNightMaskLight(float worldX, float worldY)
        {
            float light = 0f;
            for (int i = 0; i < emitters.Count; i++)
            {
                StrategyCinematicLightEmitter emitter = emitters[i];
                if (emitter == null
                    || !emitter.TryGetNightMaskLight(out Vector3 center, out float radius, out float strength))
                {
                    continue;
                }

                float dx = worldX - center.x;
                float dy = worldY - center.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance >= radius)
                {
                    continue;
                }

                float falloff = StrategyCinematicVisualMath.Smooth01(1f - distance / Mathf.Max(0.01f, radius));
                light = Mathf.Max(light, strength * falloff);
                if (light >= 0.96f)
                {
                    return light;
                }
            }

            return Mathf.Clamp01(light);
        }

        private float EvaluateNightDarknessAlpha()
        {
            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            float rain = weather != null ? weather.RainIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float weatherBoost = Mathf.Max(rain * 0.035f, storm * 0.055f);
            return Mathf.Clamp01(night * 0.34f + warm * 0.055f + weatherBoost);
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
