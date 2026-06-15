using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyNatureSpriteFactory
    {
        public static Sprite GetCarriedCoalSprite()
        {
            const int cacheKey = 12700;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedCoalSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateCoalDustGroundSprite(int variant)
        {
            Texture2D texture = CreateTexture(54, 34, $"Coal Dust Ground {variant + 1}");
            Color dust = new Color32(21, 24, 27, 155);
            Color dustMid = new Color32(38, 43, 47, 170);
            Color dustLight = new Color32(73, 82, 88, 130);
            Color soil = new Color32(45, 43, 39, 105);
            Color fleck = new Color32(124, 139, 145, 170);

            FillEllipse(texture, 27, 14, 23, 10, soil);
            FillEllipse(texture, 23, 15, 16, 7, dust);
            FillEllipse(texture, 32, 16, 15, 6, dustMid);
            DrawCoalCracks(texture, variant, dustLight, fleck);
            AddCoalFlecks(texture, variant, 7, 8, 40, 15, fleck, dust);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 46f, 24f), new Vector2(0.5f, 0.32f), StonePixelsPerUnit);
        }

        private static Sprite CreateCoalSeamSprite(int variant)
        {
            Texture2D texture = CreateTexture(72, 38, $"Coal Seam {variant + 1}");
            Color earth = new Color32(39, 38, 36, 110);
            Color seam = new Color32(18, 21, 24, 225);
            Color coal = new Color32(47, 54, 60, 220);
            Color blue = new Color32(71, 86, 96, 175);
            Color shine = new Color32(130, 145, 150, 165);

            FillEllipse(texture, 36, 15, 30, 10, earth);
            Vector2Int start = P(8, 16 + variant % 4);
            Vector2Int mid = P(34, 14 + variant % 5);
            Vector2Int end = P(64, 15 - variant % 4);
            DrawLine(texture, start, mid, seam);
            DrawLine(texture, P(start.x, start.y + 1), P(mid.x, mid.y + 1), coal);
            DrawLine(texture, mid, end, seam);
            DrawLine(texture, P(mid.x, mid.y - 1), P(end.x, end.y - 1), blue);
            DrawLine(texture, P(22, 17), P(16, 24), coal);
            DrawLine(texture, P(47, 15), P(56, 10), blue);
            AddCoalFlecks(texture, variant + 11, 10, 8, 52, 18, shine, seam);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 6f, 62f, 26f), new Vector2(0.5f, 0.34f), StonePixelsPerUnit);
        }

        private static Sprite CreateCarriedCoalSprite()
        {
            Texture2D texture = CreateTexture(34, 24, "Carried Coal");
            Color outline = Rgb(24, 23, 22);
            Color dark = Rgb(16, 19, 21);
            Color coal = Rgb(39, 46, 51);
            Color light = Rgb(104, 118, 124);

            FillEllipse(texture, 10, 10, 7, 5, outline);
            FillEllipse(texture, 10, 10, 6, 4, dark);
            FillEllipse(texture, 12, 11, 3, 2, coal);
            SetPixelSafe(texture, 8, 12, light);
            SetPixelSafe(texture, 13, 9, light);

            FillEllipse(texture, 21, 11, 8, 6, outline);
            FillEllipse(texture, 21, 11, 7, 5, coal);
            FillEllipse(texture, 23, 12, 3, 3, dark);
            SetPixelSafe(texture, 19, 9, light);
            SetPixelSafe(texture, 25, 14, light);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 30f, 17f), new Vector2(0.5f, 0.30f), StonePixelsPerUnit);
        }

        private static void DrawCoalCracks(Texture2D texture, int variant, Color dustLight, Color shine)
        {
            DrawLine(texture, P(9, 15), P(22 + variant, 13), dustLight);
            DrawLine(texture, P(22 + variant, 13), P(39, 17), dustLight);
            DrawLine(texture, P(25, 14), P(18, 21), dustLight);
            DrawLine(texture, P(34, 16), P(43, 10), shine);
        }

        private static void AddCoalFlecks(
            Texture2D texture,
            int variant,
            int startX,
            int startY,
            int width,
            int height,
            Color shine,
            Color dark)
        {
            int count = Mathf.Max(8, (width * height) / 60);
            for (int i = 0; i < count; i++)
            {
                int x = startX + ((variant * 23 + i * 11) % Mathf.Max(1, width));
                int y = startY + ((variant * 19 + i * 7) % Mathf.Max(1, height));
                SetPixelSafe(texture, x, y, i % 5 == 0 ? shine : dark);
                if (i % 7 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, shine);
                }
            }
        }
    }
}
