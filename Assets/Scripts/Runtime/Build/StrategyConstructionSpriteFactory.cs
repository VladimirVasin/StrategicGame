using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyConstructionSpriteFactory
    {
        private const float PixelsPerUnit = 24f;
        public const int StageCount = 7;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();
        private static readonly Dictionary<int, Sprite> CachedLogsSprites = new();
        private static readonly Dictionary<int, Sprite> CachedStoneSprites = new();

        public static Sprite GetConstructionSprite(StrategyBuildTool tool, int variant, int stage)
        {
            int normalizedStage = Mathf.Clamp(stage, 0, StageCount - 1);
            if (tool == StrategyBuildTool.Bridge)
            {
                return GetBridgeConstructionSprite(new Vector2Int(3, 1), normalizedStage);
            }

            int cacheKey = ((int)tool * 512) + (Mathf.Max(0, variant) * 32) + normalizedStage;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateConstructionSprite(tool, variant, normalizedStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetBridgeConstructionSprite(Vector2Int footprint, int stage)
        {
            Vector2Int normalizedFootprint = new Vector2Int(
                Mathf.Max(1, footprint.x),
                Mathf.Max(1, footprint.y));
            int normalizedStage = Mathf.Clamp(stage, 0, StageCount - 1);
            int cacheKey = 65536 + normalizedFootprint.x * 512 + normalizedFootprint.y * 16 + normalizedStage;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateBridgeConstructionSprite(normalizedFootprint, normalizedStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetConstructionLogsSprite(int amount)
        {
            int level = Mathf.Clamp(amount, 0, 8);
            if (!CachedLogsSprites.TryGetValue(level, out Sprite sprite) || sprite == null)
            {
                sprite = CreateLogsSprite(level);
                CachedLogsSprites[level] = sprite;
            }

            return sprite;
        }

        public static Sprite GetConstructionStoneSprite(int amount)
        {
            int level = Mathf.Clamp(amount, 0, 8);
            if (!CachedStoneSprites.TryGetValue(level, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStoneSprite(level);
                CachedStoneSprites[level] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateConstructionSprite(StrategyBuildTool tool, int variant, int stage)
        {
            Texture2D texture = CreateTexture(104, 92, "Construction " + tool + " Stage " + (stage + 1));
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color outline = Rgb(48, 35, 29);
            Color dirt = Rgb(112, 84, 55);
            Color plankDark = Rgb(94, 57, 35);
            Color plank = Rgb(150, 94, 48);
            Color plankLight = Rgb(205, 143, 72);
            Color stone = Rgb(126, 130, 119);
            Color stoneLight = Rgb(174, 176, 160);
            Color rope = Rgb(199, 158, 91);
            Color cloth = tool == StrategyBuildTool.House
                ? Rgb(164, 64, 45)
                : tool == StrategyBuildTool.StonecutterCamp
                    ? Rgb(112, 120, 118)
                    : tool == StrategyBuildTool.HunterCamp
                        ? Rgb(126, 93, 61)
                        : tool == StrategyBuildTool.FisherHut
                            ? Rgb(78, 120, 130)
                            : tool == StrategyBuildTool.Granary
                                ? Rgb(168, 132, 61)
                                : Rgb(130, 117, 73);

            FillEllipse(texture, 52, 16, 39, 9, shadow);
            FillPolygon(texture, new[] { P(19, 18), P(76, 18), P(86, 26), P(29, 27) }, dirt);
            DrawPolygon(texture, new[] { P(19, 18), P(76, 18), P(86, 26), P(29, 27) }, outline);

            int frameHeight = Mathf.Clamp(stage, 0, StageCount - 1);
            if (frameHeight >= 1)
            {
                DrawFoundation(texture, stone, stoneLight, outline);
            }

            if (frameHeight >= 2)
            {
                DrawPosts(texture, 30, 22, 46, plankDark, plank, outline);
                DrawPosts(texture, 73, 23, 43, plankDark, plank, outline);
                DrawBeam(texture, 28, 52, 75, 56, plankDark, plankLight, outline);
                DrawScaffold(texture, 18, 21, 86, 42, plankDark, plank, rope);
            }

            if (frameHeight >= 3)
            {
                DrawPosts(texture, 44, 24, 39, plankDark, plank, outline);
                DrawPosts(texture, 60, 23, 40, plankDark, plank, outline);
                DrawBeam(texture, 28, 40, 78, 44, plankDark, plankLight, outline);
                FillRect(texture, 35, 25, 34, 19, tool == StrategyBuildTool.StorageYard ? Rgb(132, 100, 70) : Rgb(183, 145, 91));
                DrawRectOutline(texture, 35, 25, 34, 19, outline);
            }

            if (frameHeight >= 4)
            {
                FillRect(texture, 31, 27, 43, 25, tool == StrategyBuildTool.StonecutterCamp ? Rgb(143, 136, 116) : Rgb(201, 169, 112));
                DrawRectOutline(texture, 31, 27, 43, 25, outline);
                DrawBeam(texture, 28, 52, 75, 56, plankDark, plankLight, outline);
            }

            if (frameHeight >= 5)
            {
                FillPolygon(texture, new[] { P(26, 52), P(52, 70), P(81, 52), P(72, 47), P(52, 60), P(33, 47) }, outline);
                FillPolygon(texture, new[] { P(30, 52), P(52, 66), P(77, 52), P(70, 49), P(52, 60), P(35, 49) }, cloth);
                DrawLine(texture, P(36, 51), P(52, 62), Rgb(92, 55, 40));
                DrawLine(texture, P(53, 61), P(70, 50), Rgb(92, 55, 40));
            }

            if (frameHeight >= 6)
            {
                if (StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, variant, out Sprite finalSprite) && finalSprite != null)
                {
                    DrawFinalHint(texture, tool, outline, cloth);
                }
                else
                {
                    DrawFinalHint(texture, tool, outline, cloth);
                }
            }

            DrawLooseMaterials(texture, stage, plankDark, plank, stone, stoneLight, outline);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(6f, 5f, 92f, 82f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static Sprite CreateBridgeConstructionSprite(Vector2Int footprint, int stage)
        {
            bool horizontal = footprint.x >= footprint.y;
            int lengthCells = Mathf.Max(1, horizontal ? footprint.x : footprint.y);
            int width = horizontal ? Mathf.Max(72, lengthCells * 24 + 20) : 62;
            int height = horizontal ? 56 : Mathf.Max(72, lengthCells * 24 + 20);
            Texture2D texture = CreateTexture(width, height, $"Bridge Construction {footprint.x}x{footprint.y} Stage {stage + 1}");

            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color outline = Rgb(49, 35, 26);
            Color post = Rgb(90, 55, 34);
            Color plank = Rgb(147, 91, 45);
            Color plankLight = Rgb(204, 139, 72);
            Color rope = Rgb(198, 158, 91);
            Color stone = Rgb(121, 123, 113);

            int visibleLevel = Mathf.Clamp(stage, 0, StageCount - 1);
            if (horizontal)
            {
                int centerY = height / 2;
                FillEllipse(texture, width / 2, centerY - 10, width / 2 - 8, 8, shadow);

                int posts = Mathf.Max(2, lengthCells + 1);
                for (int i = 0; i < posts; i++)
                {
                    int x = Mathf.RoundToInt(Mathf.Lerp(12, width - 12, i / (float)(posts - 1)));
                    FillRect(texture, x - 2, centerY - 17, 5, 16, outline);
                    FillRect(texture, x - 1, centerY - 16, 3, 14, post);
                    if (visibleLevel >= 2)
                    {
                        FillRect(texture, x - 2, centerY + 2, 5, 15, outline);
                        FillRect(texture, x - 1, centerY + 3, 3, 13, post);
                    }
                }

                if (visibleLevel >= 1)
                {
                    DrawThickLine(texture, P(10, centerY - 7), P(width - 10, centerY - 7), outline, 1);
                    DrawLine(texture, P(11, centerY - 6), P(width - 11, centerY - 6), plankLight);
                }

                if (visibleLevel >= 3)
                {
                    DrawThickLine(texture, P(12, centerY + 7), P(width - 8, centerY + 4), outline, 1);
                    DrawLine(texture, P(13, centerY + 6), P(width - 10, centerY + 3), plank);
                    DrawLine(texture, P(12, centerY - 14), P(width - 12, centerY - 14), rope);
                }

                if (visibleLevel >= 4)
                {
                    int planks = Mathf.Max(3, lengthCells * 2);
                    for (int i = 0; i <= planks; i++)
                    {
                        int x = Mathf.RoundToInt(Mathf.Lerp(16, width - 16, i / (float)planks));
                        DrawLine(texture, P(x, centerY - 8), P(x + 3, centerY + 5), outline);
                    }
                }

                if (visibleLevel <= 2)
                {
                    for (int i = 0; i < Mathf.Max(2, visibleLevel + 2); i++)
                    {
                        int x = 18 + i * 11;
                        FillRect(texture, x, centerY - 1, 12, 4, outline);
                        FillRect(texture, x + 1, centerY, 10, 2, plank);
                    }
                }
            }
            else
            {
                int centerX = width / 2;
                FillEllipse(texture, centerX, height / 2, 15, height / 2 - 8, shadow);

                int posts = Mathf.Max(2, lengthCells + 1);
                for (int i = 0; i < posts; i++)
                {
                    int y = Mathf.RoundToInt(Mathf.Lerp(12, height - 12, i / (float)(posts - 1)));
                    FillRect(texture, centerX - 18, y - 2, 13, 5, outline);
                    FillRect(texture, centerX - 17, y - 1, 11, 3, post);
                    if (visibleLevel >= 2)
                    {
                        FillRect(texture, centerX + 6, y - 2, 13, 5, outline);
                        FillRect(texture, centerX + 7, y - 1, 11, 3, post);
                    }
                }

                if (visibleLevel >= 1)
                {
                    DrawThickLine(texture, P(centerX - 10, 10), P(centerX - 7, height - 10), outline, 1);
                    DrawLine(texture, P(centerX - 8, 12), P(centerX - 5, height - 12), plankLight);
                }

                if (visibleLevel >= 3)
                {
                    DrawThickLine(texture, P(centerX + 9, 12), P(centerX + 14, height - 15), outline, 1);
                    DrawLine(texture, P(centerX + 7, 13), P(centerX + 11, height - 16), plank);
                    DrawLine(texture, P(centerX - 16, 15), P(centerX - 12, height - 14), rope);
                }

                if (visibleLevel >= 4)
                {
                    int planks = Mathf.Max(3, lengthCells * 2);
                    for (int i = 0; i <= planks; i++)
                    {
                        int y = Mathf.RoundToInt(Mathf.Lerp(17, height - 18, i / (float)planks));
                        DrawLine(texture, P(centerX - 8, y), P(centerX + 8, y - 2), outline);
                    }
                }

                if (visibleLevel <= 2)
                {
                    for (int i = 0; i < Mathf.Max(2, visibleLevel + 2); i++)
                    {
                        int y = 18 + i * 10;
                        FillEllipse(texture, centerX + 2, y, 5, 3, stone);
                    }
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void DrawFoundation(Texture2D texture, Color stone, Color stoneLight, Color outline)
        {
            FillPolygon(texture, new[] { P(26, 20), P(72, 20), P(82, 27), P(35, 29) }, outline);
            FillPolygon(texture, new[] { P(29, 21), P(71, 21), P(78, 26), P(36, 27) }, stone);
            DrawLine(texture, P(34, 24), P(74, 24), stoneLight);
        }

        private static void DrawPosts(Texture2D texture, int x, int y, int height, Color dark, Color mid, Color outline)
        {
            FillRect(texture, x - 2, y - 1, 6, height + 2, outline);
            FillRect(texture, x - 1, y, 4, height, dark);
            FillRect(texture, x, y, 2, height, mid);
        }

        private static void DrawBeam(Texture2D texture, int x1, int y1, int x2, int y2, Color dark, Color light, Color outline)
        {
            DrawThickLine(texture, P(x1, y1), P(x2, y2), outline, 2);
            DrawThickLine(texture, P(x1, y1), P(x2, y2), dark, 1);
            DrawLine(texture, P(x1, y1 + 1), P(x2, y2 + 1), light);
        }

        private static void DrawScaffold(Texture2D texture, int x1, int y1, int x2, int y2, Color dark, Color mid, Color rope)
        {
            DrawLine(texture, P(x1, y1), P(x2, y2), dark);
            DrawLine(texture, P(x1, y2), P(x2, y1), mid);
            DrawLine(texture, P(x1 + 4, y2 + 5), P(x2 - 4, y2 + 5), rope);
        }

        private static void DrawFinalHint(Texture2D texture, StrategyBuildTool tool, Color outline, Color cloth)
        {
            Color wall = tool == StrategyBuildTool.StorageYard ? Rgb(144, 111, 73) : Rgb(214, 180, 120);
            FillRect(texture, 34, 27, 36, 25, wall);
            DrawRectOutline(texture, 34, 27, 36, 25, outline);
            FillPolygon(texture, new[] { P(27, 53), P(52, 70), P(80, 53), P(72, 49), P(52, 62), P(35, 49) }, outline);
            FillPolygon(texture, new[] { P(31, 53), P(52, 66), P(76, 53), P(70, 51), P(52, 61), P(36, 51) }, cloth);
            FillRect(texture, 48, 28, 8, 15, Rgb(91, 54, 37));
        }

        private static void DrawLooseMaterials(Texture2D texture, int stage, Color logDark, Color log, Color stone, Color stoneLight, Color outline)
        {
            int count = Mathf.Clamp(stage + 2, 2, 8);
            for (int i = 0; i < count; i++)
            {
                int x = 18 + i * 7;
                int y = 13 + (i % 2);
                FillRect(texture, x, y, 10, 3, outline);
                FillRect(texture, x + 1, y + 1, 8, 1, log);
                SetPixelSafe(texture, x + 1, y + 1, logDark);
            }

            for (int i = 0; i < Mathf.Clamp(stage, 0, 5); i++)
            {
                int x = 73 + (i % 3) * 5;
                int y = 14 + (i / 3) * 4;
                FillEllipse(texture, x, y, 4, 2, outline);
                FillEllipse(texture, x, y + 1, 3, 2, stone);
                SetPixelSafe(texture, x - 1, y + 2, stoneLight);
            }
        }

        private static Sprite CreateLogsSprite(int level)
        {
            Texture2D texture = CreateTexture(48, 28, "Construction Logs Stock " + level);
            Color outline = Rgb(54, 35, 26);
            Color bark = Rgb(104, 63, 38);
            Color cut = Rgb(204, 146, 82);
            for (int i = 0; i < level; i++)
            {
                int x = 4 + (i % 4) * 9;
                int y = 7 + (i / 4) * 6;
                FillRect(texture, x, y, 12, 4, outline);
                FillRect(texture, x + 1, y + 1, 10, 2, bark);
                FillRect(texture, x + 9, y + 1, 2, 2, cut);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 48f, 28f), new Vector2(0.5f, 0.2f), PixelsPerUnit);
        }

        private static Sprite CreateStoneSprite(int level)
        {
            Texture2D texture = CreateTexture(42, 28, "Construction Stone Stock " + level);
            Color outline = Rgb(55, 55, 51);
            Color stone = Rgb(128, 132, 121);
            Color light = Rgb(183, 185, 170);
            for (int i = 0; i < level; i++)
            {
                int x = 5 + (i % 4) * 8;
                int y = 7 + (i / 4) * 6;
                FillEllipse(texture, x, y, 5, 3, outline);
                FillEllipse(texture, x, y + 1, 4, 2, stone);
                SetPixelSafe(texture, x - 1, y + 2, light);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 42f, 28f), new Vector2(0.5f, 0.2f), PixelsPerUnit);
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
            if (points == null || points.Length < 3)
            {
                return;
            }

            int minY = points[0].y;
            int maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            for (int y = minY; y <= maxY; y++)
            {
                List<int> nodes = new();
                int j = points.Length - 1;
                for (int i = 0; i < points.Length; i++)
                {
                    if ((points[i].y < y && points[j].y >= y) || (points[j].y < y && points[i].y >= y))
                    {
                        int x = points[i].x + (y - points[i].y) * (points[j].x - points[i].x) / Mathf.Max(1, points[j].y - points[i].y);
                        nodes.Add(x);
                    }

                    j = i;
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    FillRect(texture, nodes[i], y, nodes[i + 1] - nodes[i] + 1, 1, color);
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

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
    }
}
