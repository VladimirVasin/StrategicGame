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
                case StrategyResourceType.Berries:
                    PaintBerries(texture);
                    break;
                case StrategyResourceType.Roots:
                    PaintRoots(texture);
                    break;
                case StrategyResourceType.Mushrooms:
                    PaintMushrooms(texture);
                    break;
                case StrategyResourceType.Game:
                    PaintGame(texture);
                    break;
                case StrategyResourceType.Fish:
                    PaintFish(texture);
                    break;
                case StrategyResourceType.Iron:
                    PaintIron(texture);
                    break;
                case StrategyResourceType.Coal:
                    PaintCoal(texture);
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

        private static void PaintGame(Texture2D texture)
        {
            Color outline = Rgb(67, 37, 31);
            Color meatDark = Rgb(130, 47, 40);
            Color meat = Rgb(178, 75, 57);
            Color meatLight = Rgb(224, 127, 84);
            Color bone = Rgb(225, 209, 169);

            DrawLine(texture, 5, 8, 10, 13, bone);
            FillEllipse(texture, 5, 8, 2, 2, bone);
            FillEllipse(texture, 10, 13, 3, 2, bone);
            FillEllipse(texture, 14, 12, 8, 6, outline);
            FillEllipse(texture, 14, 12, 7, 5, meatDark);
            FillEllipse(texture, 15, 13, 5, 4, meat);
            FillEllipse(texture, 17, 14, 2, 2, meatLight);
            SetPixelSafe(texture, 9, 14, outline);
            SetPixelSafe(texture, 18, 9, outline);
        }

        private static void PaintBerries(Texture2D texture)
        {
            Color outline = Rgb(55, 41, 65);
            Color leaf = Rgb(72, 139, 70);
            Color leafLight = Rgb(119, 177, 82);
            Color berryDark = Rgb(106, 38, 92);
            Color berry = Rgb(180, 52, 89);
            Color berryLight = Rgb(231, 113, 129);

            DrawLine(texture, 12, 4, 12, 19, Rgb(74, 89, 48));
            DrawLine(texture, 12, 12, 6, 17, leaf);
            DrawLine(texture, 12, 11, 18, 17, leafLight);
            FillEllipse(texture, 8, 10, 4, 4, outline);
            FillEllipse(texture, 8, 10, 3, 3, berryDark);
            FillEllipse(texture, 13, 8, 4, 4, outline);
            FillEllipse(texture, 13, 8, 3, 3, berry);
            FillEllipse(texture, 16, 13, 4, 4, outline);
            FillEllipse(texture, 16, 13, 3, 3, berry);
            FillEllipse(texture, 11, 15, 4, 4, outline);
            FillEllipse(texture, 11, 15, 3, 3, berryDark);
            SetPixelSafe(texture, 14, 10, berryLight);
            SetPixelSafe(texture, 17, 14, berryLight);
        }

        private static void PaintRoots(Texture2D texture)
        {
            Color outline = Rgb(67, 49, 33);
            Color soil = Rgb(113, 80, 52);
            Color root = Rgb(171, 113, 66);
            Color rootLight = Rgb(207, 150, 88);
            Color leaf = Rgb(80, 151, 68);

            FillEllipse(texture, 12, 8, 8, 3, soil);
            DrawLine(texture, 10, 10, 6, 18, outline);
            DrawLine(texture, 10, 10, 7, 18, root);
            DrawLine(texture, 13, 10, 18, 19, outline);
            DrawLine(texture, 13, 10, 17, 19, rootLight);
            DrawLine(texture, 12, 10, 12, 21, outline);
            DrawLine(texture, 12, 10, 13, 20, root);
            DrawLine(texture, 12, 11, 8, 5, leaf);
            DrawLine(texture, 13, 11, 15, 4, leaf);
            DrawLine(texture, 14, 11, 19, 6, leaf);
            SetPixelSafe(texture, 9, 15, outline);
            SetPixelSafe(texture, 15, 16, outline);
        }

        private static void PaintMushrooms(Texture2D texture)
        {
            Color outline = Rgb(72, 48, 40);
            Color stem = Rgb(226, 207, 170);
            Color cap = Rgb(186, 72, 58);
            Color capLight = Rgb(229, 157, 101);
            Color tanCap = Rgb(176, 139, 87);

            FillRect(texture, 8, 6, 3, 8, outline);
            FillRect(texture, 9, 6, 1, 8, stem);
            FillEllipse(texture, 9, 15, 6, 4, outline);
            FillEllipse(texture, 9, 15, 5, 3, cap);
            SetPixelSafe(texture, 7, 16, stem);
            SetPixelSafe(texture, 11, 15, stem);
            FillRect(texture, 15, 5, 3, 7, outline);
            FillRect(texture, 16, 5, 1, 7, stem);
            FillEllipse(texture, 16, 13, 5, 3, outline);
            FillEllipse(texture, 16, 13, 4, 2, tanCap);
            FillEllipse(texture, 18, 14, 2, 1, capLight);
            DrawLine(texture, 5, 4, 20, 4, Rgb(72, 121, 66));
        }

        private static void PaintFish(Texture2D texture)
        {
            Color outline = Rgb(34, 65, 76);
            Color fishDark = Rgb(58, 118, 139);
            Color fish = Rgb(86, 157, 178);
            Color fishLight = Rgb(148, 210, 219);
            Color fin = Rgb(224, 154, 75);
            Color water = new Color(0.50f, 0.76f, 0.86f, 0.55f);

            DrawLine(texture, 3, 7, 8, 7, water);
            DrawLine(texture, 16, 18, 21, 18, water);
            FillEllipse(texture, 12, 12, 8, 5, outline);
            FillEllipse(texture, 12, 12, 7, 4, fishDark);
            FillEllipse(texture, 14, 13, 5, 3, fish);
            FillEllipse(texture, 17, 13, 2, 2, fishLight);
            FillTriangle(texture, 5, 12, 1, 7, 1, 17, outline);
            FillTriangle(texture, 5, 12, 2, 8, 2, 16, fin);
            FillTriangle(texture, 10, 16, 14, 21, 16, 16, outline);
            FillTriangle(texture, 11, 16, 14, 19, 15, 16, fin);
            SetPixelSafe(texture, 18, 14, outline);
            DrawLine(texture, 15, 16, 18, 10, outline);
        }

        private static void PaintIron(Texture2D texture)
        {
            Color outline = Rgb(47, 39, 35);
            Color oreDark = Rgb(69, 62, 58);
            Color ore = Rgb(100, 90, 82);
            Color rust = Rgb(154, 76, 38);
            Color rustLight = Rgb(203, 110, 52);
            Color shine = Rgb(194, 188, 168);

            FillEllipse(texture, 12, 11, 8, 6, outline);
            FillEllipse(texture, 12, 11, 7, 5, oreDark);
            FillEllipse(texture, 14, 12, 5, 4, ore);
            DrawLine(texture, 6, 12, 18, 8, rust);
            DrawLine(texture, 7, 14, 18, 16, rustLight);
            FillRect(texture, 9, 9, 3, 2, shine);
            SetPixelSafe(texture, 16, 11, shine);
            SetPixelSafe(texture, 13, 15, rustLight);
            SetPixelSafe(texture, 8, 13, rust);
        }

        private static void PaintCoal(Texture2D texture)
        {
            Color outline = Rgb(22, 24, 27);
            Color coalDark = Rgb(34, 38, 42);
            Color coal = Rgb(52, 57, 61);
            Color blue = Rgb(73, 86, 96);
            Color shine = Rgb(132, 148, 154);

            FillEllipse(texture, 12, 11, 8, 6, outline);
            FillEllipse(texture, 12, 11, 7, 5, coalDark);
            FillEllipse(texture, 14, 12, 5, 4, coal);
            FillTriangle(texture, 7, 11, 12, 6, 17, 12, blue);
            FillTriangle(texture, 8, 14, 13, 10, 19, 15, coal);
            DrawLine(texture, 7, 15, 17, 7, outline);
            FillRect(texture, 10, 9, 3, 2, shine);
            SetPixelSafe(texture, 16, 12, shine);
            SetPixelSafe(texture, 13, 15, blue);
            SetPixelSafe(texture, 8, 13, coalDark);
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

        private static void FillTriangle(Texture2D texture, int x0, int y0, int x1, int y1, int x2, int y2, Color color)
        {
            int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
            int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
            int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            float area = Edge(x0, y0, x1, y1, x2, y2);
            if (Mathf.Approximately(area, 0f))
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(x1, y1, x2, y2, x, y);
                    float w1 = Edge(x2, y2, x0, y0, x, y);
                    float w2 = Edge(x0, y0, x1, y1, x, y);
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static float Edge(int ax, int ay, int bx, int by, int cx, int cy)
        {
            return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
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
