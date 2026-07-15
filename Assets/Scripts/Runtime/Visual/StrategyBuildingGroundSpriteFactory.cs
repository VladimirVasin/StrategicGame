using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingGroundSpriteFactory
    {
        private const float PixelsPerUnit = 16f;
        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetCache()
        {
            CachedSprites.Clear();
        }

        public static Sprite Get(StrategyBuildTool tool, Vector2Int footprint, int variant)
        {
            Vector2Int size = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            int normalizedVariant = Mathf.Abs(variant) % 3;
            int key = (int)tool * 8192 + size.x * 256 + size.y * 8 + normalizedVariant;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = Create(tool, size, normalizedVariant);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite Create(StrategyBuildTool tool, Vector2Int footprint, int variant)
        {
            int width = footprint.x * 16 + 12;
            int height = footprint.y * 16 + 10;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{tool} Ground Detail {footprint.x}x{footprint.y} V{variant + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[width * height];
            GetPalette(tool, out Color32 soil, out Color32 dark, out Color32 light, out Color32 accent);
            float halfWidth = (width - 1) * 0.5f;
            float halfHeight = (height - 1) * 0.5f;
            int seed = (int)tool * 977 + footprint.x * 131 + footprint.y * 47 + variant * 659;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = Mathf.Abs((x - halfWidth) / Mathf.Max(1f, halfWidth));
                    float ny = Mathf.Abs((y - halfHeight) / Mathf.Max(1f, halfHeight));
                    float roundedRect = Mathf.Pow(nx, 4f) + Mathf.Pow(ny, 4f);
                    float edgeNoise = Hash01(seed, x / 2, y / 2, 11) * 0.18f - 0.09f;
                    if (roundedRect > 1f + edgeNoise)
                    {
                        continue;
                    }

                    float edge = Mathf.InverseLerp(1.02f, 0.68f, roundedRect);
                    float grain = Hash01(seed, x, y, 19);
                    Color32 color = grain < 0.14f ? dark : grain > 0.88f ? light : soil;
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(96f, 188f, edge));
                    color.a = alpha;
                    pixels[y * width + x] = color;
                }
            }

            AddSurfaceDetails(pixels, width, height, seed, tool, accent, dark);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            sprite.name = texture.name + " Sprite";
            return sprite;
        }

        private static void AddSurfaceDetails(
            Color32[] pixels,
            int width,
            int height,
            int seed,
            StrategyBuildTool tool,
            Color32 accent,
            Color32 dark)
        {
            int detailCount = Mathf.Clamp(width * height / 115, 6, 28);
            for (int i = 0; i < detailCount; i++)
            {
                int x = 3 + Hash(seed, i, 23) % Mathf.Max(1, width - 6);
                int y = 3 + Hash(seed, i, 31) % Mathf.Max(1, height - 6);
                int index = y * width + x;
                if (pixels[index].a == 0)
                {
                    continue;
                }

                bool straw = tool == StrategyBuildTool.Granary
                    || tool == StrategyBuildTool.LumberjackCamp
                    || tool == StrategyBuildTool.StarterCaravanCart;
                Color32 detail = straw && i % 2 == 0 ? accent : dark;
                detail.a = 180;
                pixels[index] = detail;
                if (straw && x + 1 < width && pixels[index + 1].a > 0)
                {
                    detail.a = 130;
                    pixels[index + 1] = detail;
                }
            }
        }

        private static void GetPalette(
            StrategyBuildTool tool,
            out Color32 soil,
            out Color32 dark,
            out Color32 light,
            out Color32 accent)
        {
            if (tool == StrategyBuildTool.StorageYard || tool == StrategyBuildTool.StonecutterCamp)
            {
                soil = new Color32(104, 91, 70, 255);
                dark = new Color32(70, 67, 59, 255);
                light = new Color32(139, 126, 93, 255);
                accent = new Color32(168, 155, 116, 255);
                return;
            }

            if (tool == StrategyBuildTool.Granary)
            {
                soil = new Color32(126, 96, 57, 255);
                dark = new Color32(86, 64, 43, 255);
                light = new Color32(159, 124, 70, 255);
                accent = new Color32(202, 166, 83, 255);
                return;
            }

            soil = new Color32(112, 83, 54, 255);
            dark = new Color32(76, 58, 43, 255);
            light = new Color32(145, 109, 68, 255);
            accent = new Color32(180, 137, 72, 255);
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            return Hash(seed + x * 31, y, salt) / (float)int.MaxValue;
        }

        private static int Hash(int seed, int value, int salt)
        {
            unchecked
            {
                int hash = seed;
                hash = hash * 374761393 + value * 668265263;
                hash = hash * 1274126177 + salt * 461845907;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & int.MaxValue;
            }
        }
    }
}
