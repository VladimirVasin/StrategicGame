using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public const int KilnWorkFrameCount = 12;

        public static Sprite GetKilnClayStockSprite(int stored)
        {
            return GetClayStockSprite(stored, 75776, "Kiln Clay Stock", StrategyVisualSequenceIds.KilnClay);
        }

        public static Sprite GetKilnCoalStockSprite(int stored)
        {
            return GetCoalStockSprite(stored, 76800, "Kiln Coal Stock", StrategyVisualSequenceIds.KilnCoal);
        }

        public static Sprite GetKilnPotteryStockSprite(int stored)
        {
            return GetPotteryStockSprite(stored, 77824, "Kiln Pottery Stock", StrategyVisualSequenceIds.KilnPottery);
        }

        public static Sprite GetStorageYardPotteryStockSprite(int stored)
        {
            return GetPotteryStockSprite(stored, 78848, "Storage Pottery Stock", StrategyVisualSequenceIds.StoragePottery);
        }

        public static Sprite GetKilnWorkSprite(int frame, int workerCount)
        {
            int normalizedFrame = Mathf.Abs(frame) % KilnWorkFrameCount;
            int workers = Mathf.Clamp(workerCount, 1, StrategyKiln.MaxWorkers);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.KilnWork + "/W" + workers, normalizedFrame, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 79872 + workers * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateKilnWorkSprite(normalizedFrame, workers);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateKilnSprite(int variant)
        {
            Texture2D texture = CreateTexture(108, 88, $"Kiln 2.5D Sprite {variant + 1}");
            Color outline = Rgb(49, 30, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color brick = variant == 1 ? Rgb(151, 76, 48) : Rgb(171, 86, 50);
            Color brickDark = Rgb(93, 52, 39);
            Color brickLight = Rgb(214, 121, 68);
            Color roof = variant == 2 ? Rgb(104, 83, 65) : Rgb(120, 68, 45);
            Color glow = Rgb(241, 149, 58);
            Color coal = Rgb(34, 36, 38);
            Color wood = Rgb(120, 75, 39);

            FillEllipse(texture, 54, 11, 41, 8, shadow);
            Vector2Int[] pad = { P(15, 18), P(49, 8), P(94, 21), P(67, 36), P(29, 34) };
            FillPolygon(texture, pad, Rgb(96, 75, 54));
            DrawPolygon(texture, pad, outline);
            FillRect(texture, 28, 22, 51, 34, outline);
            FillRect(texture, 30, 24, 47, 30, brick);
            FillEllipse(texture, 54, 34, 19, 19, outline);
            FillEllipse(texture, 54, 34, 16, 16, brickDark);
            FillEllipse(texture, 54, 31, 10, 9, glow);
            DrawBrickLines(texture, brickDark, brickLight);
            Vector2Int[] roofShape = { P(23, 54), P(54, 74), P(87, 55), P(76, 48), P(54, 62), P(32, 48) };
            FillPolygon(texture, roofShape, roof);
            DrawPolygon(texture, roofShape, outline);
            FillRect(texture, 70, 53, 10, 21, outline);
            FillRect(texture, 72, 55, 6, 18, brickDark);
            DrawPot(texture, 20, 23, outline, brickDark, brickLight);
            DrawPot(texture, 83, 23, outline, brick, brickLight);
            FillEllipse(texture, 83, 15, 8, 4, outline);
            FillEllipse(texture, 83, 15, 7, 3, coal);
            DrawThickLine(texture, P(16, 35), P(33, 42), outline, 3);
            DrawThickLine(texture, P(17, 35), P(32, 42), wood, 1);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 92f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite GetCoalStockSprite(int stored, int baseKey, string name, string sequenceId)
        {
            if (stored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stored + 1) / 2, 1, 6);
            if (TryGetBakedLayer(sequenceId, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = baseKey + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateKilnCoalStockSprite(level, name);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite GetPotteryStockSprite(int stored, int baseKey, string name, string sequenceId)
        {
            if (stored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stored + 1) / 2, 1, 6);
            if (TryGetBakedLayer(sequenceId, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = baseKey + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreatePotteryStockSprite(level, name);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateKilnCoalStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(48, 32, $"{name} {level}");
            Color outline = Rgb(22, 24, 27);
            Color coal = Rgb(47, 52, 56);
            Color shine = Rgb(102, 116, 124);
            FillEllipse(texture, 24, 7, 17, 4, new Color(0f, 0f, 0f, 0.18f));
            for (int i = 0; i < Mathf.Clamp(level + 2, 2, 8); i++)
            {
                int x = 10 + (i % 4) * 7 + (i / 4) * 3;
                int y = 10 + (i / 4) * 5 + i % 2;
                FillEllipse(texture, x, y, 5, 3, outline);
                FillEllipse(texture, x, y, 4, 2, coal);
                SetPixelSafe(texture, x + 1, y + 1, shine);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 40f, 22f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreatePotteryStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(54, 36, $"{name} {level}");
            Color outline = Rgb(57, 37, 28);
            Color clay = Rgb(179, 88, 50);
            Color light = Rgb(224, 138, 78);
            FillEllipse(texture, 27, 7, 19, 4, new Color(0f, 0f, 0f, 0.18f));
            for (int i = 0; i < Mathf.Clamp(level + 1, 2, 7); i++)
            {
                DrawPot(texture, 10 + (i % 4) * 9 + (i / 4) * 4, 10 + (i / 4) * 8, outline, clay, light);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 46f, 26f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateKilnWorkSprite(int frame, int workerCount)
        {
            Texture2D texture = CreateTexture(68, 42, $"Kiln Work Frame {frame}");
            Color outline = Rgb(45, 28, 20);
            Color flame = frame % 2 == 0 ? Rgb(242, 149, 54) : Rgb(255, 190, 75);
            Color ember = Rgb(202, 76, 42);
            FillEllipse(texture, 34, 7, 24, 5, new Color(0f, 0f, 0f, 0.16f));
            FillEllipse(texture, 34, 19, 18, 10, outline);
            FillEllipse(texture, 34, 18, 14, 8, Rgb(94, 50, 36));
            FillEllipse(texture, 31 + frame % 6, 18, 7, 8, flame);
            FillEllipse(texture, 36, 17 + frame % 3, 4, 5, ember);
            for (int i = 0; i < 10; i++)
            {
                int x = 21 + ((frame * 5 + i * 7) % 27);
                int y = 24 + ((frame * 3 + i * 5) % 11);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? flame : ember);
            }

            DrawPot(texture, 48, 12, outline, Rgb(179, 88, 50), Rgb(226, 135, 72));
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 60f, 32f), new Vector2(0.5f, 0.20f), PixelsPerUnit);
        }

        private static void DrawBrickLines(Texture2D texture, Color dark, Color light)
        {
            for (int y = 28; y <= 50; y += 6)
            {
                DrawLine(texture, P(31, y), P(76, y), dark);
            }

            for (int x = 34; x <= 72; x += 10)
            {
                DrawLine(texture, P(x, 24), P(x, 53), dark);
                SetPixelSafe(texture, x + 1, 42, light);
            }
        }

        private static void DrawPot(Texture2D texture, int x, int y, Color outline, Color clay, Color light)
        {
            FillEllipse(texture, x, y + 7, 6, 4, outline);
            FillEllipse(texture, x, y + 7, 5, 3, clay);
            FillRect(texture, x - 4, y + 4, 8, 6, outline);
            FillRect(texture, x - 3, y + 5, 6, 4, clay);
            FillEllipse(texture, x, y + 11, 4, 2, outline);
            FillEllipse(texture, x, y + 11, 3, 1, light);
            SetPixelSafe(texture, x + 2, y + 8, light);
        }
    }
}
