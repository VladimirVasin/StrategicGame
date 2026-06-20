using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResourceIconFactory
    {
        private static void PaintDish(Texture2D texture)
        {
            Color outline = Rgb(50, 42, 36);
            Color plateDark = Rgb(147, 138, 116);
            Color plate = Rgb(214, 203, 170);
            Color stew = Rgb(139, 83, 44);
            Color stewLight = Rgb(212, 137, 72);
            Color greens = Rgb(88, 150, 67);
            Color steam = new Color(0.78f, 0.75f, 0.66f, 0.85f);

            DrawLine(texture, 8, 20, 6, 22, steam);
            DrawLine(texture, 12, 19, 12, 22, steam);
            DrawLine(texture, 16, 20, 18, 22, steam);
            FillEllipse(texture, 12, 10, 9, 6, outline);
            FillEllipse(texture, 12, 10, 8, 5, plateDark);
            FillEllipse(texture, 12, 11, 6, 3, plate);
            FillEllipse(texture, 12, 12, 5, 3, stew);
            FillEllipse(texture, 14, 13, 3, 2, stewLight);
            SetPixelSafe(texture, 9, 13, greens);
            SetPixelSafe(texture, 11, 14, greens);
            SetPixelSafe(texture, 16, 12, greens);
            DrawLine(texture, 5, 7, 19, 7, outline);
            DrawLine(texture, 6, 8, 18, 8, plate);
        }

        private static void PaintClay(Texture2D texture)
        {
            Color outline = Rgb(61, 43, 33);
            Color wetDark = Rgb(112, 61, 43);
            Color clay = Rgb(171, 91, 55);
            Color clayLight = Rgb(207, 126, 78);
            Color shine = new Color(0.82f, 0.66f, 0.51f, 0.9f);

            FillEllipse(texture, 12, 10, 9, 6, outline);
            FillEllipse(texture, 12, 10, 8, 5, wetDark);
            FillEllipse(texture, 11, 11, 6, 3, clay);
            FillEllipse(texture, 15, 9, 4, 3, clayLight);
            DrawLine(texture, 6, 9, 10, 7, outline);
            DrawLine(texture, 12, 13, 18, 14, outline);
            DrawLine(texture, 9, 15, 15, 16, wetDark);
            SetPixelSafe(texture, 10, 12, shine);
            SetPixelSafe(texture, 14, 10, shine);
            SetPixelSafe(texture, 17, 11, shine);
        }

        private static void PaintPottery(Texture2D texture)
        {
            Color outline = Rgb(57, 37, 28);
            Color clayDark = Rgb(126, 63, 39);
            Color clay = Rgb(186, 92, 50);
            Color clayLight = Rgb(224, 139, 78);
            Color glaze = Rgb(73, 126, 111);

            FillEllipse(texture, 12, 7, 5, 3, outline);
            FillEllipse(texture, 12, 7, 4, 2, clayLight);
            FillRect(texture, 7, 7, 10, 8, outline);
            FillRect(texture, 8, 8, 8, 6, clay);
            FillEllipse(texture, 12, 15, 8, 5, outline);
            FillEllipse(texture, 12, 15, 7, 4, clayDark);
            FillEllipse(texture, 13, 15, 5, 3, clay);
            DrawLine(texture, 8, 10, 16, 10, glaze);
            DrawLine(texture, 9, 13, 15, 13, clayLight);
            FillRect(texture, 5, 11, 3, 5, outline);
            FillRect(texture, 16, 11, 3, 5, outline);
            SetPixelSafe(texture, 14, 12, clayLight);
        }
    }
}
