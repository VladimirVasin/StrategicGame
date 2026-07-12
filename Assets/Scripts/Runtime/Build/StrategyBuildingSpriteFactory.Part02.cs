using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {

        private static Sprite CreateFisherHutStockSprite(int level)
        {
            Texture2D texture = CreateTexture(60, 38, $"Fisher Hut Stock {level}");
            Color outline = Rgb(29, 55, 66);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color crate = Rgb(108, 72, 44);
            Color crateLight = Rgb(153, 103, 60);
            Color fishDark = Rgb(57, 111, 134);
            Color fish = Rgb(86, 161, 182);
            Color fin = Rgb(224, 151, 76);

            FillEllipse(texture, 30, 7, 20, 5, shadow);
            FillRect(texture, 9, 10, 42, 15, crate);
            FillRect(texture, 10, 11, 40, 13, crateLight);
            DrawRectOutline(texture, 9, 10, 42, 15, outline);
            DrawLine(texture, P(12, 18), P(48, 18), Rgb(94, 60, 37));

            int fishCount = Mathf.Clamp(level + 1, 2, 6);
            for (int i = 0; i < fishCount; i++)
            {
                int x = 17 + (i % 3) * 12;
                int y = 21 + (i / 3) * 6 + (i % 2);
                DrawFishIcon(texture, x, y, outline, i % 2 == 0 ? fish : fishDark, fin);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 52f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static void DrawFishingPole(Texture2D texture, int x, int y, Color outline, Color wood, Color light)
        {
            DrawThickLine(texture, P(x, y), P(x + 23, y + 35), outline, 1);
            DrawLine(texture, P(x + 1, y), P(x + 23, y + 35), wood);
            DrawLine(texture, P(x + 7, y + 10), P(x + 22, y + 34), light);
            DrawLine(texture, P(x + 23, y + 34), P(x + 34, y + 17), new Color(0.20f, 0.24f, 0.22f, 0.75f));
            FillEllipse(texture, x + 34, y + 16, 2, 3, Rgb(198, 50, 43));
            SetPixelSafe(texture, x + 34, y + 18, Rgb(240, 232, 198));
        }

        private static void DrawFishNet(Texture2D texture, int x, int y, Color outline, Color rope, Color water)
        {
            DrawLine(texture, P(x, y), P(x - 14, y + 18), outline);
            DrawLine(texture, P(x + 1, y), P(x - 13, y + 18), rope);
            DrawCanopyRim(texture, x - 18, y + 14, 8, 6, outline);
            DrawCanopyRim(texture, x - 18, y + 14, 7, 5, rope);
            DrawLine(texture, P(x - 25, y + 14), P(x - 11, y + 14), water);
            DrawLine(texture, P(x - 18, y + 8), P(x - 18, y + 20), water);
        }

        private static void DrawFishIcon(Texture2D texture, int x, int y, Color outline, Color body, Color fin)
        {
            FillEllipse(texture, x, y, 6, 3, outline);
            FillEllipse(texture, x, y, 5, 2, body);
            Vector2Int[] tailOutline = { P(x - 5, y), P(x - 10, y + 4), P(x - 10, y - 4) };
            FillPolygon(texture, tailOutline, outline);
            Vector2Int[] tail = { P(x - 5, y), P(x - 9, y + 3), P(x - 9, y - 3) };
            FillPolygon(texture, tail, fin);
            SetPixelSafe(texture, x + 4, y + 1, outline);
        }

        private static void DrawCampBow(Texture2D texture, int x, int y, Color outline, Color wood, Color light)
        {
            DrawThickLine(texture, P(x, y), P(x + 4, y + 15), outline, 1);
            DrawThickLine(texture, P(x + 4, y + 15), P(x + 1, y + 30), outline, 1);
            DrawLine(texture, P(x, y), P(x + 4, y + 15), wood);
            DrawLine(texture, P(x + 4, y + 15), P(x + 1, y + 30), wood);
            DrawLine(texture, P(x + 1, y + 1), P(x + 2, y + 29), light);
        }

        private static void DrawArrowBundle(Texture2D texture, int x, int y, Color outline, Color shaft, Color feather)
        {
            for (int i = 0; i < 4; i++)
            {
                int offset = i * 3;
                DrawLine(texture, P(x + offset, y), P(x - 7 + offset, y + 20), outline);
                DrawLine(texture, P(x + offset, y + 1), P(x - 7 + offset, y + 19), shaft);
                FillRect(texture, x + offset - 2, y + 17, 4, 2, feather);
            }
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

            DrawBuildingPolish(texture, StrategyBuildTool.StorageYard, variant);
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

        private static Sprite CreateGranarySprite(int variant)
        {
            Texture2D texture = CreateTexture(112, 96, $"Granary 2.5D Sprite {variant + 1}");

            Color outline = Rgb(42, 29, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color woodDark = variant == 1 ? Rgb(86, 53, 34) : Rgb(79, 48, 31);
            Color wood = variant == 2 ? Rgb(151, 96, 52) : Rgb(132, 82, 45);
            Color woodLight = variant == 1 ? Rgb(193, 128, 70) : Rgb(178, 114, 61);
            Color roofDark = variant == 2 ? Rgb(103, 78, 44) : Rgb(91, 68, 39);
            Color thatch = variant == 1 ? Rgb(191, 156, 74) : Rgb(177, 137, 61);
            Color thatchLight = Rgb(224, 190, 99);
            Color stone = Rgb(96, 89, 75);
            Color grain = Rgb(218, 174, 72);

            FillEllipse(texture, 56, 12, 42, 8, shadow);

            Vector2Int[] foundation = { P(16, 18), P(52, 7), P(96, 20), P(62, 35) };
            FillPolygon(texture, foundation, Rgb(93, 75, 52));
            DrawPolygon(texture, foundation, outline);
            Vector2Int[] foundationTop = { P(24, 19), P(53, 11), P(88, 21), P(61, 31) };
            FillPolygon(texture, foundationTop, Rgb(129, 102, 66));

            FillRect(texture, 28, 24, 54, 37, outline);
            FillRect(texture, 31, 26, 48, 32, wood);
            for (int x = 34; x <= 75; x += 8)
            {
                DrawLine(texture, P(x, 27), P(x, 57), woodDark);
                DrawLine(texture, P(x + 1, 27), P(x + 1, 56), woodLight);
            }

            FillRect(texture, 43, 25, 20, 30, woodDark);
            FillRect(texture, 46, 28, 15, 26, Rgb(105, 64, 36));
            DrawRectOutline(texture, 43, 25, 20, 30, outline);
            DrawLine(texture, P(47, 41), P(61, 41), woodLight);
            DrawLine(texture, P(52, 28), P(52, 54), outline);

            FillRect(texture, 67, 35, 9, 10, outline);
            FillRect(texture, 68, 36, 7, 8, Rgb(226, 190, 112));
            DrawLine(texture, P(71, 36), P(71, 44), outline);
            DrawLine(texture, P(68, 40), P(75, 40), outline);

            Vector2Int[] roof = { P(20, 60), P(54, 81), P(91, 60), P(82, 51), P(54, 69), P(29, 50) };
            FillPolygon(texture, roof, outline);
            Vector2Int[] roofInner = { P(25, 60), P(54, 77), P(86, 60), P(79, 54), P(54, 69), P(32, 53) };
            FillPolygon(texture, roofInner, thatch);
            DrawLine(texture, P(29, 60), P(54, 73), thatchLight);
            DrawLine(texture, P(55, 72), P(82, 60), thatchLight);
            for (int i = 0; i < 5; i++)
            {
                int x = 31 + i * 10;
                DrawLine(texture, P(x, 58), P(x + 13, 66), roofDark);
            }

            DrawThickLine(texture, P(27, 19), P(27, 62), outline, 2);
            DrawThickLine(texture, P(80, 20), P(80, 61), outline, 2);
            DrawThickLine(texture, P(29, 20), P(29, 61), woodLight, 1);
            DrawThickLine(texture, P(78, 21), P(78, 60), woodLight, 1);

            FillRect(texture, 20, 18, 8, 12, stone);
            FillRect(texture, 78, 19, 8, 12, stone);
            DrawRectOutline(texture, 20, 18, 8, 12, outline);
            DrawRectOutline(texture, 78, 19, 8, 12, outline);

            for (int i = 0; i < 3; i++)
            {
                int x = 21 + i * 8;
                int y = 18 + (i % 2) * 3;
                FillEllipse(texture, x, y, 5, 3, Rgb(117, 76, 43));
                FillEllipse(texture, x, y + 1, 4, 2, grain);
                DrawLine(texture, P(x - 3, y + 2), P(x + 3, y + 2), outline);
            }

            DrawBuildingPolish(texture, StrategyBuildTool.Granary, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 96f, 84f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateGranaryGameStockSprite(int level)
        {
            Texture2D texture = CreateTexture(72, 46, $"Granary Game Stock {level}");
            Color outline = Rgb(42, 29, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color hide = Rgb(128, 77, 45);
            Color meat = Rgb(145, 58, 47);
            Color meatLight = Rgb(204, 106, 76);
            Color bone = Rgb(224, 205, 161);
            Color basket = Rgb(133, 91, 48);
            Color basketLight = Rgb(186, 132, 70);

            FillEllipse(texture, 36, 8, 26, 5, shadow);
            int count = Mathf.Clamp(level + 1, 2, 7);
            for (int i = 0; i < count; i++)
            {
                int x = 15 + (i % 4) * 11 + (i / 4) * 4;
                int y = 13 + (i / 4) * 8;
                FillEllipse(texture, x, y, 7, 4, outline);
                FillEllipse(texture, x, y + 1, 6, 3, hide);
                FillEllipse(texture, x + 1, y + 1, 3, 2, meat);
                SetPixelSafe(texture, x + 2, y + 3, meatLight);
                DrawLine(texture, P(x - 5, y + 1), P(x + 5, y + 1), bone);
            }

            if (level >= 3)
            {
                FillRect(texture, 44, 13, 15, 12, basket);
                FillRect(texture, 46, 15, 11, 8, basketLight);
                DrawRectOutline(texture, 44, 13, 15, 12, outline);
                DrawLine(texture, P(45, 19), P(58, 19), outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 64f, 34f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateGranaryFishStockSprite(int level)
        {
            Texture2D texture = CreateTexture(72, 46, $"Granary Fish Stock {level}");
            Color outline = Rgb(36, 46, 48);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color barrel = Rgb(116, 74, 43);
            Color barrelLight = Rgb(173, 116, 60);
            Color hoop = Rgb(72, 79, 76);
            Color fish = Rgb(83, 151, 169);
            Color fishLight = Rgb(168, 217, 214);
            Color fin = Rgb(222, 153, 76);

            FillEllipse(texture, 36, 8, 25, 5, shadow);
            FillEllipse(texture, 33, 17, 18, 8, outline);
            FillEllipse(texture, 33, 18, 16, 7, barrel);
            FillRect(texture, 18, 16, 30, 11, barrel);
            FillRect(texture, 20, 18, 26, 7, barrelLight);
            DrawRectOutline(texture, 18, 16, 30, 11, outline);
            DrawLine(texture, P(19, 20), P(47, 20), hoop);
            DrawLine(texture, P(24, 16), P(24, 27), hoop);
            DrawLine(texture, P(42, 16), P(42, 27), hoop);

            int count = Mathf.Clamp(level + 1, 2, 7);
            for (int i = 0; i < count; i++)
            {
                int x = 19 + (i % 4) * 10 + (i / 4) * 5;
                int y = 27 + (i / 4) * 5;
                DrawFishIcon(texture, x, y, outline, i % 2 == 0 ? fish : fishLight, fin);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 64f, 34f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateBridgeSprite(Vector2Int footprint)
        {
            bool horizontal = footprint.x >= footprint.y;
            int lengthCells = Mathf.Max(1, horizontal ? footprint.x : footprint.y);
            int width = horizontal ? Mathf.Max(72, lengthCells * 24 + 20) : 62;
            int height = horizontal ? 56 : Mathf.Max(72, lengthCells * 24 + 20);
            Texture2D texture = CreateTexture(width, height, $"Bridge Sprite {footprint.x}x{footprint.y}");

            Color outline = Rgb(49, 34, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color woodDark = Rgb(86, 52, 31);
            Color wood = Rgb(142, 88, 45);
            Color woodLight = Rgb(205, 139, 70);
            Color rope = Rgb(198, 159, 91);
            Color peg = Rgb(66, 45, 31);

            if (horizontal)
            {
                int centerY = height / 2;
                FillEllipse(texture, width / 2, centerY - 11, width / 2 - 8, 8, shadow);

                Vector2Int[] deck =
                {
                    P(9, centerY - 9),
                    P(width - 14, centerY - 9),
                    P(width - 6, centerY + 5),
                    P(15, centerY + 8)
                };
                FillPolygon(texture, deck, outline);
                FillPolygon(
                    texture,
                    new[] { P(13, centerY - 7), P(width - 17, centerY - 7), P(width - 11, centerY + 3), P(18, centerY + 5) },
                    wood);

                DrawThickLine(texture, P(10, centerY - 11), P(width - 12, centerY - 11), outline, 1);
                DrawThickLine(texture, P(12, centerY + 9), P(width - 8, centerY + 6), outline, 1);
                DrawLine(texture, P(12, centerY - 10), P(width - 14, centerY - 10), woodLight);
                DrawLine(texture, P(17, centerY + 5), P(width - 12, centerY + 3), woodDark);

                int plankCount = Mathf.Max(4, lengthCells * 2);
                for (int i = 0; i <= plankCount; i++)
                {
                    int x = Mathf.RoundToInt(Mathf.Lerp(17, width - 17, i / (float)plankCount));
                    DrawLine(texture, P(x, centerY - 7), P(x + 3, centerY + 5), outline);
                    DrawLine(texture, P(x + 1, centerY - 6), P(x + 3, centerY + 4), woodLight);
                }

                for (int i = 0; i <= lengthCells; i++)
                {
                    int x = Mathf.RoundToInt(Mathf.Lerp(13, width - 13, i / (float)Mathf.Max(1, lengthCells)));
                    FillRect(texture, x - 2, centerY - 18, 5, 14, outline);
                    FillRect(texture, x - 1, centerY - 17, 3, 12, peg);
                    FillRect(texture, x - 2, centerY + 4, 5, 12, outline);
                    FillRect(texture, x - 1, centerY + 5, 3, 10, peg);
                }

                DrawLine(texture, P(14, centerY - 15), P(width - 14, centerY - 15), rope);
                DrawLine(texture, P(15, centerY + 13), P(width - 10, centerY + 10), rope);
            }
            else
            {
                int centerX = width / 2;
                FillEllipse(texture, centerX, height / 2, 15, height / 2 - 8, shadow);

                Vector2Int[] deck =
                {
                    P(centerX - 12, 9),
                    P(centerX + 7, 11),
                    P(centerX + 13, height - 15),
                    P(centerX - 8, height - 7)
                };
                FillPolygon(texture, deck, outline);
                FillPolygon(
                    texture,
                    new[] { P(centerX - 9, 13), P(centerX + 5, 14), P(centerX + 9, height - 17), P(centerX - 6, height - 11) },
                    wood);

                DrawThickLine(texture, P(centerX - 14, 10), P(centerX - 10, height - 8), outline, 1);
                DrawThickLine(texture, P(centerX + 9, 12), P(centerX + 15, height - 16), outline, 1);
                DrawLine(texture, P(centerX - 11, 13), P(centerX - 7, height - 12), woodLight);
                DrawLine(texture, P(centerX + 6, 15), P(centerX + 10, height - 18), woodDark);

                int plankCount = Mathf.Max(4, lengthCells * 2);
                for (int i = 0; i <= plankCount; i++)
                {
                    int y = Mathf.RoundToInt(Mathf.Lerp(17, height - 18, i / (float)plankCount));
                    DrawLine(texture, P(centerX - 8, y), P(centerX + 8, y - 2), outline);
                    DrawLine(texture, P(centerX - 6, y + 1), P(centerX + 6, y - 1), woodLight);
                }

                for (int i = 0; i <= lengthCells; i++)
                {
                    int y = Mathf.RoundToInt(Mathf.Lerp(15, height - 15, i / (float)Mathf.Max(1, lengthCells)));
                    FillRect(texture, centerX - 18, y - 2, 12, 5, outline);
                    FillRect(texture, centerX - 17, y - 1, 10, 3, peg);
                    FillRect(texture, centerX + 8, y - 2, 12, 5, outline);
                    FillRect(texture, centerX + 9, y - 1, 10, 3, peg);
                }

                DrawLine(texture, P(centerX - 17, 15), P(centerX - 13, height - 14), rope);
                DrawLine(texture, P(centerX + 17, 16), P(centerX + 20, height - 18), rope);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
