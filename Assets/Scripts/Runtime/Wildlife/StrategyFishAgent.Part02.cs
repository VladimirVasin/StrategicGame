using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFishAgent
    {

        private bool IsRelaxedFishTarget(Vector2Int cell)
        {
            return IsFishWaterCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius
                && CountWaterNeighbors(cell, 1) >= 2;
        }

        private bool IsFleeTarget(Vector2Int cell)
        {
            return IsFishWaterCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius + 10
                && CountWaterNeighbors(cell, 1) >= 1;
        }

        private bool IsFishWaterCell(Vector2Int cell)
        {
            CityMapWaterKind waterKind = habitatKind == StrategyFishHabitatKind.River
                ? CityMapWaterKind.River
                : CityMapWaterKind.Lake;
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.Kind == CityMapCellKind.Water
                && mapCell.WaterKind == waterKind;
        }

        private int CountWaterNeighbors(Vector2Int cell, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Water
                        && neighbor.WaterKind == (habitatKind == StrategyFishHabitatKind.River
                            ? CityMapWaterKind.River
                            : CityMapWaterKind.Lake))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private float GetWaterPreference(Vector2Int cell)
        {
            if (!IsFishWaterCell(cell))
            {
                return -10f;
            }

            int waterNeighbors = CountWaterNeighbors(cell, 2);
            int shoreNeighbors = CountShoreNeighbors(cell);
            return waterNeighbors * 0.22f + Mathf.Min(shoreNeighbors, 4) * 0.15f;
        }

        private int CountShoreNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Shore
                        && neighbor.WaterKind == (habitatKind == StrategyFishHabitatKind.River
                            ? CityMapWaterKind.River
                            : CityMapWaterKind.Lake))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(IdleAnimationRate, StrategyFishSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyFishSpritePose.Idle, frame);
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 4.0f) * 0.018f;
            SetAnimatedScale(1f, pulse);
        }

        private void AnimateSwim()
        {
            AdvanceLoopingFrame(SwimAnimationRate, StrategyFishSpriteFactory.SwimFrameCount);
            ApplySprite(StrategyFishSpritePose.Swim, frame);
            float sway = Mathf.Sin((frame / (float)StrategyFishSpriteFactory.SwimFrameCount) * Mathf.PI * 2f);
            SetAnimatedScale(1f + Mathf.Abs(sway) * 0.018f, 1f - Mathf.Abs(sway) * 0.01f);
        }

        private void AnimateFeed()
        {
            AdvanceLoopingFrame(FeedAnimationRate, StrategyFishSpriteFactory.FeedFrameCount);
            ApplySprite(StrategyFishSpritePose.Feed, frame);
            SetAnimatedScale(1f, 1f);
        }

        private void AnimateFlee()
        {
            AdvanceLoopingFrame(FleeAnimationRate, StrategyFishSpriteFactory.DartFrameCount);
            ApplySprite(StrategyFishSpritePose.Dart, frame);
            SetAnimatedScale(1.05f, 0.94f);
        }

        private void AnimateTurn()
        {
            AdvanceLoopingFrame(TurnAnimationRate, StrategyFishSpriteFactory.TurnFrameCount);
            ApplySprite(StrategyFishSpritePose.Turn, frame);
            SetAnimatedScale(0.98f, 1.02f);
        }

        private void AnimateHooked()
        {
            AdvanceLoopingFrame(HookedAnimationRate, StrategyFishSpriteFactory.HookedFrameCount);
            ApplySprite(StrategyFishSpritePose.Hooked, frame);
            float thrash = Mathf.Sin((Time.time + bobPhase) * 22f);
            transform.localRotation = Quaternion.Euler(0f, 0f, thrash * 7.5f);
            SetAnimatedScale(1.05f, 0.92f);
        }

        private void AdvanceLoopingFrame(float frameRate, int frameCount)
        {
            frameTimer += Time.deltaTime * frameRate;
            int frameSteps = Mathf.FloorToInt(frameTimer);
            if (frameSteps <= 0)
            {
                return;
            }

            frame = (frame + frameSteps) % frameCount;
            frameTimer -= frameSteps;
        }

        private void ApplySprite(StrategyFishSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (hasAppliedPose && appliedPose == pose && appliedFrame == spriteFrame)
            {
                return;
            }

            spriteRenderer.sprite = pose switch
            {
                StrategyFishSpritePose.Swim => StrategyFishSpriteFactory.GetSwimSprite(species, spriteFrame),
                StrategyFishSpritePose.Dart => StrategyFishSpriteFactory.GetDartSprite(species, spriteFrame),
                StrategyFishSpritePose.Turn => StrategyFishSpriteFactory.GetTurnSprite(species, spriteFrame),
                StrategyFishSpritePose.Feed => StrategyFishSpriteFactory.GetFeedSprite(species, spriteFrame),
                StrategyFishSpritePose.Hooked => StrategyFishSpriteFactory.GetHookedSprite(species, spriteFrame),
                _ => StrategyFishSpriteFactory.GetIdleSprite(species, spriteFrame)
            };

            appliedPose = pose;
            appliedFrame = spriteFrame;
            hasAppliedPose = true;
            SyncRippleRenderer();
        }

        private void UpdateAge()
        {
            if (lifeStage != StrategyFishLifeStage.Fry)
            {
                return;
            }

            ageSeconds += Time.deltaTime;
            if (ageSeconds >= FryMaturitySeconds)
            {
                lifeStage = StrategyFishLifeStage.Adult;
                ageSeconds = FryMaturitySeconds;
                UpdateVisualScale();
                hasAppliedPose = false;
                appliedFrame = -1;
                ApplySprite(appliedPose, frame);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "FishGrownUp",
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("shoal", shoalId),
                    StrategyDebugLogger.F("world", transform.position));
                return;
            }

            UpdateVisualScale();
        }

        private void UpdateVisualScale()
        {
            visualScale = lifeStage == StrategyFishLifeStage.Fry
                ? Mathf.Lerp(FryStartScale, FryMatureScale, Mathf.Clamp01(ageSeconds / FryMaturitySeconds))
                : 1f;
        }

        private void SetAnimatedScale(float x, float y)
        {
            transform.localScale = new Vector3(visualScale * FishGlobalScale * x, visualScale * FishGlobalScale * y, 1f);
        }

        private void EnsureRippleRenderer()
        {
            if (rippleRenderer != null)
            {
                return;
            }

            if (rippleSprite == null)
            {
                rippleSprite = CreateRippleSprite();
            }

            GameObject rippleObject = new GameObject("Fish Surface Ripple");
            rippleObject.transform.SetParent(transform, false);
            rippleObject.transform.localPosition = new Vector3(0f, -0.02f, 0.015f);
            rippleRenderer = rippleObject.AddComponent<SpriteRenderer>();
            rippleRenderer.sprite = rippleSprite;
            rippleRenderer.color = new Color(0.65f, 0.90f, 1f, 0.14f);
            SyncRippleRenderer();
        }

        private void UpdateRipple()
        {
            if (rippleRenderer == null)
            {
                return;
            }

            float alpha = state == StrategyFishBehaviorState.Fleeing
                ? 0.28f
                : state == StrategyFishBehaviorState.Hooked
                    ? 0.34f
                    : state == StrategyFishBehaviorState.Swimming
                    ? 0.18f
                    : 0.08f;
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 5f) * 0.08f;
            rippleRenderer.transform.localScale = new Vector3(
                pulse * SurfaceRippleScale,
                (0.72f + (pulse - 1f) * 0.35f) * SurfaceRippleScale,
                1f);
            rippleRenderer.color = new Color(0.65f, 0.90f, 1f, alpha);
        }

        private void SyncRippleRenderer()
        {
            if (spriteRenderer == null || rippleRenderer == null)
            {
                return;
            }

            rippleRenderer.flipX = spriteRenderer.flipX;
            rippleRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 1);
        }

        private void UpdateWorldSorting()
        {
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 1);
            SyncRippleRenderer();
        }

        private static Sprite CreateRippleSprite()
        {
            const int width = 40;
            const int height = 14;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fish Surface Ripple",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Color ripple = new Color(0.64f, 0.90f, 1f, 0.65f);
            DrawEllipseOutline(texture, width / 2, height / 2, 16, 4, ripple);
            DrawEllipseOutline(texture, width / 2 - 4, height / 2, 8, 2, new Color(0.64f, 0.90f, 1f, 0.42f));
            DrawEllipseOutline(texture, width / 2 + 5, height / 2, 9, 2, new Color(0.64f, 0.90f, 1f, 0.36f));
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 38f);
        }

        private static void DrawEllipseOutline(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float dx = x / (float)radiusX;
                    float dy = y / (float)radiusY;
                    float distance = dx * dx + dy * dy;
                    if (distance > 0.78f && distance <= 1.08f)
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
    }
}
