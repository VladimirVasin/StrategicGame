using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {

        private void AdvanceClampedFrame(float framesPerSecond, int count)
        {
            frameTimer += Time.deltaTime * framesPerSecond;
            int steps = Mathf.FloorToInt(frameTimer);
            if (steps <= 0)
            {
                return;
            }

            frame = Mathf.Min(Mathf.Max(0, count - 1), frame + steps);
            frameTimer -= steps;
        }

        private void MoveDirectlyToward(Vector3 targetWorld, float speed)
        {
            Vector3 target = new Vector3(targetWorld.x, targetWorld.y, transform.position.z);
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                StrategyWildlifeRiverCrossing.GetAdjustedSpeed(map, previous, target, speed) * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            TrackWolfMovementAttempt("direct", previous, transform.position, target, speed);
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }
        }

        private void ApplySprite(StrategyWolfSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null || appliedFrame == (((int)pose * 128) + spriteFrame))
            {
                return;
            }

            spriteRenderer.sprite = pose switch
            {
                StrategyWolfSpritePose.Walk => StrategyWolfSpriteFactory.GetWalkSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Run => StrategyWolfSpriteFactory.GetRunSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Stalk => StrategyWolfSpriteFactory.GetStalkSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Attack => StrategyWolfSpriteFactory.GetAttackSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Eat => StrategyWolfSpriteFactory.GetEatSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Howl => StrategyWolfSpriteFactory.GetHowlSprite(variant, spriteFrame),
                _ => StrategyWolfSpriteFactory.GetIdleSprite(variant, spriteFrame)
            };
            appliedFrame = ((int)pose * 128) + spriteFrame;
            SyncReadabilityRenderers();
        }

        private void EnsureClickCollider()
        {
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
            }

            collider.isTrigger = true;
            collider.radius = 0.33f;
            collider.offset = new Vector2(0.05f, 0.16f);
        }

        private void EnsureReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            readabilityShadowSprite ??= CreateReadabilityShadowSprite();
            if (shadowRenderer == null)
            {
                GameObject shadowObject = new GameObject("Wolf Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
                shadowObject.transform.localScale = new Vector3(0.86f * ReadabilityEffectScale, 0.52f * ReadabilityEffectScale, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.015f, 0.018f, 0.015f, 0.30f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Wolf Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.018f, 0.023f, 0.018f, 0.58f);
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
            UpdateSwimmingVisual();
        }

        internal void RefreshFogVisibility(StrategyFogOfWarController visibilityFog)
        {
            bool visible = visibilityFog == null
                || (TryGetCurrentCell(out Vector2Int cell) && visibilityFog.IsCellVisible(cell));
            SetRendererEnabled(spriteRenderer, visible);
            SetRendererEnabled(outlineRenderer, visible);
            SetRendererEnabled(shadowRenderer, visible);
            SetRendererEnabled(swimRippleRenderer, visible);

            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }

        private static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 42;
            const int height = 15;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Wolf Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.43f;
            float radiusY = height * 0.31f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = (dx * dx) + (dy * dy);
                    if (distance <= 1f)
                    {
                        float alpha = Mathf.Lerp(0.08f, 0.50f, 1f - distance);
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
        }

        private void OnDestroy()
        {
            ReleaseTargets();
        }
    }
}
