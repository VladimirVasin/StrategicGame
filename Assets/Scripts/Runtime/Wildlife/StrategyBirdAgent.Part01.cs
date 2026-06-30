using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBirdAgent
    {

        private bool TryPickLandingCell(bool awayFromThreat, bool waterOnly, out Vector2Int cell)
        {
            cell = default;
            Vector2 away = Vector2.zero;
            if (awayFromThreat)
            {
                away = (Vector2)groundWorld - (Vector2)lastThreatWorld;
                if (away.sqrMagnitude > 0.01f)
                {
                    away.Normalize();
                }
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            int searchRadius = awayFromThreat ? homeRadius + 14 : homeRadius;
            Vector2Int center = awayFromThreat && map.TryWorldToCell(groundWorld, out Vector2Int current) ? current : homeCell;
            for (int attempt = 0; attempt < 36; attempt++)
            {
                Vector2Int candidate = center + new Vector2Int(
                    Random.Range(-searchRadius, searchRadius + 1),
                    Random.Range(-searchRadius, searchRadius + 1));
                if (!IsLandingCandidate(candidate, waterOnly))
                {
                    continue;
                }

                Vector3 world = map.GetCellCenterWorld(candidate.x, candidate.y);
                float score = GetLandingPreference(candidate) - Vector2Int.Distance(candidate, homeCell) * 0.05f;
                if (awayFromThreat && away.sqrMagnitude > 0f)
                {
                    Vector2 direction = (Vector2)world - (Vector2)groundWorld;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        score += Vector2.Dot(direction.normalized, away) * 4.0f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool IsLandingCandidate(Vector2Int cell, bool waterOnly)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            if (waterOnly)
            {
                return species == StrategyBirdSpecies.Duck && mapCell.Kind == CityMapCellKind.Water;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind == CityMapCellKind.Water
                    || mapCell.Kind == CityMapCellKind.Shore
                    || (map.IsCellWalkable(cell) && mapCell.Kind == CityMapCellKind.Grass),
                StrategyBirdSpecies.Crow => mapCell.Kind == CityMapCellKind.Forest
                    || (map.IsCellWalkable(cell)
                        && (mapCell.Kind == CityMapCellKind.Dirt
                            || mapCell.Kind == CityMapCellKind.Grass
                            || mapCell.Kind == CityMapCellKind.Meadow)),
                _ => map.IsCellWalkable(cell)
                    && (mapCell.Kind == CityMapCellKind.Meadow
                        || mapCell.Kind == CityMapCellKind.Grass
                        || mapCell.Kind == CityMapCellKind.Dirt
                        || mapCell.Kind == CityMapCellKind.Shore)
            };
        }

        private float GetLandingPreference(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind switch
                {
                    CityMapCellKind.Water => 5.0f,
                    CityMapCellKind.Shore => 3.2f,
                    CityMapCellKind.Grass => 1.0f,
                    _ => -4f
                },
                StrategyBirdSpecies.Crow => mapCell.Kind switch
                {
                    CityMapCellKind.Forest => 4.5f,
                    CityMapCellKind.Dirt => 3.0f,
                    CityMapCellKind.Grass => 1.5f,
                    _ => -3f
                },
                _ => mapCell.Kind switch
                {
                    CityMapCellKind.Meadow => 4.8f,
                    CityMapCellKind.Grass => 4.0f,
                    CityMapCellKind.Dirt => 2.4f,
                    CityMapCellKind.Shore => 1.7f,
                    _ => -3f
                }
            };
        }

        private bool TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat)
        {
            threatWorld = default;
            threatDistance = float.MaxValue;
            noisyThreat = false;

            System.Collections.Generic.IReadOnlyList<StrategyResidentAgent> residents = population != null ? population.Residents : null;
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
                float sqr = (resident.transform.position - groundWorld).sqrMagnitude;
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
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(IdleAnimationRate, StrategyBirdSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyBirdSpritePose.Idle, frame);
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 5f) * 0.02f;
            transform.localScale = new Vector3(GetSpeciesScale(), GetSpeciesScale() * pulse, 1f);
        }

        private void AnimatePeck()
        {
            AdvanceLoopingFrame(PeckAnimationRate, StrategyBirdSpriteFactory.PeckFrameCount);
            ApplySprite(StrategyBirdSpritePose.Peck, frame);
        }

        private void AnimateHop()
        {
            AdvanceLoopingFrame(HopAnimationRate, StrategyBirdSpriteFactory.HopFrameCount);
            ApplySprite(StrategyBirdSpritePose.Hop, frame);
            float hop = Mathf.Sin((frame / (float)StrategyBirdSpriteFactory.HopFrameCount) * Mathf.PI);
            transform.position = new Vector3(groundWorld.x, groundWorld.y + hop * 0.08f, -0.064f);
        }

        private void AnimateFly()
        {
            AdvanceLoopingFrame(FlyAnimationRate, StrategyBirdSpriteFactory.FlyFrameCount);
            ApplySprite(StrategyBirdSpritePose.Fly, frame);
        }

        private void AnimateLand()
        {
            AdvanceLoopingFrame(LandAnimationRate, StrategyBirdSpriteFactory.LandFrameCount);
            ApplySprite(StrategyBirdSpritePose.Land, frame);
        }

        private void AnimateSwim()
        {
            AdvanceLoopingFrame(SwimAnimationRate, StrategyBirdSpriteFactory.SwimFrameCount);
            ApplySprite(StrategyBirdSpritePose.Swim, frame);
            float bob = Mathf.Sin((Time.time + bobPhase) * 3.4f) * 0.025f;
            transform.position = new Vector3(groundWorld.x, groundWorld.y + bob, -0.064f);
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

        private void ApplySprite(StrategyBirdSpritePose pose, int spriteFrame)
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
                StrategyBirdSpritePose.Peck => StrategyBirdSpriteFactory.GetPeckSprite(species, spriteFrame),
                StrategyBirdSpritePose.Hop => StrategyBirdSpriteFactory.GetHopSprite(species, spriteFrame),
                StrategyBirdSpritePose.Fly => StrategyBirdSpriteFactory.GetFlySprite(species, spriteFrame),
                StrategyBirdSpritePose.Land => StrategyBirdSpriteFactory.GetLandSprite(species, spriteFrame),
                StrategyBirdSpritePose.Swim => StrategyBirdSpriteFactory.GetSwimSprite(species, spriteFrame),
                _ => StrategyBirdSpriteFactory.GetIdleSprite(species, spriteFrame)
            };

            appliedPose = pose;
            appliedFrame = spriteFrame;
            hasAppliedPose = true;
            SyncShadowRenderer();
        }

        private void EnsureShadowRenderer()
        {
            if (shadowRenderer != null)
            {
                return;
            }

            if (shadowSprite == null)
            {
                shadowSprite = CreateShadowSprite();
            }

            GameObject shadowObject = new GameObject("Bird Flight Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.24f);
            SyncShadowRenderer();
        }

        private void UpdateShadow()
        {
            if (shadowRenderer == null)
            {
                return;
            }

            shadowRenderer.transform.localPosition = new Vector3(0f, -altitude, 0.015f);
            float heightFade = Mathf.Clamp01(1f - altitude * 0.38f);
            shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.08f, 0.25f, heightFade));
            float shadowScale = Mathf.Lerp(0.62f, 1.0f, heightFade) * GetSpeciesScale() * FlightShadowScale;
            shadowRenderer.transform.localScale = new Vector3(shadowScale, shadowScale * 0.52f, 1f);
        }

        private void SyncShadowRenderer()
        {
            if (spriteRenderer == null || shadowRenderer == null)
            {
                return;
            }

            shadowRenderer.flipX = spriteRenderer.flipX;
            shadowRenderer.sortingOrder = StrategyWorldSorting.ForPosition(groundWorld, -2);
        }

        private void UpdateWorldSorting()
        {
            int offset = state == StrategyBirdBehaviorState.Flying || state == StrategyBirdBehaviorState.Fleeing || state == StrategyBirdBehaviorState.Landing
                ? Mathf.RoundToInt(FlyingSortOffset)
                : Mathf.RoundToInt(LandedSortOffset);
            StrategyWorldSorting.Apply(spriteRenderer, groundWorld, offset);
            SyncShadowRenderer();
        }

        internal void RefreshFogVisibility(StrategyFogOfWarController visibilityFog)
        {
            bool visible = visibilityFog == null
                || (map != null
                    && map.TryWorldToCell(groundWorld, out Vector2Int cell)
                    && visibilityFog.IsCellVisible(cell));
            SetRendererEnabled(spriteRenderer, visible);
            SetRendererEnabled(shadowRenderer, visible);
        }

        private static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        private Vector3 GetJitteredCellWorld(Vector2Int cell, int salt, float amount)
        {
            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            Vector2 jitter = GetJitter(cell.x, cell.y, salt) * (map.CellSize * amount);
            return new Vector3(world.x + jitter.x, world.y + jitter.y, -0.064f);
        }

        private Vector2 GetJitter(int x, int y, int salt)
        {
            float jitterX = Hash01(map.ActiveSeed, x, y, salt) - 0.5f;
            float jitterY = Hash01(map.ActiveSeed, x, y, salt + 17) - 0.5f;
            return new Vector2(jitterX, jitterY);
        }

        private bool IsCurrentCellWater()
        {
            return map != null
                && map.TryGetCell(groundCell.x, groundCell.y, out CityMapCell cell)
                && cell.Kind == CityMapCellKind.Water;
        }

        private float GetSpeciesScale()
        {
            return species switch
            {
                StrategyBirdSpecies.Crow => BirdGlobalScale * 1.16f,
                StrategyBirdSpecies.Duck => BirdGlobalScale * 1.24f,
                _ => BirdGlobalScale * 0.86f
            };
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static Sprite CreateShadowSprite()
        {
            const int width = 28;
            const int height = 10;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Bird Flight Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / (width * 0.42f);
                    float dy = (y - center.y) / (height * 0.32f);
                    float distance = dx * dx + dy * dy;
                    if (distance <= 1f)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Lerp(0.08f, 0.52f, 1f - distance)));
                    }
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + x * 668265263;
                h = h * 1274126177 + y * 461845907;
                h = h * 1103515245 + salt * 12345;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & int.MaxValue) / (float)int.MaxValue;
            }
        }
    }
}
