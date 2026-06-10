using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCampfireAmbientSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        public const int FrameCount = 8;

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
            Texture2D texture = new Texture2D(36, 54, TextureFormat.RGBA32, false)
            {
                name = $"Campfire Smoke Sparks {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[36 * 54]);

            DrawSmoke(texture, frame);
            DrawSparks(texture, frame);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 36f, 54f), new Vector2(0.5f, 0.05f), PixelsPerUnit);
        }

        private static void DrawSmoke(Texture2D texture, int frame)
        {
            Color smoke = new Color(0.58f, 0.59f, 0.56f, 0.20f);
            Color smokeLight = new Color(0.76f, 0.76f, 0.70f, 0.14f);
            int sway = frame <= 3 ? frame : 7 - frame;
            int side = frame < 4 ? -1 : 1;

            FillEllipse(texture, 18 + side * sway, 31 + frame / 2, 4, 3, smoke);
            FillEllipse(texture, 16 + side * (sway + 1), 38 + frame / 2, 5, 4, smokeLight);
            if (frame % 2 == 0)
            {
                FillEllipse(texture, 21 + side * sway, 45, 4, 3, smokeLight);
            }
        }

        private static void DrawSparks(Texture2D texture, int frame)
        {
            Color spark = new Color(1f, 0.70f, 0.18f, 0.85f);
            Color sparkHot = new Color(1f, 0.94f, 0.48f, 0.90f);
            for (int i = 0; i < 5; i++)
            {
                int x = 13 + PositiveModulo(frame * 5 + i * 7, 12);
                int y = 22 + PositiveModulo(frame * 3 + i * 9, 16);
                Color color = i % 2 == 0 ? sparkHot : spark;
                SetPixelSafe(texture, x, y, color);
                if ((frame + i) % 3 == 0)
                {
                    SetPixelSafe(texture, x, y + 1, color);
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

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static int NormalizeFrame(int frame)
        {
            int normalized = frame % FrameCount;
            return normalized < 0 ? normalized + FrameCount : normalized;
        }
    }
}
