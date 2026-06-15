using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyRabbitSpriteFactory
    {

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

        private readonly struct RabbitFrame
        {
            public RabbitFrame(
                int bodyY,
                int hopY,
                int headX,
                int headY,
                int earTilt,
                int earY,
                int frontFootX,
                int backFootX,
                int bodyStretchX,
                int bodyStretchY,
                int tailY)
            {
                BodyY = bodyY;
                HopY = hopY;
                HeadX = headX;
                HeadY = headY;
                EarTilt = earTilt;
                EarY = earY;
                FrontFootX = frontFootX;
                BackFootX = backFootX;
                BodyStretchX = bodyStretchX;
                BodyStretchY = bodyStretchY;
                TailY = tailY;
            }

            public int BodyY { get; }
            public int HopY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int EarTilt { get; }
            public int EarY { get; }
            public int FrontFootX { get; }
            public int BackFootX { get; }
            public int BodyStretchX { get; }
            public int BodyStretchY { get; }
            public int TailY { get; }
        }

        private readonly struct RabbitPalette
        {
            public RabbitPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color faceDark,
                Color face,
                Color earInner,
                Color ear,
                Color muzzle,
                Color tail,
                Color nose,
                Color foot)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                FaceDark = faceDark;
                Face = face;
                EarInner = earInner;
                Ear = ear;
                Muzzle = muzzle;
                Tail = tail;
                Nose = nose;
                Foot = foot;
                FootLight = Shift(foot, 0.06f);
                Whisker = new Color(0.88f, 0.84f, 0.74f, 0.82f);
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color FaceDark { get; }
            public Color Face { get; }
            public Color EarInner { get; }
            public Color Ear { get; }
            public Color Muzzle { get; }
            public Color Tail { get; }
            public Color Nose { get; }
            public Color Foot { get; }
            public Color FootLight { get; }
            public Color Whisker { get; }
        }
    }
}
