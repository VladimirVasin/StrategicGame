using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyDeerSpritePose
    {
        Idle,
        Walk,
        Graze,
        Alert,
        Run,
        Rest
    }

    internal static class StrategyDeerSpriteFactory
    {
        private const float PixelsPerUnit = 34f;
        public const int IdleFrameCount = 6;
        public const int WalkFrameCount = 8;
        public const int GrazeFrameCount = 8;
        public const int AlertFrameCount = 6;
        public const int RunFrameCount = 10;
        public const int RestFrameCount = 6;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetIdleSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Idle, NormalizeFrame(frame, IdleFrameCount));
        }

        public static Sprite GetWalkSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Walk, NormalizeFrame(frame, WalkFrameCount));
        }

        public static Sprite GetGrazeSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Graze, NormalizeFrame(frame, GrazeFrameCount));
        }

        public static Sprite GetAlertSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Alert, NormalizeFrame(frame, AlertFrameCount));
        }

        public static Sprite GetRunSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Run, NormalizeFrame(frame, RunFrameCount));
        }

        public static Sprite GetRestSprite(StrategyDeerSex sex, int frame)
        {
            return GetSprite(sex, StrategyDeerSpritePose.Rest, NormalizeFrame(frame, RestFrameCount));
        }

        private static Sprite GetSprite(StrategyDeerSex sex, StrategyDeerSpritePose pose, int frame)
        {
            int cacheKey = ((int)sex * 4096) + ((int)pose * 128) + frame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(sex, pose, frame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyDeerSex sex, StrategyDeerSpritePose pose, int frame)
        {
            Texture2D texture = new Texture2D(78, 58, TextureFormat.RGBA32, false)
            {
                name = $"Deer {sex} {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[78 * 58]);

            DeerFrame deerFrame = GetFrame(pose, frame);
            DeerPalette palette = GetPalette(sex);
            int sexBodyBonus = sex == StrategyDeerSex.Male ? 2 : 0;
            int sexHeightBonus = sex == StrategyDeerSex.Male ? 1 : 0;
            int baseY = 8 + deerFrame.BodyY;
            int bodyCenterX = 35;
            int bodyCenterY = 25 + deerFrame.BodyY;

            FillEllipse(texture, bodyCenterX, 8, 22 + sexBodyBonus, 5, new Color(0f, 0f, 0f, 0.24f));

            if (pose == StrategyDeerSpritePose.Rest)
            {
                DrawRestingDeer(texture, sex, deerFrame, palette);
                texture.Apply(false, false);
                return Sprite.Create(texture, new Rect(4f, 4f, 70f, 50f), new Vector2(0.50f, 0.10f), PixelsPerUnit);
            }

            FillEllipse(texture, bodyCenterX - 3, bodyCenterY - 1, 20 + sexBodyBonus, 10 + sexHeightBonus, palette.Outline);
            FillEllipse(texture, bodyCenterX - 3, bodyCenterY, 18 + sexBodyBonus, 8 + sexHeightBonus, palette.BodyDark);
            FillEllipse(texture, bodyCenterX - 5, bodyCenterY + 1, 15 + sexBodyBonus, 7 + sexHeightBonus, palette.Body);
            FillEllipse(texture, bodyCenterX + 8, bodyCenterY + 1, 10 + sexBodyBonus, 7 + sexHeightBonus, palette.BodyLight);
            FillEllipse(texture, bodyCenterX - 14, bodyCenterY + 1, 8 + sexBodyBonus, 7 + sexHeightBonus, palette.Body);

            DrawTail(texture, bodyCenterX - 25, bodyCenterY + 4 + deerFrame.TailY, palette);
            DrawLegs(texture, baseY, bodyCenterY, deerFrame, palette);
            DrawNeckAndHead(texture, sex, deerFrame, palette);
            AddHideDetails(texture, sex, pose, frame, palette);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 70f, 50f), new Vector2(0.50f, 0.10f), PixelsPerUnit);
        }

        private static void DrawRestingDeer(Texture2D texture, StrategyDeerSex sex, DeerFrame deerFrame, DeerPalette palette)
        {
            int bodyCenterX = 35;
            int bodyCenterY = 17 + deerFrame.BodyY;
            int sexBodyBonus = sex == StrategyDeerSex.Male ? 2 : 0;

            FillEllipse(texture, bodyCenterX, 7, 22 + sexBodyBonus, 5, new Color(0f, 0f, 0f, 0.22f));
            FillEllipse(texture, bodyCenterX - 2, bodyCenterY, 20 + sexBodyBonus, 9, palette.Outline);
            FillEllipse(texture, bodyCenterX - 2, bodyCenterY + 1, 18 + sexBodyBonus, 7, palette.BodyDark);
            FillEllipse(texture, bodyCenterX - 4, bodyCenterY + 2, 14 + sexBodyBonus, 6, palette.Body);
            FillRect(texture, bodyCenterX - 17, 8, 20, 3, palette.LegDark);
            FillRect(texture, bodyCenterX + 3, 7, 18, 3, palette.LegDark);
            FillRect(texture, bodyCenterX - 15, 10, 18, 2, palette.Leg);
            FillRect(texture, bodyCenterX + 5, 9, 15, 2, palette.Leg);
            DrawTail(texture, bodyCenterX - 24, bodyCenterY + 3, palette);

            DeerFrame headFrame = new DeerFrame(0, 0, deerFrame.HeadY - 3, 0, 0, 0, 0, 0, 0, 46, 25 + deerFrame.HeadY);
            DrawNeckAndHead(texture, sex, headFrame, palette);
            AddHideDetails(texture, sex, StrategyDeerSpritePose.Rest, deerFrame.BodyY + 3, palette);
        }

        private static void DrawLegs(Texture2D texture, int baseY, int bodyCenterY, DeerFrame frame, DeerPalette palette)
        {
            int hipY = bodyCenterY - 5;
            int kneeY = baseY + 7;
            DrawLeg(texture, 22, hipY, 22 + frame.BackLegA, kneeY, 21 + frame.BackHoofA, baseY, palette.LegDark, palette.Outline);
            DrawLeg(texture, 31, hipY, 31 + frame.BackLegB, kneeY, 31 + frame.BackHoofB, baseY, palette.Leg, palette.Outline);
            DrawLeg(texture, 44, hipY, 44 + frame.FrontLegA, kneeY, 44 + frame.FrontHoofA, baseY, palette.LegDark, palette.Outline);
            DrawLeg(texture, 52, hipY, 52 + frame.FrontLegB, kneeY, 53 + frame.FrontHoofB, baseY, palette.Leg, palette.Outline);
        }

        private static void DrawLeg(
            Texture2D texture,
            int topX,
            int topY,
            int kneeX,
            int kneeY,
            int hoofX,
            int hoofY,
            Color leg,
            Color outline)
        {
            DrawThickLine(texture, P(topX, topY), P(kneeX, kneeY), outline, 2);
            DrawThickLine(texture, P(kneeX, kneeY), P(hoofX, hoofY), outline, 2);
            DrawThickLine(texture, P(topX, topY), P(kneeX, kneeY), leg, 1);
            DrawThickLine(texture, P(kneeX, kneeY), P(hoofX, hoofY), leg, 1);
            FillRect(texture, hoofX - 2, hoofY - 1, 5, 2, outline);
        }

        private static void DrawNeckAndHead(Texture2D texture, StrategyDeerSex sex, DeerFrame frame, DeerPalette palette)
        {
            Vector2Int neckBase = P(50, 30 + frame.BodyY);
            Vector2Int neckTop = P(frame.NeckX, frame.NeckY);
            Vector2Int head = P(frame.HeadX, frame.HeadY);

            DrawThickLine(texture, neckBase, neckTop, palette.Outline, 5);
            DrawThickLine(texture, neckBase + P(0, 1), neckTop + P(-1, 0), palette.BodyDark, 3);
            DrawThickLine(texture, neckBase + P(1, 1), neckTop + P(0, 1), palette.Body, 2);

            FillEllipse(texture, head.x, head.y, 8, 5, palette.Outline);
            FillEllipse(texture, head.x + 1, head.y, 6, 4, palette.Face);
            FillEllipse(texture, head.x + 6, head.y - 1, 5, 3, palette.Outline);
            FillEllipse(texture, head.x + 7, head.y - 1, 3, 2, palette.Muzzle);
            SetPixelSafe(texture, head.x + 3, head.y + 2, palette.Outline);
            SetPixelSafe(texture, head.x + 9, head.y - 1, palette.Outline);

            FillEllipse(texture, head.x - 2, head.y + 6 + frame.EarY, 2, 5, palette.Outline);
            FillEllipse(texture, head.x - 1, head.y + 6 + frame.EarY, 1, 4, palette.BodyLight);
            FillEllipse(texture, head.x + 4, head.y + 5 - frame.EarY, 2, 5, palette.Outline);
            FillEllipse(texture, head.x + 5, head.y + 5 - frame.EarY, 1, 4, palette.BodyLight);

            if (sex == StrategyDeerSex.Male)
            {
                DrawAntlers(texture, head.x - 1, head.y + 7, palette.Antler, palette.Outline, frame.EarY);
            }
        }

        private static void DrawTail(Texture2D texture, int x, int y, DeerPalette palette)
        {
            FillEllipse(texture, x, y, 4, 3, palette.Outline);
            FillEllipse(texture, x - 1, y + 1, 3, 2, palette.Tail);
        }

        private static void DrawAntlers(Texture2D texture, int x, int y, Color antler, Color outline, int twitch)
        {
            DrawThickLine(texture, P(x, y), P(x - 3, y + 8), outline, 2);
            DrawThickLine(texture, P(x + 4, y - 1), P(x + 8, y + 8), outline, 2);
            DrawThickLine(texture, P(x, y), P(x - 3, y + 8), antler, 1);
            DrawThickLine(texture, P(x + 4, y - 1), P(x + 8, y + 8), antler, 1);

            DrawLine(texture, P(x - 3, y + 5), P(x - 8, y + 8 + twitch), antler);
            DrawLine(texture, P(x - 2, y + 7), P(x + 1, y + 12), antler);
            DrawLine(texture, P(x + 7, y + 5), P(x + 12, y + 8 - twitch), antler);
            DrawLine(texture, P(x + 7, y + 7), P(x + 4, y + 12), antler);
        }

        private static void AddHideDetails(
            Texture2D texture,
            StrategyDeerSex sex,
            StrategyDeerSpritePose pose,
            int frame,
            DeerPalette palette)
        {
            int count = sex == StrategyDeerSex.Female ? 8 : 5;
            if (pose == StrategyDeerSpritePose.Run)
            {
                count += 2;
            }

            for (int i = 0; i < count; i++)
            {
                int x = 20 + ((frame * 11 + i * 7 + (int)pose * 3) % 31);
                int y = 24 + ((frame * 5 + i * 3) % 8);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? palette.BodyLight : palette.BodyDark);
            }
        }

        private static DeerFrame GetFrame(StrategyDeerSpritePose pose, int frame)
        {
            int normalized;
            switch (pose)
            {
                case StrategyDeerSpritePose.Walk:
                    normalized = NormalizeFrame(frame, WalkFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(1, -2, 2, -1, 1, -2, 2, 0, 1, 57, 36),
                        2 => new DeerFrame(1, -4, 4, -2, 2, -3, 3, 1, 0, 58, 36),
                        3 => new DeerFrame(0, -2, 2, -1, 1, -2, 2, 0, -1, 57, 35),
                        4 => new DeerFrame(0, 2, -2, 1, -1, 2, -2, -1, 0, 56, 35),
                        5 => new DeerFrame(1, 4, -4, 2, -2, 3, -3, -1, 1, 57, 36),
                        6 => new DeerFrame(1, 2, -2, 1, -1, 2, -2, 0, 0, 58, 36),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 57, 35)
                    };
                case StrategyDeerSpritePose.Run:
                    normalized = NormalizeFrame(frame, RunFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(2, -5, 4, -3, 3, -5, 4, 2, 1, 58, 36),
                        2 => new DeerFrame(1, -7, 6, -4, 4, -6, 6, 2, 0, 59, 35),
                        3 => new DeerFrame(0, -3, 2, -1, 1, -2, 2, 1, -1, 58, 35),
                        4 => new DeerFrame(2, 5, -4, 3, -3, 5, -4, -1, 1, 58, 36),
                        5 => new DeerFrame(1, 7, -6, 4, -4, 6, -6, -2, 0, 59, 35),
                        6 => new DeerFrame(0, 3, -2, 1, -1, 2, -2, -1, -1, 58, 35),
                        7 => new DeerFrame(1, -4, 4, -2, 2, -4, 3, 1, 0, 57, 36),
                        8 => new DeerFrame(2, 4, -4, 2, -2, 4, -3, -1, 1, 57, 36),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 35)
                    };
                case StrategyDeerSpritePose.Graze:
                    normalized = NormalizeFrame(frame, GrazeFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, -1, 0, 57, 28),
                        2 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, -1, 1, 58, 22),
                        3 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 1, 60, 18),
                        4 => new DeerFrame(0, 1, -1, 0, 0, 1, -1, 0, 0, 61, 17),
                        5 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, -1, 60, 18),
                        6 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, -1, 0, 58, 23),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, -1, 0, 57, 30)
                    };
                case StrategyDeerSpritePose.Alert:
                    normalized = NormalizeFrame(frame, AlertFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(1, 0, 0, 0, 0, 0, 0, 1, 1, 55, 42),
                        3 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 1, -1, 55, 43),
                        5 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 1, 1, 56, 42),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 1, 0, 55, 42)
                    };
                case StrategyDeerSpritePose.Rest:
                    normalized = NormalizeFrame(frame, RestFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 1, 48, 25),
                        3 => new DeerFrame(1, 0, 0, 0, 0, 0, 0, 0, -1, 47, 24),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 24)
                    };
                default:
                    normalized = NormalizeFrame(frame, IdleFrameCount);
                    return normalized switch
                    {
                        1 => new DeerFrame(1, 0, 0, 0, 0, 0, 0, 0, 1, 57, 36),
                        3 => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, -1, 57, 35),
                        5 => new DeerFrame(1, 0, 0, 0, 0, 0, 0, 0, 1, 56, 36),
                        _ => new DeerFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 57, 35)
                    };
            }
        }

        private static DeerPalette GetPalette(StrategyDeerSex sex)
        {
            if (sex == StrategyDeerSex.Male)
            {
                Color body = Rgb(126, 86, 49);
                return new DeerPalette(
                    Rgb(43, 30, 22),
                    body,
                    Shift(body, -0.17f),
                    Shift(body, 0.13f),
                    Rgb(151, 104, 58),
                    Rgb(73, 47, 30),
                    Rgb(103, 67, 39),
                    Rgb(203, 174, 121),
                    Rgb(229, 205, 151),
                    Rgb(90, 72, 50));
            }

            Color doeBody = Rgb(153, 112, 69);
            return new DeerPalette(
                Rgb(49, 34, 25),
                doeBody,
                Shift(doeBody, -0.15f),
                Shift(doeBody, 0.15f),
                Rgb(172, 128, 77),
                Rgb(85, 55, 34),
                Rgb(121, 78, 45),
                Rgb(218, 190, 142),
                Rgb(235, 214, 170),
                Rgb(108, 82, 54));
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
                FillRect(texture, x - radius, y - radius, radius * 2 + 1, radius * 2 + 1, color);
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

        private static int NormalizeFrame(int frame, int frameCount)
        {
            if (frameCount <= 0)
            {
                return 0;
            }

            int normalized = frame % frameCount;
            return normalized < 0 ? normalized + frameCount : normalized;
        }

        private readonly struct DeerFrame
        {
            public DeerFrame(
                int bodyY,
                int frontLegA,
                int frontLegB,
                int backLegA,
                int backLegB,
                int frontHoofA,
                int frontHoofB,
                int tailY,
                int earY,
                int neckX,
                int neckY)
            {
                BodyY = bodyY;
                FrontLegA = frontLegA;
                FrontLegB = frontLegB;
                BackLegA = backLegA;
                BackLegB = backLegB;
                FrontHoofA = frontHoofA;
                FrontHoofB = frontHoofB;
                BackHoofA = backLegA;
                BackHoofB = backLegB;
                TailY = tailY;
                EarY = earY;
                NeckX = neckX;
                NeckY = neckY - 4;
                HeadX = neckX + 7;
                HeadY = neckY;
            }

            public int BodyY { get; }
            public int FrontLegA { get; }
            public int FrontLegB { get; }
            public int BackLegA { get; }
            public int BackLegB { get; }
            public int FrontHoofA { get; }
            public int FrontHoofB { get; }
            public int BackHoofA { get; }
            public int BackHoofB { get; }
            public int TailY { get; }
            public int EarY { get; }
            public int NeckX { get; }
            public int NeckY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
        }

        private readonly struct DeerPalette
        {
            public DeerPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color face,
                Color legDark,
                Color leg,
                Color muzzle,
                Color tail,
                Color antler)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                Face = face;
                LegDark = legDark;
                Leg = leg;
                Muzzle = muzzle;
                Tail = tail;
                Antler = antler;
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color Face { get; }
            public Color LegDark { get; }
            public Color Leg { get; }
            public Color Muzzle { get; }
            public Color Tail { get; }
            public Color Antler { get; }
        }
    }
}
