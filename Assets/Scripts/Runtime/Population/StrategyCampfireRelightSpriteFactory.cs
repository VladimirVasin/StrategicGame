using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCampfireRelightSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        public const int RelightFrameCount = 6;
        public const int EmberFrameCount = 4;

        private static readonly Sprite[] RelightFrames = new Sprite[RelightFrameCount];
        private static readonly Sprite[] EmberFrames = new Sprite[EmberFrameCount];
        private static readonly Sprite[] BaseFrames = new Sprite[EmberFrameCount];
        private static readonly Sprite[] RelightFlameFrames = new Sprite[RelightFrameCount];
        private static readonly Sprite[] EmberFlameFrames = new Sprite[EmberFrameCount];

        public static Sprite GetRelightFrame(int frame)
        {
            int normalized = Normalize(frame, RelightFrameCount);
            if (RelightFrames[normalized] == null)
            {
                RelightFrames[normalized] = CreateFrame(
                    normalized,
                    true,
                    true,
                    true,
                    $"Campfire Relight {normalized + 1}");
            }

            return RelightFrames[normalized];
        }

        public static Sprite GetEmberFrame(int frame)
        {
            int normalized = Normalize(frame, EmberFrameCount);
            if (EmberFrames[normalized] == null)
            {
                EmberFrames[normalized] = CreateFrame(
                    normalized,
                    false,
                    true,
                    true,
                    $"Campfire Embers {normalized + 1}");
            }

            return EmberFrames[normalized];
        }

        public static Sprite GetBaseFrame(int frame)
        {
            int normalized = Normalize(frame, EmberFrameCount);
            if (BaseFrames[normalized] == null)
            {
                BaseFrames[normalized] = CreateFrame(
                    normalized,
                    false,
                    true,
                    false,
                    $"Campfire Charred Base {normalized + 1}");
            }

            return BaseFrames[normalized];
        }

        public static Sprite GetRelightFlameFrame(int frame)
        {
            int normalized = Normalize(frame, RelightFrameCount);
            if (RelightFlameFrames[normalized] == null)
            {
                RelightFlameFrames[normalized] = CreateFrame(
                    normalized,
                    true,
                    false,
                    true,
                    $"Campfire Relight Flame {normalized + 1}");
            }

            return RelightFlameFrames[normalized];
        }

        public static Sprite GetEmberFlameFrame(int frame)
        {
            int normalized = Normalize(frame, EmberFrameCount);
            if (EmberFlameFrames[normalized] == null)
            {
                EmberFlameFrames[normalized] = CreateFrame(
                    normalized,
                    false,
                    false,
                    true,
                    $"Campfire Ember Flame {normalized + 1}");
            }

            return EmberFlameFrames[normalized];
        }

        private static Sprite CreateFrame(int frame, bool relighting, bool drawBase, bool drawEffect, string spriteName)
        {
            Texture2D texture = new Texture2D(36, 34, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[36 * 34]);

            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color stone = Rgb(76, 72, 66);
            Color stoneLight = Rgb(112, 105, 92);
            Color stoneDark = Rgb(36, 34, 31);
            Color coal = Rgb(35, 28, 24);
            Color ember = Rgb(166, 46, 25);
            Color emberHot = Rgb(238, 96, 34);
            Color flame = Rgb(226, 91, 26);
            Color flameHot = Rgb(255, 207, 82);

            if (drawBase)
            {
                FillEllipse(texture, 18, 6, 13, 4, shadow);
                DrawStoneRing(texture, frame, stone, stoneLight, stoneDark);
                DrawCharredLogs(texture, coal, ember, frame, drawEffect);
            }

            if (drawEffect && relighting)
            {
                DrawRelightFlame(texture, frame, flame, flameHot);
            }
            else if (drawEffect)
            {
                DrawEmbers(texture, frame, ember, emberHot);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 36f, 34f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static void DrawStoneRing(Texture2D texture, int frame, Color stone, Color light, Color dark)
        {
            FillEllipse(texture, 18, 8, 12, 4, dark);
            FillEllipse(texture, 18, 9, 10, 3, stone);
            int offset = frame % 2;
            DrawStone(texture, 8, 8 + offset, 3, 2, stone, light);
            DrawStone(texture, 13, 6, 3, 2, dark, stone);
            DrawStone(texture, 19, 6 + offset, 4, 2, stone, light);
            DrawStone(texture, 25, 8, 3, 2, dark, stone);
            DrawStone(texture, 10, 11, 4, 2, stone, light);
            DrawStone(texture, 18, 12, 4, 2, dark, stone);
            DrawStone(texture, 26, 11 + offset, 3, 2, stone, light);
        }

        private static void DrawStone(Texture2D texture, int x, int y, int rx, int ry, Color color, Color light)
        {
            FillEllipse(texture, x, y, rx, ry, color);
            SetPixelSafe(texture, x - 1, y + 1, light);
            SetPixelSafe(texture, x, y + 1, light);
        }

        private static void DrawCharredLogs(Texture2D texture, Color coal, Color ember, int frame, bool drawEmbers)
        {
            DrawThickLine(texture, P(9, 9), P(26, 13), coal, 1);
            DrawThickLine(texture, P(26, 9), P(10, 13), coal, 1);
            FillRect(texture, 15, 9, 7, 2, coal);
            if (drawEmbers && frame % 2 == 0)
            {
                SetPixelSafe(texture, 16, 10, ember);
                SetPixelSafe(texture, 20, 11, ember);
            }
        }

        private static void DrawEmbers(Texture2D texture, int frame, Color ember, Color hot)
        {
            for (int i = 0; i < 6; i++)
            {
                int x = 13 + PositiveModulo(frame * 3 + i * 5, 11);
                int y = 10 + PositiveModulo(frame + i * 2, 4);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? hot : ember);
            }
        }

        private static void DrawRelightFlame(Texture2D texture, int frame, Color flame, Color hot)
        {
            int height = 6 + frame * 3;
            int sway = frame % 2 == 0 ? -1 : 1;
            Vector2Int[] outer =
            {
                P(14 + sway, 10),
                P(13 + sway, 14 + frame),
                P(18 + sway, 12 + height),
                P(23 + sway, 14 + frame),
                P(22 + sway, 10)
            };
            FillPolygon(texture, outer, flame);
            Vector2Int[] inner =
            {
                P(17 + sway, 10),
                P(16 + sway, 13 + frame),
                P(18 + sway, 10 + height),
                P(20 + sway, 13 + frame),
                P(20 + sway, 10)
            };
            FillPolygon(texture, inner, hot);
            for (int i = 0; i < frame + 2; i++)
            {
                SetPixelSafe(texture, 13 + PositiveModulo(frame * 4 + i * 7, 12), 21 + i, hot);
            }
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

        private static void FillEllipse(Texture2D texture, int x, int y, int rx, int ry, Color color)
        {
            int rx2 = rx * rx;
            int ry2 = ry * ry;
            int product = rx2 * ry2;
            for (int py = -ry; py <= ry; py++)
            {
                for (int px = -rx; px <= rx; px++)
                {
                    if (px * px * ry2 + py * py * rx2 <= product)
                    {
                        SetPixelSafe(texture, x + px, y + py, color);
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
                int count = 0;
                int[] nodes = new int[points.Length];
                float scanY = y + 0.5f;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2Int a = points[i];
                    Vector2Int b = points[j];
                    if ((a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY))
                    {
                        float t = (scanY - a.y) / (b.y - a.y);
                        nodes[count++] = Mathf.RoundToInt(a.x + t * (b.x - a.x));
                    }
                }

                System.Array.Sort(nodes, 0, count);
                for (int i = 0; i + 1 < count; i += 2)
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

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static int Normalize(int frame, int count)
        {
            int normalized = frame % count;
            return normalized < 0 ? normalized + count : normalized;
        }
    }
}
