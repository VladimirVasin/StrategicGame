using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyPointOfInterestSpriteFactory
    {
        private const float PixelsPerUnit = 28f;

        private static Sprite unexploredSprite;
        private static Sprite investigatedSprite;

        public static Sprite GetSprite(bool investigated)
        {
            if (investigated)
            {
                if (investigatedSprite == null)
                {
                    investigatedSprite = CreateSprite(true);
                }

                return investigatedSprite;
            }

            if (unexploredSprite == null)
            {
                unexploredSprite = CreateSprite(false);
            }

            return unexploredSprite;
        }

        private static Sprite CreateSprite(bool investigated)
        {
            Texture2D texture = new Texture2D(32, 40, TextureFormat.RGBA32, false)
            {
                name = investigated ? "Investigated Point Of Interest" : "Point Of Interest",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[texture.width * texture.height]);

            Color shadow = Rgb(40, 37, 35);
            Color outline = investigated ? Rgb(54, 66, 61) : Rgb(67, 48, 34);
            Color stone = investigated ? Rgb(106, 126, 113) : Rgb(155, 119, 72);
            Color stoneLight = investigated ? Rgb(145, 162, 145) : Rgb(210, 166, 91);
            Color rune = investigated ? Rgb(126, 205, 151) : Rgb(255, 220, 105);

            FillEllipse(texture, 16, 5, 11, 3, shadow);
            FillRect(texture, 10, 5, 12, 21, outline);
            FillRect(texture, 11, 6, 10, 19, stone);
            FillRect(texture, 12, 8, 3, 15, stoneLight);
            FillRect(texture, 9, 25, 14, 4, outline);
            FillRect(texture, 10, 26, 12, 2, stone);
            FillRect(texture, 12, 29, 8, 3, outline);
            FillRect(texture, 13, 30, 6, 1, stoneLight);

            if (investigated)
            {
                DrawLine(texture, 12, 17, 15, 13, rune);
                DrawLine(texture, 15, 13, 20, 21, rune);
                SetPixelSafe(texture, 13, 17, rune);
                SetPixelSafe(texture, 16, 13, rune);
            }
            else
            {
                FillRect(texture, 13, 19, 2, 3, rune);
                FillRect(texture, 15, 21, 4, 2, rune);
                FillRect(texture, 18, 17, 2, 5, rune);
                FillRect(texture, 15, 14, 4, 2, rune);
                FillRect(texture, 15, 10, 2, 2, rune);
            }

            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(3f, 2f, 26f, 31f),
                new Vector2(0.5f, 0.10f),
                PixelsPerUnit);
        }

        private static void FillRect(
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void FillEllipse(
            Texture2D texture,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            Color color)
        {
            int radiusXSquared = radiusX * radiusX;
            int radiusYSquared = radiusY * radiusY;
            int radiusProduct = radiusXSquared * radiusYSquared;
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSquared + y * y * radiusXSquared <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawLine(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    return;
                }

                int doubledError = error * 2;
                if (doubledError >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (doubledError <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }

        private static Color Rgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 255);
        }
    }
}
