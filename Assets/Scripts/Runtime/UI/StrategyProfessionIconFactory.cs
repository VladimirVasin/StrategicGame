using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyProfessionIconFactory
    {
        private const int Size = 32;

        private static readonly Dictionary<StrategyProfessionType, Sprite> Cache = new();

        public static Sprite GetIcon(StrategyProfessionType type)
        {
            if (Cache.TryGetValue(type, out Sprite sprite))
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "ProfessionIcon_" + type
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels32(pixels);
            DrawIcon(texture, type);
            texture.Apply(false, true);

            sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 24f);
            sprite.name = texture.name;
            Cache[type] = sprite;
            return sprite;
        }

        private static void DrawIcon(Texture2D texture, StrategyProfessionType type)
        {
            Color32 shadow = new Color32(26, 22, 18, 190);
            Color32 wood = new Color32(139, 91, 45, 255);
            Color32 brightWood = new Color32(197, 141, 70, 255);
            Color32 metal = new Color32(174, 186, 179, 255);
            Color32 darkMetal = new Color32(83, 96, 96, 255);
            Color32 cloth = new Color32(176, 126, 69, 255);
            Color32 leaf = new Color32(91, 144, 76, 255);
            Color32 water = new Color32(72, 142, 171, 255);
            Color32 amber = new Color32(222, 173, 74, 255);
            Color32 rust = new Color32(177, 82, 39, 255);

            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    DrawLine(texture, 9, 24, 23, 9, shadow, 3);
                    DrawLine(texture, 9, 24, 23, 9, wood, 2);
                    FillRect(texture, 20, 7, 8, 5, metal);
                    FillRect(texture, 23, 12, 4, 4, darkMetal);
                    break;
                case StrategyProfessionType.Stonecutter:
                    DrawLine(texture, 10, 23, 22, 10, shadow, 3);
                    DrawLine(texture, 10, 23, 22, 10, wood, 2);
                    DrawLine(texture, 9, 9, 25, 13, darkMetal, 3);
                    DrawLine(texture, 10, 8, 24, 12, metal, 2);
                    break;
                case StrategyProfessionType.Miner:
                    DrawLine(texture, 9, 24, 21, 11, shadow, 3);
                    DrawLine(texture, 9, 24, 21, 11, wood, 2);
                    DrawLine(texture, 8, 10, 25, 13, darkMetal, 3);
                    DrawLine(texture, 9, 9, 24, 12, metal, 2);
                    FillRect(texture, 19, 18, 7, 5, rust);
                    FillRect(texture, 22, 17, 3, 2, metal);
                    break;
                case StrategyProfessionType.CoalMiner:
                    DrawLine(texture, 9, 24, 21, 11, shadow, 3);
                    DrawLine(texture, 9, 24, 21, 11, wood, 2);
                    DrawLine(texture, 8, 10, 25, 13, darkMetal, 3);
                    DrawLine(texture, 9, 9, 24, 12, metal, 2);
                    FillRect(texture, 18, 18, 9, 6, new Color32(28, 32, 35, 255));
                    FillRect(texture, 22, 17, 3, 2, new Color32(104, 118, 124, 255));
                    break;
                case StrategyProfessionType.Sawyer:
                    FillRect(texture, 7, 19, 18, 4, wood);
                    FillRect(texture, 8, 20, 16, 1, brightWood);
                    DrawLine(texture, 8, 26, 25, 9, shadow, 3);
                    DrawLine(texture, 8, 26, 25, 9, metal, 2);
                    DrawLine(texture, 13, 16, 26, 25, darkMetal, 1);
                    break;
                case StrategyProfessionType.Hunter:
                    DrawArc(texture, 10, 16, 9, new Color32(126, 82, 43, 255));
                    DrawLine(texture, 12, 7, 12, 25, amber, 1);
                    DrawLine(texture, 7, 16, 25, 16, metal, 1);
                    FillRect(texture, 23, 14, 4, 4, darkMetal);
                    break;
                case StrategyProfessionType.Fisher:
                    DrawLine(texture, 11, 7, 23, 22, brightWood, 2);
                    DrawLine(texture, 23, 22, 20, 27, metal, 1);
                    DrawLine(texture, 5, 24, 13, 22, water, 2);
                    DrawLine(texture, 15, 24, 27, 22, water, 2);
                    break;
                case StrategyProfessionType.StorageWorker:
                    FillRect(texture, 7, 11, 18, 14, shadow);
                    FillRect(texture, 6, 10, 18, 14, wood);
                    DrawLine(texture, 6, 15, 23, 15, brightWood, 1);
                    DrawLine(texture, 14, 10, 14, 23, brightWood, 1);
                    FillRect(texture, 9, 7, 10, 5, cloth);
                    break;
                case StrategyProfessionType.Builder:
                    DrawLine(texture, 10, 24, 21, 13, wood, 3);
                    FillRect(texture, 16, 8, 12, 6, metal);
                    FillRect(texture, 20, 14, 5, 5, darkMetal);
                    FillRect(texture, 6, 25, 20, 3, amber);
                    break;
                case StrategyProfessionType.GranaryWorker:
                    FillRect(texture, 10, 10, 12, 16, shadow);
                    FillRect(texture, 9, 9, 12, 16, cloth);
                    FillRect(texture, 12, 7, 6, 4, amber);
                    DrawLine(texture, 7, 24, 24, 20, amber, 2);
                    FillRect(texture, 21, 15, 5, 7, leaf);
                    break;
            }
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixel(texture, px, py, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int width)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                FillRect(texture, x0 - width / 2, y0 - width / 2, width, width, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int error2 = 2 * error;
                if (error2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (error2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void DrawArc(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int x = Mathf.RoundToInt(Mathf.Sqrt(radius * radius - y * y));
                SetPixel(texture, centerX + x, centerY + y, color);
                SetPixel(texture, centerX + x + 1, centerY + y, color);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
    }
}
