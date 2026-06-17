using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResourceIconFactory
    {
        private static void PaintStone(Texture2D texture)
        {
            Color outline = Rgb(54, 57, 56);
            Color stoneDark = Rgb(86, 91, 88);
            Color stone = Rgb(121, 129, 123);
            Color stoneLight = Rgb(168, 177, 167);
            Color moss = Rgb(75, 111, 68);

            FillEllipse(texture, 11, 10, 8, 6, outline);
            FillEllipse(texture, 11, 10, 7, 5, stoneDark);
            FillTriangle(texture, 5, 11, 10, 5, 17, 11, stone);
            FillTriangle(texture, 8, 15, 13, 8, 20, 15, stoneDark);
            DrawLine(texture, 7, 14, 17, 7, outline);
            FillRect(texture, 8, 8, 4, 2, stoneLight);
            FillRect(texture, 14, 12, 3, 2, stoneLight);
            DrawLine(texture, 5, 16, 11, 16, moss);
            SetPixelSafe(texture, 18, 13, stone);
        }
    }
}
