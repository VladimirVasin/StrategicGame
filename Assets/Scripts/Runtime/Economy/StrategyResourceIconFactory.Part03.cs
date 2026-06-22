using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResourceIconFactory
    {
        private static void PaintEggs(Texture2D texture)
        {
            Color outline = Rgb(72, 49, 32);
            Color basket = Rgb(139, 88, 45);
            Color basketLight = Rgb(188, 123, 62);
            Color egg = Rgb(238, 225, 190);
            Color eggLight = Rgb(255, 247, 220);

            FillRect(texture, 4, 6, 16, 7, basket);
            DrawRectOutline(texture, 4, 6, 16, 7, outline);
            DrawLine(texture, 6, 12, 18, 12, basketLight);
            FillEllipse(texture, 8, 14, 3, 5, outline);
            FillEllipse(texture, 8, 14, 2, 4, egg);
            FillEllipse(texture, 13, 15, 4, 5, outline);
            FillEllipse(texture, 13, 15, 3, 4, eggLight);
            FillEllipse(texture, 17, 14, 3, 5, outline);
            FillEllipse(texture, 17, 14, 2, 4, egg);
        }

        private static void PaintTools(Texture2D texture)
        {
            Color outline = Rgb(42, 36, 32);
            Color metalDark = Rgb(73, 78, 80);
            Color metal = Rgb(126, 134, 132);
            Color metalLight = Rgb(204, 208, 196);
            Color wood = Rgb(132, 82, 42);
            Color woodLight = Rgb(190, 123, 62);

            DrawLine(texture, 6, 5, 17, 16, outline);
            DrawLine(texture, 7, 5, 18, 16, wood);
            DrawLine(texture, 8, 5, 19, 16, woodLight);
            FillRect(texture, 4, 3, 8, 5, outline);
            FillRect(texture, 5, 4, 6, 3, metal);
            FillRect(texture, 8, 2, 5, 3, outline);
            FillRect(texture, 9, 3, 3, 1, metalLight);

            DrawLine(texture, 6, 18, 18, 7, outline);
            DrawLine(texture, 7, 18, 19, 7, metalDark);
            DrawLine(texture, 5, 15, 8, 20, outline);
            DrawLine(texture, 17, 6, 20, 11, outline);
            DrawLine(texture, 8, 17, 12, 21, metal);
            SetPixelSafe(texture, 16, 9, metalLight);
            SetPixelSafe(texture, 11, 13, metalLight);
        }
    }
}
