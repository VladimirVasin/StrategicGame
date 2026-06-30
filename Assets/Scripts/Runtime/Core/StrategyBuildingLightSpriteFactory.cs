using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyBuildingLightSpriteKind
    {
        WallTorch,
        Lantern,
        Brazier,
        BridgeLamp
    }

    internal static class StrategyBuildingLightSpriteFactory
    {
        public const int FrameCount = 8;
        private const float PixelsPerUnit = 24f;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyBuildingLightSpriteKind kind, int frame)
        {
            return GetSprite(kind, frame, StrategyBuildingLightSpriteLayer.Combined);
        }

        public static Sprite GetBaseSprite(StrategyBuildingLightSpriteKind kind)
        {
            return GetSprite(kind, 0, StrategyBuildingLightSpriteLayer.Base);
        }

        public static Sprite GetFlameSprite(StrategyBuildingLightSpriteKind kind, int frame)
        {
            return GetSprite(kind, frame, StrategyBuildingLightSpriteLayer.Flame);
        }

        private static Sprite GetSprite(StrategyBuildingLightSpriteKind kind, int frame, StrategyBuildingLightSpriteLayer layer)
        {
            int normalizedFrame = Normalize(frame, FrameCount);
            int cacheKey = (int)kind * 256 + (int)layer * 64 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(kind, normalizedFrame, layer);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyBuildingLightSpriteKind kind, int frame, StrategyBuildingLightSpriteLayer layer)
        {
            Texture2D texture = CreateClearTexture(20, 26, $"Building Light {kind} {layer} {frame + 1}");
            bool drawBase = layer != StrategyBuildingLightSpriteLayer.Flame;
            bool drawFlame = layer != StrategyBuildingLightSpriteLayer.Base;
            switch (kind)
            {
                case StrategyBuildingLightSpriteKind.Lantern:
                    DrawLantern(texture, frame, drawBase, drawFlame);
                    break;
                case StrategyBuildingLightSpriteKind.Brazier:
                    DrawBrazier(texture, frame, drawBase, drawFlame);
                    break;
                case StrategyBuildingLightSpriteKind.BridgeLamp:
                    DrawBridgeLamp(texture, frame, drawBase, drawFlame);
                    break;
                default:
                    DrawWallTorch(texture, frame, drawBase, drawFlame);
                    break;
            }

            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.18f),
                PixelsPerUnit);
            sprite.name = texture.name;
            return sprite;
        }

        private static void DrawWallTorch(Texture2D texture, int frame, bool drawBase, bool drawFlame)
        {
            Color wood = Rgb(73, 42, 25);
            Color metal = Rgb(69, 58, 49);
            if (drawBase)
            {
                FillRect(texture, 7, 3, 2, 13, wood);
                FillRect(texture, 5, 8, 5, 2, metal);
                FillRect(texture, 8, 9, 2, 4, Rgb(104, 62, 32));
            }

            if (drawFlame)
            {
                DrawFlame(texture, 10, 16, frame, 1.0f);
            }
        }

        private static void DrawLantern(Texture2D texture, int frame, bool drawBase, bool drawFlame)
        {
            Color metal = Rgb(54, 50, 45);
            Color glass = new(1f, 0.75f, 0.34f, 0.38f);
            if (drawBase)
            {
                FillRect(texture, 9, 3, 2, 14, metal);
                FillRect(texture, 7, 14, 6, 2, metal);
                FillRect(texture, 7, 7, 6, 6, glass);
                FillRect(texture, 6, 12, 8, 2, metal);
                FillRect(texture, 7, 6, 6, 1, metal);
            }

            if (drawFlame)
            {
                FillRect(texture, 8, 8, 1, 4, new Color(1f, 0.95f, 0.62f, 0.48f));
                DrawFlame(texture, 10, 9, frame, 0.62f);
            }
        }

        private static void DrawBrazier(Texture2D texture, int frame, bool drawBase, bool drawFlame)
        {
            Color iron = Rgb(47, 43, 39);
            Color coal = Rgb(29, 28, 27);
            if (drawBase)
            {
                FillRect(texture, 5, 4, 10, 2, iron);
                FillRect(texture, 6, 6, 8, 2, coal);
                FillRect(texture, 5, 3, 2, 2, iron);
                FillRect(texture, 13, 3, 2, 2, iron);
            }

            if (drawFlame)
            {
                SetPixelSafe(texture, 8, 7, new Color(1f, 0.22f, 0.09f, 0.75f));
                SetPixelSafe(texture, 12, 7, new Color(1f, 0.44f, 0.12f, 0.65f));
                DrawFlame(texture, 10, 9, frame, 1.05f);
            }
        }

        private static void DrawBridgeLamp(Texture2D texture, int frame, bool drawBase, bool drawFlame)
        {
            Color post = Rgb(72, 49, 31);
            Color metal = Rgb(60, 55, 47);
            if (drawBase)
            {
                FillRect(texture, 9, 2, 2, 16, post);
                FillRect(texture, 7, 16, 6, 2, metal);
                FillRect(texture, 8, 10, 4, 6, new Color(1f, 0.76f, 0.32f, 0.34f));
                FillRect(texture, 7, 9, 6, 1, metal);
                FillRect(texture, 7, 15, 6, 1, metal);
            }

            if (drawFlame)
            {
                DrawFlame(texture, 10, 12, frame, 0.56f);
            }
        }

        private static void DrawFlame(Texture2D texture, int centerX, int baseY, int frame, float scale)
        {
            int sway = frame switch
            {
                1 => -1,
                2 => 1,
                4 => 1,
                6 => -1,
                _ => 0
            };
            int lift = frame == 2 || frame == 5 ? 1 : 0;
            int outerY = baseY + Mathf.RoundToInt(3f * scale) + lift;
            Color outer = new(0.95f, 0.20f, 0.07f, 0.88f);
            Color mid = new(1f, 0.55f, 0.12f, 0.95f);
            Color inner = new(1f, 0.90f, 0.38f, 0.96f);
            Color core = new(1f, 0.98f, 0.70f, 0.90f);

            FillEllipse(texture, centerX + sway, outerY, Mathf.Max(2, Mathf.RoundToInt(3f * scale)), Mathf.Max(3, Mathf.RoundToInt(5f * scale)), outer);
            FillEllipse(texture, centerX, baseY + Mathf.RoundToInt(3f * scale), Mathf.Max(1, Mathf.RoundToInt(2f * scale)), Mathf.Max(2, Mathf.RoundToInt(4f * scale)), mid);
            FillEllipse(texture, centerX, baseY + Mathf.RoundToInt(2f * scale), 1, Mathf.Max(2, Mathf.RoundToInt(3f * scale)), inner);
            SetPixelSafe(texture, centerX, baseY + Mathf.RoundToInt(2f * scale), core);
        }

        private static Texture2D CreateClearTexture(int width, int height, string textureName)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = textureName
            };
            texture.SetPixels(new Color[width * height]);
            return texture;
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    SetPixelSafe(texture, xx, yy, color);
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

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static int Normalize(int value, int count)
        {
            int normalized = value % count;
            return normalized < 0 ? normalized + count : normalized;
        }

        private enum StrategyBuildingLightSpriteLayer
        {
            Combined,
            Base,
            Flame
        }
    }
}
