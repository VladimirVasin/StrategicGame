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
            Color mud = new Color32(92, 62, 43, 118);
            Color wet = new Color32(128, 70, 47, 172);
            Color clay = new Color32(174, 93, 56, 190);
            Color light = new Color32(218, 133, 82, 145);
            Color shine = new Color32(232, 181, 125, 135);

            FillEllipse(texture, 28, 15, 24, 10, mud);
            FillEllipse(texture, 23, 16, 15, 7, wet);
            FillEllipse(texture, 34, 15, 16, 6, clay);
            DrawClayCracks(texture, variant, wet, light);
            AddClayFlecks(texture, variant, 8, 8, 42, 16, shine, clay);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 50f, 24f), new Vector2(0.5f, 0.32f), StonePixelsPerUnit);
        }

        private static Sprite CreateClayBankSprite(int variant)
        {
            Texture2D texture = CreateTexture(76, 40, $"Clay Bank {variant + 1}");
            Color bankShadow = new Color32(78, 52, 39, 118);
            Color dark = new Color32(112, 61, 42, 205);
            Color clay = new Color32(170, 86, 50, 220);
            Color orange = new Color32(210, 117, 69, 190);
            Color wet = new Color32(96, 74, 58, 150);
            Color shine = new Color32(235, 177, 117, 155);

            FillEllipse(texture, 38, 17, 32, 11, bankShadow);
            DrawLine(texture, P(9, 18 + variant % 3), P(33, 14 + variant % 4), dark);
            DrawLine(texture, P(33, 14 + variant % 4), P(67, 17 - variant % 3), dark);
            DrawLine(texture, P(10, 20 + variant % 2), P(34, 17 + variant % 4), clay);
            DrawLine(texture, P(34, 17 + variant % 4), P(66, 20 - variant % 2), clay);
            DrawLine(texture, P(16, 23), P(61, 24 + variant % 3), wet);
            DrawLine(texture, P(21, 15), P(30, 10), orange);
            DrawLine(texture, P(49, 17), P(58, 11), orange);
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

        private static void DrawClayCracks(Texture2D texture, int variant, Color wet, Color light)
        {
            DrawLine(texture, P(10, 15), P(23 + variant, 13), wet);
            DrawLine(texture, P(23 + variant, 13), P(41, 17), wet);
            DrawLine(texture, P(27, 14), P(20, 22), light);
            DrawLine(texture, P(36, 16), P(46, 11), light);
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
