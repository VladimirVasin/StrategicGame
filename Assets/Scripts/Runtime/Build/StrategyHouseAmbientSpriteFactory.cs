using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyHouseAmbientSpriteFactory
    {
        private const float PixelsPerUnit = 24f;
        private const int HouseSpriteSize = 80;
        private const int SmokeTextureWidth = 32;
        private const int SmokeTextureHeight = 24;
        private const float HousePivotX = 40f;
        private const float HousePivotY = 8f;
        public const int FrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedSmokeSprites = new();
        private static readonly Dictionary<int, Sprite> CachedWindowMasks = new();

        public static Sprite GetSprite(int variant, int frame)
        {
            int normalizedFrame = Normalize(frame, FrameCount);
            if (!CachedSmokeSprites.TryGetValue(normalizedFrame, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSmokeSprite(normalizedFrame);
                CachedSmokeSprites[normalizedFrame] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWindowMaskSprite(int variant)
        {
            int normalizedVariant = Normalize(variant, StrategyBuildingSpriteFactory.HouseVariantCount);
            if (!CachedWindowMasks.TryGetValue(normalizedVariant, out Sprite sprite) || sprite == null)
            {
                sprite = CreateWindowMaskSprite(normalizedVariant);
                CachedWindowMasks[normalizedVariant] = sprite;
            }

            return sprite;
        }

        public static Vector3 GetChimneyLocalPosition(int variant)
        {
            Vector2 mouth = Normalize(variant, StrategyBuildingSpriteFactory.HouseVariantCount) switch
            {
                1 => new Vector2(52.5f, 76f),
                2 => new Vector2(49.5f, 76f),
                3 => new Vector2(50f, 75f),
                4 => new Vector2(53.5f, 75f),
                _ => new Vector2(51.5f, 76f)
            };
            return new Vector3(
                (mouth.x - HousePivotX) / PixelsPerUnit,
                (mouth.y - HousePivotY) / PixelsPerUnit,
                0f);
        }

        private static Sprite CreateSmokeSprite(int frame)
        {
            Texture2D texture = CreateClearTexture(
                SmokeTextureWidth,
                SmokeTextureHeight,
                $"House Smoke {frame + 1}");
            DrawSmoke(texture, frame);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, SmokeTextureWidth, SmokeTextureHeight),
                new Vector2(16.5f / SmokeTextureWidth, 0f),
                PixelsPerUnit);
        }

        private static Sprite CreateWindowMaskSprite(int variant)
        {
            Texture2D texture = CreateClearTexture(
                HouseSpriteSize,
                HouseSpriteSize,
                $"House Window Mask {variant + 1}");
            DrawWindowMask(texture, variant);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, HouseSpriteSize, HouseSpriteSize),
                new Vector2(0.5f, 0.10f),
                PixelsPerUnit);
        }

        private static Texture2D CreateClearTexture(int width, int height, string textureName)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            return texture;
        }

        private static void DrawSmoke(Texture2D texture, int frame)
        {
            const int centerX = SmokeTextureWidth / 2;
            Color smoke = new(0.62f, 0.64f, 0.61f, 0.28f);
            Color smokeLight = new(0.78f, 0.79f, 0.74f, 0.18f);
            int drift = frame <= 3 ? frame : 7 - frame;
            int side = frame < 4 ? -1 : 1;

            FillEllipse(texture, centerX + side * drift, 2, 3, 2, smoke);
            FillEllipse(texture, centerX - 2 + side * (drift + 1), 7 + frame / 2, 4, 3, smokeLight);
            if (frame % 2 == 0)
            {
                FillEllipse(texture, centerX + 2 + side * drift, 12, 3, 2, smokeLight);
            }
        }

        private static void DrawWindowMask(Texture2D texture, int variant)
        {
            switch (variant)
            {
                case 1:
                    DrawWindowPair(texture, new RectInt(35, 21, 3, 6), new RectInt(52, 20, 4, 7), 1);
                    break;
                case 2:
                    DrawWindowPair(texture, new RectInt(32, 21, 4, 6), new RectInt(49, 20, 4, 7));
                    break;
                case 3:
                    DrawWindowPair(texture, new RectInt(33, 20, 3, 6), new RectInt(50, 19, 4, 7), 1);
                    break;
                case 4:
                    DrawWindowPair(texture, new RectInt(35, 20, 3, 6), new RectInt(52, 19, 4, 7));
                    break;
                default:
                    DrawWindowPair(texture, new RectInt(34, 21, 3, 6), new RectInt(50, 20, 4, 7));
                    break;
            }
        }

        private static void DrawWindowPair(
            Texture2D texture,
            RectInt left,
            RectInt right,
            int rightMullionOffset = -1)
        {
            DrawWindow(texture, left, left.width / 2);
            DrawWindow(
                texture,
                right,
                rightMullionOffset >= 0 ? rightMullionOffset : right.width / 2);
        }

        private static void DrawWindow(Texture2D texture, RectInt rect, int mullionOffset)
        {
            FillRect(texture, rect.x, rect.y, rect.width, rect.height, Color.white);
            FillRect(
                texture,
                rect.x + mullionOffset,
                rect.y,
                1,
                rect.height,
                new Color(1f, 1f, 1f, 0.30f));
            FillRect(
                texture,
                rect.x,
                rect.y + rect.height / 2,
                rect.width,
                1,
                new Color(1f, 1f, 1f, 0.36f));
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

        private static void FillEllipse(
            Texture2D texture,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            Color color)
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
