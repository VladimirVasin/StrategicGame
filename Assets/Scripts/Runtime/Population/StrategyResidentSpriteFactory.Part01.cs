using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {

        private static Sprite CreateChildPortraitSprite(StrategyResidentGender gender, int variant)
        {
            Texture2D texture = new Texture2D(40, 40, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Boy", variant, ResidentSpritePose.Portrait, 0)
                    : GetSpriteName("Girl", variant, ResidentSpritePose.Portrait, 0),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[40 * 40]);

            Color outline = Rgb(48, 33, 28);
            Color skin = variant switch
            {
                1 => Rgb(188, 132, 92),
                2 => Rgb(228, 183, 132),
                3 => Rgb(161, 111, 84),
                4 => Rgb(220, 157, 104),
                _ => Rgb(211, 164, 113)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = Color.Lerp(GetTunicColor(gender, variant), Rgb(213, 177, 95), 0.22f);
            Color tunicDark = Color.Lerp(GetTunicDarkColor(gender, variant), Rgb(84, 61, 44), 0.16f);
            Color accent = GetAccentColor(gender, variant);

            FillEllipse(texture, 20, 8, 11, 3, new Color(0f, 0f, 0f, 0.22f));
            FillRect(texture, 13, 9, 14, 9, tunicDark);
            FillRect(texture, 15, 12, 10, 8, tunic);
            DrawRectOutline(texture, 13, 9, 14, 9, outline);
            FillRect(texture, 14, 13, 3, 6, skin);
            FillRect(texture, 24, 13, 3, 6, skin);

            if (variant == 1 || variant == 4)
            {
                FillRect(texture, 16, 15, 8, 2, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 19, 10, 3, 9, accent);
            }

            FillEllipse(texture, 20, 25, 10, 10, outline);
            FillEllipse(texture, 20, 24, 8, 8, skin);
            FillRect(texture, 13, 29, 14, 4, hair);
            FillRect(texture, 12, 24, 4, 7, hair);
            FillRect(texture, 24, 24, 4, 7, hair);

            if (gender == StrategyResidentGender.Female)
            {
                FillRect(texture, 10, 19, 4, 9, hair);
                FillRect(texture, 26, 19, 4, 9, hair);
                if (variant == 1 || variant == 4)
                {
                    FillRect(texture, 12, 30, 16, 2, accent);
                    SetPixelSafe(texture, 12, 27, accent);
                    SetPixelSafe(texture, 28, 27, accent);
                }
            }
            else if (variant == 1 || variant == 3)
            {
                FillRect(texture, 12, 30, 16, 2, accent);
            }

            SetPixelSafe(texture, 17, 25, outline);
            SetPixelSafe(texture, 23, 25, outline);
            SetPixelSafe(texture, 20, 22, Rgb(135, 89, 67));
            FillRect(texture, 18, 20, 4, 1, outline);
            SetPixelSafe(texture, 16, 22, Rgb(231, 153, 134));
            SetPixelSafe(texture, 24, 22, Rgb(231, 153, 134));

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

        private static void DrawCryingOverlay(Texture2D texture, int frame, Color outline, Color skin, int bodyY)
        {
            int sobFrame = NormalizeVariant(frame, CryFrameCount);
            int tremble = sobFrame == 2 || sobFrame == 3 ? 1 : 0;
            Color tear = Rgb(104, 179, 222);
            Color tearLight = Rgb(167, 220, 245);

            FillRect(texture, 5, 17 + bodyY + tremble, 4, 2, skin);
            FillRect(texture, 11, 17 + bodyY + (sobFrame >= 3 ? 1 : 0), 4, 2, skin);
            SetPixelSafe(texture, 7, 21 + bodyY, outline);
            SetPixelSafe(texture, 8, 20 + bodyY, outline);
            SetPixelSafe(texture, 12, 21 + bodyY, outline);
            SetPixelSafe(texture, 11, 20 + bodyY, outline);

            if (sobFrame != 1)
            {
                SetPixelSafe(texture, 7, 20 + bodyY, tearLight);
                SetPixelSafe(texture, 12, 20 + bodyY, tear);
            }

            if (sobFrame == 2 || sobFrame == 4)
            {
                SetPixelSafe(texture, 7, 19 + bodyY, tear);
                SetPixelSafe(texture, 12, 19 + bodyY, tearLight);
            }

            if (sobFrame >= 3)
            {
                FillRect(texture, 8, 19 + bodyY, 4, 1, outline);
            }
        }

        private static void DrawChildCryingOverlay(Texture2D texture, int frame, Color outline, Color skin, int bodyY)
        {
            int sobFrame = NormalizeVariant(frame, CryFrameCount);
            Color tear = Rgb(104, 179, 222);
            Color tearLight = Rgb(167, 220, 245);

            FillRect(texture, 4, 14 + bodyY, 3, 2, skin);
            FillRect(texture, 11, 14 + bodyY + (sobFrame >= 3 ? 1 : 0), 3, 2, skin);
            SetPixelSafe(texture, 7, 17 + bodyY, outline);
            SetPixelSafe(texture, 8, 16 + bodyY, outline);
            SetPixelSafe(texture, 11, 17 + bodyY, outline);
            SetPixelSafe(texture, 10, 16 + bodyY, outline);

            if (sobFrame != 1)
            {
                SetPixelSafe(texture, 7, 16 + bodyY, tearLight);
                SetPixelSafe(texture, 11, 16 + bodyY, tear);
            }

            if (sobFrame == 2 || sobFrame == 4)
            {
                SetPixelSafe(texture, 7, 15 + bodyY, tear);
                SetPixelSafe(texture, 11, 15 + bodyY, tearLight);
            }
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
    }
}
