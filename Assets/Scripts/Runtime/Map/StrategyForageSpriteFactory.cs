using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyForageSpriteFactory
    {
        private const float NodePixelsPerUnit = 26f;
        private const float CarriedPixelsPerUnit = 30f;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetNodeSprite(StrategyResourceType resource, int variant, bool depleted)
        {
            int normalizedVariant = Mathf.Abs(variant) % 4;
            int key = ((int)resource * 32) + (depleted ? 16 : 0) + normalizedVariant;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateNodeSprite(resource, normalizedVariant, depleted);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedSprite(StrategyResourceType resource)
        {
            int key = 4096 + (int)resource;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedSprite(resource);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateNodeSprite(StrategyResourceType resource, int variant, bool depleted)
        {
            Texture2D texture = CreateTexture(40, 32, resource + " Forage Node");
            switch (resource)
            {
                case StrategyResourceType.Berries:
                    PaintBerryBush(texture, variant, depleted);
                    break;
                case StrategyResourceType.Mushrooms:
                    PaintMushrooms(texture, variant, depleted);
                    break;
                case StrategyResourceType.Roots:
                    PaintRoots(texture, variant, depleted);
                    break;
                default:
                    PaintRoots(texture, variant, depleted);
                    break;
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 36f, 25f), new Vector2(0.5f, 0.16f), NodePixelsPerUnit);
        }

        private static Sprite CreateCarriedSprite(StrategyResourceType resource)
        {
            Texture2D texture = CreateTexture(28, 24, resource + " Basket");
            Color outline = Rgb(64, 43, 29);
            Color basket = Rgb(130, 84, 42);
            Color basketLight = Rgb(185, 125, 62);

            FillRect(texture, 6, 5, 16, 7, basket);
            DrawRectOutline(texture, 6, 5, 16, 7, outline);
            DrawLine(texture, 8, 12, 20, 12, basketLight);
            DrawLine(texture, 8, 12, 13, 18, outline);
            DrawLine(texture, 20, 12, 15, 18, outline);

            switch (resource)
            {
                case StrategyResourceType.Berries:
                    FillEllipse(texture, 10, 15, 2, 2, Rgb(142, 42, 74));
                    FillEllipse(texture, 15, 16, 2, 2, Rgb(184, 54, 91));
                    FillEllipse(texture, 18, 14, 2, 2, Rgb(107, 39, 86));
                    break;
                case StrategyResourceType.Mushrooms:
                    FillEllipse(texture, 10, 15, 3, 2, Rgb(210, 82, 62));
                    FillRect(texture, 10, 10, 2, 5, Rgb(225, 203, 166));
                    FillEllipse(texture, 17, 15, 3, 2, Rgb(195, 157, 100));
                    FillRect(texture, 17, 10, 2, 5, Rgb(230, 210, 178));
                    break;
                default:
                    FillEllipse(texture, 13, 14, 5, 2, Rgb(147, 95, 55));
                    DrawLine(texture, 13, 15, 9, 20, Rgb(86, 145, 64));
                    DrawLine(texture, 14, 15, 17, 20, Rgb(105, 166, 74));
                    break;
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 3f, 24f, 19f), new Vector2(0.5f, 0.2f), CarriedPixelsPerUnit);
        }

        private static void PaintBerryBush(Texture2D texture, int variant, bool depleted)
        {
            Color outline = Rgb(38, 63, 34);
            Color leaf = depleted ? Rgb(74, 93, 59) : Rgb(58, 121, 62);
            Color leafLight = depleted ? Rgb(100, 113, 76) : Rgb(94, 158, 70);
            Color berry = depleted ? Rgb(68, 54, 64) : (variant % 2 == 0 ? Rgb(176, 48, 88) : Rgb(122, 45, 115));

            FillEllipse(texture, 18, 13, 13, 8, outline);
            FillEllipse(texture, 18, 13, 12, 7, leaf);
            FillEllipse(texture, 12, 16, 7, 5, leafLight);
            FillEllipse(texture, 24, 16, 7, 5, leaf);
            FillRect(texture, 17, 4, 4, 9, Rgb(92, 62, 38));
            FillRect(texture, 18, 4, 2, 9, Rgb(134, 84, 45));

            if (!depleted)
            {
                FillEllipse(texture, 11, 15, 2, 2, berry);
                FillEllipse(texture, 17, 19, 2, 2, berry);
                FillEllipse(texture, 24, 15, 2, 2, berry);
                FillEllipse(texture, 27, 19, 2, 2, berry);
                SetPixelSafe(texture, 12, 16, Color.white);
                SetPixelSafe(texture, 25, 16, Color.white);
            }
        }

        private static void PaintMushrooms(Texture2D texture, int variant, bool depleted)
        {
            Color outline = Rgb(76, 49, 42);
            Color stem = depleted ? Rgb(132, 119, 93) : Rgb(221, 202, 166);
            Color cap = depleted ? Rgb(116, 82, 69) : (variant % 2 == 0 ? Rgb(190, 70, 58) : Rgb(176, 139, 86));
            Color capLight = depleted ? Rgb(145, 104, 80) : Rgb(227, 169, 102);
            Color moss = Rgb(65, 111, 60);

            DrawLine(texture, 7, 7, 31, 7, moss);
            PaintMushroom(texture, 12, 10, 5, outline, cap, capLight, stem, !depleted);
            PaintMushroom(texture, 21, 12, 6, outline, cap, capLight, stem, !depleted);
            PaintMushroom(texture, 27, 9, 4, outline, cap, capLight, stem, false);
        }

        private static void PaintMushroom(
            Texture2D texture,
            int x,
            int y,
            int radius,
            Color outline,
            Color cap,
            Color capLight,
            Color stem,
            bool spots)
        {
            FillRect(texture, x - 1, y - 6, 3, 7, outline);
            FillRect(texture, x, y - 6, 1, 7, stem);
            FillEllipse(texture, x, y, radius, radius / 2 + 1, outline);
            FillEllipse(texture, x, y, radius - 1, radius / 2, cap);
            FillEllipse(texture, x + 1, y + 1, Mathf.Max(1, radius / 2), 1, capLight);
            if (spots)
            {
                SetPixelSafe(texture, x - 2, y + 1, Rgb(245, 223, 183));
                SetPixelSafe(texture, x + 2, y, Rgb(245, 223, 183));
            }
        }

        private static void PaintRoots(Texture2D texture, int variant, bool depleted)
        {
            Color outline = Rgb(67, 49, 33);
            Color soil = depleted ? Rgb(92, 72, 52) : Rgb(118, 87, 55);
            Color root = depleted ? Rgb(117, 91, 62) : Rgb(180, 123, 70);
            Color leaf = depleted ? Rgb(82, 102, 66) : Rgb(79, 151, 67);
            Color leafLight = depleted ? Rgb(105, 122, 78) : Rgb(126, 183, 78);

            FillEllipse(texture, 19, 8, 12, 4, outline);
            FillEllipse(texture, 19, 8, 11, 3, soil);
            FillEllipse(texture, 18, 11, 5, 3, outline);
            FillEllipse(texture, 18, 11, 4, 2, root);
            DrawLine(texture, 17, 12, 12, 17, outline);
            DrawLine(texture, 20, 12, 25, 18, outline);
            DrawLine(texture, 18, 14, 15, 20, root);
            DrawLine(texture, 21, 13, 26, 19, root);

            if (!depleted)
            {
                DrawLine(texture, 18, 14, 13, 24, leaf);
                DrawLine(texture, 19, 15, 19, 26, leafLight);
                DrawLine(texture, 20, 14, 26, 23, leaf);
            }
        }

        private static Texture2D CreateTexture(int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            return texture;
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
