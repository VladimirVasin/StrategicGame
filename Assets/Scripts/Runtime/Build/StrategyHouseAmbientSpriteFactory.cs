using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyHouseAmbientSpriteFactory
    {
        private const float HousePixelsPerUnit = 48f;
        private const float SmokePixelsPerUnit = 24f;
        private const int HouseSpriteSize = 160;
        private const int SmokeTextureWidth = 32;
        private const int SmokeTextureHeight = 24;
        private const float HousePivotX = 80f;
        private const float HousePivotY = 16f;
        public const int FrameCount = 8;

        // Row bitsets run bottom-to-top inside each pane and include only authored glass pixels.
        private static readonly WindowMaskProfile[] WindowMaskProfiles =
        {
            new(
                new RectInt(62, 41, 8, 13),
                new ushort[] { 0x060, 0x064, 0x06E, 0x06F, 0x067, 0x027, 0x047, 0x060, 0x066, 0x06F, 0x06F, 0x00F, 0x00F },
                new RectInt(93, 39, 9, 15),
                new ushort[] { 0x002, 0x00E, 0x0EF, 0x1EF, 0x1EF, 0x1EE, 0x1EC, 0x1C2, 0x08F, 0x0EF, 0x0EE, 0x0EE, 0x1EE, 0x1E0, 0x0C0 }),
            new(
                new RectInt(66, 41, 6, 14),
                new ushort[] { 0x020, 0x030, 0x036, 0x037, 0x037, 0x037, 0x007, 0x037, 0x034, 0x037, 0x037, 0x037, 0x007, 0x003 },
                new RectInt(97, 40, 8, 15),
                new ushort[] { 0x007, 0x027, 0x077, 0x0F7, 0x0F7, 0x0F7, 0x0F4, 0x0E7, 0x007, 0x0F7, 0x0F7, 0x0F7, 0x0F6, 0x0E0, 0x0C0 }),
            new(
                new RectInt(60, 41, 7, 14),
                new ushort[] { 0x020, 0x070, 0x036, 0x037, 0x037, 0x037, 0x007, 0x033, 0x036, 0x077, 0x077, 0x027, 0x007, 0x001 },
                new RectInt(92, 40, 8, 14),
                new ushort[] { 0x007, 0x027, 0x0E7, 0x0E7, 0x0E7, 0x0E7, 0x0E0, 0x0E7, 0x027, 0x0E7, 0x0E7, 0x0E7, 0x0E4, 0x0E0 }),
            new(
                new RectInt(62, 40, 6, 14),
                new ushort[] { 0x030, 0x030, 0x037, 0x037, 0x037, 0x037, 0x027, 0x031, 0x036, 0x037, 0x037, 0x037, 0x007, 0x001 },
                new RectInt(92, 39, 8, 14),
                new ushort[] { 0x007, 0x027, 0x0E7, 0x0E7, 0x0E7, 0x0E6, 0x0E7, 0x0C7, 0x067, 0x0E7, 0x0E7, 0x0E7, 0x0E0, 0x0E0 }),
            new(
                new RectInt(67, 40, 6, 14),
                new ushort[] { 0x030, 0x030, 0x037, 0x037, 0x037, 0x037, 0x027, 0x030, 0x033, 0x037, 0x037, 0x017, 0x003, 0x000 },
                new RectInt(98, 39, 9, 14),
                new ushort[] { 0x00F, 0x06F, 0x1EF, 0x1EF, 0x1EF, 0x1EC, 0x1C6, 0x00F, 0x0EF, 0x1EE, 0x1EE, 0x1EE, 0x1E0, 0x080 })
        };

        private static readonly Dictionary<int, Sprite> CachedSmokeSprites = new();
        private static readonly Dictionary<int, Sprite> CachedWindowMasks = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetCaches()
        {
            CachedSmokeSprites.Clear();
            CachedWindowMasks.Clear();
        }

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
                1 => new Vector2(99f, 151f),
                2 => new Vector2(92f, 151f),
                3 => new Vector2(92f, 151f),
                4 => new Vector2(100f, 149f),
                _ => new Vector2(95f, 151f)
            };
            return new Vector3(
                (mouth.x - HousePivotX) / HousePixelsPerUnit,
                (mouth.y - HousePivotY) / HousePixelsPerUnit,
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
                SmokePixelsPerUnit);
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
                HousePixelsPerUnit);
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
            WindowMaskProfile profile = WindowMaskProfiles[
                Normalize(variant, WindowMaskProfiles.Length)];
            DrawWindowGlass(texture, profile.LeftRect, profile.LeftRows);
            DrawWindowGlass(texture, profile.RightRect, profile.RightRows);
        }

        private static void DrawWindowGlass(
            Texture2D texture,
            RectInt rect,
            ushort[] rows)
        {
            int rowCount = Mathf.Min(rect.height, rows.Length);
            for (int y = 0; y < rowCount; y++)
            {
                ushort bits = rows[y];
                for (int x = 0; x < rect.width; x++)
                {
                    if ((bits & (1 << x)) != 0)
                    {
                        SetPixelSafe(texture, rect.x + x, rect.y + y, Color.white);
                    }
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

        private readonly struct WindowMaskProfile
        {
            public WindowMaskProfile(
                RectInt leftRect,
                ushort[] leftRows,
                RectInt rightRect,
                ushort[] rightRows)
            {
                LeftRect = leftRect;
                LeftRows = leftRows;
                RightRect = rightRect;
                RightRows = rightRows;
            }

            public RectInt LeftRect { get; }
            public ushort[] LeftRows { get; }
            public RectInt RightRect { get; }
            public ushort[] RightRows { get; }
        }
    }
}
