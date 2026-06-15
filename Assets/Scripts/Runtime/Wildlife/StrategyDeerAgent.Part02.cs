using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDeerAgent
    {

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (Mathf.Abs(transform.position.x - world.x) > 0.05f)
            {
                spriteRenderer.flipX = transform.position.x > world.x;
            }

            SyncReadabilityRenderers();
        }

        private void EnsureReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (readabilityShadowSprite == null)
            {
                readabilityShadowSprite = CreateReadabilityShadowSprite();
            }

            if (shadowRenderer == null)
            {
                GameObject shadowObject = new GameObject("Deer Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.07f, 0.02f);
                shadowObject.transform.localScale = sex == StrategyDeerSex.Male
                    ? new Vector3(1.25f * ReadabilityEffectScale, 0.78f * ReadabilityEffectScale, 1f)
                    : new Vector3(1.08f * ReadabilityEffectScale, 0.68f * ReadabilityEffectScale, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.02f, 0.025f, 0.02f, 0.32f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Deer Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.025f, 0.035f, 0.025f, 0.58f);
            }

            SyncReadabilityRenderers();
        }

        private void SyncReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.sprite = spriteRenderer.sprite;
                outlineRenderer.flipX = spriteRenderer.flipX;
                outlineRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 1);
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 2);
            }
        }

        private void UpdateWorldSorting()
        {
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            SyncReadabilityRenderers();
        }

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 48;
            const int height = 18;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Deer Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.45f;
            float radiusY = height * 0.34f;
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

                    float alpha = Mathf.Lerp(0.10f, 0.54f, 1f - distance);
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
        }
    }
}
