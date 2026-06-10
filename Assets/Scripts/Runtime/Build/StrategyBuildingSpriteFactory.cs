using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingSpriteFactory
    {
        private const float PixelsPerUnit = 24f;
        public const int HouseVariantCount = 5;
        public const int LumberjackCampVariantCount = 3;
        public const int StonecutterCampVariantCount = 3;
        public const int StorageYardVariantCount = 3;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static int GetVariantCount(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => HouseVariantCount,
                StrategyBuildTool.LumberjackCamp => LumberjackCampVariantCount,
                StrategyBuildTool.StonecutterCamp => StonecutterCampVariantCount,
                StrategyBuildTool.StorageYard => StorageYardVariantCount,
                _ => 1
            };
        }

        public static bool TryGetBuildSprite(StrategyBuildTool tool, out Sprite sprite)
        {
            return TryGetBuildSprite(tool, 0, out sprite);
        }

        public static bool TryGetBuildSprite(StrategyBuildTool tool, int variant, out Sprite sprite)
        {
            int variantCount = GetVariantCount(tool);
            if (tool != StrategyBuildTool.House
                && tool != StrategyBuildTool.LumberjackCamp
                && tool != StrategyBuildTool.StonecutterCamp
                && tool != StrategyBuildTool.StorageYard)
            {
                sprite = null;
                return false;
            }

            int normalizedVariant = NormalizeVariant(variant, variantCount);
            int cacheKey = GetCacheKey(tool, normalizedVariant);
            if (!CachedSprites.TryGetValue(cacheKey, out sprite) || sprite == null)
            {
                sprite = tool switch
                {
                    StrategyBuildTool.LumberjackCamp => CreateLumberjackCampSprite(normalizedVariant),
                    StrategyBuildTool.StonecutterCamp => CreateStonecutterCampSprite(normalizedVariant),
                    StrategyBuildTool.StorageYard => CreateStorageYardSprite(normalizedVariant),
                    _ => CreateHouseSprite(normalizedVariant)
                };
                CachedSprites[cacheKey] = sprite;
            }

            return sprite != null;
        }

        public static Sprite GetLumberjackCampStockSprite(int logsStored)
        {
            if (logsStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((logsStored + 1) / 2, 1, 5);
            int cacheKey = 32768 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateLumberjackCampStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStonecutterCampStockSprite(int stoneStored)
        {
            if (stoneStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stoneStored + 2) / 3, 1, 5);
            int cacheKey = 36864 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStonecutterCampStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardStockSprite(int logsStored)
        {
            if (logsStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((logsStored + 2) / 3, 1, 6);
            int cacheKey = 40960 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStorageYardStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardStoneStockSprite(int stoneStored)
        {
            if (stoneStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stoneStored + 3) / 4, 1, 6);
            int cacheKey = 45056 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStorageYardStoneStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateHouseSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 96, $"House 2.5D Sprite {variant + 1}");

            Color outline = Rgb(48, 34, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stoneDark = variant switch
            {
                1 => Rgb(62, 67, 70),
                4 => Rgb(88, 75, 58),
                _ => Rgb(72, 68, 61)
            };
            Color stoneLight = variant switch
            {
                1 => Rgb(132, 139, 137),
                4 => Rgb(144, 121, 86),
                _ => Rgb(123, 115, 97)
            };
            Color wall = variant switch
            {
                1 => Rgb(156, 153, 139),
                2 => Rgb(213, 186, 130),
                3 => Rgb(198, 169, 129),
                4 => Rgb(222, 212, 179),
                _ => Rgb(205, 173, 122)
            };
            Color wallLight = variant switch
            {
                1 => Rgb(184, 181, 164),
                2 => Rgb(238, 210, 157),
                3 => Rgb(224, 193, 149),
                4 => Rgb(244, 232, 194),
                _ => Rgb(230, 198, 144)
            };
            Color wallSide = variant switch
            {
                1 => Rgb(112, 113, 106),
                2 => Rgb(166, 132, 82),
                3 => Rgb(142, 104, 72),
                4 => Rgb(175, 158, 124),
                _ => Rgb(163, 128, 89)
            };
            Color timber = variant switch
            {
                1 => Rgb(72, 60, 50),
                2 => Rgb(116, 80, 42),
                3 => Rgb(61, 44, 34),
                4 => Rgb(96, 51, 36),
                _ => Rgb(93, 57, 34)
            };
            Color timberLight = variant switch
            {
                1 => Rgb(111, 95, 74),
                2 => Rgb(156, 109, 53),
                3 => Rgb(102, 76, 55),
                4 => Rgb(142, 76, 52),
                _ => Rgb(130, 83, 48)
            };
            Color roof = variant switch
            {
                1 => Rgb(70, 84, 95),
                2 => Rgb(184, 143, 54),
                3 => Rgb(111, 67, 42),
                4 => Rgb(146, 52, 45),
                _ => Rgb(143, 89, 40)
            };
            Color roofLight = variant switch
            {
                1 => Rgb(119, 141, 151),
                2 => Rgb(226, 187, 83),
                3 => Rgb(167, 106, 61),
                4 => Rgb(206, 88, 67),
                _ => Rgb(190, 128, 57)
            };
            Color roofDark = variant switch
            {
                1 => Rgb(42, 53, 64),
                2 => Rgb(125, 91, 39),
                3 => Rgb(69, 43, 31),
                4 => Rgb(92, 37, 39),
                _ => Rgb(92, 56, 32)
            };
            Color window = Rgb(92, 142, 152);
            Color windowLight = Rgb(185, 219, 210);
            Color door = variant switch
            {
                1 => Rgb(68, 50, 39),
                3 => Rgb(76, 49, 35),
                4 => Rgb(102, 45, 35),
                _ => Rgb(92, 55, 32)
            };

            int frontLeft = variant switch { 2 => 20, 3 => 27, _ => 23 };
            int frontRight = variant switch { 2 => 63, 3 => 59, 4 => 62, _ => 60 };
            int wallTop = variant switch { 1 => 51, 3 => 55, _ => 50 };
            int roofPeakY = variant switch { 2 => 72, 3 => 81, 4 => 74, _ => 76 };
            int roofLeft = variant == 2 ? 12 : 15;
            int roofRightFront = variant switch { 3 => 66, 4 => 73, _ => 69 };
            int roofEaveY = variant == 2 ? 49 : 51;
            int roofBaseY = variant == 3 ? 45 : 42;

            FillEllipse(texture, 48, 14, 34, 8, shadow);

            Vector2Int[] platform = variant == 2
                ? new[] { P(14, 17), P(47, 7), P(84, 18), P(51, 31) }
                : new[] { P(17, 17), P(47, 7), P(81, 18), P(51, 30) };
            FillPolygon(texture, platform, stoneDark);
            DrawPolygon(texture, platform, outline);
            Vector2Int[] platformTop = variant == 2
                ? new[] { P(19, 18), P(48, 10), P(79, 19), P(51, 28) }
                : new[] { P(22, 18), P(48, 10), P(76, 19), P(51, 27) };
            FillPolygon(texture, platformTop, stoneLight);

            Vector2Int[] sideWall = { P(frontRight - 1, 22), P(76, 29), P(76, wallTop + 2), P(frontRight - 1, wallTop - 1) };
            FillPolygon(texture, sideWall, wallSide);
            DrawPolygon(texture, sideWall, outline);

            Vector2Int[] frontWall = { P(frontLeft, 22), P(frontRight, 22), P(frontRight, wallTop), P(frontLeft, wallTop) };
            FillPolygon(texture, frontWall, wall);
            DrawPolygon(texture, frontWall, outline);

            Vector2Int[] gable = { P(frontLeft + 3, wallTop), P(43, variant == 3 ? 69 : 65), P(frontRight + 1, wallTop) };
            FillPolygon(texture, gable, wallLight);
            DrawPolygon(texture, gable, outline);

            DrawWallDetails(texture, variant, outline, timber, timberLight, wallSide, frontLeft, frontRight, wallTop);
            DrawDoorAndWindows(texture, variant, outline, roofLight, window, windowLight, door, frontLeft, frontRight);

            int chimneyLeft = variant switch { 1 => 31, 2 => 60, 3 => 53, 4 => 56, _ => 55 };
            int chimneyBottom = variant == 3 ? 65 : 61;
            Vector2Int[] chimney =
            {
                P(chimneyLeft, chimneyBottom),
                P(chimneyLeft + 7, chimneyBottom + 3),
                P(chimneyLeft + 7, chimneyBottom + 15),
                P(chimneyLeft, chimneyBottom + 12)
            };
            FillPolygon(texture, chimney, variant == 1 ? Rgb(91, 83, 75) : Rgb(111, 78, 62));
            DrawPolygon(texture, chimney, outline);

            Vector2Int[] roofFront = { P(roofLeft, roofEaveY), P(43, roofPeakY), P(roofRightFront, roofEaveY + 1), P(frontRight, roofBaseY), P(frontLeft + 3, roofBaseY) };
            FillPolygon(texture, roofFront, roof);
            DrawPolygon(texture, roofFront, outline);
            Vector2Int[] roofSide = { P(43, roofPeakY), P(84, variant == 3 ? 61 : 58), P(76, 44), P(roofRightFront, roofEaveY + 1) };
            FillPolygon(texture, roofSide, roofDark);
            DrawPolygon(texture, roofSide, outline);

            DrawRoofDetails(texture, variant, outline, roofLight, roofDark, roofLeft, roofEaveY, roofPeakY, roofBaseY, frontLeft, frontRight, roofRightFront);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 80f, 80f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static void DrawWallDetails(
            Texture2D texture,
            int variant,
            Color outline,
            Color timber,
            Color timberLight,
            Color wallSide,
            int frontLeft,
            int frontRight,
            int wallTop)
        {
            if (variant == 1)
            {
                for (int y = 27; y < wallTop - 2; y += 6)
                {
                    DrawLine(texture, P(frontLeft + 2, y), P(frontRight - 2, y), wallSide);
                }

                for (int x = frontLeft + 5; x < frontRight - 3; x += 10)
                {
                    DrawLine(texture, P(x, 23), P(x, wallTop - 2), wallSide);
                }

                return;
            }

            int timberTop = wallTop - 3;
            FillRect(texture, frontLeft + 1, 22, 4, wallTop - 21, timber);
            FillRect(texture, frontRight - 3, 22, 4, wallTop - 21, timber);
            FillRect(texture, frontLeft + 2, timberTop, frontRight - frontLeft - 3, 4, timber);
            DrawThickLine(texture, P(frontLeft + 4, 23), P(frontRight - 4, timberTop), timberLight, 1);

            if (variant != 2)
            {
                DrawThickLine(texture, P(frontRight - 4, 23), P(frontLeft + 5, timberTop), timber, 1);
            }

            if (variant == 3)
            {
                FillRect(texture, frontLeft + 14, 23, 3, wallTop - 22, timber);
                FillRect(texture, frontLeft + 25, 23, 3, wallTop - 22, timberLight);
            }

            if (variant == 4)
            {
                FillRect(texture, 28, 36, 24, 3, timberLight);
                FillRect(texture, 29, 22, 3, 15, timber);
                FillRect(texture, 49, 22, 3, 15, timber);
            }
        }

        private static void DrawDoorAndWindows(
            Texture2D texture,
            int variant,
            Color outline,
            Color roofLight,
            Color window,
            Color windowLight,
            Color door,
            int frontLeft,
            int frontRight)
        {
            int doorX = variant switch { 3 => 38, 4 => 33, _ => 35 };
            int doorW = variant == 1 ? 11 : 10;
            FillRect(texture, doorX, 22, doorW, 19, door);
            if (variant == 1)
            {
                FillEllipse(texture, doorX + doorW / 2, 40, doorW / 2, 4, door);
            }

            DrawRectOutline(texture, doorX, 22, doorW, 19, outline);
            SetPixelSafe(texture, doorX + doorW - 2, 31, roofLight);

            if (variant == 3)
            {
                DrawWindow(texture, 31, 36, 6, 8, outline, window, windowLight);
                DrawWindow(texture, 51, 36, 6, 8, outline, window, windowLight);
                DrawWindow(texture, 39, 51, 7, 7, outline, window, windowLight);
            }
            else if (variant == 4)
            {
                DrawWindow(texture, 52, 34, 8, 8, outline, window, windowLight);
                FillRect(texture, 64, 31, 5, 12, Rgb(45, 102, 75));
                DrawRectOutline(texture, 64, 31, 5, 12, outline);
            }
            else
            {
                DrawWindow(texture, frontRight - 11, 34, 8, 8, outline, window, windowLight);
            }

            if (variant != 3)
            {
                DrawWindow(texture, 64, 34, 7, 8, outline, Rgb(69, 108, 117), windowLight);
            }

            if (variant == 2)
            {
                FillRect(texture, frontLeft + 5, 34, 7, 7, Rgb(94, 125, 89));
                DrawRectOutline(texture, frontLeft + 5, 34, 7, 7, outline);
            }
        }

        private static void DrawWindow(Texture2D texture, int x, int y, int width, int height, Color outline, Color window, Color windowLight)
        {
            FillRect(texture, x, y, width, height, window);
            FillRect(texture, x + 1, y + height - 3, Mathf.Max(1, width - 2), 2, windowLight);
            DrawRectOutline(texture, x, y, width, height, outline);
            DrawLine(texture, P(x + width / 2, y), P(x + width / 2, y + height - 1), outline);
        }

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

        private static Sprite CreateStorageYardSprite(int variant)
        {
            Texture2D texture = CreateTexture(112, 88, $"Storage Yard 2.5D Sprite {variant + 1}");

            Color outline = Rgb(43, 32, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color dirtDark = variant == 1 ? Rgb(82, 70, 49) : Rgb(76, 64, 43);
            Color dirtLight = variant == 2 ? Rgb(139, 116, 77) : Rgb(122, 102, 66);
            Color plankDark = variant == 2 ? Rgb(91, 61, 38) : Rgb(83, 55, 35);
            Color plank = variant == 1 ? Rgb(133, 91, 53) : Rgb(120, 77, 45);
            Color plankLight = variant == 2 ? Rgb(185, 129, 72) : Rgb(166, 111, 60);
            Color rope = Rgb(177, 144, 82);
            Color cloth = variant == 1 ? Rgb(92, 116, 88) : variant == 2 ? Rgb(129, 104, 72) : Rgb(116, 84, 67);

            FillEllipse(texture, 56, 12, 41, 8, shadow);

            Vector2Int[] ground = { P(12, 19), P(49, 7), P(99, 20), P(62, 37) };
            FillPolygon(texture, ground, dirtDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(20, 20), P(50, 11), P(90, 21), P(61, 32) };
            FillPolygon(texture, groundTop, dirtLight);

            for (int i = 0; i < 4; i++)
            {
                int x = 26 + i * 13;
                DrawThickLine(texture, P(x, 17), P(x + 22, 25), plankDark, 2);
                DrawThickLine(texture, P(x + 1, 18), P(x + 21, 25), plank, 1);
                DrawLine(texture, P(x + 3, 21), P(x + 19, 26), plankLight);
            }

            DrawThickLine(texture, P(23, 28), P(23, 49), plankDark, 2);
            DrawThickLine(texture, P(88, 29), P(88, 49), plankDark, 2);
            DrawThickLine(texture, P(25, 44), P(88, 45), plankDark, 2);
            DrawThickLine(texture, P(25, 45), P(88, 46), plankLight, 1);

            Vector2Int[] awning = { P(33, 47), P(57, 62), P(80, 47), P(73, 40), P(57, 53), P(40, 39) };
            FillPolygon(texture, awning, cloth);
            DrawPolygon(texture, awning, outline);
            DrawLine(texture, P(42, 45), P(70, 45), rope);
            DrawLine(texture, P(49, 49), P(58, 55), rope);

            for (int i = 0; i < 3; i++)
            {
                int x = 20 + i * 10;
                int y = 22 + (i % 2) * 3;
                FillRect(texture, x, y, 10, 9, Rgb(108, 72, 42));
                FillRect(texture, x + 1, y + 1, 8, 7, Rgb(148, 101, 58));
                DrawRectOutline(texture, x, y, 10, 9, outline);
                DrawLine(texture, P(x + 1, y + 5), P(x + 8, y + 5), plankLight);
            }

            FillRect(texture, 75, 22, 12, 10, Rgb(103, 79, 48));
            FillRect(texture, 76, 23, 10, 8, Rgb(148, 112, 66));
            DrawRectOutline(texture, 75, 22, 12, 10, outline);
            DrawLine(texture, P(81, 23), P(81, 31), outline);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 96f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateStorageYardStockSprite(int level)
        {
            Texture2D texture = CreateTexture(84, 48, $"Storage Yard Stock {level}");
            Color outline = Rgb(39, 29, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            Color logDark = Rgb(80, 50, 30);
            Color log = Rgb(124, 78, 42);
            Color logLight = Rgb(182, 120, 60);
            Color cut = Rgb(222, 176, 100);
            Color rings = Rgb(109, 71, 43);
            Color crate = Rgb(136, 94, 54);
            Color crateLight = Rgb(183, 129, 72);

            FillEllipse(texture, 42, 8, 31, 6, shadow);

            int rows = Mathf.Clamp(level, 1, 6);
            for (int row = 0; row < rows; row++)
            {
                int logsInRow = Mathf.Min(5, 2 + row);
                int y = 11 + row * 5;
                int startX = 40 - logsInRow * 7;
                for (int i = 0; i < logsInRow; i++)
                {
                    int x = startX + i * 14 + (row % 2) * 3;
                    DrawStockLog(texture, x, y, 16, 5, logDark, log, logLight, cut, rings, outline);
                }
            }

            if (level >= 3)
            {
                FillRect(texture, 9, 12, 12, 11, crate);
                FillRect(texture, 10, 13, 10, 9, crateLight);
                DrawRectOutline(texture, 9, 12, 12, 11, outline);
                DrawLine(texture, P(10, 18), P(20, 18), outline);
            }

            if (level >= 5)
            {
                FillRect(texture, 62, 14, 13, 12, crate);
                FillRect(texture, 63, 15, 11, 10, crateLight);
                DrawRectOutline(texture, 62, 14, 13, 12, outline);
                DrawLine(texture, P(68, 15), P(68, 25), outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 76f, 37f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateStorageYardStoneStockSprite(int level)
        {
            Texture2D texture = CreateTexture(64, 44, $"Storage Yard Stone Stock {level}");
            Color outline = Rgb(38, 39, 38);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color stoneDark = Rgb(86, 92, 88);
            Color stone = Rgb(124, 132, 126);
            Color stoneLight = Rgb(176, 184, 174);
            Color moss = Rgb(85, 114, 73);

            FillEllipse(texture, 32, 8, 23, 5, shadow);

            int rows = Mathf.Clamp(level, 1, 6);
            for (int row = 0; row < rows; row++)
            {
                int stonesInRow = Mathf.Min(5, 2 + row);
                int y = 10 + row * 5;
                int startX = 32 - stonesInRow * 5;
                for (int i = 0; i < stonesInRow; i++)
                {
                    int x = startX + i * 10 + (row % 2) * 2;
                    int radiusX = 4 + ((i + row) % 2);
                    int radiusY = 3 + ((i + row + 1) % 2);
                    FillEllipse(texture, x, y, radiusX, radiusY, stoneDark);
                    FillEllipse(texture, x - 1, y + 1, Mathf.Max(2, radiusX - 1), Mathf.Max(2, radiusY - 1), stone);
                    SetPixelSafe(texture, x - 2, y + radiusY, stoneLight);
                    SetPixelSafe(texture, x - 1, y + radiusY, stoneLight);
                    SetPixelSafe(texture, x + radiusX - 1, y - 1, outline);
                }
            }

            if (level >= 4)
            {
                DrawLine(texture, P(15, 13), P(23, 14), moss);
                DrawLine(texture, P(41, 17), P(51, 18), moss);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 56f, 32f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
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
            DrawLine(texture, P(x, y), P(x + width - 1, y), color);
            DrawLine(texture, P(x, y + height - 1), P(x + width - 1, y + height - 1), color);
            DrawLine(texture, P(x, y), P(x, y + height - 1), color);
            DrawLine(texture, P(x + width - 1, y), P(x + width - 1, y + height - 1), color);
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

        private static void FillPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            int minY = points[0].y;
            int maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            List<int> nodes = new();
            for (int y = minY; y <= maxY; y++)
            {
                nodes.Clear();
                float scanY = y + 0.5f;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2Int a = points[i];
                    Vector2Int b = points[j];
                    bool crosses = (a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY);
                    if (!crosses)
                    {
                        continue;
                    }

                    float t = (scanY - a.y) / (b.y - a.y);
                    nodes.Add(Mathf.RoundToInt(a.x + t * (b.x - a.x)));
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    for (int x = nodes[i]; x <= nodes[i + 1]; x++)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                DrawLine(texture, points[i], points[(i + 1) % points.Length], color);
            }
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

        private static void DrawPickHead(Texture2D texture, int x, int y, Color outline, Color metal, Color metalLight)
        {
            DrawThickLine(texture, P(x - 6, y + 2), P(x + 6, y - 2), outline, 1);
            DrawLine(texture, P(x - 5, y + 2), P(x + 5, y - 2), metal);
            SetPixelSafe(texture, x - 6, y + 3, metalLight);
            SetPixelSafe(texture, x + 6, y - 3, metalLight);
            FillRect(texture, x - 1, y - 1, 3, 3, outline);
            FillRect(texture, x, y, 2, 2, metal);
        }

        private static void DrawCanopyRim(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = i * (Mathf.PI * 2f / 24f);
                int x = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * radiusX);
                int y = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * radiusY);
                SetPixelSafe(texture, x, y, color);
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

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static Color Shift(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private static int GetCacheKey(StrategyBuildTool tool, int variant)
        {
            return ((int)tool * 16) + variant;
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
    }
}
