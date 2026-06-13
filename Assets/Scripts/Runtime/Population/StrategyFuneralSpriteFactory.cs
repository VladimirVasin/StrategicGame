using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyFuneralSpriteFactory
    {
        public const int DeathFrameCount = 8;
        public const int GraveVariantCount = 4;
        private const float PixelsPerUnit = 32f;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetDeathSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = Normalize(variant, StrategyResidentSpriteFactory.VariantCountPerGender);
            int normalizedFrame = Mathf.Clamp(frame, 0, DeathFrameCount - 1);
            int key = ((int)gender * 100000)
                + ((int)lifeStage * 10000)
                + normalizedVariant * 100
                + normalizedFrame;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateDeathSprite(gender, normalizedVariant, lifeStage, normalizedFrame);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetGraveSprite(int variant)
        {
            int normalizedVariant = Normalize(variant, GraveVariantCount);
            int key = 800000 + normalizedVariant;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateGraveSprite(normalizedVariant);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedCorpseSprite()
        {
            const int key = 900000;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedCorpseSprite();
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetDraggedCorpseSprite()
        {
            const int key = 900001;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateDraggedCorpseSprite();
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCorpseRopeSprite()
        {
            const int key = 900002;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCorpseRopeSprite();
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateDeathSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int width = 40;
            int height = 30;
            Texture2D texture = CreateTexture(width, height, $"Resident Death {gender} {lifeStage} {variant} {frame}");

            Color outline = Rgb(43, 31, 29);
            Color skin = GetSkin(variant);
            Color hair = GetHair(gender, variant);
            Color tunic = GetTunic(gender, variant);
            Color tunicDark = Darken(tunic, 0.68f);
            Color leg = Rgb(55, 46, 40);

            bool child = lifeStage == StrategyResidentLifeStage.Child;
            float scale = child ? 0.78f : 1f;

            if (frame <= 2)
            {
                int lean = frame * 2;
                int baseX = 20 + lean;
                int footY = 5;
                DrawStandingBody(texture, baseX, footY, scale, outline, skin, hair, tunic, tunicDark, leg, frame);
            }
            else if (frame <= 4)
            {
                int bodyY = frame == 3 ? 10 : 8;
                DrawFallingBody(texture, 20, bodyY, scale, outline, skin, hair, tunic, tunicDark, leg, frame);
            }
            else
            {
                DrawLyingBody(texture, 20, 9, scale, outline, skin, hair, tunic, tunicDark, leg, frame);
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.16f), PixelsPerUnit);
        }

        private static Sprite CreateGraveSprite(int variant)
        {
            int width = 52;
            int height = 42;
            Texture2D texture = CreateTexture(width, height, $"Cemetery Grave {variant + 1}");

            Color shadow = Rgba(30, 27, 24, 95);
            Color soil = variant switch
            {
                1 => Rgb(93, 68, 48),
                2 => Rgb(78, 73, 63),
                3 => Rgb(107, 76, 52),
                _ => Rgb(86, 61, 43)
            };
            Color soilLight = Rgb(137, 103, 75);
            Color outline = Rgb(43, 34, 29);
            Color wood = Rgb(107, 71, 43);
            Color woodLight = Rgb(161, 111, 66);
            Color stone = Rgb(112, 116, 108);
            Color stoneLight = Rgb(160, 164, 150);

            FillEllipse(texture, 26, 11, 20, 7, shadow);
            FillEllipse(texture, 26, 14, 18, 6, outline);
            FillEllipse(texture, 26, 15, 16, 5, soil);
            FillEllipse(texture, 22, 17, 8, 2, soilLight);

            if (variant == 1)
            {
                FillRect(texture, 24, 17, 5, 18, outline);
                FillRect(texture, 25, 18, 3, 16, wood);
                FillRect(texture, 17, 27, 19, 5, outline);
                FillRect(texture, 18, 28, 17, 3, woodLight);
            }
            else if (variant == 2)
            {
                FillEllipse(texture, 26, 29, 8, 10, outline);
                FillEllipse(texture, 26, 29, 6, 8, stone);
                FillRect(texture, 22, 19, 9, 11, outline);
                FillRect(texture, 23, 20, 7, 9, stone);
                FillRect(texture, 25, 27, 3, 8, stoneLight);
            }
            else if (variant == 3)
            {
                FillRect(texture, 21, 17, 4, 15, outline);
                FillRect(texture, 22, 18, 2, 13, wood);
                FillRect(texture, 28, 18, 4, 13, outline);
                FillRect(texture, 29, 19, 2, 11, woodLight);
            }
            else
            {
                FillRect(texture, 24, 17, 5, 17, outline);
                FillRect(texture, 25, 18, 3, 15, wood);
                FillRect(texture, 18, 27, 17, 4, outline);
                FillRect(texture, 19, 28, 15, 2, woodLight);
                SetPixelSafe(texture, 17, 15, Rgb(188, 176, 105));
                SetPixelSafe(texture, 18, 16, Rgb(221, 207, 119));
                SetPixelSafe(texture, 35, 15, Rgb(170, 128, 176));
                SetPixelSafe(texture, 36, 16, Rgb(202, 151, 207));
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(2f, 4f, 48f, 34f), new Vector2(0.5f, 0.16f), PixelsPerUnit);
        }

        private static Sprite CreateCarriedCorpseSprite()
        {
            int width = 58;
            int height = 28;
            Texture2D texture = CreateTexture(width, height, "Carried Corpse Shroud");

            Color outline = Rgb(47, 36, 30);
            Color wood = Rgb(102, 70, 44);
            Color woodLight = Rgb(159, 108, 62);
            Color cloth = Rgb(205, 194, 165);
            Color clothShadow = Rgb(154, 142, 119);
            Color cord = Rgb(113, 83, 54);

            FillRect(texture, 5, 10, 48, 3, outline);
            FillRect(texture, 6, 11, 46, 1, wood);
            FillRect(texture, 7, 12, 44, 1, woodLight);
            FillRect(texture, 2, 9, 8, 2, outline);
            FillRect(texture, 48, 9, 8, 2, outline);
            FillRect(texture, 2, 10, 8, 1, woodLight);
            FillRect(texture, 48, 10, 8, 1, woodLight);

            FillEllipse(texture, 29, 16, 21, 8, outline);
            FillEllipse(texture, 29, 17, 19, 6, cloth);
            FillEllipse(texture, 17, 17, 7, 5, clothShadow);
            FillEllipse(texture, 41, 16, 8, 5, clothShadow);
            FillRect(texture, 14, 18, 30, 2, clothShadow);
            FillRect(texture, 19, 12, 3, 10, cord);
            FillRect(texture, 36, 12, 3, 10, cord);
            SetPixelSafe(texture, 27, 21, Rgb(235, 226, 193));
            SetPixelSafe(texture, 28, 21, Rgb(235, 226, 193));
            SetPixelSafe(texture, 29, 21, Rgb(235, 226, 193));

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 4f, width, 22f), new Vector2(0.5f, 0.35f), PixelsPerUnit);
        }

        private static Sprite CreateDraggedCorpseSprite()
        {
            int width = 54;
            int height = 24;
            Texture2D texture = CreateTexture(width, height, "Dragged Corpse Shroud");

            Color shadow = Rgba(30, 24, 20, 95);
            Color outline = Rgb(47, 36, 30);
            Color cloth = Rgb(202, 192, 165);
            Color clothLight = Rgb(229, 220, 190);
            Color clothShadow = Rgb(148, 137, 117);
            Color cord = Rgb(112, 82, 53);

            FillEllipse(texture, 27, 7, 23, 5, shadow);
            FillEllipse(texture, 27, 12, 22, 7, outline);
            FillEllipse(texture, 27, 13, 20, 6, cloth);
            FillEllipse(texture, 15, 13, 8, 5, clothShadow);
            FillEllipse(texture, 40, 12, 7, 4, clothShadow);
            FillRect(texture, 13, 15, 29, 2, clothShadow);
            FillRect(texture, 19, 8, 3, 11, cord);
            FillRect(texture, 35, 8, 3, 11, cord);
            FillRect(texture, 4, 13, 9, 2, outline);
            FillRect(texture, 5, 14, 8, 1, cord);
            SetPixelSafe(texture, 25, 17, clothLight);
            SetPixelSafe(texture, 26, 17, clothLight);
            SetPixelSafe(texture, 27, 17, clothLight);
            SetPixelSafe(texture, 28, 17, clothLight);

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 3f, width, 18f), new Vector2(0.5f, 0.22f), PixelsPerUnit);
        }

        private static Sprite CreateCorpseRopeSprite()
        {
            int width = 16;
            int height = 3;
            Texture2D texture = CreateTexture(width, height, "Corpse Drag Rope");

            Color dark = Rgb(73, 53, 34);
            Color rope = Rgb(125, 91, 56);
            Color light = Rgb(174, 128, 74);
            for (int x = 0; x < width; x++)
            {
                SetPixelSafe(texture, x, 0, dark);
                SetPixelSafe(texture, x, 1, rope);
                if (x % 3 != 1)
                {
                    SetPixelSafe(texture, x, 2, light);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void DrawStandingBody(
            Texture2D texture,
            int x,
            int footY,
            float scale,
            Color outline,
            Color skin,
            Color hair,
            Color tunic,
            Color tunicDark,
            Color leg,
            int frame)
        {
            int bodyHeight = Mathf.RoundToInt(11 * scale);
            int headRadius = Mathf.Max(3, Mathf.RoundToInt(4 * scale));
            int bodyWidth = Mathf.Max(5, Mathf.RoundToInt(7 * scale));
            int sway = frame - 1;

            FillRect(texture, x - 4 + sway, footY, 3, 8, outline);
            FillRect(texture, x + 1 + sway, footY, 3, 8, outline);
            FillRect(texture, x - 3 + sway, footY + 1, 1, 7, leg);
            FillRect(texture, x + 2 + sway, footY + 1, 1, 7, leg);
            FillRect(texture, x - bodyWidth / 2 + sway, footY + 7, bodyWidth, bodyHeight, outline);
            FillRect(texture, x - bodyWidth / 2 + 1 + sway, footY + 8, bodyWidth - 2, bodyHeight - 2, tunic);
            FillRect(texture, x - bodyWidth / 2 + 1 + sway, footY + 8, bodyWidth - 2, 3, tunicDark);
            FillEllipse(texture, x + sway, footY + bodyHeight + 13, headRadius, headRadius, outline);
            FillEllipse(texture, x + sway, footY + bodyHeight + 13, headRadius - 1, headRadius - 1, skin);
            FillRect(texture, x - headRadius + sway, footY + bodyHeight + 15, headRadius * 2, 2, hair);
        }

        private static void DrawFallingBody(
            Texture2D texture,
            int x,
            int y,
            float scale,
            Color outline,
            Color skin,
            Color hair,
            Color tunic,
            Color tunicDark,
            Color leg,
            int frame)
        {
            int lean = frame == 3 ? -5 : -9;
            DrawThickLine(texture, new Vector2Int(x + 6, y + 5), new Vector2Int(x + lean, y + 16), outline, 3);
            DrawThickLine(texture, new Vector2Int(x + 5, y + 5), new Vector2Int(x + lean, y + 15), tunic, 2);
            FillEllipse(texture, x + lean - 4, y + 18, Mathf.RoundToInt(4 * scale), Mathf.RoundToInt(4 * scale), outline);
            FillEllipse(texture, x + lean - 4, y + 18, Mathf.RoundToInt(3 * scale), Mathf.RoundToInt(3 * scale), skin);
            FillRect(texture, x + lean - 8, y + 20, 8, 2, hair);
            DrawThickLine(texture, new Vector2Int(x + 7, y + 4), new Vector2Int(x + 14, y + 2), outline, 2);
            DrawThickLine(texture, new Vector2Int(x + 6, y + 4), new Vector2Int(x + 12, y + 2), leg, 1);
            DrawThickLine(texture, new Vector2Int(x + 3, y + 4), new Vector2Int(x - 3, y + 1), outline, 2);
            DrawThickLine(texture, new Vector2Int(x + 3, y + 4), new Vector2Int(x - 2, y + 1), leg, 1);
            FillRect(texture, x + lean + 3, y + 12, 6, 2, tunicDark);
        }

        private static void DrawLyingBody(
            Texture2D texture,
            int x,
            int y,
            float scale,
            Color outline,
            Color skin,
            Color hair,
            Color tunic,
            Color tunicDark,
            Color leg,
            int frame)
        {
            int height = Mathf.Max(5, Mathf.RoundToInt(7 * scale));
            int bodyLength = Mathf.Max(14, Mathf.RoundToInt(20 * scale));
            int calmOffset = 0;
            FillEllipse(texture, x, y - 1, bodyLength / 2 + 5, 5, Rgba(28, 24, 22, 85));
            FillRect(texture, x - bodyLength / 2, y + 2, bodyLength, height, outline);
            FillRect(texture, x - bodyLength / 2 + 1, y + 3, bodyLength - 2, height - 2, tunic);
            FillRect(texture, x - 1, y + 3, bodyLength / 2, 2, tunicDark);
            FillRect(texture, x + bodyLength / 2 - 1, y + 2, 8, height, outline);
            FillRect(texture, x + bodyLength / 2, y + 3, 6, height - 2, leg);
            FillEllipse(texture, x - bodyLength / 2 - 5, y + 5 + calmOffset, Mathf.RoundToInt(4 * scale), Mathf.RoundToInt(4 * scale), outline);
            FillEllipse(texture, x - bodyLength / 2 - 5, y + 5 + calmOffset, Mathf.RoundToInt(3 * scale), Mathf.RoundToInt(3 * scale), skin);
            FillRect(texture, x - bodyLength / 2 - 9, y + 7 + calmOffset, 8, 2, hair);
        }

        private static Texture2D CreateTexture(int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            return texture;
        }

        private static int Normalize(int value, int count)
        {
            return count <= 0 ? 0 : ((value % count) + count) % count;
        }

        private static Color GetSkin(int variant)
        {
            return variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
        }

        private static Color GetHair(StrategyResidentGender gender, int variant)
        {
            return variant switch
            {
                1 => Rgb(51, 39, 32),
                2 => Rgb(202, 151, 77),
                3 => Rgb(93, 58, 37),
                4 => Rgb(214, 196, 138),
                _ => gender == StrategyResidentGender.Male ? Rgb(107, 64, 38) : Rgb(128, 76, 43)
            };
        }

        private static Color GetTunic(StrategyResidentGender gender, int variant)
        {
            if (gender == StrategyResidentGender.Female)
            {
                return variant switch
                {
                    1 => Rgb(151, 78, 94),
                    2 => Rgb(80, 128, 102),
                    3 => Rgb(104, 88, 151),
                    4 => Rgb(169, 115, 67),
                    _ => Rgb(91, 119, 153)
                };
            }

            return variant switch
            {
                1 => Rgb(116, 83, 63),
                2 => Rgb(55, 117, 128),
                3 => Rgb(122, 64, 64),
                4 => Rgb(91, 116, 73),
                _ => Rgb(69, 116, 143)
            };
        }

        private static Color Darken(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        private static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static Color Rgba(int r, int g, int b, int a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
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
            int rx = Mathf.Max(1, radiusX);
            int ry = Mathf.Max(1, radiusY);
            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    float nx = x / (float)rx;
                    float ny = y / (float)ry;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawThickLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color, int radius)
        {
            int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
                FillEllipse(texture, x, y, radius, radius, color);
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
