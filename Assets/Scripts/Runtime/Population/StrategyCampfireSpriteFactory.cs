using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCampfireSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        public const int FrameCount = 6;

        private static readonly Sprite[] CachedFrames = new Sprite[FrameCount];

        public static Sprite GetFrame(int frame)
        {
            int normalizedFrame = NormalizeFrame(frame);
            if (CachedFrames[normalizedFrame] == null)
            {
                CachedFrames[normalizedFrame] = CreateFrame(normalizedFrame);
            }

            return CachedFrames[normalizedFrame];
        }

        private static Sprite CreateFrame(int frame)
        {
            Texture2D texture = new Texture2D(36, 34, TextureFormat.RGBA32, false)
            {
                name = $"Campfire Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[36 * 34]);

            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stone = Rgb(91, 86, 78);
            Color stoneLight = Rgb(139, 131, 115);
            Color stoneDark = Rgb(50, 47, 43);
            Color log = Rgb(105, 65, 36);
            Color logLight = Rgb(154, 98, 49);
            Color ember = Rgb(210, 67, 32);
            Color flameOuter = Rgb(211, 79, 28);
            Color flameMid = Rgb(245, 139, 38);
            Color flameInner = Rgb(255, 223, 96);
            Color flameCore = Rgb(255, 244, 178);

            FillEllipse(texture, 18, 6, 13, 4, shadow);
            DrawStoneRing(texture, frame, stone, stoneLight, stoneDark);
            DrawLogs(texture, log, logLight, stoneDark, ember);
            DrawFlame(texture, frame, flameOuter, flameMid, flameInner, flameCore);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 36f, 34f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static void DrawStoneRing(Texture2D texture, int frame, Color stone, Color stoneLight, Color stoneDark)
        {
            FillEllipse(texture, 18, 8, 12, 4, stoneDark);
            FillEllipse(texture, 18, 9, 10, 3, stone);

            int offset = frame % 2;
            DrawStone(texture, 8, 8 + offset, 3, 2, stone, stoneLight);
            DrawStone(texture, 13, 6, 3, 2, stoneDark, stone);
            DrawStone(texture, 19, 6 + offset, 4, 2, stone, stoneLight);
            DrawStone(texture, 25, 8, 3, 2, stoneDark, stone);
            DrawStone(texture, 10, 11, 4, 2, stone, stoneLight);
            DrawStone(texture, 18, 12, 4, 2, stoneDark, stone);
            DrawStone(texture, 26, 11 + offset, 3, 2, stone, stoneLight);
        }

        private static void DrawStone(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color baseColor, Color light)
        {
            FillEllipse(texture, centerX, centerY, radiusX, radiusY, baseColor);
            SetPixelSafe(texture, centerX - 1, centerY + 1, light);
            SetPixelSafe(texture, centerX, centerY + 1, light);
        }

        private static void DrawLogs(Texture2D texture, Color log, Color logLight, Color outline, Color ember)
        {
            DrawThickLine(texture, P(9, 8), P(26, 13), outline, 2);
            DrawThickLine(texture, P(9, 9), P(26, 14), log, 1);
            DrawLine(texture, P(12, 10), P(23, 13), logLight);

            DrawThickLine(texture, P(27, 8), P(10, 13), outline, 2);
            DrawThickLine(texture, P(27, 9), P(10, 14), log, 1);
            DrawLine(texture, P(24, 10), P(13, 13), logLight);

            FillRect(texture, 16, 9, 5, 3, ember);
            FillRect(texture, 18, 11, 2, 1, logLight);
        }

        private static void DrawFlame(
            Texture2D texture,
            int frame,
            Color outer,
            Color mid,
            Color inner,
            Color core)
        {
            int sway = frame switch
            {
                1 => 1,
                2 => 2,
                3 => 0,
                4 => -2,
                5 => -1,
                _ => 0
            };
            int height = frame switch
            {
                1 => 3,
                2 => 1,
                3 => 4,
                4 => 2,
                5 => 5,
                _ => 0
            };
            int split = frame % 3;

            Vector2Int[] outerFlame =
            {
                P(12 + sway, 10),
                P(10 + sway, 18 + split),
                P(14 + sway, 24 + height),
                P(18 + sway, 31 - split),
                P(23 + sway, 23 + height / 2),
                P(25 + sway, 16 + split),
                P(22 + sway, 10)
            };
            FillPolygon(texture, outerFlame, outer);

            Vector2Int[] midFlame =
            {
                P(14 + sway, 11),
                P(13 + sway, 18 + split),
                P(17 + sway, 27 + height / 2),
                P(21 + sway, 19 + height / 2),
                P(22 + sway, 11)
            };
            FillPolygon(texture, midFlame, mid);

            Vector2Int[] innerFlame =
            {
                P(16 + sway, 11),
                P(15 + sway, 17 + split),
                P(18 + sway, 24 + height / 2),
                P(20 + sway, 16 + split),
                P(20 + sway, 11)
            };
            FillPolygon(texture, innerFlame, inner);

            Vector2Int[] coreFlame =
            {
                P(17 + sway, 11),
                P(17 + sway, 16 + split),
                P(19 + sway, 20 + height / 3),
                P(20 + sway, 14 + split),
                P(19 + sway, 11)
            };
            FillPolygon(texture, coreFlame, core);
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

            for (int y = minY; y <= maxY; y++)
            {
                int nodeCount = 0;
                int[] nodes = new int[points.Length];
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
                    nodes[nodeCount] = Mathf.RoundToInt(a.x + t * (b.x - a.x));
                    nodeCount++;
                }

                System.Array.Sort(nodes, 0, nodeCount);
                for (int i = 0; i + 1 < nodeCount; i += 2)
                {
                    for (int x = nodes[i]; x <= nodes[i + 1]; x++)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

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

        private static int NormalizeFrame(int frame)
        {
            int normalized = frame % FrameCount;
            return normalized < 0 ? normalized + FrameCount : normalized;
        }
    }
}
