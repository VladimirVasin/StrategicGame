using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {

        private void BuildWorldPath(Vector2Int startCell, Vector2Int targetCell, Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new();
            Vector2Int current = targetCell;
            while (current != startCell)
            {
                cells.Add(current);
                if (!cameFrom.TryGetValue(current, out current))
                {
                    path.Clear();
                    pathIndex = 0;
                    return;
                }
            }

            cells.Reverse();
            path.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                if (i == cells.Count - 1)
                {
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.072f));
            }

            pathIndex = 0;
        }

        private bool TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat)
        {
            threatWorld = default;
            threatDistance = float.MaxValue;
            noisyThreat = false;

            IReadOnlyList<StrategyResidentAgent> residents = population != null ? population.Residents : null;
            if (residents == null || residents.Count <= 0)
            {
                return false;
            }

            float bestSqr = float.MaxValue;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null)
                {
                    continue;
                }

                bool residentIsNoisy = IsNoisyResidentActivity(resident.Activity);
                float radius = residentIsNoisy ? NoisyAlertRadius : AlertRadius;
                float sqr = (resident.transform.position - transform.position).sqrMagnitude;
                if (sqr > radius * radius || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                threatWorld = resident.transform.position;
                noisyThreat = residentIsNoisy;
            }

            if (bestSqr >= float.MaxValue)
            {
                return false;
            }

            threatDistance = Mathf.Sqrt(bestSqr);
            return true;
        }

        private static bool IsNoisyResidentActivity(StrategyResidentAgent.ResidentActivity activity)
        {
            return activity == StrategyResidentAgent.ResidentActivity.ChoppingTree
                || activity == StrategyResidentAgent.ResidentActivity.BuckingTree
                || activity == StrategyResidentAgent.ResidentActivity.MiningStone
                || activity == StrategyResidentAgent.ResidentActivity.BuildingConstruction
                || activity == StrategyResidentAgent.ResidentActivity.AimingBow
                || activity == StrategyResidentAgent.ResidentActivity.ButcheringRabbit
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }

        private bool IsRelaxedRabbitTarget(Vector2Int cell)
        {
            return IsRabbitWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius
                && GetTerrainPreference(cell) >= 0f;
        }

        private bool IsFleeTarget(Vector2Int cell)
        {
            return IsRabbitWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius + 10
                && GetTerrainPreference(cell) > -2f;
        }

        private bool IsRabbitWalkCell(Vector2Int cell)
        {
            return StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, cell);
        }

        private float GetTerrainPreference(Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 4.2f,
                CityMapCellKind.Grass => 3.2f,
                CityMapCellKind.Forest => 1.45f,
                CityMapCellKind.Dirt => 0.35f,
                CityMapCellKind.Shore => -0.75f,
                _ => -10f
            };
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(IdleAnimationRate, StrategyRabbitSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Idle, frame);
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 7.5f) * 0.02f;
            SetAnimatedScale(1f, pulse);
        }

        private void AnimateHop()
        {
            AdvanceLoopingFrame(HopAnimationRate, StrategyRabbitSpriteFactory.HopFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Hop, frame);
            float hopPulse = Mathf.Sin((frame / (float)StrategyRabbitSpriteFactory.HopFrameCount) * Mathf.PI);
            SetAnimatedScale(1f + hopPulse * 0.035f, 1f - hopPulse * 0.025f);
        }

        private void AnimateNibble()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(NibbleAnimationRate, StrategyRabbitSpriteFactory.NibbleFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Nibble, frame);
        }

        private void AnimateAlert()
        {
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 11f) * 0.018f;
            SetAnimatedScale(1f + (pulse - 1f) * 0.35f, pulse);
            AdvanceLoopingFrame(AlertAnimationRate, StrategyRabbitSpriteFactory.AlertFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Alert, frame);
        }

        private void AnimateFlee()
        {
            AdvanceLoopingFrame(FleeAnimationRate, StrategyRabbitSpriteFactory.FleeFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Flee, frame);
            float hopPulse = Mathf.Sin((frame / (float)StrategyRabbitSpriteFactory.FleeFrameCount) * Mathf.PI * 2f);
            SetAnimatedScale(1f + Mathf.Abs(hopPulse) * 0.05f, 1f - Mathf.Abs(hopPulse) * 0.025f);
        }

        private void AnimateGroom()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(GroomAnimationRate, StrategyRabbitSpriteFactory.GroomFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Groom, frame);
        }

        private void AnimateRest()
        {
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 3.5f) * 0.012f;
            SetAnimatedScale(1f, pulse);
            AdvanceLoopingFrame(RestAnimationRate, StrategyRabbitSpriteFactory.RestFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Rest, frame);
        }

        private void AnimateHit()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceClampedFrame(HitAnimationRate, StrategyRabbitSpriteFactory.HitFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Hit, frame);
        }

        private void AnimateDeath()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceClampedFrame(DeathAnimationRate, StrategyRabbitSpriteFactory.DeathFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Death, frame);
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

        private void AdvanceClampedFrame(float frameRate, int frameCount)
        {
            frameTimer += Time.deltaTime * frameRate;
            int frameSteps = Mathf.FloorToInt(frameTimer);
            if (frameSteps <= 0)
            {
                return;
            }

            frame = Mathf.Min(frame + frameSteps, Mathf.Max(0, frameCount - 1));
            frameTimer -= frameSteps;
        }

        private void ApplySprite(StrategyRabbitSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (hasAppliedPose && appliedPose == pose && appliedFrame == spriteFrame)
            {
                return;
            }

            StrategyRabbitSex spriteSex = lifeStage == StrategyRabbitLifeStage.Kit ? StrategyRabbitSex.Female : sex;
            spriteRenderer.sprite = pose switch
            {
                StrategyRabbitSpritePose.Hop => StrategyRabbitSpriteFactory.GetHopSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Nibble => StrategyRabbitSpriteFactory.GetNibbleSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Alert => StrategyRabbitSpriteFactory.GetAlertSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Flee => StrategyRabbitSpriteFactory.GetFleeSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Groom => StrategyRabbitSpriteFactory.GetGroomSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Rest => StrategyRabbitSpriteFactory.GetRestSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Hit => StrategyRabbitSpriteFactory.GetHitSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Death => StrategyRabbitSpriteFactory.GetDeathSprite(spriteSex, spriteFrame),
                StrategyRabbitSpritePose.Carcass => StrategyRabbitSpriteFactory.GetCarcassSprite(spriteSex),
                _ => StrategyRabbitSpriteFactory.GetIdleSprite(spriteSex, spriteFrame)
            };

            appliedPose = pose;
            appliedFrame = spriteFrame;
            hasAppliedPose = true;
            SyncReadabilityRenderers();
        }

        private void UpdateAge()
        {
            if (lifeStage != StrategyRabbitLifeStage.Kit)
            {
                return;
            }

            ageSeconds += Time.deltaTime;
            if (ageSeconds >= KitMaturitySeconds)
            {
                lifeStage = StrategyRabbitLifeStage.Adult;
                ageSeconds = KitMaturitySeconds;
                UpdateVisualScale();
                hasAppliedPose = false;
                appliedFrame = -1;
                ApplySprite(appliedPose, frame);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "RabbitGrownUp",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("group", groupId),
                    StrategyDebugLogger.F("world", transform.position));
                return;
            }

            UpdateVisualScale();
        }

        private void UpdateVisualScale()
        {
            visualScale = lifeStage == StrategyRabbitLifeStage.Kit
                ? Mathf.Lerp(KitStartScale, KitMatureScale, Mathf.Clamp01(ageSeconds / KitMaturitySeconds))
                : 1f;
        }

        private void SetAnimatedScale(float x, float y)
        {
            transform.localScale = new Vector3(visualScale * RabbitGlobalScale * x, visualScale * RabbitGlobalScale * y, 1f);
        }

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (Mathf.Abs(transform.position.x - world.x) > 0.04f)
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
                GameObject shadowObject = new GameObject("Rabbit Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.045f, 0.02f);
                shadowObject.transform.localScale = new Vector3(0.72f * ReadabilityEffectScale, 0.45f * ReadabilityEffectScale, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.015f, 0.018f, 0.015f, 0.34f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Rabbit Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.018f, 0.024f, 0.018f, 0.62f);
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

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 34;
            const int height = 14;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Rabbit Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.43f;
            float radiusY = height * 0.32f;
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

                    float alpha = Mathf.Lerp(0.08f, 0.52f, 1f - distance);
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
        }
    }
}
