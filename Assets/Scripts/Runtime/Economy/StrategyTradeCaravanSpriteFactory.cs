using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyTradeCaravanSpriteFactory
    {
        private static Sprite cachedSprite;

        public static Sprite GetSprite()
        {
            if (cachedSprite == null)
            {
                cachedSprite = CreateSprite();
            }

            return cachedSprite;
        }

        private static Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(72, 46, TextureFormat.RGBA32, false)
            {
                name = "Trade Caravan Sprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            Color outline = Rgb(43, 29, 22);
            Color wood = Rgb(126, 78, 39);
            Color woodLight = Rgb(183, 119, 57);
            Color cloth = Rgb(204, 157, 86);
            Color clothDark = Rgb(132, 86, 54);
            Color wheel = Rgb(68, 48, 35);
            Color coin = Rgb(229, 177, 62);

            FillEllipse(texture, 36, 7, 25, 5, new Color(0f, 0f, 0f, 0.22f));
            FillRect(texture, 18, 13, 34, 14, outline);
            FillRect(texture, 20, 15, 30, 10, wood);
            DrawLine(texture, 20, 20, 50, 20, woodLight);
            DrawLine(texture, 29, 15, 29, 25, Rgb(86, 54, 32));
            DrawLine(texture, 41, 15, 41, 25, Rgb(86, 54, 32));

            FillPolygon(texture, new[] { P(15, 27), P(29, 39), P(52, 38), P(59, 27) }, outline);
            FillPolygon(texture, new[] { P(18, 27), P(30, 36), P(51, 35), P(56, 27) }, cloth);
            DrawLine(texture, 31, 36, 36, 27, clothDark);
            DrawLine(texture, 45, 35, 41, 27, clothDark);

            FillEllipse(texture, 22, 12, 6, 6, outline);
            FillEllipse(texture, 22, 12, 4, 4, wheel);
            FillEllipse(texture, 47, 12, 6, 6, outline);
            FillEllipse(texture, 47, 12, 4, 4, wheel);

            DrawLine(texture, 51, 18, 65, 15, outline);
            DrawLine(texture, 51, 18, 64, 16, woodLight);
            FillEllipse(texture, 59, 26, 6, 8, outline);
            FillEllipse(texture, 59, 26, 4, 6, Rgb(137, 91, 55));
            FillRect(texture, 57, 18, 5, 8, outline);
            FillRect(texture, 58, 19, 3, 6, Rgb(96, 55, 39));

            FillEllipse(texture, 12, 20, 5, 6, outline);
            FillEllipse(texture, 12, 20, 3, 4, coin);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 64f, 36f), new Vector2(0.5f, 0.16f), 24f);
        }

        private static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static void FillRect(Texture2D texture, int x, int y, int w, int h, Color color)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                for (int xx = x; xx < x + w; xx++)
                {
                    SetPixel(texture, xx, yy, color);
                }
            }
        }

        private static void FillEllipse(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float dx = (x - cx) / Mathf.Max(1f, rx);
                    float dy = (y - cy) / Mathf.Max(1f, ry);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void FillPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (IsInside(points, x, y))
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static bool IsInside(Vector2Int[] points, int x, int y)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                if ((points[i].y > y) != (points[j].y > y)
                    && x < (points[j].x - points[i].x) * (y - points[i].y) / Mathf.Max(1f, points[j].y - points[i].y) + points[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                SetPixel(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int doubled = error * 2;
                if (doubled >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (doubled <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
