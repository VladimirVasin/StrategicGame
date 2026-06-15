using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyConstructionSpriteFactory
    {

        private static void DrawThickLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color, int radius)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int sx = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y);
            int sy = from.y < to.y ? 1 : -1;
            int err = dx + dy;
            int x = from.x;
            int y = from.y;

            while (true)
            {
                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        if (Mathf.Abs(ox) + Mathf.Abs(oy) <= radius)
                        {
                            SetPixelSafe(texture, x + ox, y + oy, color);
                        }
                    }
                }

                if (x == to.x && y == to.y)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
        {
            DrawThickLine(texture, from, to, color, 0);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
    }
}
