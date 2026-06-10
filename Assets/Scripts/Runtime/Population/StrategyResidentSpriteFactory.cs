using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyResidentSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        public const int VariantCountPerGender = 5;
        public const int WalkFrameCount = 8;
        public const int WoodcutFrameCount = 10;
        public const int StonecutFrameCount = 10;
        public const int ConstructionFrameCount = 12;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyResidentGender gender)
        {
            return GetSprite(gender, 0);
        }

        public static Sprite GetSprite(StrategyResidentGender gender, int variant)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Idle, 0);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Idle, 0);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWalkSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, WalkFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Walk, normalizedFrame);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Walk, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWoodcutSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, WoodcutFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Woodcut, normalizedFrame);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Woodcut, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStonecutSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, StonecutFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Stonecut, normalizedFrame);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Stonecut, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetConstructionSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, ConstructionFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Construction, normalizedFrame);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Construction, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetPortraitSprite(StrategyResidentGender gender, int variant)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Portrait, 0);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Portrait, 0);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyResidentGender gender, int variant, ResidentSpritePose pose, int frame)
        {
            if (pose == ResidentSpritePose.Portrait)
            {
                return CreatePortraitSprite(gender, variant);
            }

            Texture2D texture = new Texture2D(20, 28, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Male", variant, pose, frame)
                    : GetSpriteName("Female", variant, pose, frame),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[20 * 28]);

            Color outline = Rgb(45, 31, 27);
            Color skin = variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color leg = variant == 3 ? Rgb(48, 43, 42) : Rgb(61, 51, 43);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame walkFrame = pose switch
            {
                ResidentSpritePose.Walk => GetWalkFrame(frame),
                ResidentSpritePose.Woodcut => GetWoodcutBodyFrame(frame),
                ResidentSpritePose.Stonecut => GetStonecutBodyFrame(frame),
                ResidentSpritePose.Construction => GetConstructionBodyFrame(frame),
                _ => ResidentWalkFrame.Idle
            };

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, walkFrame);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, walkFrame);
            }

            if (pose == ResidentSpritePose.Woodcut)
            {
                DrawWoodcutAxe(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Stonecut)
            {
                DrawStonecutPickaxe(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Construction)
            {
                DrawConstructionHammer(texture, frame, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 20f, 28f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static Sprite CreatePortraitSprite(StrategyResidentGender gender, int variant)
        {
            Texture2D texture = new Texture2D(40, 40, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Male", variant, ResidentSpritePose.Portrait, 0)
                    : GetSpriteName("Female", variant, ResidentSpritePose.Portrait, 0),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[40 * 40]);

            Color outline = Rgb(45, 31, 27);
            Color skin = variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color accent = GetAccentColor(gender, variant);

            FillEllipse(texture, 20, 7, 13, 4, new Color(0f, 0f, 0f, 0.24f));
            FillRect(texture, 12, 10, 16, 11, tunicDark);
            FillRect(texture, 14, 13, 12, 10, tunic);
            FillRect(texture, 17, 20, 6, 5, skin);
            DrawRectOutline(texture, 12, 10, 16, 11, outline);

            if (variant == 1 || variant == 4)
            {
                FillRect(texture, 14, 16, 12, 2, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 18, 11, 4, 11, accent);
            }
            else
            {
                FillRect(texture, 13, 13, 3, 8, accent);
                FillRect(texture, 24, 13, 3, 8, accent);
            }

            FillEllipse(texture, 11, 25, 3, 4, skin);
            FillEllipse(texture, 29, 25, 3, 4, skin);
            FillEllipse(texture, 20, 25, 10, 11, outline);
            FillEllipse(texture, 20, 24, 8, 9, skin);

            if (gender == StrategyResidentGender.Male)
            {
                FillRect(texture, 13, 30, 14, 5, hair);
                FillRect(texture, 12, 25, 4, 6, hair);
                FillRect(texture, 24, 25, 4, 6, hair);
                if (variant == 1 || variant == 3 || variant == 4)
                {
                    FillRect(texture, 15, 18, 10, 3, hair);
                    FillRect(texture, 16, 16, 8, 2, hair);
                }
                else
                {
                    FillRect(texture, 18, 17, 4, 1, outline);
                }
            }
            else
            {
                FillRect(texture, 12, 30, 16, 5, hair);
                FillRect(texture, 10, 21, 4, 10, hair);
                FillRect(texture, 26, 21, 4, 10, hair);
                if (variant == 1 || variant == 4)
                {
                    FillRect(texture, 12, 31, 16, 2, accent);
                    FillRect(texture, 11, 27, 3, 3, accent);
                    FillRect(texture, 26, 27, 3, 3, accent);
                }
                else if (variant == 2)
                {
                    FillRect(texture, 28, 18, 2, 7, hair);
                    FillRect(texture, 29, 15, 1, 4, hair);
                }
            }

            SetPixelSafe(texture, 17, 25, outline);
            SetPixelSafe(texture, 23, 25, outline);
            SetPixelSafe(texture, 20, 22, Rgb(135, 89, 67));
            FillRect(texture, 18, 19, 4, 1, outline);
            SetPixelSafe(texture, 16, 22, Rgb(230, 151, 132));
            SetPixelSafe(texture, 24, 22, Rgb(230, 151, 132));

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 36f, 34f), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void DrawMale(
            Texture2D texture,
            int variant,
            Color outline,
            Color skin,
            Color hair,
            Color tunic,
            Color tunicDark,
            Color leg,
            Color accent,
            ResidentWalkFrame frame)
        {
            int bodyY = frame.BodyYOffset;
            int legHeight = 7 + bodyY;
            FillRect(texture, 7 + frame.LeftLegX, 2, 2, legHeight, leg);
            FillRect(texture, 11 + frame.RightLegX, 2, 2, legHeight, leg);
            FillRect(texture, 8 + frame.LeftFootX, 1, 3, 2, outline);
            FillRect(texture, 11 + frame.RightFootX, 1, 3, 2, outline);

            FillRect(texture, 6, 8 + bodyY, 8, 10, tunic);
            FillRect(texture, 6, 8 + bodyY, 2, 10, tunicDark);
            DrawRectOutline(texture, 6, 8 + bodyY, 8, 10, outline);
            FillRect(texture, 5 + frame.LeftArmX, 13 + bodyY + frame.LeftArmY, 2, 6, skin);
            FillRect(texture, 13 + frame.RightArmX, 13 + bodyY + frame.RightArmY, 2, 6, skin);

            if (variant == 2)
            {
                FillRect(texture, 8, 8 + bodyY, 4, 9, accent);
                FillRect(texture, 8, 13 + bodyY, 4, 1, outline);
            }
            else if (variant == 3)
            {
                FillRect(texture, 4 + frame.LeftArmX, 9 + bodyY + frame.LeftArmY, 3, 8, accent);
                FillRect(texture, 13 + frame.RightArmX, 9 + bodyY + frame.RightArmY, 3, 8, accent);
            }
            else if (variant == 4)
            {
                FillRect(texture, 6, 12 + bodyY, 8, 2, accent);
                SetPixelSafe(texture, 10, 17 + bodyY, accent);
            }

            FillEllipse(texture, 10, 21 + bodyY, 5, 5, skin);
            FillRect(texture, 6, 22 + bodyY, 8, 4, hair);
            FillRect(texture, 5, 20 + bodyY, 2, 3, hair);
            FillRect(texture, 13, 20 + bodyY, 2, 3, hair);

            if (variant == 1)
            {
                FillRect(texture, 6, 24 + bodyY, 8, 3, accent);
                FillRect(texture, 5, 23 + bodyY, 10, 2, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 7, 18 + bodyY, 6, 2, hair);
                FillRect(texture, 8, 17 + bodyY, 4, 2, hair);
            }
            else if (variant == 3)
            {
                FillRect(texture, 5, 23 + bodyY, 10, 2, accent);
                SetPixelSafe(texture, 10, 26 + bodyY, accent);
            }
            else if (variant == 4)
            {
                FillRect(texture, 7, 25 + bodyY, 7, 2, accent);
                FillRect(texture, 5, 23 + bodyY, 10, 1, accent);
            }

            SetPixelSafe(texture, 8, 21 + bodyY, outline);
            SetPixelSafe(texture, 12, 21 + bodyY, outline);
        }

        private static void DrawFemale(
            Texture2D texture,
            int variant,
            Color outline,
            Color skin,
            Color hair,
            Color tunic,
            Color tunicDark,
            Color leg,
            Color accent,
            ResidentWalkFrame frame)
        {
            int bodyY = frame.BodyYOffset;
            int legHeight = 7 + bodyY;
            FillRect(texture, 7 + frame.LeftLegX, 2, 2, legHeight, leg);
            FillRect(texture, 11 + frame.RightLegX, 2, 2, legHeight, leg);
            FillRect(texture, 8 + frame.LeftFootX, 1, 3, 2, outline);
            FillRect(texture, 11 + frame.RightFootX, 1, 3, 2, outline);

            FillRect(texture, 6, 10 + bodyY, 8, 8, tunic);
            FillRect(texture, 5, 6 + bodyY, 10, 6, tunicDark);
            FillRect(texture, 6, 6 + bodyY, 8, 3, tunic);
            DrawRectOutline(texture, 5, 6 + bodyY, 10, 12, outline);
            FillRect(texture, 5 + frame.LeftArmX, 13 + bodyY + frame.LeftArmY, 2, 6, skin);
            FillRect(texture, 13 + frame.RightArmX, 13 + bodyY + frame.RightArmY, 2, 6, skin);

            if (variant == 1)
            {
                FillRect(texture, 7, 7 + bodyY, 6, 10, accent);
                FillRect(texture, 6, 14 + bodyY, 8, 1, outline);
            }
            else if (variant == 2)
            {
                FillRect(texture, 8, 7 + bodyY, 4, 10, accent);
                FillRect(texture, 7, 13 + bodyY, 6, 1, outline);
            }
            else if (variant == 3)
            {
                FillRect(texture, 4 + frame.LeftArmX, 10 + bodyY + frame.LeftArmY, 2, 8, accent);
                FillRect(texture, 14 + frame.RightArmX, 10 + bodyY + frame.RightArmY, 2, 8, accent);
            }
            else if (variant == 4)
            {
                FillRect(texture, 5, 14 + bodyY, 10, 3, accent);
                SetPixelSafe(texture, 10, 10 + bodyY, accent);
            }

            FillEllipse(texture, 10, 21 + bodyY, 5, 5, skin);
            FillRect(texture, 6, 22 + bodyY, 8, 4, hair);
            FillRect(texture, 5, 20 + bodyY, 2, 4, hair);
            FillRect(texture, 13, 20 + bodyY, 2, 4, hair);

            if (variant == 1)
            {
                FillRect(texture, 5, 23 + bodyY, 10, 3, accent);
                FillRect(texture, 6, 20 + bodyY, 2, 3, accent);
                FillRect(texture, 12, 20 + bodyY, 2, 3, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 14, 18 + bodyY, 2, 5, hair);
                FillRect(texture, 15, 15 + bodyY, 1, 4, hair);
            }
            else if (variant == 3)
            {
                FillRect(texture, 4, 18 + bodyY, 2, 6, hair);
                FillRect(texture, 14, 18 + bodyY, 2, 6, hair);
            }
            else if (variant == 4)
            {
                FillRect(texture, 5, 24 + bodyY, 10, 2, accent);
                FillRect(texture, 8, 26 + bodyY, 4, 1, accent);
            }

            SetPixelSafe(texture, 8, 21 + bodyY, outline);
            SetPixelSafe(texture, 12, 21 + bodyY, outline);
        }

        private static Color GetHairColor(StrategyResidentGender gender, int variant)
        {
            if (gender == StrategyResidentGender.Male)
            {
                return variant switch
                {
                    1 => Rgb(96, 62, 35),
                    2 => Rgb(157, 111, 57),
                    3 => Rgb(42, 35, 31),
                    4 => Rgb(102, 75, 44),
                    _ => Rgb(67, 43, 29)
                };
            }

            return variant switch
            {
                1 => Rgb(86, 55, 38),
                2 => Rgb(173, 121, 62),
                3 => Rgb(50, 38, 32),
                4 => Rgb(130, 65, 46),
                _ => Rgb(116, 74, 38)
            };
        }

        private static Color GetTunicColor(StrategyResidentGender gender, int variant)
        {
            if (gender == StrategyResidentGender.Male)
            {
                return variant switch
                {
                    1 => Rgb(78, 125, 73),
                    2 => Rgb(127, 87, 50),
                    3 => Rgb(79, 86, 107),
                    4 => Rgb(142, 66, 49),
                    _ => Rgb(56, 106, 132)
                };
            }

            return variant switch
            {
                1 => Rgb(83, 128, 88),
                2 => Rgb(151, 105, 55),
                3 => Rgb(62, 98, 136),
                4 => Rgb(142, 64, 61),
                _ => Rgb(132, 75, 102)
            };
        }

        private static Color GetTunicDarkColor(StrategyResidentGender gender, int variant)
        {
            if (gender == StrategyResidentGender.Male)
            {
                return variant switch
                {
                    1 => Rgb(52, 88, 52),
                    2 => Rgb(88, 61, 39),
                    3 => Rgb(48, 55, 73),
                    4 => Rgb(93, 45, 39),
                    _ => Rgb(37, 73, 94)
                };
            }

            return variant switch
            {
                1 => Rgb(48, 91, 61),
                2 => Rgb(98, 70, 43),
                3 => Rgb(41, 69, 103),
                4 => Rgb(92, 43, 50),
                _ => Rgb(94, 47, 74)
            };
        }

        private static Color GetAccentColor(StrategyResidentGender gender, int variant)
        {
            if (gender == StrategyResidentGender.Male)
            {
                return variant switch
                {
                    1 => Rgb(119, 91, 46),
                    2 => Rgb(188, 152, 92),
                    3 => Rgb(48, 45, 62),
                    4 => Rgb(204, 167, 79),
                    _ => Rgb(120, 78, 48)
                };
            }

            return variant switch
            {
                1 => Rgb(190, 152, 80),
                2 => Rgb(227, 202, 150),
                3 => Rgb(73, 47, 77),
                4 => Rgb(215, 151, 94),
                _ => Rgb(170, 98, 126)
            };
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

        private static void DrawRectOutline(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            FillRect(texture, x, y, width, 1, color);
            FillRect(texture, x, y + height - 1, width, 1, color);
            FillRect(texture, x, y, 1, height, color);
            FillRect(texture, x + width - 1, y, 1, height, color);
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

        private static void DrawWoodcutAxe(Texture2D texture, int frame, Color outline)
        {
            WoodcutToolFrame tool = GetWoodcutToolFrame(frame);
            Color handleDark = Rgb(75, 48, 30);
            Color handle = Rgb(142, 88, 43);
            Color metal = Rgb(133, 146, 140);
            Color metalLight = Rgb(204, 214, 205);

            DrawThickLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), outline, 1);
            DrawLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), handle);
            if (tool.FrameIndex % 2 == 0)
            {
                DrawLine(texture, P(tool.HandleFromX + 1, tool.HandleFromY), P(tool.HandleToX + 1, tool.HandleToY), handleDark);
            }

            DrawAxeHead(texture, tool.HeadX, tool.HeadY, tool.HeadDirection, outline, metal, metalLight);
        }

        private static void DrawStonecutPickaxe(Texture2D texture, int frame, Color outline)
        {
            WoodcutToolFrame tool = GetStonecutToolFrame(frame);
            Color handleDark = Rgb(69, 49, 34);
            Color handle = Rgb(128, 85, 45);
            Color metal = Rgb(132, 145, 139);
            Color metalLight = Rgb(207, 216, 206);

            DrawThickLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), outline, 1);
            DrawLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), handle);
            if (tool.FrameIndex % 2 == 0)
            {
                DrawLine(texture, P(tool.HandleFromX + 1, tool.HandleFromY), P(tool.HandleToX + 1, tool.HandleToY), handleDark);
            }

            DrawPickHead(texture, tool.HeadX, tool.HeadY, tool.HeadDirection, outline, metal, metalLight);
        }

        private static void DrawConstructionHammer(Texture2D texture, int frame, Color outline)
        {
            WoodcutToolFrame tool = GetConstructionToolFrame(frame);
            Color handleDark = Rgb(76, 49, 30);
            Color handle = Rgb(145, 91, 45);
            Color metal = Rgb(124, 130, 126);
            Color metalLight = Rgb(205, 210, 203);

            DrawThickLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), outline, 1);
            DrawLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), handle);
            if (tool.FrameIndex % 2 == 0)
            {
                DrawLine(texture, P(tool.HandleFromX + 1, tool.HandleFromY), P(tool.HandleToX + 1, tool.HandleToY), handleDark);
            }

            DrawHammerHead(texture, tool.HeadX, tool.HeadY, tool.HeadDirection, outline, metal, metalLight);
        }

        private static void DrawAxeHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            FillRect(texture, x - 1, y - 1, 3, 3, outline);
            FillRect(texture, x, y, 2, 2, metal);
            FillRect(texture, dir > 0 ? x + dir : x - 3, y + 1, 3, 2, metal);
            FillRect(texture, dir > 0 ? x + dir : x - 3, y - 2, 3, 2, metal);
            SetPixelSafe(texture, x + dir * 3, y + 1, metalLight);
            SetPixelSafe(texture, x + dir * 3, y - 1, metalLight);
        }

        private static void DrawPickHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            DrawThickLine(texture, P(x - dir * 5, y + 2), P(x + dir * 5, y - 2), outline, 1);
            DrawLine(texture, P(x - dir * 5, y + 2), P(x + dir * 5, y - 2), metal);
            SetPixelSafe(texture, x - dir * 6, y + 3, metalLight);
            SetPixelSafe(texture, x + dir * 6, y - 3, metalLight);
            FillRect(texture, x - 1, y - 1, 3, 3, outline);
            FillRect(texture, x, y, 2, 2, metal);
        }

        private static void DrawHammerHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            FillRect(texture, x - 3, y - 2, 7, 5, outline);
            FillRect(texture, x - 2, y - 1, 5, 3, metal);
            FillRect(texture, x + dir * 2, y, 3, 2, metalLight);
        }

        private static void DrawThickLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color, int radius)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int sx = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y);
            int sy = from.y < to.y ? 1 : -1;
            int err = dx + dy;
            int x = from.x;
            int y = from.y;

            while (true)
            {
                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        if (Mathf.Abs(ox) + Mathf.Abs(oy) <= radius)
                        {
                            SetPixelSafe(texture, x + ox, y + oy, color);
                        }
                    }
                }

                if (x == to.x && y == to.y)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
        {
            DrawThickLine(texture, from, to, color, 0);
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static string GetSpriteName(string genderName, int variant, ResidentSpritePose pose, int frame)
        {
            return pose switch
            {
                ResidentSpritePose.Idle => $"{genderName} Resident Sprite {variant + 1}",
                ResidentSpritePose.Portrait => $"{genderName} Resident Portrait {variant + 1}",
                ResidentSpritePose.Woodcut => $"{genderName} Resident Woodcut {variant + 1}-{frame + 1}",
                ResidentSpritePose.Stonecut => $"{genderName} Resident Stonecut {variant + 1}-{frame + 1}",
                ResidentSpritePose.Construction => $"{genderName} Resident Construction {variant + 1}-{frame + 1}",
                _ => $"{genderName} Resident Walk {variant + 1}-{frame + 1}"
            };
        }

        private static ResidentWalkFrame GetWalkFrame(int frame)
        {
            return WalkFrames[NormalizeVariant(frame, WalkFrameCount)];
        }

        private static ResidentWalkFrame GetWoodcutBodyFrame(int frame)
        {
            return WoodcutBodyFrames[NormalizeVariant(frame, WoodcutFrameCount)];
        }

        private static ResidentWalkFrame GetStonecutBodyFrame(int frame)
        {
            return StonecutBodyFrames[NormalizeVariant(frame, StonecutFrameCount)];
        }

        private static ResidentWalkFrame GetConstructionBodyFrame(int frame)
        {
            return ConstructionBodyFrames[NormalizeVariant(frame, ConstructionFrameCount)];
        }

        private static WoodcutToolFrame GetWoodcutToolFrame(int frame)
        {
            return WoodcutToolFrames[NormalizeVariant(frame, WoodcutFrameCount)];
        }

        private static WoodcutToolFrame GetStonecutToolFrame(int frame)
        {
            return StonecutToolFrames[NormalizeVariant(frame, StonecutFrameCount)];
        }

        private static WoodcutToolFrame GetConstructionToolFrame(int frame)
        {
            return ConstructionToolFrames[NormalizeVariant(frame, ConstructionFrameCount)];
        }

        private static int GetCacheKey(StrategyResidentGender gender, int variant, ResidentSpritePose pose, int frame)
        {
            return ((int)gender * 2048) + (variant * 256) + ((int)pose * 48) + frame;
        }

        private static int NormalizeVariant(int variant, int variantCount)
        {
            if (variantCount <= 0)
            {
                return 0;
            }

            int normalized = variant % variantCount;
            return normalized < 0 ? normalized + variantCount : normalized;
        }

        private enum ResidentSpritePose
        {
            Idle,
            Walk,
            Portrait,
            Woodcut,
            Stonecut,
            Construction
        }

        private static readonly ResidentWalkFrame[] WalkFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 1, -1, 1, -1),
            new ResidentWalkFrame(1, -2, 2, -2, 2, 1, -1, 1, -1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, 0, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new ResidentWalkFrame(0, 1, -1, 1, -1, -1, 1, -1, 1),
            new ResidentWalkFrame(1, 2, -2, 2, -2, -1, 1, -1, 1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, 0, 0, 0)
        };

        private static readonly ResidentWalkFrame[] WoodcutBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, -1, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -1, -2, 2, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] WoodcutToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(8, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(9, 12, 13, 16, 21, 16, 21, 1)
        };

        private static readonly ResidentWalkFrame[] StonecutBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, -1, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -1, -2, 2, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] StonecutToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(8, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(9, 12, 13, 16, 21, 16, 21, 1)
        };

        private static readonly ResidentWalkFrame[] ConstructionBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 1, 1, -1, -1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, 0, -1, 1, 2),
            new ResidentWalkFrame(1, 0, 0, 0, 0, -1, -2, 3, 3),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -1, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] ConstructionToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 9, 15, 16, 18, 16, 18, 1),
            new WoodcutToolFrame(8, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(9, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(10, 12, 14, 17, 20, 17, 20, 1),
            new WoodcutToolFrame(11, 12, 13, 16, 21, 16, 21, 1)
        };

        private readonly struct ResidentWalkFrame
        {
            public ResidentWalkFrame(
                int bodyYOffset,
                int leftLegX,
                int rightLegX,
                int leftFootX,
                int rightFootX,
                int leftArmX,
                int rightArmX,
                int leftArmY,
                int rightArmY)
            {
                BodyYOffset = bodyYOffset;
                LeftLegX = leftLegX;
                RightLegX = rightLegX;
                LeftFootX = leftFootX;
                RightFootX = rightFootX;
                LeftArmX = leftArmX;
                RightArmX = rightArmX;
                LeftArmY = leftArmY;
                RightArmY = rightArmY;
            }

            public static ResidentWalkFrame Idle => new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0);

            public int BodyYOffset { get; }
            public int LeftLegX { get; }
            public int RightLegX { get; }
            public int LeftFootX { get; }
            public int RightFootX { get; }
            public int LeftArmX { get; }
            public int RightArmX { get; }
            public int LeftArmY { get; }
            public int RightArmY { get; }
        }

        private readonly struct WoodcutToolFrame
        {
            public WoodcutToolFrame(
                int frameIndex,
                int handleFromX,
                int handleFromY,
                int handleToX,
                int handleToY,
                int headX,
                int headY,
                int headDirection)
            {
                FrameIndex = frameIndex;
                HandleFromX = handleFromX;
                HandleFromY = handleFromY;
                HandleToX = handleToX;
                HandleToY = handleToY;
                HeadX = headX;
                HeadY = headY;
                HeadDirection = headDirection;
            }

            public int FrameIndex { get; }
            public int HandleFromX { get; }
            public int HandleFromY { get; }
            public int HandleToX { get; }
            public int HandleToY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int HeadDirection { get; }
        }
    }
}
