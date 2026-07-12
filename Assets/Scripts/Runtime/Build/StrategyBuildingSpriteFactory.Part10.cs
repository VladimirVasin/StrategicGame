using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public const int ForgeWorkFrameCount = 12;

        public static Sprite GetForgeIronStockSprite(int stored)
        {
            return GetForgeOreStockSprite(stored, 81920, "Forge Iron Stock", true, StrategyVisualSequenceIds.ForgeIron);
        }

        public static Sprite GetForgeCoalStockSprite(int stored)
        {
            return GetForgeOreStockSprite(stored, 82944, "Forge Coal Stock", false, StrategyVisualSequenceIds.ForgeCoal);
        }

        public static Sprite GetForgeLogStockSprite(int stored)
        {
            return GetForgeLogStockSprite(stored, 83968, "Forge Log Stock", StrategyVisualSequenceIds.ForgeLogs);
        }

        public static Sprite GetForgeToolsStockSprite(int stored)
        {
            return GetToolsStockSprite(stored, 84992, "Forge Tools Stock", StrategyVisualSequenceIds.ForgeTools);
        }

        public static Sprite GetStorageYardToolsStockSprite(int stored)
        {
            return GetToolsStockSprite(stored, 86016, "Storage Tools Stock", StrategyVisualSequenceIds.StorageTools);
        }

        public static Sprite GetForgeWorkSprite(int frame)
        {
            int normalizedFrame = Mathf.Abs(frame) % ForgeWorkFrameCount;
            if (TryGetBakedLayer(StrategyVisualSequenceIds.ForgeWork, normalizedFrame, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 87040 + normalizedFrame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateForgeWorkSprite(normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateForgeSprite(int variant)
        {
            Texture2D texture = CreateTexture(108, 88, $"Forge 2.5D Sprite {variant + 1}");
            Color outline = Rgb(46, 30, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color stone = variant == 1 ? Rgb(93, 88, 81) : Rgb(107, 89, 72);
            Color stoneLight = variant == 1 ? Rgb(150, 148, 136) : Rgb(157, 124, 89);
            Color wood = Rgb(104, 63, 35);
            Color woodLight = Rgb(162, 99, 49);
            Color roof = variant == 2 ? Rgb(91, 72, 56) : Rgb(117, 64, 42);
            Color roofLight = variant == 2 ? Rgb(148, 117, 78) : Rgb(178, 93, 55);
            Color coal = Rgb(34, 37, 39);
            Color metal = Rgb(92, 102, 102);
            Color glow = Rgb(245, 135, 45);

            FillEllipse(texture, 54, 11, 42, 8, shadow);
            Vector2Int[] pad = { P(15, 18), P(50, 7), P(94, 20), P(68, 35), P(28, 34) };
            FillPolygon(texture, pad, Rgb(88, 70, 52));
            DrawPolygon(texture, pad, outline);
            Vector2Int[] floor = { P(22, 20), P(51, 12), P(84, 21), P(65, 31), P(32, 30) };
            FillPolygon(texture, floor, stoneLight);

            FillRect(texture, 26, 21, 9, 40, outline);
            FillRect(texture, 28, 23, 5, 36, wood);
            FillRect(texture, 76, 21, 9, 39, outline);
            FillRect(texture, 78, 23, 5, 35, woodLight);
            Vector2Int[] roofShape = { P(18, 58), P(52, 76), P(91, 58), P(80, 50), P(53, 63), P(29, 50) };
            FillPolygon(texture, roofShape, roof);
            DrawPolygon(texture, roofShape, outline);
            DrawThickLine(texture, P(25, 58), P(84, 58), roofLight, 1);

            FillRect(texture, 36, 21, 32, 25, outline);
            FillRect(texture, 38, 23, 28, 21, stone);
            FillEllipse(texture, 52, 33, 15, 11, outline);
            FillEllipse(texture, 52, 32, 12, 8, Rgb(76, 43, 31));
            FillEllipse(texture, 52, 31, 7, 5, glow);
            FillRect(texture, 62, 42, 10, 26, outline);
            FillRect(texture, 64, 44, 6, 23, Rgb(79, 57, 48));

            DrawAnvil(texture, 24, 23, outline, metal, Rgb(174, 178, 165));
            DrawForgeCoalPile(texture, 72, 24, outline, coal, Rgb(89, 98, 102));
            DrawSmallLogStack(texture, 14, 19, outline, wood, woodLight);
            DrawToolPile(texture, 84, 23, outline, metal, Rgb(199, 199, 184));

            DrawBuildingPolish(texture, StrategyBuildTool.Forge, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 92f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite GetForgeOreStockSprite(int stored, int baseKey, string name, bool iron, string sequenceId)
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
                sprite = CreateForgeOreStockSprite(level, name, iron);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite GetForgeLogStockSprite(int stored, int baseKey, string name, string sequenceId)
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
                sprite = CreateForgeLogStockSprite(level, name);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite GetToolsStockSprite(int stored, int baseKey, string name, string sequenceId)
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
                sprite = CreateToolsStockSprite(level, name);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateForgeOreStockSprite(int level, string name, bool iron)
        {
            Texture2D texture = CreateTexture(48, 32, $"{name} {level}");
            Color outline = iron ? Rgb(47, 39, 35) : Rgb(22, 24, 27);
            Color ore = iron ? Rgb(105, 88, 76) : Rgb(48, 53, 56);
            Color light = iron ? Rgb(203, 110, 52) : Rgb(116, 128, 132);
            FillEllipse(texture, 24, 7, 17, 4, new Color(0f, 0f, 0f, 0.18f));
            for (int i = 0; i < Mathf.Clamp(level + 2, 2, 8); i++)
            {
                int x = 10 + (i % 4) * 7 + (i / 4) * 3;
                int y = 10 + (i / 4) * 5 + i % 2;
                FillEllipse(texture, x, y, 5, 3, outline);
                FillEllipse(texture, x, y, 4, 2, ore);
                SetPixelSafe(texture, x + 1, y + 1, light);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 40f, 22f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateForgeLogStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(52, 34, $"{name} {level}");
            Color outline = Rgb(77, 49, 29);
            Color bark = Rgb(118, 73, 38);
            Color cut = Rgb(203, 151, 82);
            for (int i = 0; i < Mathf.Clamp(level + 1, 2, 7); i++)
            {
                int x = 8 + (i % 3) * 11 + (i / 3) * 4;
                int y = 8 + (i / 3) * 8;
                DrawThickLine(texture, P(x, y), P(x + 11, y + 2), outline, 3);
                DrawThickLine(texture, P(x, y), P(x + 11, y + 2), bark, 1);
                FillEllipse(texture, x + 12, y + 2, 3, 3, outline);
                FillEllipse(texture, x + 12, y + 2, 2, 2, cut);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 44f, 24f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateToolsStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(52, 34, $"{name} {level}");
            Color outline = Rgb(42, 36, 32);
            Color metal = Rgb(125, 133, 131);
            Color light = Rgb(204, 208, 196);
            Color wood = Rgb(132, 82, 42);
            for (int i = 0; i < Mathf.Clamp(level + 1, 2, 7); i++)
            {
                int x = 8 + (i % 3) * 12 + (i / 3) * 3;
                int y = 9 + (i / 3) * 8;
                DrawLine(texture, P(x, y), P(x + 8, y + 7), outline);
                DrawLine(texture, P(x + 1, y), P(x + 9, y + 7), wood);
                FillRect(texture, x - 2, y - 2, 7, 4, outline);
                FillRect(texture, x - 1, y - 1, 5, 2, metal);
                SetPixelSafe(texture, x + 3, y, light);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 44f, 24f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static Sprite CreateForgeWorkSprite(int frame)
        {
            Texture2D texture = CreateTexture(72, 44, $"Forge Work Frame {frame}");
            Color outline = Rgb(42, 30, 25);
            Color metal = Rgb(91, 102, 101);
            Color hot = frame % 2 == 0 ? Rgb(245, 136, 45) : Rgb(255, 198, 77);
            Color sparks = Rgb(255, 226, 122);
            FillEllipse(texture, 36, 7, 25, 5, new Color(0f, 0f, 0f, 0.16f));
            DrawAnvil(texture, 32, 14, outline, metal, Rgb(190, 192, 176));
            FillRect(texture, 30, 20, 14, 3, hot);
            int swing = frame < 6 ? frame : 12 - frame;
            Vector2Int handleStart = P(38 - swing, 30 + swing / 2);
            Vector2Int handleEnd = P(48, 19);
            DrawThickLine(texture, handleStart, handleEnd, outline, 2);
            DrawThickLine(texture, handleStart, handleEnd, Rgb(130, 80, 40), 1);
            FillRect(texture, handleStart.x - 3, handleStart.y - 3, 9, 5, outline);
            FillRect(texture, handleStart.x - 2, handleStart.y - 2, 7, 3, metal);
            if (swing <= 1)
            {
                for (int i = 0; i < 9; i++)
                {
                    int x = 35 + (i % 3) * 4 - frame % 2;
                    int y = 23 + i / 3 * 3;
                    SetPixelSafe(texture, x, y, i % 2 == 0 ? sparks : hot);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 64f, 34f), new Vector2(0.5f, 0.20f), PixelsPerUnit);
        }

        private static void DrawAnvil(Texture2D texture, int x, int y, Color outline, Color metal, Color light)
        {
            FillRect(texture, x - 10, y + 6, 22, 5, outline);
            FillRect(texture, x - 8, y + 7, 18, 3, metal);
            FillPolygon(texture, new[] { P(x + 8, y + 7), P(x + 18, y + 9), P(x + 8, y + 11) }, outline);
            FillPolygon(texture, new[] { P(x + 8, y + 8), P(x + 15, y + 9), P(x + 8, y + 10) }, light);
            FillRect(texture, x - 4, y, 9, 8, outline);
            FillRect(texture, x - 2, y + 1, 5, 7, metal);
            FillRect(texture, x - 8, y - 3, 18, 4, outline);
            FillRect(texture, x - 6, y - 2, 14, 2, metal);
            DrawLine(texture, P(x - 6, y + 9), P(x + 8, y + 9), light);
        }

        private static void DrawForgeCoalPile(Texture2D texture, int x, int y, Color outline, Color coal, Color light)
        {
            FillEllipse(texture, x, y, 10, 5, outline);
            FillEllipse(texture, x, y, 9, 4, coal);
            for (int i = 0; i < 7; i++)
            {
                SetPixelSafe(texture, x - 5 + i * 2, y + i % 3, light);
            }
        }

        private static void DrawSmallLogStack(Texture2D texture, int x, int y, Color outline, Color bark, Color light)
        {
            for (int i = 0; i < 3; i++)
            {
                DrawThickLine(texture, P(x, y + i * 4), P(x + 17, y + 2 + i * 4), outline, 3);
                DrawThickLine(texture, P(x, y + i * 4), P(x + 17, y + 2 + i * 4), bark, 1);
                SetPixelSafe(texture, x + 15, y + 2 + i * 4, light);
            }
        }

        private static void DrawToolPile(Texture2D texture, int x, int y, Color outline, Color metal, Color light)
        {
            DrawLine(texture, P(x - 5, y + 2), P(x + 7, y + 11), outline);
            DrawLine(texture, P(x - 4, y + 2), P(x + 8, y + 11), metal);
            FillRect(texture, x - 8, y, 8, 4, outline);
            FillRect(texture, x - 7, y + 1, 6, 2, metal);
            DrawLine(texture, P(x + 5, y + 1), P(x - 4, y + 11), outline);
            DrawLine(texture, P(x + 6, y + 1), P(x - 3, y + 11), light);
        }
    }
}
