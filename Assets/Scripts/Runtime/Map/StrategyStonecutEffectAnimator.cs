using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStonecutEffectAnimator : MonoBehaviour
    {
        private const int FrameCount = 6;
        private const float FrameDuration = 0.06f;

        private SpriteRenderer spriteRenderer;
        private Vector3 drift;
        private int variant;
        private int frame;
        private float frameTimer;

        public static void Spawn(Vector3 world, int sortingOrder, int seed)
        {
            GameObject effect = new GameObject("Stonecut Hit Effect");
            effect.transform.position = world;

            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            StrategyStonecutEffectAnimator animator = effect.AddComponent<StrategyStonecutEffectAnimator>();
            animator.Configure(renderer, seed);
        }

        private void Configure(SpriteRenderer renderer, int seed)
        {
            spriteRenderer = renderer;
            variant = Mathf.Abs(seed) % 9;
            frame = 0;
            frameTimer = 0f;
            drift = new Vector3(((variant % 3) - 1) * 0.06f, 0.15f + (variant % 2) * 0.05f, 0f);
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

            spriteRenderer.sprite = StrategyStonecutEffectSpriteFactory.GetSprite(variant, frame);
            float alpha = Mathf.Lerp(1f, 0.12f, frame / (float)(FrameCount - 1));
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    internal static class StrategyStonecutEffectSpriteFactory
    {
        private const int FrameCount = 6;
        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(int variant, int frame)
        {
            int normalizedVariant = Mathf.Abs(variant) % 9;
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
                name = "Stonecut Hit Effect " + variant + "-" + frame,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Color chip = Rgb(137, 143, 136);
            Color chipDark = Rgb(72, 78, 75);
            Color chipLight = Rgb(196, 203, 192);
            Color dust = new Color(0.58f, 0.57f, 0.52f, 0.46f);

            int spread = 2 + frame * 3;
            int rise = frame * 2;
            for (int i = 0; i < 14; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                int x = 21 + side * (2 + ((variant * 5 + i * 7 + frame * 3) % Mathf.Max(3, spread + 3)));
                int y = 8 + rise + ((variant * 9 + i * 4) % 9);
                Color color = i % 5 == 0 ? chipLight : i % 3 == 0 ? chipDark : chip;
                SetPixelSafe(texture, x, y, color);
                if (i % 4 == frame % 4)
                {
                    SetPixelSafe(texture, x + side, y + 1, color);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int x = 13 + ((variant * 11 + i * 5 + frame * 4) % 18);
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
