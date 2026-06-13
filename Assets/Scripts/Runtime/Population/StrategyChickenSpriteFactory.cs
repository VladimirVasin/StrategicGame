using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyChickenSpriteFactory
    {
        private const float AnimalVisualScale = 0.6f;
        private const float PixelsPerUnit = 32f / AnimalVisualScale;
        public const int WalkFrameCount = 6;
        public const int PeckFrameCount = 5;

        private static readonly Sprite[] IdleFrames = new Sprite[1];
        private static readonly Sprite[] WalkFrames = new Sprite[WalkFrameCount];
        private static readonly Sprite[] PeckFrames = new Sprite[PeckFrameCount];

        public static Sprite GetSprite()
        {
            if (IdleFrames[0] == null)
            {
                IdleFrames[0] = CreateSprite(ChickenSpritePose.Idle, 0);
            }

            return IdleFrames[0];
        }

        public static Sprite GetWalkSprite(int frame)
        {
            int normalizedFrame = NormalizeFrame(frame, WalkFrameCount);
            if (WalkFrames[normalizedFrame] == null)
            {
                WalkFrames[normalizedFrame] = CreateSprite(ChickenSpritePose.Walk, normalizedFrame);
            }

            return WalkFrames[normalizedFrame];
        }

        public static Sprite GetPeckSprite(int frame)
        {
            int normalizedFrame = NormalizeFrame(frame, PeckFrameCount);
            if (PeckFrames[normalizedFrame] == null)
            {
                PeckFrames[normalizedFrame] = CreateSprite(ChickenSpritePose.Peck, normalizedFrame);
            }

            return PeckFrames[normalizedFrame];
        }

        private static Sprite CreateSprite(ChickenSpritePose pose, int frame)
        {
            Texture2D texture = new Texture2D(18, 16, TextureFormat.RGBA32, false)
            {
                name = pose == ChickenSpritePose.Idle
                    ? "Chicken Sprite"
                    : $"Chicken {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[18 * 16]);

            Color outline = Rgb(54, 39, 31);
            Color feather = Rgb(236, 225, 190);
            Color featherLight = Rgb(255, 246, 214);
            Color wing = Rgb(205, 188, 150);
            Color comb = Rgb(178, 45, 40);
            Color beak = Rgb(223, 142, 46);
            Color leg = Rgb(111, 74, 43);

            ChickenFrame chickenFrame = GetFrame(pose, frame);

            FillEllipse(texture, 9, 7 + chickenFrame.BodyY, 6, 4, outline);
            FillEllipse(texture, 9, 8 + chickenFrame.BodyY, 5, 4, feather);
            FillEllipse(texture, 11, 8 + chickenFrame.BodyY + chickenFrame.WingY, 2, 2, wing);
            FillEllipse(texture, 6 + chickenFrame.HeadX, 11 + chickenFrame.HeadY, 3, 3, outline);
            FillEllipse(texture, 6 + chickenFrame.HeadX, 11 + chickenFrame.HeadY, 2, 2, featherLight);
            SetPixelSafe(texture, 5 + chickenFrame.HeadX, 12 + chickenFrame.HeadY, outline);
            FillRect(texture, 4 + chickenFrame.HeadX, 14 + chickenFrame.HeadY, 3, 1, comb);
            FillRect(texture, 15 + chickenFrame.HeadX, 9 + chickenFrame.HeadY, 2, 1, beak);
            SetPixelSafe(texture, 7 + chickenFrame.HeadX, 12 + chickenFrame.HeadY, outline);
            FillRect(texture, 7 + chickenFrame.LeftLegX, 1, 1, 3 + chickenFrame.BodyY, leg);
            FillRect(texture, 11 + chickenFrame.RightLegX, 1, 1, 3 + chickenFrame.BodyY, leg);
            FillRect(texture, 6 + chickenFrame.LeftFootX, 0, 3, 1, leg);
            FillRect(texture, 10 + chickenFrame.RightFootX, 0, 3, 1, leg);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 18f, 16f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static ChickenFrame GetFrame(ChickenSpritePose pose, int frame)
        {
            if (pose == ChickenSpritePose.Peck)
            {
                return NormalizeFrame(frame, PeckFrameCount) switch
                {
                    1 => new ChickenFrame(0, 1, -1, 0, 0, 0, 0, 1),
                    2 => new ChickenFrame(0, 2, -3, 0, 0, 0, 0, 2),
                    3 => new ChickenFrame(0, 1, -2, 0, 0, 0, 0, 1),
                    _ => ChickenFrame.Idle
                };
            }

            if (pose != ChickenSpritePose.Walk)
            {
                return ChickenFrame.Idle;
            }

            return NormalizeFrame(frame, WalkFrameCount) switch
            {
                1 => new ChickenFrame(0, 0, 0, -1, 1, -1, 1, -1),
                2 => new ChickenFrame(1, 0, 1, -2, 2, -2, 2, 1),
                3 => new ChickenFrame(0, 0, 0, -1, 1, -1, 1, 0),
                4 => new ChickenFrame(0, 0, 0, 1, -1, 1, -1, -1),
                5 => new ChickenFrame(1, 0, 1, 2, -2, 2, -2, 1),
                _ => ChickenFrame.Idle
            };
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

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
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

        private enum ChickenSpritePose
        {
            Idle,
            Walk,
            Peck
        }

        private readonly struct ChickenFrame
        {
            public ChickenFrame(
                int bodyY,
                int headX,
                int headY,
                int leftLegX,
                int rightLegX,
                int leftFootX,
                int rightFootX,
                int wingY)
            {
                BodyY = bodyY;
                HeadX = headX;
                HeadY = headY;
                LeftLegX = leftLegX;
                RightLegX = rightLegX;
                LeftFootX = leftFootX;
                RightFootX = rightFootX;
                WingY = wingY;
            }

            public static ChickenFrame Idle => new ChickenFrame(0, 0, 0, 0, 0, 0, 0, 0);

            public int BodyY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int LeftLegX { get; }
            public int RightLegX { get; }
            public int LeftFootX { get; }
            public int RightFootX { get; }
            public int WingY { get; }
        }
    }
}
