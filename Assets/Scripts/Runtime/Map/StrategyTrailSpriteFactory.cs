using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyTrailSpriteFactory
    {
        private const int Pixels = 16;
        private const float PixelsPerUnit = 16f;
        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;
        private const int CardinalMask = North | East | South | West;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(int mask, int level, int variant)
        {
            int normalizedMask = mask & CardinalMask;
            int normalizedLevel = Mathf.Clamp(level, 1, 3);
            int normalizedVariant = Mathf.Abs(variant) % 4;
            string sequenceId = $"Trail/M{normalizedMask}/L{normalizedLevel}";
            if (StrategyVisualCatalogProvider.TryGetSequenceSprite(sequenceId, normalizedVariant, out Sprite authored))
            {
                return authored;
            }

            int key = normalizedMask + normalizedLevel * 16 + normalizedVariant * 64;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(normalizedMask, normalizedLevel, normalizedVariant);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(int mask, int level, int variant)
        {
            Texture2D texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false)
            {
                name = $"Trail {mask} L{level} V{variant}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[Pixels * Pixels]);
            PaintTrail(texture, mask, level, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, Pixels, Pixels), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void PaintTrail(Texture2D texture, int mask, int level, int variant)
        {
            int width = level == 1 ? 2 : level == 2 ? 3 : 4;
            int edgeWidth = Mathf.Min(Pixels, width + 2);
            Color edge = GetEdgeColor(level);
            Color body = GetBodyColor(level);
            Color highlight = GetHighlightColor(level);

            PaintShape(texture, mask, edgeWidth, edge);
            PaintShape(texture, mask, width, body);
            PaintDetail(texture, mask, width, highlight, variant, level);
        }

        private static void PaintShape(Texture2D texture, int mask, int width, Color color)
        {
            int center = Pixels / 2;
            PaintBrush(texture, center, center, width, color);

            if (mask == 0)
            {
                PaintBrush(texture, center - 1, center, width + 1, color);
                PaintBrush(texture, center + 1, center, width, color);
                return;
            }

            if ((mask & North) != 0)
            {
                PaintLine(texture, center, center, center, Pixels - 1, width, color);
            }

            if ((mask & South) != 0)
            {
                PaintLine(texture, center, center, center, 0, width, color);
            }

            if ((mask & East) != 0)
            {
                PaintLine(texture, center, center, Pixels - 1, center, width, color);
            }

            if ((mask & West) != 0)
            {
                PaintLine(texture, center, center, 0, center, width, color);
            }

        }

        private static void PaintDetail(Texture2D texture, int mask, int width, Color color, int variant, int level)
        {
            int detailCount = level == 1 ? 1 + variant / 2 : 5 + variant;
            for (int i = 0; i < detailCount; i++)
            {
                int x = 2 + PositiveModulo(Hash(variant, mask, i, 31), Pixels - 4);
                int y = 2 + PositiveModulo(Hash(variant, mask, i, 73), Pixels - 4);
                if (texture.GetPixel(x, y).a <= 0.01f)
                {
                    continue;
                }

                SetPixelSafe(texture, x, y, color);
                if ((i + variant) % 3 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, color);
                }
            }
        }

        private static void PaintLine(
            Texture2D texture,
            int startX,
            int startY,
            int endX,
            int endY,
            int width,
            Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY));
            if (steps <= 0)
            {
                PaintBrush(texture, startX, startY, width, color);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));
                PaintBrush(texture, x, y, width, color);
            }
        }

        private static void PaintBrush(Texture2D texture, int x, int y, int width, Color color)
        {
            int radius = Mathf.Max(1, width / 2);
            FillEllipse(texture, x, y, radius, radius, color);
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int rx = Mathf.Max(1, radiusX);
            int ry = Mathf.Max(1, radiusY);
            int rxSqr = rx * rx;
            int rySqr = ry * ry;
            int product = rxSqr * rySqr;
            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    if (x * x * rySqr + y * y * rxSqr <= product)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }

        private static Color GetEdgeColor(int level)
        {
            return level == 1 ? Rgba(58, 70, 42, 42) : level == 2 ? Rgba(72, 54, 34, 132) : Rgba(55, 42, 29, 166);
        }

        private static Color GetBodyColor(int level)
        {
            return level == 1 ? Rgba(119, 102, 60, 68) : level == 2 ? Rgba(136, 93, 52, 170) : Rgba(121, 78, 43, 215);
        }

        private static Color GetHighlightColor(int level)
        {
            return level == 1 ? Rgba(164, 142, 83, 38) : level == 2 ? Rgba(182, 132, 70, 98) : Rgba(190, 142, 82, 124);
        }

        private static Color Rgba(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static int Hash(int a, int b, int c, int salt)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + c * 1274126177 + salt * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
