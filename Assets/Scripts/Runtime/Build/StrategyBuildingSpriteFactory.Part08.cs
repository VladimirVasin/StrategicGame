using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public static Sprite GetClayPitStockSprite(int clayStored)
        {
            return GetClayStockSprite(clayStored, 73728, "Clay Pit Stock", StrategyVisualSequenceIds.ClayPitClay);
        }

        public static Sprite GetStorageYardClayStockSprite(int clayStored)
        {
            return GetClayStockSprite(clayStored, 74752, "Storage Clay Stock", StrategyVisualSequenceIds.StorageClay);
        }

        private static Sprite GetClayStockSprite(int stored, int baseKey, string name, string sequenceId = null)
        {
            if (stored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stored + 1) / 2, 1, 6);
            if (sequenceId != null && TryGetBakedLayer(sequenceId, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = baseKey + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateClayStockSprite(level, name);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateClayPitSprite(int variant)
        {
            Texture2D texture = CreateTexture(108, 86, $"Clay Pit 2.5D Sprite {variant + 1}");
            Color outline = Rgb(49, 31, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color soil = variant == 1 ? Rgb(82, 62, 44) : Rgb(91, 67, 45);
            Color soilDark = Rgb(56, 43, 34);
            Color clay = variant == 2 ? Rgb(183, 90, 51) : Rgb(165, 81, 48);
            Color clayLight = Rgb(225, 135, 76);
            Color wet = Rgb(104, 73, 58);
            Color wood = Rgb(117, 73, 38);
            Color woodLight = Rgb(178, 112, 57);

            FillEllipse(texture, 54, 12, 42, 8, shadow);
            Vector2Int[] berm = { P(12, 23), P(48, 9), P(98, 23), P(71, 45), P(31, 43) };
            FillPolygon(texture, berm, soil);
            DrawPolygon(texture, berm, outline);

            Vector2Int[] pitDark = { P(24, 27), P(50, 18), P(84, 28), P(66, 39), P(38, 38) };
            FillPolygon(texture, pitDark, soilDark);
            DrawPolygon(texture, pitDark, outline);
            Vector2Int[] pitWet = { P(31, 29), P(51, 23), P(77, 30), P(63, 36), P(42, 36) };
            FillPolygon(texture, pitWet, wet);
            Vector2Int[] clayPool = { P(37, 30), P(52, 26), P(69, 31), P(60, 34), P(44, 34) };
            FillPolygon(texture, clayPool, clay);
            DrawLine(texture, P(41, 31), P(65, 32), clayLight);

            DrawThickLine(texture, P(16, 36), P(43, 50), outline, 5);
            DrawThickLine(texture, P(16, 36), P(43, 50), wood, 3);
            DrawThickLine(texture, P(92, 35), P(67, 51), outline, 5);
            DrawThickLine(texture, P(92, 35), P(67, 51), wood, 3);
            DrawThickLine(texture, P(30, 51), P(78, 51), outline, 5);
            DrawThickLine(texture, P(31, 51), P(77, 51), woodLight, 3);

            for (int i = 0; i < 9; i++)
            {
                int x = 21 + ((variant * 11 + i * 9) % 67);
                int y = 22 + ((variant * 7 + i * 5) % 21);
                SetPixelSafe(texture, x, y, i % 3 == 0 ? clayLight : clay);
            }

            DrawBuildingPolish(texture, StrategyBuildTool.ClayPit, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 92f, 70f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateClayStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(54, 34, $"{name} {level}");
            Color outline = Rgb(70, 42, 28);
            Color clay = Rgb(165, 83, 49);
            Color clayLight = Rgb(219, 132, 75);
            Color wet = Rgb(105, 72, 54);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);

            FillEllipse(texture, 27, 7, 19, 4, shadow);
            for (int i = 0; i < Mathf.Clamp(level + 2, 2, 8); i++)
            {
                int x = 12 + (i % 4) * 7 + (i / 4) * 3;
                int y = 11 + (i / 4) * 5 + (i % 2);
                FillEllipse(texture, x, y, 5, 3, outline);
                FillEllipse(texture, x, y, 4, 2, i % 3 == 0 ? wet : clay);
                SetPixelSafe(texture, x + 1, y + 1, clayLight);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 46f, 24f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }
    }
}
