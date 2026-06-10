using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyHouseAmbientSpriteFactory
    {
        private const float PixelsPerUnit = 24f;
        public const int FrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(int variant, int frame)
        {
            int normalizedVariant = Normalize(variant, StrategyBuildingSpriteFactory.HouseVariantCount);
            int normalizedFrame = Normalize(frame, FrameCount);
            int cacheKey = normalizedVariant * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(normalizedVariant, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(int variant, int frame)
        {
            Texture2D texture = new Texture2D(80, 80, TextureFormat.RGBA32, false)
            {
                name = $"House Ambient {variant + 1}-{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[80 * 80]);

            DrawSmoke(texture, variant, frame);
            DrawWindowGlow(texture, variant, frame);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 80f, 80f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static void DrawSmoke(Texture2D texture, int variant, int frame)
        {
            int chimneyX = variant switch
            {
                1 => 56,
                2 => 49,
                3 => 52,
                4 => 51,
                _ => 50
            };
            int chimneyY = variant == 3 ? 71 : 67;
            Color smoke = new Color(0.62f, 0.64f, 0.61f, 0.28f);
            Color smokeLight = new Color(0.78f, 0.79f, 0.74f, 0.18f);
            int drift = frame <= 3 ? frame : 7 - frame;
            int side = frame < 4 ? -1 : 1;

            FillEllipse(texture, chimneyX + side * drift, chimneyY + frame / 2, 3, 2, smoke);
            FillEllipse(texture, chimneyX - 2 + side * (drift + 1), chimneyY + 5 + frame / 2, 4, 3, smokeLight);
            if (frame % 2 == 0)
            {
                FillEllipse(texture, chimneyX + 2 + side * drift, chimneyY + 10, 3, 2, smokeLight);
            }
        }

        private static void DrawWindowGlow(Texture2D texture, int variant, int frame)
        {
            float pulse = frame switch
            {
                1 => 0.75f,
                2 => 0.95f,
                3 => 0.82f,
                5 => 0.68f,
                6 => 0.88f,
                _ => 0.58f
            };
            Color glow = new Color(1f, 0.78f, 0.34f, 0.20f + pulse * 0.28f);
            Color glowHot = new Color(1f, 0.93f, 0.55f, 0.30f + pulse * 0.18f);

            if (variant == 3)
            {
                FillRect(texture, 23, 30, 6, 6, glow);
                FillRect(texture, 43, 30, 6, 6, glow);
                SetPixelSafe(texture, 26, 34, glowHot);
                SetPixelSafe(texture, 46, 34, glowHot);
                return;
            }

            if (variant == 4)
            {
                FillRect(texture, 44, 28, 7, 6, glow);
                SetPixelSafe(texture, 47, 32, glowHot);
                return;
            }

            FillRect(texture, 41, 28, 7, 6, glow);
            FillRect(texture, 56, 28, 6, 6, new Color(1f, 0.70f, 0.28f, glow.a * 0.7f));
            SetPixelSafe(texture, 44, 32, glowHot);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int radiusXSqr = radiusX * radiusX;
            int radiusYSqr = radiusY * radiusY;
            int radiusProduct = radiusXSqr * radiusYSqr;
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSqr + y * y * radiusXSqr <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
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

        private static int Normalize(int value, int count)
        {
            int normalized = value % count;
            return normalized < 0 ? normalized + count : normalized;
        }
    }
}
