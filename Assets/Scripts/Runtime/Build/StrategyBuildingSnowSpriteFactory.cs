using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingSnowSpriteFactory
    {
        private static readonly Dictionary<int, Sprite> Cache = new();

        public static Sprite GetSnowCapSprite(Sprite baseSprite)
        {
            if (baseSprite == null)
            {
                return null;
            }

            int cacheKey = baseSprite.GetHashCode();
            if (Cache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = CreateSnowCapSprite(baseSprite);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite CreateSnowCapSprite(Sprite baseSprite)
        {
            Rect sourceRect = baseSprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height));
            Texture2D texture = CreateClearTexture(width, height, baseSprite.name + " Snow Cap");

            bool painted = TryPaintFromSourceAlpha(baseSprite, texture, width, height);
            if (!painted)
            {
                PaintFallbackCap(texture, width, height);
            }

            texture.Apply(false, true);
            Vector2 pivot = new Vector2(
                Mathf.Clamp01(baseSprite.pivot.x / width),
                Mathf.Clamp01(baseSprite.pivot.y / height));
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                pivot,
                baseSprite.pixelsPerUnit);
            sprite.name = baseSprite.name + " Snow Cap Sprite";
            return sprite;
        }

        private static bool TryPaintFromSourceAlpha(Sprite baseSprite, Texture2D target, int width, int height)
        {
            Texture2D source = baseSprite.texture;
            Rect sourceRect = baseSprite.textureRect;
            int startX = Mathf.RoundToInt(sourceRect.xMin);
            int startY = Mathf.RoundToInt(sourceRect.yMin);
            bool paintedAny = false;

            try
            {
                for (int x = 0; x < width; x++)
                {
                    int top = FindTopOpaquePixel(source, startX + x, startY, height);
                    if (top < 0)
                    {
                        continue;
                    }

                    int seed = Mathf.Abs(baseSprite.GetHashCode() * 31 + x * 97);
                    int depth = 1 + Mathf.FloorToInt(Hash01(seed, x, top, 13) * 3f);
                    for (int d = 0; d <= depth; d++)
                    {
                        int y = top - d;
                        if (y < 0)
                        {
                            break;
                        }

                        Color sourcePixel = source.GetPixel(startX + x, startY + y);
                        if (sourcePixel.a <= 0.08f)
                        {
                            continue;
                        }

                        float tone = Hash01(seed, x, y, 29);
                        Color snow = Color.Lerp(
                            new Color(0.78f, 0.88f, 0.94f, 0.76f),
                            new Color(0.98f, 1f, 1f, 0.94f),
                            tone);
                        snow.a *= d == 0 ? 1f : Mathf.Lerp(0.82f, 0.46f, d / 3f);
                        target.SetPixel(x, y, snow);
                        paintedAny = true;
                    }

                    int shadowY = top - depth - 1;
                    if (shadowY >= 0 && source.GetPixel(startX + x, startY + shadowY).a > 0.08f)
                    {
                        target.SetPixel(x, shadowY, new Color(0.50f, 0.62f, 0.68f, 0.24f));
                    }
                }

                PaintSmallRoofPatches(baseSprite, target, width, height, startX, startY);
            }
            catch (UnityException)
            {
                ClearTexture(target);
                return false;
            }

            return paintedAny;
        }

        private static int FindTopOpaquePixel(Texture2D source, int x, int startY, int height)
        {
            for (int y = height - 1; y >= 0; y--)
            {
                if (source.GetPixel(x, startY + y).a > 0.08f)
                {
                    return y;
                }
            }

            return -1;
        }

        private static void PaintSmallRoofPatches(
            Sprite baseSprite,
            Texture2D target,
            int width,
            int height,
            int startX,
            int startY)
        {
            Texture2D source = baseSprite.texture;
            int patchMinY = Mathf.RoundToInt(height * 0.52f);
            for (int x = 2; x < width - 2; x += 3)
            {
                for (int y = patchMinY; y < height - 2; y += 5)
                {
                    float roll = Hash01(baseSprite.GetHashCode(), x, y, 71);
                    if (roll > 0.20f || source.GetPixel(startX + x, startY + y).a <= 0.08f)
                    {
                        continue;
                    }

                    int length = 1 + Mathf.FloorToInt(Hash01(baseSprite.GetHashCode(), x, y, 83) * 3f);
                    for (int i = 0; i < length; i++)
                    {
                        if (x + i < width && source.GetPixel(startX + x + i, startY + y).a > 0.08f)
                        {
                            target.SetPixel(x + i, y, new Color(0.90f, 0.97f, 1f, 0.48f));
                        }
                    }
                }
            }
        }

        private static void PaintFallbackCap(Texture2D target, int width, int height)
        {
            int capY = Mathf.RoundToInt(height * 0.72f);
            for (int x = 4; x < width - 4; x++)
            {
                int y = capY + Mathf.RoundToInt(Mathf.Sin(x * 0.31f) * 2f);
                target.SetPixel(x, y, new Color(0.95f, 1f, 1f, 0.86f));
                if (y > 0)
                {
                    target.SetPixel(x, y - 1, new Color(0.72f, 0.84f, 0.92f, 0.36f));
                }
            }
        }

        private static Texture2D CreateClearTexture(int width, int height, string name)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            ClearTexture(texture);
            return texture;
        }

        private static void ClearTexture(Texture2D texture)
        {
            Color clear = Color.clear;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int n = seed;
                n = n * 73856093 ^ x * 19349663 ^ y * 83492791 ^ salt * 374761393;
                n = (n << 13) ^ n;
                return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
            }
        }
    }
}
