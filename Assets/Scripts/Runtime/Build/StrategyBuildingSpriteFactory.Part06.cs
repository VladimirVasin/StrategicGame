using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public static Sprite GetCoalPitStockSprite(int coalStored)
        {
            if (coalStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((coalStored + 2) / 3, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.CoalPitCoal, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 63488 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCoalStockSprite(level, "Coal Pit Stock");
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardCoalStockSprite(int coalStored)
        {
            if (coalStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((coalStored + 3) / 4, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.StorageCoal, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 65536 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCoalStockSprite(level, "Storage Coal Stock");
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateCoalPitSprite(int variant)
        {
            Texture2D texture = CreateTexture(100, 88, $"Coal Pit 2.5D Sprite {variant + 1}");
            Color outline = Rgb(28, 25, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.26f);
            Color earthDark = variant == 1 ? Rgb(51, 48, 42) : Rgb(58, 52, 44);
            Color earth = variant == 2 ? Rgb(96, 77, 55) : Rgb(78, 68, 54);
            Color coalDark = Rgb(15, 18, 20);
            Color coal = Rgb(35, 41, 46);
            Color coalLight = Rgb(88, 99, 105);
            Color woodDark = Rgb(61, 39, 28);
            Color wood = Rgb(104, 68, 39);
            Color woodLight = Rgb(166, 110, 58);

            FillEllipse(texture, 50, 10, 38, 8, shadow);
            Vector2Int[] ground = { P(12, 18), P(44, 7), P(88, 19), P(58, 36) };
            FillPolygon(texture, ground, earthDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] top = { P(21, 18), P(46, 11), P(79, 20), P(57, 31) };
            FillPolygon(texture, top, earth);

            FillEllipse(texture, 50, 29, 28, 14, outline);
            FillEllipse(texture, 50, 29, 24, 11, coalDark);
            FillEllipse(texture, 53, 30, 15, 7, coal);
            DrawCoalRim(texture, outline, woodDark, wood, woodLight);
            DrawCoalChunks(texture, variant, coal, coalLight);
            DrawSupportPosts(texture, variant, outline, woodDark, wood, woodLight);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 84f, 74f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateCoalStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(60, 38, $"{name} {level}");
            Color outline = Rgb(25, 24, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color dark = Rgb(18, 21, 23);
            Color coal = Rgb(42, 49, 54);
            Color light = Rgb(105, 119, 124);

            FillEllipse(texture, 30, 7, 20, 5, shadow);
            int chunks = Mathf.Clamp(level + 3, 4, 9);
            for (int i = 0; i < chunks; i++)
            {
                int x = 11 + (i % 5) * 8 + (i / 5) * 4;
                int y = 10 + (i / 5) * 7 + (i % 2) * 2;
                FillEllipse(texture, x, y, 6, 4, outline);
                FillEllipse(texture, x + 1, y + 1, 5, 3, i % 2 == 0 ? coal : dark);
                SetPixelSafe(texture, x + 3, y + 3, light);
                if (i % 3 == 0)
                {
                    SetPixelSafe(texture, x + 4, y + 2, light);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 4f, 50f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static void DrawCoalRim(Texture2D texture, Color outline, Color woodDark, Color wood, Color woodLight)
        {
            DrawThickLine(texture, P(25, 25), P(45, 18), woodDark, 3);
            DrawThickLine(texture, P(45, 18), P(75, 25), woodDark, 3);
            DrawThickLine(texture, P(25, 35), P(45, 44), woodDark, 3);
            DrawThickLine(texture, P(45, 44), P(75, 35), woodDark, 3);
            DrawThickLine(texture, P(26, 25), P(45, 20), wood, 2);
            DrawThickLine(texture, P(45, 20), P(74, 25), wood, 2);
            DrawThickLine(texture, P(27, 35), P(45, 42), wood, 2);
            DrawThickLine(texture, P(45, 42), P(73, 35), wood, 2);
            DrawLine(texture, P(31, 24), P(68, 27), woodLight);
            DrawLine(texture, P(31, 36), P(67, 34), outline);
        }

        private static void DrawCoalChunks(Texture2D texture, int variant, Color coal, Color coalLight)
        {
            for (int i = 0; i < 14; i++)
            {
                int x = 31 + ((variant * 17 + i * 9) % 37);
                int y = 21 + ((variant * 11 + i * 5) % 16);
                SetPixelSafe(texture, x, y, i % 4 == 0 ? coalLight : coal);
                if (i % 5 == 0)
                {
                    FillEllipse(texture, x, y, 2, 1, coal);
                }
            }
        }

        private static void DrawSupportPosts(
            Texture2D texture,
            int variant,
            Color outline,
            Color woodDark,
            Color wood,
            Color woodLight)
        {
            int left = variant == 2 ? 27 : 30;
            int right = variant == 1 ? 72 : 69;
            DrawThickLine(texture, P(left, 20), P(left - 2, 51), outline, 4);
            DrawThickLine(texture, P(right, 21), P(right + 2, 50), outline, 4);
            DrawThickLine(texture, P(left, 21), P(left - 2, 50), woodDark, 3);
            DrawThickLine(texture, P(right, 22), P(right + 2, 49), woodDark, 3);
            DrawThickLine(texture, P(left + 1, 22), P(left - 1, 49), wood, 2);
            DrawThickLine(texture, P(right + 1, 23), P(right + 1, 48), wood, 2);
            DrawThickLine(texture, P(left - 5, 50), P(50, 67), outline, 4);
            DrawThickLine(texture, P(50, 67), P(right + 6, 50), outline, 4);
            DrawThickLine(texture, P(left - 4, 50), P(50, 64), wood, 2);
            DrawThickLine(texture, P(50, 64), P(right + 5, 50), wood, 2);
            DrawLine(texture, P(left + 3, 43), P(right - 3, 44), woodLight);
        }
    }
}
