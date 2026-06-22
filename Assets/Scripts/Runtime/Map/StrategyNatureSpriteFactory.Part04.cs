using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyNatureSpriteFactory
    {
        public static Sprite GetCarriedClaySprite()
        {
            const int cacheKey = 12800;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedClaySprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateClayPatchSprite(int variant)
        {
            Texture2D texture = CreateTexture(58, 34, $"Clay Patch {variant + 1}");
            Color mud = new Color32(93, 61, 42, 120);
            Color wet = new Color32(122, 78, 58, 188);
            Color clay = new Color32(194, 104, 58, 215);
            Color light = new Color32(235, 151, 88, 170);
            Color shine = new Color32(247, 205, 143, 165);
            Color wetBlue = new Color32(72, 88, 86, 110);

            FillEllipse(texture, 28, 15, 24, 10, mud);
            FillEllipse(texture, 24, 16, 17, 8, wet);
            FillEllipse(texture, 35, 15, 17, 7, clay);
            FillEllipse(texture, 30, 13, 11, 4, light);
            DrawClayWetStreaks(texture, variant, wetBlue, light, shine);
            AddClayFlecks(texture, variant, 8, 8, 42, 16, shine, clay);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 50f, 24f), new Vector2(0.5f, 0.32f), StonePixelsPerUnit);
        }

        private static Sprite CreateClayBankSprite(int variant)
        {
            Texture2D texture = CreateTexture(76, 40, $"Clay Bank {variant + 1}");
            Color bankShadow = new Color32(74, 51, 38, 125);
            Color dark = new Color32(117, 67, 44, 215);
            Color clay = new Color32(190, 96, 51, 230);
            Color orange = new Color32(231, 132, 70, 205);
            Color wet = new Color32(80, 87, 82, 145);
            Color shine = new Color32(249, 198, 128, 170);

            FillEllipse(texture, 38, 17, 32, 11, bankShadow);
            DrawClayBankLayer(texture, P(9, 18 + variant % 3), P(34, 14 + variant % 4), P(67, 17 - variant % 3), dark);
            DrawClayBankLayer(texture, P(10, 21 + variant % 2), P(35, 18 + variant % 4), P(66, 20 - variant % 2), clay);
            DrawClayBankLayer(texture, P(14, 24), P(38, 25 + variant % 2), P(63, 24 + variant % 3), wet);
            DrawLine(texture, P(20, 15), P(32, 11), orange);
            DrawLine(texture, P(48, 16), P(60, 12), orange);
            DrawLine(texture, P(24, 19), P(47, 18), shine);
            AddClayFlecks(texture, variant + 17, 10, 9, 56, 19, shine, clay);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 6f, 66f, 28f), new Vector2(0.5f, 0.34f), StonePixelsPerUnit);
        }

        private static Sprite CreateCarriedClaySprite()
        {
            Texture2D texture = CreateTexture(34, 24, "Carried Clay");
            Color outline = Rgb(73, 43, 29);
            Color dark = Rgb(118, 65, 43);
            Color clay = Rgb(171, 85, 49);
            Color light = Rgb(223, 132, 75);

            FillEllipse(texture, 10, 10, 7, 5, outline);
            FillEllipse(texture, 10, 10, 6, 4, dark);
            FillEllipse(texture, 12, 11, 3, 2, clay);
            SetPixelSafe(texture, 8, 12, light);
            SetPixelSafe(texture, 13, 9, light);

            FillEllipse(texture, 21, 11, 8, 6, outline);
            FillEllipse(texture, 21, 11, 7, 5, clay);
            FillEllipse(texture, 23, 12, 3, 3, dark);
            SetPixelSafe(texture, 19, 9, light);
            SetPixelSafe(texture, 25, 14, light);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 30f, 17f), new Vector2(0.5f, 0.30f), StonePixelsPerUnit);
        }

        private static void DrawClayWetStreaks(Texture2D texture, int variant, Color wet, Color light, Color shine)
        {
            DrawLine(texture, P(10, 18), P(23 + variant, 17), wet);
            DrawLine(texture, P(23 + variant, 17), P(45, 18), wet);
            DrawLine(texture, P(15, 13), P(31, 12 + variant % 2), light);
            DrawLine(texture, P(29, 14), P(44, 13), shine);
            SetPixelSafe(texture, 18, 20, shine);
            SetPixelSafe(texture, 39, 16, shine);
        }

        private static void DrawClayBankLayer(Texture2D texture, Vector2Int start, Vector2Int mid, Vector2Int end, Color color)
        {
            DrawLine(texture, start, mid, color);
            DrawLine(texture, mid, end, color);
            DrawLine(texture, P(start.x, start.y + 1), P(mid.x, mid.y + 1), color);
            DrawLine(texture, P(mid.x, mid.y + 1), P(end.x, end.y + 1), color);
        }

        private static void AddClayFlecks(
            Texture2D texture,
            int variant,
            int startX,
            int startY,
            int width,
            int height,
            Color shine,
            Color clay)
        {
            int count = Mathf.Max(10, (width * height) / 52);
            for (int i = 0; i < count; i++)
            {
                int x = startX + ((variant * 17 + i * 13) % Mathf.Max(1, width));
                int y = startY + ((variant * 23 + i * 5) % Mathf.Max(1, height));
                SetPixelSafe(texture, x, y, i % 6 == 0 ? shine : clay);
                if (i % 8 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, shine);
                }
            }
        }
    }
}
