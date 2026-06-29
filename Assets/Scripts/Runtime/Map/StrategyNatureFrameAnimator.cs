using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyNatureFrameAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.42f;

        private SpriteRenderer overlayRenderer;
        private StrategyNaturePropKind kind;
        private int variant;
        private int frameIndex;
        private float frameTimer;

        internal void Configure(SpriteRenderer baseRenderer, StrategyNaturePropKind propKind, int propVariant, float phase)
        {
            kind = propKind;
            variant = propVariant;
            frameIndex = Mathf.FloorToInt(Mathf.Abs(phase) * 10f) % StrategyNatureAmbientSpriteFactory.FrameCount;
            frameTimer = Mathf.Abs(phase) % FrameDuration;
            EnsureOverlay(baseRenderer);
            ApplyFrame();
        }

        public void SetOverlayVisible(bool visible)
        {
            enabled = visible;
            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = visible;
            }
        }

        private void Update()
        {
            if (overlayRenderer == null)
            {
                return;
            }

            frameTimer += Time.unscaledDeltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frameIndex = (frameIndex + 1) % StrategyNatureAmbientSpriteFactory.FrameCount;
            ApplyFrame();
        }

        private void EnsureOverlay(SpriteRenderer baseRenderer)
        {
            if (overlayRenderer != null)
            {
                return;
            }

            GameObject overlay = new GameObject("Leaf Frame Overlay");
            overlay.transform.SetParent(transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localScale = Vector3.one;
            overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sortingOrder = baseRenderer != null ? baseRenderer.sortingOrder + 1 : 4;
            overlayRenderer.flipX = baseRenderer != null && baseRenderer.flipX;
            overlayRenderer.color = Color.white;
        }

        private void ApplyFrame()
        {
            if (overlayRenderer != null)
            {
                overlayRenderer.sprite = StrategyNatureAmbientSpriteFactory.GetSprite(kind, variant, frameIndex);
            }
        }
    }

    internal static class StrategyNatureAmbientSpriteFactory
    {
        public const int FrameCount = 4;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyNaturePropKind kind, int variant, int frame)
        {
            int normalizedVariant = Normalize(variant, StrategyNatureSpriteFactory.GetVariantCount(kind));
            int normalizedFrame = Normalize(frame, FrameCount);
            int cacheKey = ((int)kind * 512) + normalizedVariant * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(kind, normalizedVariant, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyNaturePropKind kind, int variant, int frame)
        {
            GetLayout(kind, out int width, out int height, out float pixelsPerUnit, out Vector2 pivot);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{kind} Leaf Overlay {variant + 1}-{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            DrawLeafFlicker(texture, kind, variant, frame);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, pixelsPerUnit);
        }

        private static void DrawLeafFlicker(Texture2D texture, StrategyNaturePropKind kind, int variant, int frame)
        {
            int count = kind switch
            {
                StrategyNaturePropKind.ForestGroup => 28,
                StrategyNaturePropKind.LargeTree => 22,
                StrategyNaturePropKind.SmallTree => 14,
                StrategyNaturePropKind.Bush => 10,
                _ => 10
            };
            int minY = kind == StrategyNaturePropKind.Bush ? texture.height / 3 : texture.height / 2;
            int maxY = texture.height - 5;
            Color light = kind == StrategyNaturePropKind.Bush
                ? new Color(0.70f, 0.92f, 0.42f, 0.34f)
                : new Color(0.66f, 0.88f, 0.38f, 0.30f);
            Color warm = new Color(0.92f, 0.78f, 0.32f, 0.22f);
            Color dark = new Color(0.18f, 0.35f, 0.18f, 0.22f);

            for (int i = 0; i < count; i++)
            {
                int x = 4 + Hash(variant, frame, i, 17) % Mathf.Max(1, texture.width - 8);
                int y = minY + Hash(variant, frame, i, 31) % Mathf.Max(1, maxY - minY);
                int drift = frame == 1 ? 1 : frame == 3 ? -1 : 0;
                Color color = i % 5 == 0 ? warm : i % 3 == 0 ? dark : light;
                SetPixelSafe(texture, x + drift, y, color);
                if (i % 4 == frame)
                {
                    SetPixelSafe(texture, x + drift + 1, y, color);
                }
            }
        }

        private static void GetLayout(StrategyNaturePropKind kind, out int width, out int height, out float pixelsPerUnit, out Vector2 pivot)
        {
            switch (kind)
            {
                case StrategyNaturePropKind.SmallTree:
                    width = 38;
                    height = 52;
                    pixelsPerUnit = 30f;
                    pivot = new Vector2(0.5f, 0.08f);
                    return;
                case StrategyNaturePropKind.Bush:
                    width = 32;
                    height = 24;
                    pixelsPerUnit = 28f;
                    pivot = new Vector2(0.5f, 0.16f);
                    return;
                case StrategyNaturePropKind.ForestGroup:
                    width = 74;
                    height = 64;
                    pixelsPerUnit = 30f;
                    pivot = new Vector2(0.5f, 0.08f);
                    return;
                default:
                    width = 56;
                    height = 72;
                    pixelsPerUnit = StrategyNatureSpriteFactory.LargeTreePixelsPerUnit;
                    pivot = new Vector2(0.5f, 0.08f);
                    return;
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static int Hash(int a, int b, int c, int salt)
        {
            unchecked
            {
                int h = 17;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h ^= salt * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }

        private static int Normalize(int value, int count)
        {
            int normalized = value % count;
            return normalized < 0 ? normalized + count : normalized;
        }
    }
}
