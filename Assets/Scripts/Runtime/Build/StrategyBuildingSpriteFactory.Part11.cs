using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static Sprite CreateTradingPostSprite(int variant)
        {
            Texture2D texture = CreateTexture(112, 92, $"Trading Post 2.5D Sprite {variant + 1}");
            Color outline = Rgb(46, 31, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color wood = variant == 1 ? Rgb(104, 73, 45) : Rgb(118, 76, 38);
            Color woodLight = variant == 1 ? Rgb(162, 117, 67) : Rgb(178, 112, 52);
            Color cloth = variant == 2 ? Rgb(76, 107, 126) : Rgb(142, 82, 49);
            Color clothLight = variant == 2 ? Rgb(116, 156, 173) : Rgb(204, 129, 64);
            Color canvas = Rgb(216, 187, 123);
            Color canvasDark = Rgb(147, 105, 60);
            Color crate = Rgb(138, 91, 48);
            Color sack = Rgb(164, 134, 82);
            Color coin = Rgb(226, 174, 63);

            FillEllipse(texture, 56, 11, 43, 8, shadow);
            Vector2Int[] platform = { P(14, 17), P(50, 7), P(98, 20), P(70, 36), P(28, 34) };
            FillPolygon(texture, platform, Rgb(86, 65, 47));
            DrawPolygon(texture, platform, outline);
            FillPolygon(texture, new[] { P(22, 19), P(51, 12), P(86, 21), P(66, 31), P(34, 30) }, Rgb(127, 101, 69));

            DrawPost(texture, 28, 24, 50, outline, wood);
            DrawPost(texture, 80, 24, 50, outline, woodLight);
            Vector2Int[] awningBack = { P(21, 53), P(54, 77), P(96, 56), P(83, 47), P(55, 63), P(31, 45) };
            FillPolygon(texture, awningBack, cloth);
            DrawPolygon(texture, awningBack, outline);
            DrawThickLine(texture, P(27, 54), P(89, 56), clothLight, 1);
            DrawThickLine(texture, P(38, 56), P(55, 69), canvas, 2);
            DrawThickLine(texture, P(70, 56), P(55, 69), canvasDark, 2);

            FillRect(texture, 30, 21, 52, 20, outline);
            FillRect(texture, 32, 23, 48, 16, wood);
            DrawLine(texture, P(32, 33), P(80, 33), woodLight);
            DrawLine(texture, P(40, 23), P(40, 38), Rgb(85, 53, 31));
            DrawLine(texture, P(63, 23), P(63, 38), Rgb(85, 53, 31));

            DrawCrates(texture, outline, crate, woodLight);
            DrawSacks(texture, outline, sack);
            DrawCoinSign(texture, outline, coin);
            DrawHangingGoods(texture, outline, canvas, clothLight);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 5f, 96f, 76f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static void DrawPost(Texture2D texture, int x, int y, int top, Color outline, Color wood)
        {
            FillRect(texture, x - 3, y, 7, top - y, outline);
            FillRect(texture, x - 1, y + 1, 3, top - y - 2, wood);
        }

        private static void DrawCrates(Texture2D texture, Color outline, Color crate, Color light)
        {
            FillRect(texture, 16, 18, 15, 13, outline);
            FillRect(texture, 18, 20, 11, 9, crate);
            DrawLine(texture, P(18, 24), P(29, 24), light);
            DrawLine(texture, P(23, 20), P(23, 29), Rgb(86, 55, 32));

            FillRect(texture, 80, 18, 16, 14, outline);
            FillRect(texture, 82, 20, 12, 10, crate);
            DrawLine(texture, P(82, 25), P(94, 25), light);
            DrawLine(texture, P(88, 20), P(88, 30), Rgb(86, 55, 32));
        }

        private static void DrawSacks(Texture2D texture, Color outline, Color sack)
        {
            FillEllipse(texture, 38, 20, 6, 9, outline);
            FillEllipse(texture, 38, 21, 5, 7, sack);
            DrawLine(texture, P(34, 25), P(42, 25), Rgb(116, 86, 52));
            FillEllipse(texture, 72, 21, 7, 9, outline);
            FillEllipse(texture, 72, 22, 6, 7, Rgb(190, 151, 88));
        }

        private static void DrawCoinSign(Texture2D texture, Color outline, Color coin)
        {
            FillRect(texture, 51, 38, 10, 16, outline);
            FillRect(texture, 54, 38, 4, 16, Rgb(92, 58, 34));
            FillEllipse(texture, 56, 57, 11, 9, outline);
            FillEllipse(texture, 56, 57, 9, 7, coin);
            DrawLine(texture, P(52, 57), P(60, 57), Rgb(255, 222, 113));
            DrawLine(texture, P(56, 53), P(56, 61), Rgb(160, 103, 36));
        }

        private static void DrawHangingGoods(Texture2D texture, Color outline, Color canvas, Color clothLight)
        {
            FillEllipse(texture, 43, 45, 4, 6, outline);
            FillEllipse(texture, 43, 45, 3, 5, canvas);
            FillEllipse(texture, 67, 45, 4, 6, outline);
            FillEllipse(texture, 67, 45, 3, 5, clothLight);
            DrawLine(texture, P(39, 50), P(71, 50), outline);
        }
    }
}
