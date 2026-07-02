using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyWeatherVisualController : MonoBehaviour
    {
        private const int CloudPixelsPerCell = 1;
        private const int MistPixelsPerCell = 1;
        private const float CloudFrameSeconds = 0.44f;
        private const float MistFrameSeconds = 0.38f;
        private const int MaxRainDrops = 96;
        private const float RainViewPadding = 3.5f;

        private readonly List<RainDropVisual> rainDrops = new();
        private CityMapController map;
        private Camera strategyCamera;
        private StrategyWeatherController weather;
        private StrategyWindController wind;
        private SpriteRenderer cloudRenderer;
        private SpriteRenderer mistRenderer;
        private SpriteRenderer wetRenderer;
        private Texture2D cloudTexture;
        private Texture2D mistTexture;
        private Color[] cloudPixels;
        private Color[] mistPixels;
        private float cloudTimer;
        private float mistTimer;
        private bool rainWasActive;
        private static Sprite whiteSprite;
        private static Sprite rainDropSprite;

        public void Configure(
            CityMapController mapController,
            Camera camera,
            StrategyWeatherController weatherController,
            StrategyWindController windController)
        {
            map = mapController;
            strategyCamera = camera;
            weather = weatherController;
            wind = windController;
            EnsureRenderers();
            ResizeRenderers();
            UpdateRainDrops(0f);
            PaintCloudFrame();
            PaintMistFrame();
            ApplyWetOverlay();
            StrategyDebugLogger.Info(
                "Weather",
                "VisualsConfigured",
                StrategyDebugLogger.F("rainOrder", StrategyWorldSorting.WeatherRainOverlayOrder),
                StrategyDebugLogger.F("mistOrder", StrategyWorldSorting.WeatherMistOverlayOrder));
        }

        private void Update()
        {
            if (map == null || weather == null)
            {
                return;
            }

            EnsureRenderers();
            ResizeRenderers();
            ApplyWetOverlay();
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            UpdateRainDrops(dt);

            cloudTimer += dt;
            if (cloudTimer >= CloudFrameSeconds)
            {
                cloudTimer -= CloudFrameSeconds;
                PaintCloudFrame();
            }

            mistTimer += dt;
            if (mistTimer >= MistFrameSeconds)
            {
                mistTimer -= MistFrameSeconds;
                PaintMistFrame();
            }
        }

        private void EnsureRenderers()
        {
            EnsureRainDrops();
            EnsureTextureRenderer(
                ref cloudRenderer,
                ref cloudTexture,
                ref cloudPixels,
                CloudPixelsPerCell,
                "Weather Cloud Shadow Overlay",
                StrategyWorldSorting.WeatherCloudShadowOrder);
            EnsureTextureRenderer(
                ref mistRenderer,
                ref mistTexture,
                ref mistPixels,
                MistPixelsPerCell,
                "Weather Mist Overlay",
                StrategyWorldSorting.WeatherMistOverlayOrder);
            if (wetRenderer == null)
            {
                GameObject wetObject = new GameObject("Weather Wet Ground Overlay");
                wetObject.transform.SetParent(transform, false);
                wetRenderer = wetObject.AddComponent<SpriteRenderer>();
                wetRenderer.sprite = GetWhiteSprite();
                wetRenderer.sortingOrder = StrategyWorldSorting.WeatherGroundOverlayOrder;
                wetRenderer.color = Color.clear;
            }
        }

        private void EnsureTextureRenderer(
            ref SpriteRenderer renderer,
            ref Texture2D texture,
            ref Color[] pixels,
            int pixelsPerCell,
            string objectName,
            int sortingOrder)
        {
            if (map == null)
            {
                return;
            }

            int width = map.Width * pixelsPerCell;
            int height = map.Height * pixelsPerCell;
            bool recreateSprite = false;
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                if (renderer != null && renderer.sprite != null)
                {
                    Destroy(renderer.sprite);
                    renderer.sprite = null;
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = objectName + " Texture"
                };
                pixels = new Color[width * height];
                recreateSprite = true;
            }

            if (renderer == null)
            {
                GameObject overlayObject = new GameObject(objectName);
                overlayObject.transform.SetParent(transform, false);
                renderer = overlayObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = sortingOrder;
            }

            if (renderer.sprite == null || recreateSprite)
            {
                float pixelsPerUnit = pixelsPerCell / map.CellSize;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
                sprite.name = objectName + " Sprite";
                renderer.sprite = sprite;
            }
        }

        private void ResizeRenderers()
        {
            Bounds bounds = map.WorldBounds;
            Vector3 mapCenter = new Vector3(bounds.center.x, bounds.center.y, -0.16f);
            PositionRenderer(cloudRenderer, mapCenter);
            PositionRenderer(mistRenderer, mapCenter);

            if (wetRenderer != null)
            {
                wetRenderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.15f);
                wetRenderer.transform.localScale = new Vector3(
                    Mathf.Max(1f, bounds.size.x),
                    Mathf.Max(1f, bounds.size.y),
                    1f);
            }
        }

        private void PositionRenderer(SpriteRenderer renderer, Vector3 position)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = position;
            renderer.transform.localScale = Vector3.one;
        }

        private void ApplyWetOverlay()
        {
            if (wetRenderer == null || weather == null)
            {
                return;
            }

            float alpha = weather.WetnessIntensity * 0.105f + weather.RainIntensity * 0.025f;
            wetRenderer.enabled = alpha > 0.004f;
            wetRenderer.color = new Color(0.02f, 0.055f, 0.055f, Mathf.Clamp01(alpha));
        }

        private void EnsureRainDrops()
        {
            while (rainDrops.Count < MaxRainDrops)
            {
                GameObject dropObject = new GameObject("Weather Rain Drop");
                dropObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetRainDropSprite();
                renderer.sortingOrder = StrategyWorldSorting.WeatherRainOverlayOrder;
                renderer.enabled = false;
                rainDrops.Add(new RainDropVisual
                {
                    Renderer = renderer,
                    Speed = Random.Range(8.5f, 16.5f),
                    Scale = Random.Range(0.70f, 1.18f),
                    Alpha = Random.Range(0.72f, 1f)
                });
            }
        }

        private void UpdateRainDrops(float dt)
        {
            if (weather == null)
            {
                return;
            }

            float rain = weather.RainIntensity;
            if (rain <= 0.015f)
            {
                for (int i = 0; i < rainDrops.Count; i++)
                {
                    if (rainDrops[i].Renderer != null)
                    {
                        rainDrops[i].Renderer.enabled = false;
                    }
                }

                rainWasActive = false;
                return;
            }

            EnsureRainDrops();
            Rect view = GetCameraRainBounds();
            Vector2 move = GetRainMoveDirection();
            Quaternion rotation = GetRainRotation(move);
            int activeCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(22f, MaxRainDrops, rain)), 0, MaxRainDrops);
            float alpha = Mathf.Lerp(0.13f, 0.30f, rain);
            float speedBoost = Mathf.Lerp(0.82f, 1.35f, rain + weather.StormIntensity * 0.35f);

            for (int i = 0; i < rainDrops.Count; i++)
            {
                RainDropVisual drop = rainDrops[i];
                SpriteRenderer renderer = drop.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                bool active = i < activeCount;
                renderer.enabled = active;
                if (!active)
                {
                    continue;
                }

                if (!rainWasActive)
                {
                    RespawnRainDrop(drop, view, false);
                }

                Transform dropTransform = renderer.transform;
                Vector3 position = dropTransform.position + new Vector3(move.x, move.y, 0f) * drop.Speed * speedBoost * dt;
                if (position.x < view.xMin - RainViewPadding
                    || position.x > view.xMax + RainViewPadding
                    || position.y < view.yMin - RainViewPadding
                    || position.y > view.yMax + RainViewPadding)
                {
                    RespawnRainDrop(drop, view, true);
                    position = dropTransform.position;
                }
                else
                {
                    dropTransform.position = position;
                }

                dropTransform.rotation = rotation;
                float scale = drop.Scale * Mathf.Lerp(0.82f, 1.22f, rain);
                dropTransform.localScale = new Vector3(0.62f, scale, 1f);
                renderer.color = new Color(0.70f, 0.86f, 0.94f, alpha * drop.Alpha);
            }

            rainWasActive = true;
        }

        private void OnDestroy()
        {
            DestroyTextureRenderer(cloudRenderer, cloudTexture);
            DestroyTextureRenderer(mistRenderer, mistTexture);
            rainDrops.Clear();
        }

        private Rect GetCameraRainBounds()
        {
            if (strategyCamera != null && strategyCamera.orthographic)
            {
                Vector3 center = strategyCamera.transform.position;
                float height = strategyCamera.orthographicSize * 2f + RainViewPadding * 2f;
                float width = height * Mathf.Max(0.1f, strategyCamera.aspect) + RainViewPadding * 2f;
                return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            }

            Bounds bounds = map != null ? map.WorldBounds : new Bounds(Vector3.zero, new Vector3(40f, 24f, 1f));
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        private Vector2 GetRainMoveDirection()
        {
            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            return new Vector2(windDirection.x * 0.42f, -1f).normalized;
        }

        private static Quaternion GetRainRotation(Vector2 move)
        {
            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90f;
            return Quaternion.Euler(0f, 0f, angle);
        }

        private void RespawnRainDrop(RainDropVisual drop, Rect view, bool fromLeadingEdge)
        {
            if (drop.Renderer == null)
            {
                return;
            }

            Vector2 move = GetRainMoveDirection();
            float x = Random.Range(view.xMin, view.xMax);
            float y = fromLeadingEdge ? view.yMax + Random.Range(0f, RainViewPadding) : Random.Range(view.yMin, view.yMax);
            if (fromLeadingEdge && move.x > 0.05f)
            {
                x = Random.Range(view.xMin - RainViewPadding, view.xMax);
            }
            else if (fromLeadingEdge && move.x < -0.05f)
            {
                x = Random.Range(view.xMin, view.xMax + RainViewPadding);
            }

            drop.Renderer.transform.position = new Vector3(x, y, -0.13f);
        }
    }
}
