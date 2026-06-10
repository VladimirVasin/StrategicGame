using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyResourceIconFactory
    {
        private const float PixelsPerUnit = 24f;
        private static readonly Dictionary<StrategyResourceType, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyResourceType type)
        {
            if (!CachedSprites.TryGetValue(type, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(type);
                CachedSprites[type] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyResourceType type)
        {
            Texture2D texture = new Texture2D(24, 24, TextureFormat.RGBA32, false)
            {
                name = type + " Resource Icon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[24 * 24]);

            switch (type)
            {
                case StrategyResourceType.Eggs:
                    PaintEggs(texture);
                    break;
                case StrategyResourceType.Turnip:
                    PaintTurnip(texture);
                    break;
                case StrategyResourceType.Cabbage:
                    PaintCabbage(texture);
                    break;
                case StrategyResourceType.Onion:
                    PaintOnion(texture);
                    break;
                case StrategyResourceType.Carrot:
                    PaintCarrot(texture);
                    break;
                case StrategyResourceType.Potato:
                    PaintPotato(texture);
                    break;
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

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

        private static void PaintTurnip(Texture2D texture)
        {
            Color outline = Rgb(61, 47, 45);
            Color leaf = Rgb(74, 151, 68);
            Color leafLight = Rgb(120, 184, 76);
            Color bulb = Rgb(232, 218, 192);
            Color purple = Rgb(154, 86, 151);

            FillRect(texture, 10, 16, 2, 5, leaf);
            DrawLine(texture, 11, 18, 7, 21, leafLight);
            DrawLine(texture, 12, 18, 17, 21, leaf);
            FillEllipse(texture, 12, 10, 7, 7, outline);
            FillEllipse(texture, 12, 10, 6, 6, bulb);
            FillRect(texture, 7, 11, 10, 4, purple);
            SetPixelSafe(texture, 12, 3, outline);
            SetPixelSafe(texture, 11, 2, outline);
        }

        private static void PaintCabbage(Texture2D texture)
        {
            Color outline = Rgb(36, 83, 42);
            Color leafDark = Rgb(66, 137, 59);
            Color leaf = Rgb(102, 171, 77);
            Color leafLight = Rgb(153, 202, 94);

            FillEllipse(texture, 12, 11, 8, 8, outline);
            FillEllipse(texture, 12, 11, 7, 7, leaf);
            FillEllipse(texture, 9, 12, 4, 5, leafDark);
            FillEllipse(texture, 15, 12, 4, 5, leafLight);
            DrawLine(texture, 6, 12, 18, 10, outline);
            DrawLine(texture, 9, 6, 14, 17, outline);
            SetPixelSafe(texture, 12, 12, leafLight);
        }

        private static void PaintOnion(Texture2D texture)
        {
            Color outline = Rgb(72, 49, 34);
            Color onion = Rgb(210, 149, 58);
            Color onionLight = Rgb(235, 187, 83);
            Color dry = Rgb(122, 78, 39);

            FillEllipse(texture, 12, 10, 7, 8, outline);
            FillEllipse(texture, 12, 10, 6, 7, onion);
            FillEllipse(texture, 14, 11, 3, 5, onionLight);
            DrawLine(texture, 12, 3, 12, 17, dry);
            DrawLine(texture, 9, 5, 8, 15, dry);
            DrawLine(texture, 15, 5, 16, 15, dry);
            DrawLine(texture, 12, 17, 9, 21, dry);
            DrawLine(texture, 12, 17, 15, 21, dry);
        }

        private static void PaintCarrot(Texture2D texture)
        {
            Color outline = Rgb(87, 52, 30);
            Color carrot = Rgb(220, 105, 39);
            Color carrotLight = Rgb(243, 145, 53);
            Color leaf = Rgb(70, 149, 64);

            DrawLine(texture, 12, 4, 6, 16, outline);
            DrawLine(texture, 12, 4, 18, 16, outline);
            DrawLine(texture, 6, 16, 18, 16, outline);
            for (int y = 5; y <= 15; y++)
            {
                int half = Mathf.Max(1, (y - 4) / 2);
                FillRect(texture, 12 - half, y, half * 2 + 1, 1, carrot);
            }

            DrawLine(texture, 13, 6, 11, 14, carrotLight);
            DrawLine(texture, 9, 10, 13, 10, outline);
            DrawLine(texture, 8, 14, 12, 14, outline);
            DrawLine(texture, 12, 16, 8, 21, leaf);
            DrawLine(texture, 12, 16, 12, 22, leaf);
            DrawLine(texture, 12, 16, 17, 21, leaf);
        }

        private static void PaintPotato(Texture2D texture)
        {
            Color outline = Rgb(75, 52, 34);
            Color potato = Rgb(151, 105, 63);
            Color potatoLight = Rgb(184, 133, 78);
            Color eye = Rgb(91, 63, 40);

            FillEllipse(texture, 12, 11, 8, 6, outline);
            FillEllipse(texture, 12, 11, 7, 5, potato);
            FillEllipse(texture, 14, 12, 4, 3, potatoLight);
            SetPixelSafe(texture, 8, 12, eye);
            SetPixelSafe(texture, 11, 9, eye);
            SetPixelSafe(texture, 15, 13, eye);
            SetPixelSafe(texture, 17, 10, eye);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void DrawRectOutline(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            FillRect(texture, x, y, width, 1, color);
            FillRect(texture, x, y + height - 1, width, 1, color);
            FillRect(texture, x, y, 1, height, color);
            FillRect(texture, x + width - 1, y, 1, height, color);
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int radiusXSqr = radiusX * radiusX;
            int radiusYSqr = radiusY * radiusY;
            int radiusProduct = radiusXSqr * radiusYSqr;

            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSqr + y * y * radiusXSqr <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = err * 2;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
