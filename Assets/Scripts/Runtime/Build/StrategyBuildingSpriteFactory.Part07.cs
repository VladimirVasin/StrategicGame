using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public const int SawmillWorkFrameCount = 12;

        public static Sprite GetSawmillLogStockSprite(int logsStored)
        {
            return GetPlankResourceStockSprite(logsStored, 69632, "Sawmill Log Stock", false);
        }

        public static Sprite GetSawmillPlankStockSprite(int planksStored)
        {
            return GetPlankResourceStockSprite(planksStored, 70656, "Sawmill Plank Stock", true);
        }

        public static Sprite GetStorageYardPlankStockSprite(int planksStored)
        {
            return GetPlankResourceStockSprite(planksStored, 71680, "Storage Plank Stock", true);
        }

        public static Sprite GetSawmillWorkSprite(int frame, int workerCount)
        {
            int normalizedFrame = Mathf.Abs(frame) % SawmillWorkFrameCount;
            int workers = Mathf.Clamp(workerCount, 1, StrategySawmill.MaxWorkers);
            int cacheKey = 72704 + workers * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSawmillWorkSprite(normalizedFrame, workers);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite GetPlankResourceStockSprite(int stored, int baseKey, string name, bool planks)
        {
            if (stored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stored + 2) / 3, 1, 6);
            int cacheKey = baseKey + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateBoardStockSprite(level, name, planks);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSawmillSprite(int variant)
        {
            Texture2D texture = CreateTexture(116, 92, $"Sawmill 2.5D Sprite {variant + 1}");
            Color outline = Rgb(47, 31, 21);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color earth = Rgb(89, 73, 49);
            Color timber = variant == 1 ? Rgb(101, 65, 35) : Rgb(119, 75, 38);
            Color timberDark = Rgb(61, 39, 26);
            Color timberLight = Rgb(172, 112, 58);
            Color roof = variant == 2 ? Rgb(116, 91, 64) : Rgb(131, 72, 43);
            Color roofLight = variant == 2 ? Rgb(166, 130, 84) : Rgb(184, 104, 58);
            Color blade = Rgb(194, 202, 190);

            FillEllipse(texture, 58, 10, 42, 8, shadow);
            Vector2Int[] baseShape = { P(13, 20), P(51, 8), P(101, 22), P(66, 40) };
            FillPolygon(texture, baseShape, earth);
            DrawPolygon(texture, baseShape, outline);
            DrawSawmillFrame(texture, outline, timberDark, timber, timberLight);
            DrawSawmillRoof(texture, outline, roof, roofLight);
            DrawSawmillBench(texture, outline, timberDark, timber, timberLight, blade);
            DrawBoardStack(texture, 75, 19, 5, outline, timber, timberLight);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 100f, 78f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateBoardStockSprite(int level, string name, bool planks)
        {
            Texture2D texture = CreateTexture(62, 40, $"{name} {level}");
            Color outline = Rgb(49, 32, 21);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color wood = planks ? Rgb(171, 111, 57) : Rgb(119, 73, 36);
            Color woodLight = planks ? Rgb(215, 153, 83) : Rgb(169, 103, 50);

            FillEllipse(texture, 31, 7, 21, 5, shadow);
            DrawBoardStack(texture, 9, 10, Mathf.Clamp(level + 1, 2, 7), outline, wood, woodLight);
            if (!planks)
            {
                for (int i = 0; i < Mathf.Min(level, 4); i++)
                {
                    FillEllipse(texture, 15 + i * 8, 14 + i, 5, 3, outline);
                    FillEllipse(texture, 15 + i * 8, 14 + i, 4, 2, wood);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 54f, 30f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateSawmillWorkSprite(int frame, int workerCount)
        {
            Texture2D texture = CreateTexture(86, 44, $"Sawmill Work Frame {frame}");
            Color clearShadow = new Color(0f, 0f, 0f, 0.16f);
            Color outline = Rgb(48, 31, 21);
            Color wood = Rgb(151, 90, 41);
            Color light = Rgb(218, 150, 70);
            Color saw = Rgb(205, 213, 202);
            Color dust = new Color(0.93f, 0.73f, 0.38f, 0.72f);

            FillEllipse(texture, 43, 7, 31, 5, clearShadow);
            DrawThickLine(texture, P(13, 23), P(73, 23), outline, 5);
            DrawThickLine(texture, P(14, 24), P(72, 24), wood, 3);
            int sawX = 22 + Mathf.RoundToInt(Mathf.PingPong(frame, SawmillWorkFrameCount / 2) * 6f);
            DrawThickLine(texture, P(sawX, 31), P(sawX + 34, 17), outline, 3);
            DrawThickLine(texture, P(sawX, 31), P(sawX + 34, 17), saw, 2);
            DrawSawdust(texture, sawX + 27, 19, frame, dust);
            DrawBoardStack(texture, 53, 9, 3 + frame % 3, outline, wood, light);

            if (workerCount > 1)
            {
                FillEllipse(texture, 18, 28, 4, 5, new Color(0.62f, 0.38f, 0.23f, 0.80f));
                FillEllipse(texture, 66, 27, 4, 5, new Color(0.62f, 0.38f, 0.23f, 0.80f));
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 78f, 34f), new Vector2(0.5f, 0.20f), PixelsPerUnit);
        }

        private static void DrawSawmillFrame(Texture2D texture, Color outline, Color dark, Color wood, Color light)
        {
            DrawThickLine(texture, P(24, 20), P(24, 57), outline, 5);
            DrawThickLine(texture, P(88, 21), P(88, 56), outline, 5);
            DrawThickLine(texture, P(25, 21), P(25, 56), dark, 3);
            DrawThickLine(texture, P(88, 22), P(88, 55), dark, 3);
            DrawThickLine(texture, P(18, 52), P(56, 72), outline, 4);
            DrawThickLine(texture, P(56, 72), P(95, 53), outline, 4);
            DrawThickLine(texture, P(20, 52), P(56, 69), wood, 2);
            DrawThickLine(texture, P(56, 69), P(93, 53), wood, 2);
            DrawLine(texture, P(30, 38), P(84, 39), light);
        }

        private static void DrawSawmillRoof(Texture2D texture, Color outline, Color roof, Color light)
        {
            Vector2Int[] roofShape = { P(15, 52), P(56, 77), P(101, 54), P(90, 47), P(56, 65), P(25, 47) };
            FillPolygon(texture, roofShape, roof);
            DrawPolygon(texture, roofShape, outline);
            DrawLine(texture, P(24, 54), P(56, 71), light);
            DrawLine(texture, P(56, 71), P(92, 54), light);
        }

        private static void DrawSawmillBench(Texture2D texture, Color outline, Color dark, Color wood, Color light, Color blade)
        {
            DrawThickLine(texture, P(27, 27), P(80, 28), outline, 5);
            DrawThickLine(texture, P(28, 28), P(79, 29), wood, 3);
            DrawThickLine(texture, P(39, 40), P(70, 22), outline, 3);
            DrawThickLine(texture, P(39, 40), P(70, 22), blade, 2);
            DrawLine(texture, P(29, 31), P(76, 32), light);
            DrawThickLine(texture, P(35, 15), P(35, 31), dark, 3);
            DrawThickLine(texture, P(72, 17), P(72, 31), dark, 3);
        }

        private static void DrawBoardStack(Texture2D texture, int x, int y, int count, Color outline, Color wood, Color light)
        {
            for (int i = 0; i < count; i++)
            {
                int ox = x + (i % 2) * 3;
                int oy = y + i * 3;
                FillRect(texture, ox, oy, 28, 4, outline);
                FillRect(texture, ox + 1, oy + 1, 26, 2, i % 2 == 0 ? wood : light);
            }
        }

        private static void DrawSawdust(Texture2D texture, int x, int y, int frame, Color dust)
        {
            for (int i = 0; i < 8; i++)
            {
                int px = x + ((frame * 3 + i * 5) % 13) - 6;
                int py = y + ((frame + i * 7) % 9) - 3;
                SetPixelSafe(texture, px, py, dust);
            }
        }
    }
}
