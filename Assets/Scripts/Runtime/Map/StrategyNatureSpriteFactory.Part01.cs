using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyNatureSpriteFactory
    {

        private static Sprite CreateRockClusterSprite(int variant)
        {
            Texture2D texture = CreateTexture(78, 48, $"Rock Cluster {variant + 1}");
            Color outline = Rgb(44, 42, 40);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color stoneA = GetStoneColor(variant);
            Color stoneB = GetStoneColor(variant + 2);
            Color stoneC = GetStoneColor(variant + 4);

            FillEllipse(texture, 39, 9, 29, 5, shadow);
            DrawClusterRock(texture, 17, 11, 15, 18, stoneA, outline, variant);
            DrawClusterRock(texture, 34, 10, 19, 23, stoneB, outline, variant + 3);
            DrawClusterRock(texture, 55, 12, 16, 17, stoneC, outline, variant + 6);
            DrawClusterRock(texture, 28, 7, 11, 13, Shift(stoneA, 0.06f), outline, variant + 9);
            DrawClusterRock(texture, 48, 8, 12, 14, Shift(stoneB, -0.07f), outline, variant + 12);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 5f, 68f, 37f), new Vector2(0.5f, 0.14f), StonePixelsPerUnit);
        }

        private static Sprite CreateCliffSprite(int variant)
        {
            Texture2D texture = CreateTexture(112, 82, $"Stone Cliff {variant + 1}");
            Color outline = Rgb(42, 40, 38);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stone = GetStoneColor(variant + 1);
            Color dark = Shift(stone, -0.22f);
            Color midDark = Shift(stone, -0.10f);
            Color light = Shift(stone, 0.18f);
            Color moss = Rgb(76, 106, 63);

            FillEllipse(texture, 56, 10, 42, 7, shadow);

            Vector2Int[] silhouette =
            {
                P(11, 14), P(18, 42), P(31, 67), P(55, 75), P(83, 68),
                P(101, 39), P(96, 18), P(73, 10), P(47, 12), P(27, 8)
            };
            FillPolygon(texture, silhouette, dark);
            DrawPolygon(texture, silhouette, outline);

            Vector2Int[] leftFace = { P(13, 15), P(19, 40), P(31, 63), P(49, 72), P(48, 28), P(29, 9) };
            Vector2Int[] centerFace = { P(48, 28), P(50, 72), P(78, 65), P(92, 35), P(73, 11) };
            Vector2Int[] rightFace = { P(73, 11), P(92, 35), P(98, 19), P(91, 15), P(82, 12) };
            FillPolygon(texture, leftFace, midDark);
            FillPolygon(texture, centerFace, stone);
            FillPolygon(texture, rightFace, light);
            DrawPolygon(texture, leftFace, outline);
            DrawPolygon(texture, centerFace, outline);
            DrawPolygon(texture, rightFace, outline);

            DrawLine(texture, P(29, 15), P(39, 57), Shift(midDark, -0.12f));
            DrawLine(texture, P(57, 21), P(60, 69), Shift(stone, -0.18f));
            DrawLine(texture, P(80, 19), P(70, 61), Shift(stone, 0.10f));
            DrawLine(texture, P(21, 40), P(43, 45), light);
            DrawLine(texture, P(56, 51), P(82, 44), light);
            DrawLine(texture, P(32, 66), P(77, 65), Shift(dark, -0.08f));

            FillRect(texture, 30, 63, 8, 3, moss);
            FillRect(texture, 66, 64, 12, 3, moss);
            FillRect(texture, 82, 29, 7, 2, Shift(moss, 0.08f));
            AddStoneSpeckles(texture, variant, 18, 18, 78, 49, light, dark);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(6f, 5f, 100f, 72f), new Vector2(0.5f, 0.10f), StonePixelsPerUnit);
        }

        private static void DrawClusterRock(Texture2D texture, int centerX, int baseY, int radiusX, int height, Color stone, Color outline, int variant)
        {
            Color dark = Shift(stone, -0.18f);
            Color light = Shift(stone, 0.16f);
            Vector2Int[] points =
            {
                P(centerX - radiusX, baseY + 2),
                P(centerX - radiusX / 2, baseY + height - 2),
                P(centerX + variant % 3 - 1, baseY + height + 4),
                P(centerX + radiusX / 2, baseY + height),
                P(centerX + radiusX, baseY + 3),
                P(centerX + radiusX / 3, baseY)
            };
            FillPolygon(texture, points, stone);
            DrawPolygon(texture, points, outline);
            DrawLine(texture, P(centerX - radiusX / 2, baseY + 4), P(centerX, baseY + height + 1), dark);
            DrawLine(texture, P(centerX + radiusX / 3, baseY + 5), P(centerX, baseY + height + 1), light);
            AddStoneSpeckles(texture, variant, centerX - radiusX + 2, baseY + 4, radiusX * 2 - 4, height - 1, light, dark);
        }

        private static void AddStoneSpeckles(
            Texture2D texture,
            int variant,
            int startX,
            int startY,
            int width,
            int height,
            Color light,
            Color dark)
        {
            int count = Mathf.Max(6, (width * height) / 70);
            for (int i = 0; i < count; i++)
            {
                int x = startX + ((variant * 17 + i * 11) % Mathf.Max(1, width));
                int y = startY + ((variant * 13 + i * 7) % Mathf.Max(1, height));
                SetPixelSafe(texture, x, y, i % 3 == 0 ? light : dark);
                if (i % 5 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, i % 2 == 0 ? light : dark);
                }
            }
        }

        private static void DrawLogPiece(
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color dark,
            Color mid,
            Color light,
            Color cut,
            Color rings,
            Color outline)
        {
            FillRect(texture, x + 2, y, width - 3, height, dark);
            FillRect(texture, x + 3, y + 1, width - 4, Mathf.Max(1, height - 2), mid);
            DrawRectOutline(texture, x + 2, y, width - 2, height, outline);
            DrawLine(texture, P(x + 5, y + height - 2), P(x + width - 4, y + height - 2), light);
            FillEllipse(texture, x + 2, y + height / 2, 3, Mathf.Max(2, height / 2), cut);
            DrawCanopyRim(texture, x + 2, y + height / 2, 3, Mathf.Max(2, height / 2), outline);
            FillEllipse(texture, x + 2, y + height / 2, 1, 1, rings);
        }

        private static void DrawMiniTree(Texture2D texture, int baseX, int baseY, float scale, Color leaf, Color trunk, Color outline)
        {
            int trunkWidth = Mathf.Max(3, Mathf.RoundToInt(5f * scale));
            int trunkHeight = Mathf.Max(12, Mathf.RoundToInt(20f * scale));
            FillRect(texture, baseX - trunkWidth / 2, baseY, trunkWidth, trunkHeight, Shift(trunk, -0.08f));
            FillRect(texture, baseX - trunkWidth / 2 + 1, baseY, trunkWidth, trunkHeight, trunk);

            int rx = Mathf.RoundToInt(13f * scale);
            int ry = Mathf.RoundToInt(12f * scale);
            int cy = baseY + trunkHeight + Mathf.RoundToInt(9f * scale);
            FillEllipse(texture, baseX, cy, rx, ry, Shift(leaf, -0.16f));
            FillEllipse(texture, baseX - Mathf.RoundToInt(5f * scale), cy + Mathf.RoundToInt(2f * scale), Mathf.RoundToInt(8f * scale), Mathf.RoundToInt(7f * scale), leaf);
            FillEllipse(texture, baseX + Mathf.RoundToInt(6f * scale), cy + Mathf.RoundToInt(3f * scale), Mathf.RoundToInt(9f * scale), Mathf.RoundToInt(7f * scale), Shift(leaf, 0.10f));
            DrawCanopyRim(texture, baseX, cy, rx, ry, outline);
        }

        private static void DrawPineCanopy(
            Texture2D texture,
            int centerX,
            int topY,
            int halfWidth,
            int height,
            Color leaf,
            Color leafDark,
            Color leafLight,
            Color outline)
        {
            Vector2Int[] bottom = { P(centerX - halfWidth, topY - height + 9), P(centerX, topY - 4), P(centerX + halfWidth, topY - height + 9) };
            Vector2Int[] middle = { P(centerX - halfWidth + 6, topY - height + 22), P(centerX, topY + 4), P(centerX + halfWidth - 5, topY - height + 22) };
            Vector2Int[] top = { P(centerX - halfWidth + 12, topY - height + 34), P(centerX, topY + 11), P(centerX + halfWidth - 12, topY - height + 34) };
            FillPolygon(texture, bottom, leafDark);
            FillPolygon(texture, middle, leaf);
            FillPolygon(texture, top, leafLight);
            DrawPolygon(texture, bottom, outline);
            DrawPolygon(texture, middle, outline);
            DrawPolygon(texture, top, outline);
        }

        private static void DrawTallPineCanopy(Texture2D texture, int centerX, int topY, Color leaf, Color leafDark, Color leafLight, Color outline)
        {
            DrawPineCanopy(texture, centerX, topY, 23, 49, leaf, leafDark, leafLight, outline);
            FillRect(texture, centerX - 2, 28, 5, 9, leafDark);
        }

        private static void AddLeafDetails(Texture2D texture, int variant, Color leafLight, Color leafDark)
        {
            for (int i = 0; i < 18; i++)
            {
                int x = 14 + ((variant * 17 + i * 9) % 36);
                int y = 39 + ((variant * 13 + i * 5) % 26);
                SetPixelSafe(texture, x, y, i % 3 == 0 ? leafLight : leafDark);
            }
        }

        private static void DrawCanopyRim(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = i * (Mathf.PI * 2f / 24f);
                int x = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * radiusX);
                int y = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * radiusY);
                SetPixelSafe(texture, x, y, color);
            }
        }

        private static Color GetLeafColor(int variant)
        {
            return NormalizeVariant(variant, 5) switch
            {
                1 => Rgb(48, 93, 49),
                2 => Rgb(102, 124, 55),
                3 => Rgb(41, 78, 55),
                4 => Rgb(132, 101, 49),
                _ => Rgb(60, 118, 58)
            };
        }

        private static Color GetBushColor(int variant)
        {
            return NormalizeVariant(variant, 4) switch
            {
                1 => Rgb(73, 128, 61),
                2 => Rgb(55, 104, 68),
                3 => Rgb(104, 135, 64),
                _ => Rgb(67, 116, 55)
            };
        }

        private static Color GetTrunkColor(int variant)
        {
            return NormalizeVariant(variant, 5) switch
            {
                1 => Rgb(86, 57, 37),
                2 => Rgb(112, 78, 44),
                3 => Rgb(68, 52, 40),
                4 => Rgb(96, 65, 43),
                _ => Rgb(101, 69, 42)
            };
        }

        private static Color GetStoneColor(int variant)
        {
            return NormalizeVariant(variant, 6) switch
            {
                1 => Rgb(116, 121, 111),
                2 => Rgb(101, 105, 108),
                3 => Rgb(129, 119, 101),
                4 => Rgb(93, 100, 94),
                5 => Rgb(137, 137, 126),
                _ => Rgb(111, 113, 105)
            };
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

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(b, c, P(x, y));
                    float w1 = Edge(c, a, P(x, y));
                    float w2 = Edge(a, b, P(x, y));
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static float Edge(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
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

        private static Color Shift(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static int NormalizeVariant(int variant, int variantCount)
        {
            if (variantCount <= 0)
            {
                return 0;
            }

            int normalized = variant % variantCount;
            return normalized < 0 ? normalized + variantCount : normalized;
        }
    }
}
