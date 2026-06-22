using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyNatureSpriteFactory
    {
        public static Sprite GetCarriedIronSprite()
        {
            const int cacheKey = 12600;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedIronSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateBoulderSprite(int variant)
        {
            Texture2D texture = CreateTexture(44, 34, $"Boulder {variant + 1}");
            Color outline = Rgb(45, 43, 40);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color stone = GetStoneColor(variant);
            Color dark = Shift(stone, -0.18f);
            Color light = Shift(stone, 0.16f);

            FillEllipse(texture, 22, 7, 15, 4, shadow);

            Vector2Int[] back = { P(9, 14), P(19, 25), P(33, 23), P(38, 14), P(29, 8), P(15, 9) };
            Vector2Int[] face = { P(8, 13), P(13, 22), P(27, 25), P(37, 17), P(31, 9), P(16, 8) };
            FillPolygon(texture, back, dark);
            FillPolygon(texture, face, stone);
            DrawPolygon(texture, back, outline);
            DrawPolygon(texture, face, outline);

            DrawLine(texture, P(15, 21), P(22, 12), dark);
            DrawLine(texture, P(23, 24), P(30, 12), dark);
            DrawLine(texture, P(17, 11), P(29, 10), light);
            DrawLine(texture, P(12, 15), P(18, 12), light);
            AddStoneSpeckles(texture, variant, 9, 10, 27, 14, light, dark);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 36f, 25f), new Vector2(0.5f, 0.18f), StonePixelsPerUnit);
        }

        private static Sprite CreateIronStainedGroundSprite(int variant)
        {
            Texture2D texture = CreateTexture(54, 34, $"Iron-stained Ground {variant + 1}");
            Color graphite = new Color32(45, 48, 49, 170);
            Color oreDark = new Color32(55, 45, 42, 210);
            Color rust = new Color32(139, 63, 34, 205);
            Color rustLight = new Color32(208, 107, 48, 180);
            Color metal = new Color32(193, 188, 165, 230);

            FillEllipse(texture, 27, 14, 24, 10, graphite);
            FillPolygon(texture, new[] { P(8, 15), P(18, 9), P(31, 11), P(45, 17), P(34, 23), P(17, 22) }, oreDark);
            DrawJaggedIronVein(texture, variant, rust, rustLight, metal);
            AddIronShardCluster(texture, variant, metal, rustLight, oreDark);
            AddIronFlecks(texture, variant, 8, 8, 39, 16, metal, oreDark);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 46f, 24f), new Vector2(0.5f, 0.32f), StonePixelsPerUnit);
        }

        private static Sprite CreateIronVeinSprite(int variant)
        {
            Texture2D texture = CreateTexture(72, 38, $"Iron Vein {variant + 1}");
            Color earth = new Color32(42, 45, 45, 125);
            Color seam = new Color32(47, 42, 40, 230);
            Color rust = new Color32(158, 68, 33, 225);
            Color rustLight = new Color32(223, 119, 51, 190);
            Color metal = new Color32(202, 198, 176, 235);

            FillEllipse(texture, 36, 15, 30, 10, earth);
            Vector2Int start = P(8, 15 + variant % 5);
            Vector2Int mid = P(33, 16 - variant % 4);
            Vector2Int end = P(64, 13 + variant % 6);
            DrawLine(texture, start, mid, seam);
            DrawLine(texture, P(start.x, start.y + 1), P(mid.x, mid.y + 1), seam);
            DrawLine(texture, P(start.x, start.y - 1), P(mid.x, mid.y - 1), rust);
            DrawLine(texture, mid, end, seam);
            DrawLine(texture, P(mid.x, mid.y - 1), P(end.x, end.y - 1), seam);
            DrawLine(texture, P(mid.x, mid.y + 1), P(end.x, end.y + 1), rust);
            DrawLine(texture, P(23, 16), P(17, 24), rustLight);
            DrawLine(texture, P(45, 15), P(55, 8), rustLight);
            DrawLine(texture, P(31, 19), P(39, 26), metal);
            AddIronFlecks(texture, variant + 7, 10, 8, 52, 18, metal, seam);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 6f, 62f, 26f), new Vector2(0.5f, 0.34f), StonePixelsPerUnit);
        }

        private static Sprite CreateCarriedIronSprite()
        {
            Texture2D texture = CreateTexture(34, 24, "Carried Iron");
            Color outline = Rgb(43, 35, 31);
            Color oreDark = Rgb(65, 58, 54);
            Color ore = Rgb(105, 92, 80);
            Color rust = Rgb(158, 72, 35);
            Color rustLight = Rgb(211, 112, 52);
            Color metal = Rgb(194, 188, 168);

            FillEllipse(texture, 10, 10, 7, 5, outline);
            FillEllipse(texture, 10, 10, 6, 4, oreDark);
            FillEllipse(texture, 12, 11, 3, 2, ore);
            DrawLine(texture, P(5, 11), P(15, 8), rust);
            SetPixelSafe(texture, 8, 12, rustLight);
            SetPixelSafe(texture, 12, 9, metal);

            FillEllipse(texture, 21, 11, 8, 6, outline);
            FillEllipse(texture, 21, 11, 7, 5, ore);
            FillEllipse(texture, 23, 12, 3, 3, oreDark);
            DrawLine(texture, P(16, 13), P(28, 10), rustLight);
            SetPixelSafe(texture, 19, 9, metal);
            SetPixelSafe(texture, 25, 14, rust);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 30f, 17f), new Vector2(0.5f, 0.30f), StonePixelsPerUnit);
        }

        private static void DrawIronCracks(Texture2D texture, int variant, Color rustLight, Color metal)
        {
            DrawLine(texture, P(9, 15), P(23 + variant, 12), rustLight);
            DrawLine(texture, P(23 + variant, 12), P(38, 17), rustLight);
            DrawLine(texture, P(26, 14), P(19, 21), rustLight);
            DrawLine(texture, P(34, 16), P(43, 10), metal);
        }

        private static void DrawJaggedIronVein(Texture2D texture, int variant, Color rust, Color rustLight, Color metal)
        {
            Vector2Int a = P(10, 13 + variant % 3);
            Vector2Int b = P(21, 10 + variant % 4);
            Vector2Int c = P(34, 15 - variant % 3);
            Vector2Int d = P(45, 12 + variant % 4);
            DrawLine(texture, a, b, rust);
            DrawLine(texture, b, c, rustLight);
            DrawLine(texture, c, d, rust);
            DrawLine(texture, P(25, 13), P(20, 21), metal);
            DrawLine(texture, P(35, 15), P(43, 22), metal);
        }

        private static void AddIronShardCluster(Texture2D texture, int variant, Color metal, Color rustLight, Color oreDark)
        {
            for (int i = 0; i < 7; i++)
            {
                int x = 11 + ((variant * 11 + i * 7) % 33);
                int y = 10 + ((variant * 5 + i * 9) % 13);
                FillRect(texture, x, y, 2, 1, i % 3 == 0 ? metal : rustLight);
                SetPixelSafe(texture, x + 1, y + 1, oreDark);
            }
        }

        private static void AddIronFlecks(
            Texture2D texture,
            int variant,
            int startX,
            int startY,
            int width,
            int height,
            Color metal,
            Color dark)
        {
            int count = Mathf.Max(8, (width * height) / 58);
            for (int i = 0; i < count; i++)
            {
                int x = startX + ((variant * 19 + i * 13) % Mathf.Max(1, width));
                int y = startY + ((variant * 17 + i * 7) % Mathf.Max(1, height));
                SetPixelSafe(texture, x, y, i % 4 == 0 ? metal : dark);
                if (i % 6 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, metal);
                }
            }
        }
    }
}
