using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static Sprite CreateMineSprite(int variant)
        {
            Texture2D texture = CreateTexture(100, 92, $"Mine 2.5D Sprite {variant + 1}");
            Color outline = Rgb(37, 29, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color dirtDark = variant == 1 ? Rgb(68, 56, 45) : Rgb(76, 61, 47);
            Color dirt = variant == 2 ? Rgb(123, 93, 61) : Rgb(106, 81, 56);
            Color woodDark = Rgb(66, 43, 31);
            Color wood = variant == 2 ? Rgb(122, 78, 43) : Rgb(103, 67, 40);
            Color woodLight = Rgb(171, 112, 61);
            Color rust = Rgb(154, 71, 37);
            Color rustLight = Rgb(211, 119, 60);
            Color iron = Rgb(144, 143, 127);

            FillEllipse(texture, 50, 10, 37, 8, shadow);

            Vector2Int[] ground = { P(13, 18), P(45, 7), P(87, 19), P(58, 35) };
            FillPolygon(texture, ground, dirtDark);
            DrawPolygon(texture, ground, outline);
            Vector2Int[] groundTop = { P(21, 18), P(46, 10), P(79, 20), P(57, 30) };
            FillPolygon(texture, groundTop, dirt);

            FillEllipse(texture, 50, 31, 24, 14, Rgb(29, 26, 24));
            DrawCanopyRim(texture, 50, 31, 24, 14, outline);
            FillEllipse(texture, 50, 28, 16, 9, new Color(0.03f, 0.025f, 0.02f, 0.96f));

            DrawThickLine(texture, P(32, 22), P(32, 54), woodDark, 3);
            DrawThickLine(texture, P(68, 22), P(68, 53), woodDark, 3);
            DrawThickLine(texture, P(33, 23), P(33, 54), wood, 2);
            DrawThickLine(texture, P(69, 23), P(69, 53), wood, 2);
            DrawThickLine(texture, P(29, 53), P(50, 70), woodDark, 3);
            DrawThickLine(texture, P(50, 70), P(72, 53), woodDark, 3);
            DrawThickLine(texture, P(31, 53), P(50, 67), wood, 2);
            DrawThickLine(texture, P(50, 67), P(70, 53), wood, 2);
            DrawLine(texture, P(35, 48), P(65, 49), woodLight);
            DrawLine(texture, P(39, 59), P(61, 59), woodLight);

            DrawMineRail(texture, 36, 16, outline, wood, iron);
            DrawMineCart(texture, 66, 20, outline, woodDark, rust, rustLight, iron);

            for (int i = 0; i < 10; i++)
            {
                int x = 22 + ((variant * 17 + i * 11) % 55);
                int y = 14 + ((variant * 13 + i * 7) % 18);
                SetPixelSafe(texture, x, y, i % 3 == 0 ? rustLight : rust);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 84f, 76f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateIronStockSprite(int level, string name)
        {
            Texture2D texture = CreateTexture(60, 38, $"{name} {level}");
            Color outline = Rgb(45, 35, 31);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color rustDark = Rgb(97, 48, 34);
            Color rust = Rgb(155, 73, 38);
            Color rustLight = Rgb(213, 118, 58);
            Color metal = Rgb(170, 166, 142);

            FillEllipse(texture, 30, 7, 20, 5, shadow);
            int chunks = Mathf.Clamp(level + 2, 3, 8);
            for (int i = 0; i < chunks; i++)
            {
                int x = 13 + (i % 4) * 9 + (i / 4) * 4;
                int y = 10 + (i / 4) * 7 + (i % 2) * 2;
                FillEllipse(texture, x, y, 6, 4, rustDark);
                FillEllipse(texture, x + 1, y + 1, 5, 3, i % 2 == 0 ? rust : metal);
                SetPixelSafe(texture, x + 3, y + 3, rustLight);
                DrawCanopyRim(texture, x, y, 6, 4, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 4f, 50f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static void DrawMineRail(Texture2D texture, int x, int y, Color outline, Color wood, Color metal)
        {
            DrawLine(texture, P(x - 17, y), P(x + 16, y + 8), outline);
            DrawLine(texture, P(x - 15, y + 2), P(x + 19, y + 10), outline);
            DrawLine(texture, P(x - 17, y + 1), P(x + 16, y + 9), metal);
            DrawLine(texture, P(x - 15, y + 3), P(x + 19, y + 11), metal);
            for (int i = 0; i < 4; i++)
            {
                DrawLine(texture, P(x - 11 + i * 8, y + i * 2), P(x - 6 + i * 8, y + 5 + i * 2), wood);
            }
        }

        private static void DrawMineCart(Texture2D texture, int x, int y, Color outline, Color dark, Color rust, Color light, Color metal)
        {
            Vector2Int[] cart = { P(x - 13, y + 4), P(x + 9, y + 4), P(x + 5, y + 17), P(x - 10, y + 17) };
            FillPolygon(texture, cart, dark);
            DrawPolygon(texture, cart, outline);
            FillRect(texture, x - 8, y + 8, 13, 6, rust);
            DrawLine(texture, P(x - 6, y + 14), P(x + 4, y + 14), light);
            FillEllipse(texture, x - 7, y + 3, 3, 3, metal);
            FillEllipse(texture, x + 5, y + 3, 3, 3, metal);
        }
    }
}
