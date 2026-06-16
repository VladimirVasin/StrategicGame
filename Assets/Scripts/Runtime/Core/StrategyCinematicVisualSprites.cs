using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyCinematicVisualSprites
    {
        private static Sprite glowSprite;
        private static Sprite lampCoreSprite;
        private static Sprite windowMaskSprite;
        private static Sprite interiorShadowSprite;
        private static Sprite puddleSprite;
        private static Sprite branchSprite;
        private static Sprite whiteSprite;

        public static Sprite GetGlowSprite()
        {
            if (glowSprite != null)
            {
                return glowSprite;
            }

            Texture2D texture = CreateClearTexture(32, 20, "Cinematic Pixel Glow");
            Vector2 center = new(15.5f, 9.5f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float dx = Mathf.Abs((x - center.x) / 15.5f);
                    float dy = Mathf.Abs((y - center.y) / 9.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d);
                    alpha = alpha * alpha * 0.72f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            glowSprite = CreateSprite(texture, 16f, "Cinematic Pixel Glow Sprite");
            return glowSprite;
        }

        public static Sprite GetLampCoreSprite()
        {
            if (lampCoreSprite != null)
            {
                return lampCoreSprite;
            }

            Texture2D texture = CreateClearTexture(8, 8, "Cinematic Lamp Core");
            FillRect(texture, 3, 1, 2, 6, Color.white);
            FillRect(texture, 2, 3, 4, 2, new Color(1f, 1f, 1f, 0.85f));
            lampCoreSprite = CreateSprite(texture, 18f, "Cinematic Lamp Core Sprite");
            return lampCoreSprite;
        }

        public static Sprite GetWindowMaskSprite()
        {
            if (windowMaskSprite != null)
            {
                return windowMaskSprite;
            }

            Texture2D texture = CreateClearTexture(26, 12, "Cinematic Window Mask");
            DrawWindow(texture, 2, 3);
            DrawWindow(texture, 17, 3);
            FillRect(texture, 12, 2, 2, 8, new Color(1f, 1f, 1f, 0.25f));
            windowMaskSprite = CreateSprite(texture, 18f, "Cinematic Window Mask Sprite");
            return windowMaskSprite;
        }

        public static Sprite GetInteriorShadowSprite()
        {
            if (interiorShadowSprite != null)
            {
                return interiorShadowSprite;
            }

            Texture2D texture = CreateClearTexture(6, 10, "Cinematic Interior Shadow");
            FillRect(texture, 2, 1, 2, 8, Color.white);
            interiorShadowSprite = CreateSprite(texture, 18f, "Cinematic Interior Shadow Sprite");
            return interiorShadowSprite;
        }

        public static Sprite GetPuddleSprite()
        {
            if (puddleSprite != null)
            {
                return puddleSprite;
            }

            Texture2D texture = CreateClearTexture(18, 8, "Cinematic Puddle");
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float dx = Mathf.Abs((x - 8.5f) / 8.5f);
                    float dy = Mathf.Abs((y - 3.5f) / 3.5f);
                    float d = dx * dx + dy * dy;
                    if (d <= 1f)
                    {
                        float alpha = (1f - d) * 0.68f;
                        Color color = new(0.46f, 0.65f, 0.72f, alpha);
                        if (y == 5 && x > 4 && x < 14)
                        {
                            color = new Color(0.86f, 0.95f, 1f, alpha * 1.4f);
                        }

                        texture.SetPixel(x, y, color);
                    }
                }
            }

            puddleSprite = CreateSprite(texture, 18f, "Cinematic Puddle Sprite");
            return puddleSprite;
        }

        public static Sprite GetBranchSprite()
        {
            if (branchSprite != null)
            {
                return branchSprite;
            }

            Texture2D texture = CreateClearTexture(64, 34, "Cinematic Foreground Branch");
            Color branch = new(0.01f, 0.02f, 0.018f, 1f);
            for (int i = 0; i < 54; i++)
            {
                int x = 4 + i;
                int y = 26 - Mathf.RoundToInt(i * 0.36f + Mathf.Sin(i * 0.26f) * 2f);
                FillRect(texture, x, y, 4, 3, branch);
                if (i % 7 == 0)
                {
                    FillRect(texture, x - 1, y + 4, 8, 4, branch);
                }

                if (i % 9 == 4)
                {
                    FillRect(texture, x + 1, y - 5, 7, 4, branch);
                }
            }

            branchSprite = CreateSprite(texture, 18f, "Cinematic Foreground Branch Sprite");
            return branchSprite;
        }

        public static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Cinematic White Pixel"
            };
            texture.SetPixel(0, 0, Color.white);
            whiteSprite = CreateSprite(texture, 1f, "Cinematic White Pixel Sprite");
            return whiteSprite;
        }

        private static Texture2D CreateClearTexture(int width, int height, string textureName)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = textureName
            };
            Color clear = Color.clear;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            return texture;
        }

        private static void DrawWindow(Texture2D texture, int x, int y)
        {
            FillRect(texture, x, y, 7, 6, Color.white);
            FillRect(texture, x + 3, y, 1, 6, new Color(1f, 1f, 1f, 0.38f));
            FillRect(texture, x, y + 3, 7, 1, new Color(1f, 1f, 1f, 0.38f));
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                if (yy < 0 || yy >= texture.height)
                {
                    continue;
                }

                for (int xx = x; xx < x + width; xx++)
                {
                    if (xx < 0 || xx >= texture.width)
                    {
                        continue;
                    }

                    texture.SetPixel(xx, yy, color);
                }
            }
        }

        private static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit, string spriteName)
        {
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.name = spriteName;
            return sprite;
        }
    }
}
