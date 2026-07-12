using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {

        private static void DrawRoofDetails(
            Texture2D texture,
            int variant,
            Color outline,
            Color roofLight,
            Color roofDark,
            int roofLeft,
            int roofEaveY,
            int roofPeakY,
            int roofBaseY,
            int frontLeft,
            int frontRight,
            int roofRightFront)
        {
            DrawThickLine(texture, P(roofLeft + 4, roofEaveY), P(43, roofPeakY - 4), roofLight, 1);
            DrawThickLine(texture, P(frontLeft + 2, roofBaseY + 3), P(frontRight, roofBaseY + 3), roofLight, 1);
            DrawThickLine(texture, P(43, roofPeakY - 2), P(82, variant == 3 ? 60 : 57), roofLight, 1);

            if (variant == 2)
            {
                for (int x = 23; x <= 62; x += 8)
                {
                    DrawLine(texture, P(x, roofBaseY + 2), P(x + 14, roofEaveY + 16), Rgb(132, 94, 37));
                }

                for (int x = 18; x <= 64; x += 5)
                {
                    DrawLine(texture, P(x, roofEaveY - 1), P(x + 2, roofEaveY - 5), roofLight);
                }
            }
            else if (variant == 1)
            {
                for (int y = roofBaseY + 6; y <= roofPeakY - 7; y += 6)
                {
                    DrawLine(texture, P(24, y), P(61, y + 1), Rgb(45, 60, 70));
                }
            }
            else
            {
                DrawLine(texture, P(30, roofBaseY + 5), P(45, roofBaseY + 21), roofDark);
                DrawLine(texture, P(43, roofBaseY + 3), P(55, roofBaseY + 15), roofDark);
                DrawLine(texture, P(57, roofBaseY + 3), P(roofRightFront - 4, roofEaveY), roofDark);
                DrawLine(texture, P(53, roofPeakY - 6), P(78, variant == 3 ? 61 : 58), roofDark);
                DrawLine(texture, P(60, roofPeakY - 10), P(79, variant == 3 ? 59 : 56), roofDark);
            }

            if (variant == 4)
            {
                FillRect(texture, 32, 38, 18, 3, roofDark);
                DrawRectOutline(texture, 32, 38, 18, 3, outline);
            }

            DrawThickLine(texture, P(roofLeft + 3, roofEaveY - 1), P(frontLeft + 3, roofBaseY), outline, 1);
            DrawThickLine(texture, P(frontRight, roofBaseY), P(roofRightFront, roofEaveY + 1), outline, 1);
            DrawThickLine(texture, P(43, roofPeakY), P(84, variant == 3 ? 61 : 58), outline, 1);
        }

        private static Sprite CreateLumberjackCampSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 96, $"Lumberjack Camp 2.5D Sprite {variant + 1}");

            Color outline = Rgb(44, 31, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color soilDark = variant == 1 ? Rgb(85, 71, 45) : Rgb(73, 63, 42);
            Color soilLight = variant == 2 ? Rgb(137, 114, 69) : Rgb(119, 100, 60);
            Color logDark = variant == 2 ? Rgb(82, 52, 31) : Rgb(91, 58, 35);
            Color log = variant == 1 ? Rgb(132, 84, 42) : Rgb(119, 74, 40);
            Color logLight = variant == 2 ? Rgb(183, 124, 62) : Rgb(169, 111, 55);
            Color canvas = variant switch
            {
                1 => Rgb(119, 136, 76),
                2 => Rgb(144, 116, 69),
                _ => Rgb(126, 93, 55)
            };
            Color canvasDark = variant switch
            {
                1 => Rgb(75, 95, 58),
                2 => Rgb(103, 78, 45),
                _ => Rgb(82, 62, 42)
            };
            Color canvasLight = variant switch
            {
                1 => Rgb(169, 181, 104),
                2 => Rgb(188, 152, 87),
                _ => Rgb(171, 127, 70)
            };
            Color metal = Rgb(128, 139, 135);

            FillEllipse(texture, 48, 13, 35, 8, shadow);

            Vector2Int[] ground = { P(13, 18), P(46, 7), P(84, 18), P(53, 32) };
            FillPolygon(texture, ground, soilDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(20, 18), P(47, 10), P(77, 19), P(52, 28) };
            FillPolygon(texture, groundTop, soilLight);

            DrawThickLine(texture, P(30, 21), P(30, 50), logDark, 2);
            DrawThickLine(texture, P(67, 22), P(67, 48), logDark, 2);
            DrawThickLine(texture, P(30, 22), P(30, 50), log, 1);
            DrawThickLine(texture, P(67, 23), P(67, 48), log, 1);

            Vector2Int[] rearCanvas = { P(31, 26), P(66, 27), P(66, 43), P(31, 41) };
            FillPolygon(texture, rearCanvas, canvasDark);
            DrawPolygon(texture, rearCanvas, outline);
            FillRect(texture, 36, 29, 25, 10, Rgb(55, 45, 32));
            DrawLine(texture, P(38, 31), P(59, 32), logLight);

            Vector2Int[] roofFront = { P(20, 43), P(47, 68), P(75, 44), P(66, 36), P(47, 57), P(29, 35) };
            FillPolygon(texture, roofFront, canvas);
            DrawPolygon(texture, roofFront, outline);
            Vector2Int[] roofSide = { P(47, 68), P(83, 54), P(75, 44), P(66, 36) };
            FillPolygon(texture, roofSide, canvasDark);
            DrawPolygon(texture, roofSide, outline);
            DrawThickLine(texture, P(28, 43), P(47, 62), canvasLight, 1);
            DrawLine(texture, P(37, 41), P(55, 56), canvasDark);
            DrawLine(texture, P(51, 57), P(77, 48), canvasLight);

            for (int i = 0; i < 4; i++)
            {
                int y = 18 + i * 5;
                int x = 57 - (i % 2) * 3;
                FillRect(texture, x, y, 21, 4, log);
                DrawRectOutline(texture, x, y, 21, 4, outline);
                FillEllipse(texture, x + 2, y + 2, 2, 2, logLight);
                FillEllipse(texture, x + 18, y + 2, 2, 2, logDark);
            }

            FillEllipse(texture, 24, 22, 9, 4, logDark);
            FillRect(texture, 18, 22, 12, 10, log);
            DrawRectOutline(texture, 18, 22, 12, 10, outline);
            FillEllipse(texture, 24, 32, 7, 3, logLight);
            DrawLine(texture, P(20, 32), P(28, 32), outline);

            DrawThickLine(texture, P(24, 34), P(36, 47), logLight, 1);
            Vector2Int[] axeHead = { P(34, 45), P(43, 47), P(40, 53), P(33, 50) };
            FillPolygon(texture, axeHead, metal);
            DrawPolygon(texture, axeHead, outline);

            DrawLine(texture, P(27, 17), P(48, 11), soilDark);
            DrawLine(texture, P(51, 27), P(71, 20), soilDark);

            DrawBuildingPolish(texture, StrategyBuildTool.LumberjackCamp, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 80f, 76f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateLumberjackCampStockSprite(int level)
        {
            Texture2D texture = CreateTexture(64, 40, $"Lumberjack Camp Stock {level}");
            Color outline = Rgb(40, 30, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            Color logDark = Rgb(84, 53, 32);
            Color log = Rgb(126, 78, 42);
            Color logLight = Rgb(181, 119, 58);
            Color cut = Rgb(220, 171, 96);
            Color rings = Rgb(112, 71, 42);

            FillEllipse(texture, 32, 7, 23, 5, shadow);

            int rows = Mathf.Clamp(level, 1, 5);
            for (int row = 0; row < rows; row++)
            {
                int logsInRow = Mathf.Min(4, 1 + row);
                int y = 10 + row * 5;
                int startX = 30 - logsInRow * 7;
                for (int i = 0; i < logsInRow; i++)
                {
                    int x = startX + i * 14 + (row % 2) * 3;
                    DrawStockLog(texture, x, y, 16, 5, logDark, log, logLight, cut, rings, outline);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(6f, 5f, 52f, 30f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateStonecutterCampSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 92, $"Stonecutter Camp 2.5D Sprite {variant + 1}");

            Color outline = Rgb(42, 35, 30);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color dirtDark = variant == 1 ? Rgb(78, 71, 61) : Rgb(68, 63, 56);
            Color dirtLight = variant == 2 ? Rgb(137, 125, 102) : Rgb(114, 105, 87);
            Color stoneDark = variant == 2 ? Rgb(78, 84, 82) : Rgb(86, 88, 82);
            Color stone = variant == 1 ? Rgb(130, 136, 126) : Rgb(117, 125, 120);
            Color stoneLight = Rgb(188, 194, 181);
            Color wood = variant == 2 ? Rgb(109, 73, 43) : Rgb(126, 82, 45);
            Color woodDark = Rgb(72, 51, 35);
            Color cloth = variant switch
            {
                1 => Rgb(117, 103, 81),
                2 => Rgb(95, 117, 105),
                _ => Rgb(128, 93, 74)
            };
            Color clothDark = Shift(cloth, -0.16f);
            Color metal = Rgb(132, 145, 139);

            FillEllipse(texture, 48, 12, 35, 8, shadow);

            Vector2Int[] ground = { P(12, 18), P(45, 7), P(84, 19), P(54, 33) };
            FillPolygon(texture, ground, dirtDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(20, 18), P(46, 10), P(77, 20), P(53, 29) };
            FillPolygon(texture, groundTop, dirtLight);

            DrawThickLine(texture, P(27, 21), P(27, 49), woodDark, 2);
            DrawThickLine(texture, P(68, 22), P(68, 47), woodDark, 2);
            DrawThickLine(texture, P(28, 22), P(28, 49), wood, 1);
            DrawThickLine(texture, P(69, 23), P(69, 47), wood, 1);

            Vector2Int[] rearCloth = { P(31, 28), P(66, 29), P(66, 42), P(31, 41) };
            FillPolygon(texture, rearCloth, clothDark);
            DrawPolygon(texture, rearCloth, outline);

            Vector2Int[] roofFront = { P(20, 43), P(47, 66), P(75, 44), P(66, 37), P(47, 56), P(29, 36) };
            FillPolygon(texture, roofFront, cloth);
            DrawPolygon(texture, roofFront, outline);
            Vector2Int[] roofSide = { P(47, 66), P(83, 53), P(75, 44), P(66, 37) };
            FillPolygon(texture, roofSide, clothDark);
            DrawPolygon(texture, roofSide, outline);
            DrawThickLine(texture, P(30, 43), P(47, 60), Shift(cloth, 0.14f), 1);
            DrawLine(texture, P(52, 56), P(77, 47), Shift(cloth, 0.12f));

            for (int i = 0; i < 5; i++)
            {
                int x = 18 + (i % 3) * 12 + (i / 3) * 5;
                int y = 18 + (i / 3) * 7 + (i % 2) * 2;
                FillEllipse(texture, x, y, 8, 5, stoneDark);
                FillEllipse(texture, x - 1, y + 1, 6, 4, stone);
                SetPixelSafe(texture, x - 3, y + 3, stoneLight);
                DrawCanopyRim(texture, x, y, 8, 5, outline);
            }

            FillRect(texture, 58, 18, 20, 6, stoneDark);
            FillRect(texture, 59, 20, 18, 5, stone);
            DrawRectOutline(texture, 58, 18, 20, 8, outline);
            DrawLine(texture, P(61, 24), P(74, 24), stoneLight);

            DrawThickLine(texture, P(22, 34), P(38, 47), wood, 1);
            DrawPickHead(texture, 38, 47, outline, metal, stoneLight);
            DrawThickLine(texture, P(63, 33), P(48, 45), woodDark, 1);
            DrawPickHead(texture, 48, 45, outline, metal, stoneLight);

            DrawBuildingPolish(texture, StrategyBuildTool.StonecutterCamp, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 80f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateStonecutterCampStockSprite(int level)
        {
            Texture2D texture = CreateTexture(60, 38, $"Stonecutter Camp Stock {level}");
            Color outline = Rgb(38, 39, 38);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color stoneDark = Rgb(78, 84, 82);
            Color stone = Rgb(124, 132, 126);
            Color stoneLight = Rgb(178, 186, 176);

            FillEllipse(texture, 30, 7, 20, 5, shadow);
            int stones = Mathf.Clamp(level + 2, 3, 8);
            for (int i = 0; i < stones; i++)
            {
                int x = 14 + (i % 4) * 9 + (i / 4) * 4;
                int y = 10 + (i / 4) * 6 + (i % 2) * 2;
                int rx = 4 + (i % 2);
                int ry = 3 + ((i + 1) % 2);
                FillEllipse(texture, x, y, rx, ry, stoneDark);
                FillEllipse(texture, x - 1, y + 1, Mathf.Max(2, rx - 1), Mathf.Max(2, ry - 1), stone);
                SetPixelSafe(texture, x - 2, y + ry, stoneLight);
                SetPixelSafe(texture, x + rx - 1, y - 1, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 4f, 50f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static void DrawStockLog(
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color dark,
            Color mid,
            Color light,
            Color cut,
            Color rings,
            Color outline)
        {
            FillRect(texture, x + 2, y, width - 3, height, dark);
            FillRect(texture, x + 3, y + 1, width - 4, Mathf.Max(1, height - 2), mid);
            DrawRectOutline(texture, x + 2, y, width - 2, height, outline);
            DrawLine(texture, P(x + 5, y + height - 2), P(x + width - 4, y + height - 2), light);
            FillEllipse(texture, x + 2, y + height / 2, 2, Mathf.Max(2, height / 2), cut);
            DrawLine(texture, P(x + 1, y + height / 2), P(x + 3, y + height / 2), rings);
        }

        private static Sprite CreateHunterCampSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 96, $"Hunter Camp 2.5D Sprite {variant + 1}");

            Color outline = Rgb(42, 30, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.23f);
            Color dirtDark = variant == 1 ? Rgb(78, 66, 43) : Rgb(70, 61, 41);
            Color dirtLight = variant == 2 ? Rgb(137, 112, 72) : Rgb(119, 98, 60);
            Color woodDark = Rgb(75, 48, 30);
            Color wood = variant == 1 ? Rgb(133, 88, 48) : Rgb(118, 76, 43);
            Color woodLight = Rgb(179, 122, 65);
            Color hide = variant switch
            {
                1 => Rgb(116, 93, 62),
                2 => Rgb(103, 114, 77),
                _ => Rgb(129, 89, 58)
            };
            Color hideDark = Shift(hide, -0.18f);
            Color hideLight = Shift(hide, 0.14f);
            Color leather = Rgb(96, 58, 37);
            Color feather = Rgb(207, 185, 129);

            FillEllipse(texture, 48, 12, 35, 8, shadow);

            Vector2Int[] ground = { P(13, 18), P(46, 7), P(84, 18), P(53, 33) };
            FillPolygon(texture, ground, dirtDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(20, 18), P(47, 10), P(77, 19), P(52, 28) };
            FillPolygon(texture, groundTop, dirtLight);

            DrawThickLine(texture, P(30, 22), P(30, 52), woodDark, 2);
            DrawThickLine(texture, P(65, 23), P(65, 50), woodDark, 2);
            DrawThickLine(texture, P(31, 22), P(31, 52), wood, 1);
            DrawThickLine(texture, P(66, 23), P(66, 50), wood, 1);

            Vector2Int[] rearHide = { P(33, 29), P(63, 29), P(65, 42), P(32, 41) };
            FillPolygon(texture, rearHide, hideDark);
            DrawPolygon(texture, rearHide, outline);

            Vector2Int[] awning = { P(20, 43), P(47, 65), P(76, 44), P(67, 38), P(47, 56), P(29, 37) };
            FillPolygon(texture, awning, hide);
            DrawPolygon(texture, awning, outline);
            DrawThickLine(texture, P(30, 43), P(47, 59), hideLight, 1);
            DrawLine(texture, P(52, 55), P(77, 46), hideLight);
            DrawLine(texture, P(40, 44), P(64, 44), leather);
            DrawLine(texture, P(45, 48), P(55, 55), leather);

            DrawCampBow(texture, 22, 25, outline, wood, woodLight);
            DrawCampBow(texture, 72, 28, outline, woodDark, woodLight);
            DrawArrowBundle(texture, 56, 21, outline, woodLight, feather);

            FillRect(texture, 31, 18, 17, 10, Rgb(109, 69, 39));
            FillRect(texture, 32, 19, 15, 8, Rgb(152, 101, 54));
            DrawRectOutline(texture, 31, 18, 17, 10, outline);
            DrawLine(texture, P(34, 25), P(45, 25), woodLight);

            FillEllipse(texture, 62, 17, 10, 5, new Color(0f, 0f, 0f, 0.18f));
            FillEllipse(texture, 61, 20, 5, 5, leather);
            FillEllipse(texture, 66, 18, 3, 3, Rgb(154, 107, 72));
            DrawCanopyRim(texture, 61, 20, 5, 5, outline);

            DrawBuildingPolish(texture, StrategyBuildTool.HunterCamp, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 80f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateHunterCampStockSprite(int level)
        {
            Texture2D texture = CreateTexture(58, 38, $"Hunter Camp Stock {level}");
            Color outline = Rgb(50, 30, 25);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color meatDark = Rgb(103, 38, 34);
            Color meat = Rgb(166, 70, 54);
            Color meatLight = Rgb(214, 118, 81);
            Color bone = Rgb(222, 205, 164);
            Color rack = Rgb(93, 58, 35);

            FillEllipse(texture, 29, 7, 19, 5, shadow);
            DrawThickLine(texture, P(8, 11), P(48, 18), rack, 1);
            DrawThickLine(texture, P(9, 10), P(47, 17), outline, 0);

            int pieces = Mathf.Clamp(level + 1, 2, 6);
            for (int i = 0; i < pieces; i++)
            {
                int x = 12 + (i % 3) * 13 + (i / 3) * 4;
                int y = 13 + (i / 3) * 8 + (i % 2) * 2;
                FillEllipse(texture, x, y, 5, 4, outline);
                FillEllipse(texture, x, y, 4, 3, i % 2 == 0 ? meat : meatDark);
                FillEllipse(texture, x + 3, y + 2, 2, 2, meatLight);
                FillRect(texture, x - 6, y - 1, 4, 2, bone);
                FillEllipse(texture, x - 7, y, 2, 2, bone);
                SetPixelSafe(texture, x + 4, y - 3, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 50f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateFisherHutSprite(int variant)
        {
            Texture2D texture = CreateTexture(100, 96, $"Fisher Hut 2.5D Sprite {variant + 1}");

            Color outline = Rgb(34, 36, 31);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color dirtDark = variant == 1 ? Rgb(74, 69, 48) : Rgb(67, 62, 44);
            Color dirtLight = variant == 2 ? Rgb(134, 116, 76) : Rgb(118, 103, 67);
            Color plankDark = Rgb(71, 50, 33);
            Color plank = variant == 1 ? Rgb(129, 87, 51) : Rgb(113, 75, 45);
            Color plankLight = Rgb(174, 121, 70);
            Color wall = variant == 2 ? Rgb(151, 128, 82) : Rgb(139, 111, 70);
            Color wallLight = Shift(wall, 0.15f);
            Color roof = variant switch
            {
                1 => Rgb(83, 117, 121),
                2 => Rgb(95, 91, 74),
                _ => Rgb(71, 107, 122)
            };
            Color roofDark = Shift(roof, -0.18f);
            Color water = new Color(0.29f, 0.55f, 0.68f, 0.72f);
            Color waterLight = new Color(0.55f, 0.80f, 0.88f, 0.58f);

            FillEllipse(texture, 50, 12, 36, 8, shadow);
            Vector2Int[] ground = { P(12, 19), P(48, 8), P(88, 19), P(55, 34) };
            FillPolygon(texture, ground, dirtDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(20, 19), P(49, 11), P(80, 20), P(55, 28) };
            FillPolygon(texture, groundTop, dirtLight);

            Vector2Int[] waterPatch = { P(54, 9), P(86, 17), P(62, 28), P(33, 18) };
            FillPolygon(texture, waterPatch, water);
            DrawLine(texture, P(40, 18), P(75, 21), waterLight);
            DrawLine(texture, P(49, 14), P(83, 18), waterLight);

            DrawThickLine(texture, P(28, 18), P(28, 46), plankDark, 2);
            DrawThickLine(texture, P(72, 20), P(72, 45), plankDark, 2);
            DrawThickLine(texture, P(29, 19), P(29, 46), plank, 1);
            DrawThickLine(texture, P(73, 21), P(73, 45), plank, 1);

            Vector2Int[] bodySide = { P(37, 24), P(70, 25), P(70, 46), P(37, 45) };
            FillPolygon(texture, bodySide, Shift(wall, -0.10f));
            DrawPolygon(texture, bodySide, outline);
            Vector2Int[] bodyFront = { P(25, 25), P(43, 21), P(43, 45), P(25, 48) };
            FillPolygon(texture, bodyFront, wall);
            DrawPolygon(texture, bodyFront, outline);
            FillRect(texture, 30, 27, 8, 18, plankDark);
            FillRect(texture, 31, 28, 6, 16, plank);
            DrawRectOutline(texture, 30, 27, 8, 18, outline);
            FillRect(texture, 54, 31, 10, 8, Rgb(91, 143, 150));
            DrawRectOutline(texture, 54, 31, 10, 8, outline);
            DrawLine(texture, P(55, 35), P(63, 35), wallLight);

            Vector2Int[] roofFront = { P(19, 46), P(43, 66), P(73, 45), P(68, 39), P(43, 57), P(27, 39) };
            FillPolygon(texture, roofFront, roof);
            DrawPolygon(texture, roofFront, outline);
            Vector2Int[] roofSide = { P(43, 57), P(73, 45), P(80, 39), P(53, 52) };
            FillPolygon(texture, roofSide, roofDark);
            DrawPolygon(texture, roofSide, outline);
            DrawThickLine(texture, P(30, 44), P(43, 60), Shift(roof, 0.15f), 1);
            DrawLine(texture, P(50, 55), P(75, 44), Shift(roof, 0.12f));

            for (int i = 0; i < 3; i++)
            {
                int x = 45 + i * 5;
                DrawLine(texture, P(x, 20), P(x + 9, 43), outline);
                DrawLine(texture, P(x + 1, 20), P(x + 10, 43), plankLight);
            }

            DrawFishingPole(texture, 22, 25, outline, plankDark, plankLight);
            DrawFishNet(texture, 76, 25, outline, Rgb(178, 163, 112), waterLight);
            DrawFishIcon(texture, 59, 19, outline, Rgb(70, 137, 161), Rgb(223, 151, 76));

            DrawBuildingPolish(texture, StrategyBuildTool.FisherHut, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 84f, 76f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }
    }
}
