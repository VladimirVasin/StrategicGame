using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyShadowShape
    {
        SoftEllipse,
        CastOval,
        WideCastOval
    }

    internal static class StrategyShadowSpriteFactory
    {
        private static Sprite softEllipse;
        private static Sprite castOval;
        private static Sprite wideCastOval;

        public static Sprite Get(StrategyShadowShape shape)
        {
            return shape switch
            {
                StrategyShadowShape.CastOval => castOval ??= CreateShadowSprite("Cast Shadow Oval", 96, 34, 0.44f, 0.30f, 0.52f),
                StrategyShadowShape.WideCastOval => wideCastOval ??= CreateShadowSprite("Wide Cast Shadow Oval", 128, 40, 0.46f, 0.28f, 0.50f),
                _ => softEllipse ??= CreateShadowSprite("Soft Ground Shadow Ellipse", 64, 24, 0.42f, 0.31f, 0.54f)
            };
        }

        private static Sprite CreateShadowSprite(
            string name,
            int width,
            int height,
            float radiusXFactor,
            float radiusYFactor,
            float centerAlpha)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels(new Color[width * height]);
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = Mathf.Max(1f, width * radiusXFactor);
            float radiusY = Mathf.Max(1f, height * radiusYFactor);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = dx * dx + dy * dy;
                    if (distance > 1f)
                    {
                        continue;
                    }

                    float edge = Mathf.Clamp01(1f - distance);
                    float alpha = centerAlpha * edge * edge;
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                32f);
            sprite.name = name + " Sprite";
            return sprite;
        }
    }
}
