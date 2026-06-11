using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyFishSpecies
    {
        Minnow,
        Carp,
        Perch
    }

    internal enum StrategyFishSpritePose
    {
        Idle,
        Swim,
        Dart,
        Turn,
        Feed,
        Hooked
    }

    internal static class StrategyFishSpriteFactory
    {
        private const float PixelsPerUnit = 42f;
        public const int IdleFrameCount = 6;
        public const int SwimFrameCount = 8;
        public const int DartFrameCount = 8;
        public const int TurnFrameCount = 6;
        public const int FeedFrameCount = 6;
        public const int HookedFrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetIdleSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Idle, NormalizeFrame(frame, IdleFrameCount));
        }

        public static Sprite GetSwimSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Swim, NormalizeFrame(frame, SwimFrameCount));
        }

        public static Sprite GetDartSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Dart, NormalizeFrame(frame, DartFrameCount));
        }

        public static Sprite GetTurnSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Turn, NormalizeFrame(frame, TurnFrameCount));
        }

        public static Sprite GetFeedSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Feed, NormalizeFrame(frame, FeedFrameCount));
        }

        public static Sprite GetHookedSprite(StrategyFishSpecies species, int frame)
        {
            return GetSprite(species, StrategyFishSpritePose.Hooked, NormalizeFrame(frame, HookedFrameCount));
        }

        private static Sprite GetSprite(StrategyFishSpecies species, StrategyFishSpritePose pose, int frame)
        {
            int cacheKey = ((int)species * 4096) + ((int)pose * 128) + frame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(species, pose, frame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyFishSpecies species, StrategyFishSpritePose pose, int frame)
        {
            const int width = 58;
            const int height = 34;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Fish {species} {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            FishFrame fishFrame = GetFrame(pose, frame);
            FishPalette palette = GetPalette(species);
            DrawFish(texture, species, pose, frame, fishFrame, palette);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 50f, 26f), new Vector2(0.5f, 0.45f), PixelsPerUnit);
        }

        private static void DrawFish(
            Texture2D texture,
            StrategyFishSpecies species,
            StrategyFishSpritePose pose,
            int spriteFrame,
            FishFrame frame,
            FishPalette palette)
        {
            int bodyX = 29 + frame.BodyX;
            int bodyY = 16 + frame.BodyY;
            int bodyRadiusX = species == StrategyFishSpecies.Minnow ? 13 : 16;
            int bodyRadiusY = species == StrategyFishSpecies.Carp ? 7 : 6;
            if (pose == StrategyFishSpritePose.Dart)
            {
                bodyRadiusX += 2;
                bodyRadiusY = Mathf.Max(4, bodyRadiusY - 1);
            }
            else if (pose == StrategyFishSpritePose.Hooked)
            {
                bodyRadiusX += 1;
                bodyRadiusY = Mathf.Max(4, bodyRadiusY - 1);
            }

            DrawRippleGlints(texture, bodyX, bodyY, spriteFrame, pose);
            DrawTail(texture, bodyX - bodyRadiusX + 1, bodyY, frame.TailSwing, palette);
            DrawFins(texture, bodyX, bodyY, frame.FinY, palette);

            FillEllipse(texture, bodyX, bodyY, bodyRadiusX + 1, bodyRadiusY + 1, palette.Outline);
            FillEllipse(texture, bodyX, bodyY, bodyRadiusX, bodyRadiusY, palette.BodyDark);
            FillEllipse(texture, bodyX + 2, bodyY + 1, bodyRadiusX - 2, Mathf.Max(3, bodyRadiusY - 1), palette.Body);
            FillEllipse(texture, bodyX + 8, bodyY + 2, Mathf.Max(4, bodyRadiusX - 8), Mathf.Max(2, bodyRadiusY - 3), palette.BodyLight);

            DrawHead(texture, bodyX + bodyRadiusX - 2, bodyY + frame.HeadY, palette);
            DrawSpeciesDetails(texture, species, pose, spriteFrame, bodyX, bodyY, palette);

            if (pose == StrategyFishSpritePose.Feed)
            {
                DrawBubbles(texture, bodyX + bodyRadiusX + 6, bodyY + frame.HeadY, spriteFrame);
            }
            else if (pose == StrategyFishSpritePose.Hooked)
            {
                DrawHook(texture, bodyX + bodyRadiusX + 3, bodyY + frame.HeadY, spriteFrame, palette.Outline);
            }
        }

        private static void DrawTail(Texture2D texture, int baseX, int baseY, int swing, FishPalette palette)
        {
            Vector2Int tailBase = P(baseX, baseY);
            Vector2Int topTip = P(baseX - 11, baseY + 7 + swing);
            Vector2Int middleTip = P(baseX - 6, baseY);
            Vector2Int bottomTip = P(baseX - 11, baseY - 7 + swing);

            FillTriangle(texture, tailBase + P(1, 0), topTip, middleTip, palette.Outline);
            FillTriangle(texture, tailBase + P(1, 0), middleTip, bottomTip, palette.Outline);
            FillTriangle(texture, tailBase, topTip + P(2, -1), middleTip + P(1, 0), palette.Fin);
            FillTriangle(texture, tailBase, middleTip + P(1, 0), bottomTip + P(2, 1), palette.FinDark);
            DrawLine(texture, tailBase, P(baseX - 8, baseY + swing), palette.FinLight);
        }

        private static void DrawFins(Texture2D texture, int bodyX, int bodyY, int finY, FishPalette palette)
        {
            FillTriangle(texture, P(bodyX - 3, bodyY + 5), P(bodyX + 3, bodyY + 13 + finY), P(bodyX + 8, bodyY + 5), palette.Outline);
            FillTriangle(texture, P(bodyX - 2, bodyY + 5), P(bodyX + 3, bodyY + 11 + finY), P(bodyX + 7, bodyY + 5), palette.Fin);
            FillTriangle(texture, P(bodyX + 2, bodyY - 4), P(bodyX + 9, bodyY - 10 - finY), P(bodyX + 11, bodyY - 4), palette.Outline);
            FillTriangle(texture, P(bodyX + 3, bodyY - 4), P(bodyX + 9, bodyY - 8 - finY), P(bodyX + 10, bodyY - 4), palette.FinDark);
        }

        private static void DrawHead(Texture2D texture, int headX, int headY, FishPalette palette)
        {
            FillEllipse(texture, headX + 3, headY, 5, 5, palette.Outline);
            FillEllipse(texture, headX + 2, headY, 4, 4, palette.Body);
            FillEllipse(texture, headX + 5, headY - 1, 2, 2, palette.Muzzle);
            SetPixelSafe(texture, headX + 4, headY + 2, palette.Outline);
            DrawLine(texture, P(headX - 2, headY + 4), P(headX - 3, headY - 4), palette.Gill);
        }

        private static void DrawSpeciesDetails(
            Texture2D texture,
            StrategyFishSpecies species,
            StrategyFishSpritePose pose,
            int frame,
            int bodyX,
            int bodyY,
            FishPalette palette)
        {
            switch (species)
            {
                case StrategyFishSpecies.Carp:
                    DrawLine(texture, P(bodyX - 10, bodyY + 2), P(bodyX + 12, bodyY + 4), palette.Marking);
                    DrawLine(texture, P(bodyX - 8, bodyY - 2), P(bodyX + 10, bodyY - 1), palette.Marking);
                    break;
                case StrategyFishSpecies.Perch:
                    for (int i = 0; i < 5; i++)
                    {
                        int x = bodyX - 9 + i * 5;
                        DrawLine(texture, P(x, bodyY + 6), P(x + 2, bodyY - 5), palette.Marking);
                    }

                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        int x = bodyX - 10 + i * 4;
                        int y = bodyY + ((frame + i + (int)pose) % 5) - 2;
                        SetPixelSafe(texture, x, y, palette.Marking);
                    }

                    break;
            }
        }

        private static void DrawBubbles(Texture2D texture, int x, int y, int frame)
        {
            Color bubble = new Color(0.78f, 0.94f, 1f, 0.55f);
            SetPixelSafe(texture, x + 1, y + 3 + frame % 2, bubble);
            SetPixelSafe(texture, x + 4, y + 5 + frame % 3, bubble);
            SetPixelSafe(texture, x + 7, y + 2 + frame % 2, bubble);
        }

        private static void DrawHook(Texture2D texture, int x, int y, int frame, Color outline)
        {
            Color line = new Color(0.86f, 0.91f, 0.88f, 0.82f);
            Color metal = Rgb(210, 216, 202);
            int sway = frame % 2 == 0 ? -1 : 1;
            DrawLine(texture, P(x + sway, y + 12), P(x, y + 2), line);
            DrawLine(texture, P(x, y + 2), P(x + 4, y - 1), outline);
            DrawLine(texture, P(x + 1, y + 2), P(x + 4, y), metal);
            SetPixelSafe(texture, x + 2, y - 2, outline);
        }

        private static void DrawRippleGlints(Texture2D texture, int x, int y, int frame, StrategyFishSpritePose pose)
        {
            if (pose == StrategyFishSpritePose.Idle && frame % 3 != 0)
            {
                return;
            }

            Color glint = new Color(0.70f, 0.91f, 1f, pose == StrategyFishSpritePose.Dart ? 0.28f : 0.18f);
            DrawLine(texture, P(x - 16, y + 10 + frame % 2), P(x - 7, y + 10 + frame % 2), glint);
            DrawLine(texture, P(x + 4, y - 9 - frame % 2), P(x + 13, y - 9 - frame % 2), glint);
        }

        private static FishFrame GetFrame(StrategyFishSpritePose pose, int frame)
        {
            int normalized;
            switch (pose)
            {
                case StrategyFishSpritePose.Swim:
                    normalized = NormalizeFrame(frame, SwimFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(0, 1, 2, 1, 0),
                        2 => new FishFrame(1, 1, 3, 1, 0),
                        3 => new FishFrame(0, 0, 1, 0, 0),
                        4 => new FishFrame(-1, -1, -2, -1, 0),
                        5 => new FishFrame(-1, -1, -3, -1, 0),
                        6 => new FishFrame(0, 0, -1, 0, 0),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
                case StrategyFishSpritePose.Dart:
                    normalized = NormalizeFrame(frame, DartFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(1, 1, 4, 1, 0),
                        2 => new FishFrame(2, 0, 5, 1, -1),
                        3 => new FishFrame(1, -1, 2, 0, 0),
                        4 => new FishFrame(-1, 0, -4, -1, 1),
                        5 => new FishFrame(-2, 1, -5, -1, 0),
                        6 => new FishFrame(-1, 0, -2, 0, -1),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
                case StrategyFishSpritePose.Turn:
                    normalized = NormalizeFrame(frame, TurnFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(0, 0, 2, 1, 1),
                        2 => new FishFrame(1, 0, 4, 2, 1),
                        3 => new FishFrame(1, 1, 3, 1, 0),
                        4 => new FishFrame(0, 0, -2, -1, -1),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
                case StrategyFishSpritePose.Feed:
                    normalized = NormalizeFrame(frame, FeedFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(0, 0, 1, 0, -1),
                        2 => new FishFrame(0, -1, 0, -1, -2),
                        3 => new FishFrame(0, -1, -1, -1, -1),
                        4 => new FishFrame(0, 0, 0, 0, 0),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
                case StrategyFishSpritePose.Hooked:
                    normalized = NormalizeFrame(frame, HookedFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(2, 1, 5, 1, -1),
                        2 => new FishFrame(-1, 2, -4, 1, 1),
                        3 => new FishFrame(-2, 0, -5, -1, 2),
                        4 => new FishFrame(1, -2, 4, -1, -1),
                        5 => new FishFrame(2, -1, 5, 0, -2),
                        6 => new FishFrame(-1, 1, -3, 1, 1),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
                default:
                    normalized = NormalizeFrame(frame, IdleFrameCount);
                    return normalized switch
                    {
                        1 => new FishFrame(0, 0, 1, 0, 0),
                        3 => new FishFrame(0, 1, -1, 0, 0),
                        5 => new FishFrame(0, 0, 1, 0, 0),
                        _ => new FishFrame(0, 0, 0, 0, 0)
                    };
            }
        }

        private static FishPalette GetPalette(StrategyFishSpecies species)
        {
            switch (species)
            {
                case StrategyFishSpecies.Carp:
                    return new FishPalette(
                        Rgb(36, 38, 33),
                        Rgb(163, 132, 72),
                        Rgb(122, 93, 56),
                        Rgb(207, 174, 93),
                        Rgb(189, 112, 58),
                        Rgb(132, 76, 49),
                        Rgb(229, 184, 95),
                        Rgb(88, 58, 41),
                        Rgb(229, 202, 141),
                        Rgb(73, 58, 42));
                case StrategyFishSpecies.Perch:
                    return new FishPalette(
                        Rgb(28, 36, 33),
                        Rgb(86, 133, 95),
                        Rgb(50, 91, 73),
                        Rgb(137, 173, 105),
                        Rgb(72, 126, 99),
                        Rgb(43, 80, 65),
                        Rgb(190, 138, 67),
                        Rgb(37, 71, 58),
                        Rgb(202, 221, 159),
                        Rgb(42, 70, 49));
                default:
                    return new FishPalette(
                        Rgb(24, 35, 46),
                        Rgb(77, 139, 171),
                        Rgb(49, 91, 126),
                        Rgb(132, 194, 209),
                        Rgb(83, 153, 187),
                        Rgb(42, 85, 125),
                        Rgb(174, 225, 225),
                        Rgb(31, 74, 112),
                        Rgb(204, 236, 228),
                        Rgb(69, 117, 148));
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

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            float area = Edge(a, b, c);
            if (Mathf.Approximately(area, 0f))
            {
                DrawLine(texture, a, b, color);
                DrawLine(texture, b, c, color);
                DrawLine(texture, c, a, color);
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2Int point = P(x, y);
                    float w0 = Edge(b, c, point);
                    float w1 = Edge(c, a, point);
                    float w2 = Edge(a, b, point);
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

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static int NormalizeFrame(int frame, int frameCount)
        {
            if (frameCount <= 0)
            {
                return 0;
            }

            int normalized = frame % frameCount;
            return normalized < 0 ? normalized + frameCount : normalized;
        }

        private readonly struct FishFrame
        {
            public FishFrame(int bodyX, int bodyY, int tailSwing, int finY, int headY)
            {
                BodyX = bodyX;
                BodyY = bodyY;
                TailSwing = tailSwing;
                FinY = finY;
                HeadY = headY;
            }

            public int BodyX { get; }
            public int BodyY { get; }
            public int TailSwing { get; }
            public int FinY { get; }
            public int HeadY { get; }
        }

        private readonly struct FishPalette
        {
            public FishPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color fin,
                Color finDark,
                Color finLight,
                Color gill,
                Color muzzle,
                Color marking)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                Fin = fin;
                FinDark = finDark;
                FinLight = finLight;
                Gill = gill;
                Muzzle = muzzle;
                Marking = marking;
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color Fin { get; }
            public Color FinDark { get; }
            public Color FinLight { get; }
            public Color Gill { get; }
            public Color Muzzle { get; }
            public Color Marking { get; }
        }
    }
}
