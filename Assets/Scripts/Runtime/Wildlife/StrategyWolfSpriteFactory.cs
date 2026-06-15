using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyWolfSpritePose
    {
        Idle,
        Walk,
        Run,
        Stalk,
        Attack,
        Eat,
        Howl
    }

    internal static class StrategyWolfSpriteFactory
    {
        private const float AnimalVisualScale = 0.64f;
        private const float PixelsPerUnit = 38f / AnimalVisualScale;
        public const int IdleFrameCount = 6;
        public const int WalkFrameCount = 8;
        public const int RunFrameCount = 8;
        public const int StalkFrameCount = 6;
        public const int AttackFrameCount = 7;
        public const int EatFrameCount = 8;
        public const int HowlFrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetIdleSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Idle, NormalizeFrame(frame, IdleFrameCount));
        }

        public static Sprite GetWalkSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Walk, NormalizeFrame(frame, WalkFrameCount));
        }

        public static Sprite GetRunSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Run, NormalizeFrame(frame, RunFrameCount));
        }

        public static Sprite GetStalkSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Stalk, NormalizeFrame(frame, StalkFrameCount));
        }

        public static Sprite GetAttackSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Attack, Mathf.Clamp(frame, 0, AttackFrameCount - 1));
        }

        public static Sprite GetEatSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Eat, NormalizeFrame(frame, EatFrameCount));
        }

        public static Sprite GetHowlSprite(int variant, int frame)
        {
            return GetSprite(variant, StrategyWolfSpritePose.Howl, NormalizeFrame(frame, HowlFrameCount));
        }

        private static Sprite GetSprite(int variant, StrategyWolfSpritePose pose, int frame)
        {
            int normalizedVariant = Mathf.Abs(variant) % 4;
            int cacheKey = (normalizedVariant * 4096) + ((int)pose * 128) + frame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(normalizedVariant, pose, frame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(int variant, StrategyWolfSpritePose pose, int frame)
        {
            const int width = 66;
            const int height = 46;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Wolf {variant + 1} {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            WolfPalette palette = GetPalette(variant);
            WolfFrame wolfFrame = GetFrame(pose, frame);
            DrawWolf(texture, pose, frame, wolfFrame, palette);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 3f, 60f, 40f), new Vector2(0.48f, 0.14f), PixelsPerUnit);
        }

        private static WolfFrame GetFrame(StrategyWolfSpritePose pose, int frame)
        {
            float phase = frame / (float)Mathf.Max(1, GetFrameCount(pose));
            int wave = Mathf.RoundToInt(Mathf.Sin(phase * Mathf.PI * 2f) * 2f);
            int fastWave = Mathf.RoundToInt(Mathf.Sin(phase * Mathf.PI * 4f) * 2f);
            return pose switch
            {
                StrategyWolfSpritePose.Walk => new WolfFrame(0, Mathf.Abs(wave) / 2, wave, -wave, wave / 2, wave / 2, 0, 0, 0),
                StrategyWolfSpritePose.Run => new WolfFrame(fastWave / 2, Mathf.Abs(fastWave) / 2, fastWave + 1, -fastWave - 1, fastWave / 2, 0, 0, 0, 1),
                StrategyWolfSpritePose.Stalk => new WolfFrame(-1, -2 + Mathf.Abs(wave) / 2, wave / 2, -wave / 2, -2, -2, 0, -1, -2),
                StrategyWolfSpritePose.Attack => new WolfFrame(frame < 3 ? frame : 3 - frame / 2, frame >= 3 ? -1 : 0, 3, -2, 2, frame >= 2 ? 1 : 0, frame >= 2 && frame <= 5 ? 1 : 0, 0, 2),
                StrategyWolfSpritePose.Eat => new WolfFrame(0, -2, wave / 3, -wave / 3, -3, -5 + Mathf.Abs(wave), frame % 2, -1, -2),
                StrategyWolfSpritePose.Howl => new WolfFrame(0, 0, wave / 2, -wave / 2, -1, 6 + Mathf.Abs(wave), 1, 2, 1),
                _ => new WolfFrame(0, Mathf.Abs(wave) / 3, wave / 3, -wave / 3, wave / 2, wave / 3, 0, 0, 0)
            };
        }

        private static int GetFrameCount(StrategyWolfSpritePose pose)
        {
            return pose switch
            {
                StrategyWolfSpritePose.Walk => WalkFrameCount,
                StrategyWolfSpritePose.Run => RunFrameCount,
                StrategyWolfSpritePose.Stalk => StalkFrameCount,
                StrategyWolfSpritePose.Attack => AttackFrameCount,
                StrategyWolfSpritePose.Eat => EatFrameCount,
                StrategyWolfSpritePose.Howl => HowlFrameCount,
                _ => IdleFrameCount
            };
        }

        private static void DrawWolf(
            Texture2D texture,
            StrategyWolfSpritePose pose,
            int spriteFrame,
            WolfFrame frame,
            WolfPalette palette)
        {
            int bodyX = 30 + frame.BodyX;
            int bodyY = 18 + frame.BodyY;
            int headX = 46 + frame.BodyX + (pose == StrategyWolfSpritePose.Attack ? 3 : 0);
            int headY = bodyY + 7 + frame.HeadY;
            if (pose == StrategyWolfSpritePose.Eat)
            {
                headX = 45 + frame.BodyX;
                headY = bodyY + frame.HeadY;
            }

            FillEllipse(texture, 29, 9, 20, 5, new Color(0f, 0f, 0f, 0.22f));
            DrawTail(texture, bodyX - 18, bodyY + 4, frame.TailY, palette);
            DrawLegs(texture, bodyX, bodyY, frame, palette);

            FillEllipse(texture, bodyX - 2, bodyY + 2, 18 + frame.Stretch, 9, palette.Outline);
            FillEllipse(texture, bodyX - 2, bodyY + 3, 17 + frame.Stretch, 8, palette.BodyDark);
            FillEllipse(texture, bodyX + 1, bodyY + 4, 13 + frame.Stretch, 6, palette.Body);
            FillEllipse(texture, bodyX + 7, bodyY + 5, 7, 4, palette.Highlight);
            DrawShoulder(texture, bodyX + 13, bodyY + 5, palette);

            DrawNeck(texture, headX - 7, headY - 4, bodyX + 12, bodyY + 7, palette);
            DrawHead(texture, headX, headY, pose, frame, palette);

            if (pose == StrategyWolfSpritePose.Eat)
            {
                DrawGroundMeal(texture, bodyX + 17, 8, spriteFrame, palette);
            }
        }

        private static void DrawTail(Texture2D texture, int x, int y, int tailY, WolfPalette palette)
        {
            DrawLine(texture, x + 1, y + tailY, x - 11, y + tailY + 5, palette.Outline, 5);
            DrawLine(texture, x + 1, y + tailY + 1, x - 10, y + tailY + 5, palette.BodyDark, 3);
            SetPixelSafe(texture, x - 12, y + tailY + 5, palette.Outline);
        }

        private static void DrawLegs(Texture2D texture, int bodyX, int bodyY, WolfFrame frame, WolfPalette palette)
        {
            int groundY = 8;
            DrawLeg(texture, bodyX - 10, bodyY - 4, groundY, frame.FrontLeg, palette);
            DrawLeg(texture, bodyX - 4, bodyY - 4, groundY, -frame.FrontLeg / 2, palette);
            DrawLeg(texture, bodyX + 8, bodyY - 4, groundY, frame.BackLeg, palette);
            DrawLeg(texture, bodyX + 14, bodyY - 4, groundY, -frame.BackLeg / 2, palette);
        }

        private static void DrawLeg(Texture2D texture, int x, int y, int groundY, int bend, WolfPalette palette)
        {
            DrawLine(texture, x, y, x + bend / 2, groundY + 2, palette.Outline, 3);
            DrawLine(texture, x, y - 1, x + bend / 2, groundY + 3, palette.BodyDark, 1);
            DrawLine(texture, x + bend / 2, groundY + 2, x + bend, groundY, palette.Outline, 3);
            DrawLine(texture, x + bend / 2, groundY + 3, x + bend, groundY + 1, palette.BodyDark, 1);
            DrawLine(texture, x + bend, groundY, x + bend + 4, groundY, palette.Outline, 2);
        }

        private static void DrawShoulder(Texture2D texture, int x, int y, WolfPalette palette)
        {
            FillEllipse(texture, x, y, 7, 7, palette.Outline);
            FillEllipse(texture, x, y + 1, 6, 6, palette.BodyDark);
            FillEllipse(texture, x + 1, y + 2, 4, 4, palette.Body);
        }

        private static void DrawNeck(Texture2D texture, int headX, int headY, int bodyX, int bodyY, WolfPalette palette)
        {
            DrawLine(texture, bodyX, bodyY, headX, headY, palette.Outline, 7);
            DrawLine(texture, bodyX, bodyY + 1, headX, headY, palette.BodyDark, 5);
            DrawLine(texture, bodyX + 1, bodyY + 2, headX + 1, headY, palette.Body, 3);
        }

        private static void DrawHead(
            Texture2D texture,
            int headX,
            int headY,
            StrategyWolfSpritePose pose,
            WolfFrame frame,
            WolfPalette palette)
        {
            FillEllipse(texture, headX - 2, headY, 8, 7, palette.Outline);
            FillEllipse(texture, headX - 1, headY + 1, 7, 6, palette.FaceDark);
            FillEllipse(texture, headX + 2, headY + 2, 5, 4, palette.Face);

            DrawTriangle(texture, headX - 6, headY + 6, headX - 2, headY + 13 + frame.EarY, headX + 1, headY + 6, palette.Outline);
            DrawTriangle(texture, headX - 5, headY + 6, headX - 2, headY + 11 + frame.EarY, headX, headY + 6, palette.Ear);
            DrawTriangle(texture, headX + 2, headY + 6, headX + 6, headY + 13 + frame.EarY, headX + 8, headY + 5, palette.Outline);
            DrawTriangle(texture, headX + 3, headY + 6, headX + 6, headY + 11 + frame.EarY, headX + 7, headY + 5, palette.Ear);

            int snoutY = pose == StrategyWolfSpritePose.Howl ? headY + 4 : headY - 1;
            int snoutX = pose == StrategyWolfSpritePose.Howl ? headX + 8 : headX + 9;
            FillEllipse(texture, snoutX, snoutY, 7, frame.MouthOpen > 0 ? 4 : 3, palette.Outline);
            FillEllipse(texture, snoutX + 1, snoutY, 6, frame.MouthOpen > 0 ? 3 : 2, palette.Muzzle);
            SetPixelSafe(texture, snoutX + 7, snoutY, palette.Nose);
            SetPixelSafe(texture, headX + 4, headY + 3, palette.Outline);
            if (frame.MouthOpen > 0)
            {
                DrawLine(texture, snoutX + 2, snoutY - 2, snoutX + 7, snoutY - 3, palette.Mouth, 1);
                SetPixelSafe(texture, snoutX + 5, snoutY - 4, Color.white);
            }
        }

        private static void DrawGroundMeal(Texture2D texture, int x, int y, int frame, WolfPalette palette)
        {
            FillEllipse(texture, x, y, 8, 3, new Color(0.19f, 0.08f, 0.05f, 0.84f));
            FillEllipse(texture, x - 3 + (frame % 3), y + 1, 3, 2, palette.BodyDark);
        }

        private static WolfPalette GetPalette(int variant)
        {
            return variant switch
            {
                1 => new WolfPalette(
                    new Color(0.10f, 0.12f, 0.12f, 1f),
                    new Color(0.26f, 0.29f, 0.29f, 1f),
                    new Color(0.38f, 0.40f, 0.38f, 1f),
                    new Color(0.52f, 0.55f, 0.50f, 1f),
                    new Color(0.46f, 0.42f, 0.36f, 1f),
                    new Color(0.18f, 0.10f, 0.08f, 1f)),
                2 => new WolfPalette(
                    new Color(0.12f, 0.10f, 0.09f, 1f),
                    new Color(0.34f, 0.28f, 0.22f, 1f),
                    new Color(0.48f, 0.39f, 0.29f, 1f),
                    new Color(0.63f, 0.52f, 0.39f, 1f),
                    new Color(0.57f, 0.48f, 0.36f, 1f),
                    new Color(0.16f, 0.08f, 0.06f, 1f)),
                3 => new WolfPalette(
                    new Color(0.08f, 0.08f, 0.08f, 1f),
                    new Color(0.18f, 0.19f, 0.20f, 1f),
                    new Color(0.27f, 0.29f, 0.31f, 1f),
                    new Color(0.42f, 0.44f, 0.45f, 1f),
                    new Color(0.34f, 0.31f, 0.29f, 1f),
                    new Color(0.14f, 0.06f, 0.05f, 1f)),
                _ => new WolfPalette(
                    new Color(0.10f, 0.11f, 0.10f, 1f),
                    new Color(0.30f, 0.32f, 0.29f, 1f),
                    new Color(0.45f, 0.47f, 0.41f, 1f),
                    new Color(0.60f, 0.61f, 0.53f, 1f),
                    new Color(0.50f, 0.46f, 0.38f, 1f),
                    new Color(0.16f, 0.08f, 0.06f, 1f))
            };
        }

        private static int NormalizeFrame(int frame, int count)
        {
            return count <= 0 ? 0 : Mathf.Abs(frame) % count;
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int minX = centerX - radiusX;
            int maxX = centerX + radiusX;
            int minY = centerY - radiusY;
            int maxY = centerY + radiusY;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) / (float)Mathf.Max(1, radiusX);
                    float dy = (y - centerY) / (float)Mathf.Max(1, radiusY);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawTriangle(Texture2D texture, int ax, int ay, int bx, int by, int cx, int cy, Color color)
        {
            int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
            int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            int minY = Mathf.Min(ay, Mathf.Min(by, cy));
            int maxY = Mathf.Max(ay, Mathf.Max(by, cy));
            float area = Edge(ax, ay, bx, by, cx, cy);
            if (Mathf.Approximately(area, 0f))
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(bx, by, cx, cy, x, y);
                    float w1 = Edge(cx, cy, ax, ay, x, y);
                    float w2 = Edge(ax, ay, bx, by, x, y);
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static float Edge(int ax, int ay, int bx, int by, int px, int py)
        {
            return ((px - ax) * (by - ay)) - ((py - ay) * (bx - ax));
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            int radius = Mathf.Max(0, thickness / 2);
            while (true)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        SetPixelSafe(texture, x0 + x, y0 + y, color);
                    }
                }

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
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

        private readonly struct WolfFrame
        {
            public WolfFrame(
                int bodyX,
                int bodyY,
                int frontLeg,
                int backLeg,
                int tailY,
                int headY,
                int mouthOpen,
                int earY,
                int stretch)
            {
                BodyX = bodyX;
                BodyY = bodyY;
                FrontLeg = frontLeg;
                BackLeg = backLeg;
                TailY = tailY;
                HeadY = headY;
                MouthOpen = mouthOpen;
                EarY = earY;
                Stretch = stretch;
            }

            public int BodyX { get; }
            public int BodyY { get; }
            public int FrontLeg { get; }
            public int BackLeg { get; }
            public int TailY { get; }
            public int HeadY { get; }
            public int MouthOpen { get; }
            public int EarY { get; }
            public int Stretch { get; }
        }

        private readonly struct WolfPalette
        {
            public WolfPalette(Color outline, Color bodyDark, Color body, Color highlight, Color ear, Color mouth)
            {
                Outline = outline;
                BodyDark = bodyDark;
                Body = body;
                Highlight = highlight;
                Ear = ear;
                Mouth = mouth;
                FaceDark = Color.Lerp(bodyDark, outline, 0.25f);
                Face = Color.Lerp(body, highlight, 0.35f);
                Muzzle = Color.Lerp(highlight, Color.white, 0.25f);
                Nose = new Color(0.035f, 0.03f, 0.025f, 1f);
            }

            public Color Outline { get; }
            public Color BodyDark { get; }
            public Color Body { get; }
            public Color Highlight { get; }
            public Color Ear { get; }
            public Color Mouth { get; }
            public Color FaceDark { get; }
            public Color Face { get; }
            public Color Muzzle { get; }
            public Color Nose { get; }
        }
    }
}
