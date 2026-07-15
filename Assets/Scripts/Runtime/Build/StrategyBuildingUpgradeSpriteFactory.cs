using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingUpgradeSpriteFactory
    {
        public const int AnimationFrameCount = 6;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetCaches()
        {
            CachedSprites.Clear();
            StrategyChickenCoopVisualProfile.ResetCache();
        }

        public static Sprite GetSprite(StrategyBuildingUpgradeType type)
        {
            return GetAnimatedSprite(type, 0);
        }

        public static Sprite GetAnimatedSprite(StrategyBuildingUpgradeType type, int frame)
        {
            if (type == StrategyBuildingUpgradeType.ChickenCoop
                && StrategyChickenCoopVisualProfile.TryGetAuthoredUpgradeSprite(frame, out Sprite authored))
            {
                return authored;
            }

            return GetProceduralAnimatedSprite(type, frame);
        }

        internal static Sprite GetProceduralAnimatedSprite(StrategyBuildingUpgradeType type, int frame)
        {
            int cacheKey = GetCacheKey(type, frame);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = type == StrategyBuildingUpgradeType.GardenBeds
                    ? CreateGardenBedsSprite(NormalizeFrame(frame))
                    : CreateChickenCoopSprite(NormalizeFrame(frame));
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateGardenBedsSprite(int frame)
        {
            Texture2D texture = CreateTexture(72, 40, $"Garden Beds Sprite {frame + 1}");
            Color outline = Rgb(49, 35, 25);
            Color earth = Rgb(104, 68, 38);
            Color earthLight = Rgb(146, 91, 48);
            Color sprout = Rgb(66, 142, 61);
            Color sproutLight = Rgb(110, 178, 74);
            Color matureLeaf = Rgb(139, 196, 86);
            Color crop = Rgb(224, 177, 76);
            int growth = Mathf.Clamp(frame, 0, AnimationFrameCount - 1);

            FillEllipse(texture, 36, 10, 31, 7, new Color(0f, 0f, 0f, 0.20f));
            for (int row = 0; row < 3; row++)
            {
                int y = 12 + row * 7;
                FillRect(texture, 8, y, 56, 5, earth);
                DrawRectOutline(texture, 8, y, 56, 5, outline);
                DrawLine(texture, P(10, y + 3), P(62, y + 3), earthLight);

                for (int x = 14; x <= 58; x += 9)
                {
                    if (growth <= 0)
                    {
                        continue;
                    }

                    int stemHeight = growth >= 3 ? 4 : growth >= 2 ? 3 : 1;
                    FillRect(texture, x, y + 5, 1, stemHeight, sprout);
                    if (growth >= 2)
                    {
                        int leafY = y + 6 + Mathf.Min(2, growth - 2);
                        SetPixelSafe(texture, x - 1, leafY, sproutLight);
                        SetPixelSafe(texture, x + 1, leafY, sproutLight);
                    }

                    if (growth >= 3)
                    {
                        int leafY = y + 8;
                        SetPixelSafe(texture, x - 2, leafY, matureLeaf);
                        SetPixelSafe(texture, x + 2, leafY, matureLeaf);
                    }

                    if (growth >= 4)
                    {
                        int cropY = y + 9;
                        if (growth >= 5)
                        {
                            FillEllipse(texture, x, cropY, 2, 1, crop);
                            SetPixelSafe(texture, x, cropY + 1, Rgb(246, 207, 98));
                        }
                        else
                        {
                            SetPixelSafe(texture, x, cropY, crop);
                        }
                    }
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 5f, 66f, 31f), new Vector2(0.5f, 0.12f), 36f);
        }

        private static Sprite CreateChickenCoopSprite(int frame)
        {
            Texture2D texture = CreateTexture(56, 56, $"Chicken Coop Sprite {frame + 1}");
            Color outline = Rgb(48, 32, 25);
            Color wood = Rgb(136, 82, 43);
            Color woodLight = Rgb(182, 119, 58);
            Color roof = Rgb(116, 54, 37);
            Color roofLight = Rgb(166, 79, 49);
            Color straw = Rgb(198, 152, 66);
            Color dark = Rgb(58, 43, 35);

            FillEllipse(texture, 28, 9, 19, 5, new Color(0f, 0f, 0f, 0.20f));

            Vector2Int[] body = { P(12, 13), P(41, 13), P(41, 34), P(12, 34) };
            FillPolygon(texture, body, wood);
            DrawPolygon(texture, body, outline);
            DrawLine(texture, P(16, 15), P(16, 32), woodLight);
            DrawLine(texture, P(25, 15), P(25, 32), woodLight);
            DrawLine(texture, P(34, 15), P(34, 32), woodLight);

            Vector2Int[] roofShape = { P(8, 34), P(27, 48), P(45, 34), P(40, 29), P(13, 29) };
            FillPolygon(texture, roofShape, roof);
            DrawPolygon(texture, roofShape, outline);
            DrawLine(texture, P(12, 34), P(27, 45), roofLight);
            DrawLine(texture, P(18, 31), P(38, 31), roofLight);

            bool doorOpen = frame >= 2 && frame <= 4;
            FillRect(texture, 23, 13, 9, 12, dark);
            DrawRectOutline(texture, 23, 13, 9, 12, outline);
            if (doorOpen)
            {
                FillRect(texture, 20, 13, 4, 12, woodLight);
                DrawRectOutline(texture, 20, 13, 4, 12, outline);
            }

            int strawShift = frame >= 2 && frame <= 4 ? 1 : 0;
            FillRect(texture, 36 + strawShift, 16, 4, 9, straw);
            FillRect(texture, 37 + strawShift, 15, 2, 12, outline);
            FillRect(texture, 10, 10, 34, 2, outline);
            if (frame >= 3)
            {
                int eggRadiusX = frame >= 5 ? 3 : frame >= 4 ? 2 : 1;
                int eggRadiusY = frame >= 5 ? 2 : 1;
                FillEllipse(texture, 18, 11, eggRadiusX, eggRadiusY, Rgb(239, 226, 185));
                if (frame >= 4)
                {
                    SetPixelSafe(texture, 17, 12, Rgb(255, 244, 210));
                }

                if (frame >= 5)
                {
                    SetPixelSafe(texture, 15, 14, Rgb(255, 244, 210));
                    SetPixelSafe(texture, 22, 13, Rgb(255, 244, 210));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 6f, 46f, 46f), new Vector2(0.5f, 0.12f), 42f);
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
            DrawLine(texture, P(x, y), P(x + width - 1, y), color);
            DrawLine(texture, P(x, y + height - 1), P(x + width - 1, y + height - 1), color);
            DrawLine(texture, P(x, y), P(x, y + height - 1), color);
            DrawLine(texture, P(x + width - 1, y), P(x + width - 1, y + height - 1), color);
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

        private static void FillPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            int minY = points[0].y;
            int maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            List<int> nodes = new();
            for (int y = minY; y <= maxY; y++)
            {
                nodes.Clear();
                float scanY = y + 0.5f;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2Int a = points[i];
                    Vector2Int b = points[j];
                    bool crosses = (a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY);
                    if (!crosses)
                    {
                        continue;
                    }

                    float t = (scanY - a.y) / (b.y - a.y);
                    nodes.Add(Mathf.RoundToInt(a.x + t * (b.x - a.x)));
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    for (int x = nodes[i]; x <= nodes[i + 1]; x++)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                DrawLine(texture, points[i], points[(i + 1) % points.Length], color);
            }
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
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
                SetPixelSafe(texture, x, y, color);
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

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static int GetCacheKey(StrategyBuildingUpgradeType type, int frame)
        {
            return ((int)type * 32) + NormalizeFrame(frame);
        }

        private static int NormalizeFrame(int frame)
        {
            int normalized = frame % AnimationFrameCount;
            return normalized < 0 ? normalized + AnimationFrameCount : normalized;
        }
    }
}
