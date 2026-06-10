using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWoodcutEffectAnimator : MonoBehaviour
    {
        private const int FrameCount = 6;
        private const float FrameDuration = 0.065f;

        private SpriteRenderer spriteRenderer;
        private Vector3 drift;
        private int variant;
        private int frame;
        private float frameTimer;

        public static void Spawn(Vector3 world, int sortingOrder, int seed)
        {
            GameObject effect = new GameObject("Woodcut Hit Effect");
            effect.transform.position = world;

            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            StrategyWoodcutEffectAnimator animator = effect.AddComponent<StrategyWoodcutEffectAnimator>();
            animator.Configure(renderer, seed);
        }

        private void Configure(SpriteRenderer renderer, int seed)
        {
            spriteRenderer = renderer;
            variant = Mathf.Abs(seed) % 7;
            frame = 0;
            frameTimer = 0f;
            drift = new Vector3(((variant % 3) - 1) * 0.10f, 0.18f + (variant % 2) * 0.04f, 0f);
            ApplyFrame();
        }

        private void Update()
        {
            transform.position += drift * Time.deltaTime;
            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frame++;
            if (frame >= FrameCount)
            {
                Destroy(gameObject);
                return;
            }

            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyWoodcutEffectSpriteFactory.GetSprite(variant, frame);
            float alpha = Mathf.Lerp(1f, 0.15f, frame / (float)(FrameCount - 1));
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    internal static class StrategyWoodcutEffectSpriteFactory
    {
        private const int FrameCount = 6;
        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(int variant, int frame)
        {
            int normalizedVariant = Mathf.Abs(variant) % 7;
            int normalizedFrame = Mathf.Abs(frame) % FrameCount;
            int key = normalizedVariant * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(normalizedVariant, normalizedFrame);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(int variant, int frame)
        {
            const int width = 42;
            const int height = 30;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Woodcut Hit Effect " + variant + "-" + frame,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Color chip = Rgb(198, 130, 64);
            Color chipDark = Rgb(113, 69, 38);
            Color dust = new Color(0.74f, 0.67f, 0.48f, 0.50f);
            Color leaf = new Color(0.44f, 0.68f, 0.28f, 0.62f);

            int spread = 2 + frame * 3;
            int rise = frame * 2;
            for (int i = 0; i < 12; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                int x = 21 + side * (2 + ((variant * 7 + i * 5 + frame * 3) % Mathf.Max(3, spread + 3)));
                int y = 8 + rise + ((variant * 11 + i * 3) % 9);
                Color color = i % 5 == 0 ? leaf : i % 3 == 0 ? chipDark : chip;
                SetPixelSafe(texture, x, y, color);
                if (i % 4 == frame % 4)
                {
                    SetPixelSafe(texture, x + side, y + 1, color);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int x = 15 + ((variant * 13 + i * 5 + frame * 4) % 16);
                int y = 5 + ((variant * 3 + i * 2 + frame) % 7);
                SetPixelSafe(texture, x, y, dust);
                if (frame < 3)
                {
                    SetPixelSafe(texture, x + 1, y, dust);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.20f), 32f);
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
    }
}
