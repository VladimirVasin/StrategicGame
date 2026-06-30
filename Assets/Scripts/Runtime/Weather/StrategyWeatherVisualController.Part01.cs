using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWeatherVisualController
    {

        private static Sprite GetRainDropSprite()
        {
            if (rainDropSprite != null)
            {
                return rainDropSprite;
            }

            Texture2D texture = new Texture2D(3, 18, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Weather Rain Drop"
            };
            Color clear = Color.clear;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int y = 1; y < texture.height - 1; y++)
            {
                float t = y / (float)(texture.height - 1);
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.82f;
                texture.SetPixel(1, y, new Color(0.76f, 0.90f, 0.98f, alpha));
                if (y > 3 && y < texture.height - 4)
                {
                    texture.SetPixel(2, y, new Color(0.76f, 0.90f, 0.98f, alpha * 0.35f));
                }
            }

            texture.Apply(false, true);
            rainDropSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                28f);
            rainDropSprite.name = "Weather Rain Drop Sprite";
            return rainDropSprite;
        }

        private static void DestroyTextureRenderer(SpriteRenderer renderer, Texture2D texture)
        {
            if (renderer != null && renderer.sprite != null)
            {
                Destroy(renderer.sprite);
            }

            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Weather Overlay Pixel"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "Weather Overlay Pixel Sprite";
            return whiteSprite;
        }

        private static void ClearPixels(Color[] pixels)
        {
            System.Array.Clear(pixels, 0, pixels.Length);
        }

        private static void ApplyTexture(Texture2D texture, Color[] pixels)
        {
            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        private sealed class RainDropVisual
        {
            public SpriteRenderer Renderer;
            public float Speed;
            public float Scale;
            public float Alpha;
        }
    }
}
