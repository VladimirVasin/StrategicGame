using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBirdSpecies
    {
        Sparrow,
        Crow,
        Duck
    }

    internal enum StrategyBirdSpritePose
    {
        Idle,
        Peck,
        Hop,
        Fly,
        Land,
        Swim
    }

    internal static class StrategyBirdSpriteFactory
    {
        private const float AnimalVisualScale = 0.6f;
        private const float PixelsPerUnit = 36f / AnimalVisualScale;
        public const int IdleFrameCount = 6;
        public const int PeckFrameCount = 6;
        public const int HopFrameCount = 6;
        public const int FlyFrameCount = 8;
        public const int LandFrameCount = 6;
        public const int SwimFrameCount = 6;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetIdleSprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Idle, NormalizeFrame(frame, IdleFrameCount));
        }

        public static Sprite GetPeckSprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Peck, NormalizeFrame(frame, PeckFrameCount));
        }

        public static Sprite GetHopSprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Hop, NormalizeFrame(frame, HopFrameCount));
        }

        public static Sprite GetFlySprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Fly, NormalizeFrame(frame, FlyFrameCount));
        }

        public static Sprite GetLandSprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Land, NormalizeFrame(frame, LandFrameCount));
        }

        public static Sprite GetSwimSprite(StrategyBirdSpecies species, int frame)
        {
            return GetSprite(species, StrategyBirdSpritePose.Swim, NormalizeFrame(frame, SwimFrameCount));
        }

        private static Sprite GetSprite(StrategyBirdSpecies species, StrategyBirdSpritePose pose, int frame)
        {
            int cacheKey = ((int)species * 4096) + ((int)pose * 128) + frame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(species, pose, frame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyBirdSpecies species, StrategyBirdSpritePose pose, int frame)
        {
            const int width = 48;
            const int height = 36;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Bird {species} {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            BirdFrame birdFrame = GetFrame(pose, frame);
            BirdPalette palette = GetPalette(species);
            DrawBird(texture, species, pose, frame, birdFrame, palette);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 40f, 28f), new Vector2(0.5f, 0.18f), PixelsPerUnit);
        }

        private static void DrawBird(
            Texture2D texture,
            StrategyBirdSpecies species,
            StrategyBirdSpritePose pose,
            int spriteFrame,
            BirdFrame frame,
            BirdPalette palette)
        {
            int bodyX = 24;
            int bodyY = 13 + frame.BodyY;
            int bodyRadiusX = species == StrategyBirdSpecies.Duck ? 11 : 8;
            int bodyRadiusY = species == StrategyBirdSpecies.Duck ? 6 : 5;
            int headX = bodyX + bodyRadiusX - 1 + frame.HeadX;
            int headY = bodyY + 5 + frame.HeadY;

            if (pose == StrategyBirdSpritePose.Fly)
            {
                DrawFlyingWings(texture, bodyX, bodyY, frame.WingY, palette);
            }
            else
            {
                DrawRestingWing(texture, bodyX, bodyY, frame.WingY, palette);
            }

            if (pose == StrategyBirdSpritePose.Swim && species == StrategyBirdSpecies.Duck)
            {
                DrawWaterLine(texture, bodyX, bodyY - 3, spriteFrame);
            }

            FillEllipse(texture, bodyX, bodyY, bodyRadiusX + 1, bodyRadiusY + 1, palette.Outline);
            FillEllipse(texture, bodyX, bodyY, bodyRadiusX, bodyRadiusY, palette.BodyDark);
            FillEllipse(texture, bodyX + 2, bodyY + 1, Mathf.Max(3, bodyRadiusX - 3), Mathf.Max(2, bodyRadiusY - 2), palette.Body);
            FillEllipse(texture, headX, headY, species == StrategyBirdSpecies.Duck ? 5 : 4, species == StrategyBirdSpecies.Duck ? 4 : 3, palette.Outline);
            FillEllipse(texture, headX, headY, species == StrategyBirdSpecies.Duck ? 4 : 3, species == StrategyBirdSpecies.Duck ? 3 : 2, palette.Head);

            DrawBeak(texture, species, headX + 4, headY - 1, palette);
            SetPixelSafe(texture, headX + 2, headY + 1, palette.Eye);

            if (pose != StrategyBirdSpritePose.Fly && pose != StrategyBirdSpritePose.Swim)
            {
                DrawLegs(texture, species, bodyX, bodyY, frame.LegOffset, palette);
            }

            if (pose == StrategyBirdSpritePose.Peck)
            {
                DrawSeed(texture, headX + 6, headY - 7 + spriteFrame % 2, palette);
            }
        }

        private static void DrawFlyingWings(Texture2D texture, int bodyX, int bodyY, int wingY, BirdPalette palette)
        {
            FillTriangle(texture, P(bodyX - 4, bodyY + 2), P(bodyX - 15, bodyY + 13 + wingY), P(bodyX - 2, bodyY + 6), palette.Outline);
            FillTriangle(texture, P(bodyX - 3, bodyY + 2), P(bodyX - 13, bodyY + 11 + wingY), P(bodyX - 1, bodyY + 6), palette.Wing);
            FillTriangle(texture, P(bodyX + 3, bodyY + 1), P(bodyX + 13, bodyY - 10 - wingY), P(bodyX + 5, bodyY + 5), palette.Outline);
            FillTriangle(texture, P(bodyX + 4, bodyY + 1), P(bodyX + 11, bodyY - 8 - wingY), P(bodyX + 6, bodyY + 5), palette.WingLight);
        }

        private static void DrawRestingWing(Texture2D texture, int bodyX, int bodyY, int wingY, BirdPalette palette)
        {
            FillEllipse(texture, bodyX - 2, bodyY + wingY, 7, 4, palette.Outline);
            FillEllipse(texture, bodyX - 1, bodyY + wingY, 6, 3, palette.Wing);
            DrawLine(texture, P(bodyX - 6, bodyY + wingY + 2), P(bodyX + 4, bodyY + wingY - 2), palette.WingLight);
        }

        private static void DrawBeak(Texture2D texture, StrategyBirdSpecies species, int x, int y, BirdPalette palette)
        {
            int length = species == StrategyBirdSpecies.Duck ? 7 : 4;
            FillTriangle(texture, P(x - 1, y + 2), P(x + length, y), P(x - 1, y - 2), palette.Beak);
            SetPixelSafe(texture, x + length, y, palette.Outline);
        }

        private static void DrawLegs(Texture2D texture, StrategyBirdSpecies species, int bodyX, int bodyY, int legOffset, BirdPalette palette)
        {
            int footY = bodyY - 8;
            int legHeight = species == StrategyBirdSpecies.Duck ? 3 : 4;
            DrawLine(texture, P(bodyX - 3, bodyY - 4), P(bodyX - 4 + legOffset, footY), palette.Leg);
            DrawLine(texture, P(bodyX + 3, bodyY - 4), P(bodyX + 3 - legOffset, footY), palette.Leg);
            DrawLine(texture, P(bodyX - 7 + legOffset, footY), P(bodyX - 2 + legOffset, footY), palette.Leg);
            DrawLine(texture, P(bodyX + 1 - legOffset, footY), P(bodyX + 7 - legOffset, footY), palette.Leg);
            if (legHeight > 3)
            {
                SetPixelSafe(texture, bodyX - 4 + legOffset, footY + 1, palette.Leg);
                SetPixelSafe(texture, bodyX + 3 - legOffset, footY + 1, palette.Leg);
            }
        }

        private static void DrawWaterLine(Texture2D texture, int x, int y, int frame)
        {
            Color water = new Color(0.58f, 0.82f, 0.94f, 0.45f);
            DrawLine(texture, P(x - 14, y + frame % 2), P(x - 3, y + frame % 2), water);
            DrawLine(texture, P(x + 4, y - frame % 2), P(x + 15, y - frame % 2), water);
        }

        private static void DrawSeed(Texture2D texture, int x, int y, BirdPalette palette)
        {
            SetPixelSafe(texture, x, y, palette.Seed);
            SetPixelSafe(texture, x + 2, y - 1, palette.Seed);
        }

        private static BirdFrame GetFrame(StrategyBirdSpritePose pose, int frame)
        {
            int normalized;
            switch (pose)
            {
                case StrategyBirdSpritePose.Peck:
                    normalized = NormalizeFrame(frame, PeckFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(0, 0, -1, -2, 0),
                        2 => new BirdFrame(0, 1, -2, -5, 0),
                        3 => new BirdFrame(0, 1, -2, -6, 0),
                        4 => new BirdFrame(0, 0, -1, -3, 0),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
                case StrategyBirdSpritePose.Hop:
                    normalized = NormalizeFrame(frame, HopFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(1, 1, 0, 0, 1),
                        2 => new BirdFrame(3, 2, 0, 1, -1),
                        3 => new BirdFrame(2, 1, 0, 0, 1),
                        4 => new BirdFrame(0, -1, 0, 0, -1),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
                case StrategyBirdSpritePose.Fly:
                    normalized = NormalizeFrame(frame, FlyFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(0, 1, 0, 0, 4),
                        2 => new BirdFrame(1, 2, 0, 0, 7),
                        3 => new BirdFrame(0, 1, 0, 0, 4),
                        4 => new BirdFrame(-1, -1, 0, 0, -5),
                        5 => new BirdFrame(-1, -2, 0, 0, -8),
                        6 => new BirdFrame(0, -1, 0, 0, -5),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
                case StrategyBirdSpritePose.Land:
                    normalized = NormalizeFrame(frame, LandFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(1, 2, 0, 0, 5),
                        2 => new BirdFrame(1, 1, 0, 0, 2),
                        3 => new BirdFrame(0, 0, 0, 0, -1),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
                case StrategyBirdSpritePose.Swim:
                    normalized = NormalizeFrame(frame, SwimFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(0, 0, 0, 0, 1),
                        3 => new BirdFrame(0, 1, 0, 0, -1),
                        5 => new BirdFrame(0, 0, 0, 0, 1),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
                default:
                    normalized = NormalizeFrame(frame, IdleFrameCount);
                    return normalized switch
                    {
                        1 => new BirdFrame(1, 0, 0, 0, 1),
                        3 => new BirdFrame(0, 0, 0, 0, -1),
                        5 => new BirdFrame(1, 0, 0, 0, 1),
                        _ => new BirdFrame(0, 0, 0, 0, 0)
                    };
            }
        }

        private static BirdPalette GetPalette(StrategyBirdSpecies species)
        {
            switch (species)
            {
                case StrategyBirdSpecies.Crow:
                    return new BirdPalette(
                        Rgb(15, 18, 20),
                        Rgb(36, 42, 46),
                        Rgb(22, 27, 31),
                        Rgb(48, 55, 60),
                        Rgb(25, 31, 35),
                        Rgb(55, 62, 66),
                        Rgb(29, 33, 36),
                        Rgb(50, 45, 36),
                        Rgb(20, 18, 16),
                        Rgb(23, 19, 17),
                        Rgb(141, 110, 70));
                case StrategyBirdSpecies.Duck:
                    return new BirdPalette(
                        Rgb(37, 43, 31),
                        Rgb(121, 97, 54),
                        Rgb(89, 69, 43),
                        Rgb(157, 127, 70),
                        Rgb(48, 103, 76),
                        Rgb(61, 130, 89),
                        Rgb(73, 113, 84),
                        Rgb(216, 142, 47),
                        Rgb(183, 93, 37),
                        Rgb(24, 23, 18),
                        Rgb(198, 158, 92));
                default:
                    return new BirdPalette(
                        Rgb(50, 37, 27),
                        Rgb(126, 91, 55),
                        Rgb(88, 61, 39),
                        Rgb(165, 124, 73),
                        Rgb(94, 66, 42),
                        Rgb(181, 142, 87),
                        Rgb(113, 78, 48),
                        Rgb(203, 155, 78),
                        Rgb(129, 83, 45),
                        Rgb(33, 25, 20),
                        Rgb(196, 150, 88));
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

        private readonly struct BirdFrame
        {
            public BirdFrame(int bodyY, int wingY, int headX, int headY, int legOffset)
            {
                BodyY = bodyY;
                WingY = wingY;
                HeadX = headX;
                HeadY = headY;
                LegOffset = legOffset;
            }

            public int BodyY { get; }
            public int WingY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int LegOffset { get; }
        }

        private readonly struct BirdPalette
        {
            public BirdPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color wing,
                Color wingLight,
                Color head,
                Color beak,
                Color leg,
                Color eye,
                Color seed)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                Wing = wing;
                WingLight = wingLight;
                Head = head;
                Beak = beak;
                Leg = leg;
                Eye = eye;
                Seed = seed;
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color Wing { get; }
            public Color WingLight { get; }
            public Color Head { get; }
            public Color Beak { get; }
            public Color Leg { get; }
            public Color Eye { get; }
            public Color Seed { get; }
        }
    }
}
