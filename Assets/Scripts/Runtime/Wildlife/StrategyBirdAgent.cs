using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBirdBehaviorState
    {
        Idle,
        Pecking,
        Hopping,
        Flying,
        Fleeing,
        Landing,
        Swimming
    }

    [DisallowMultipleComponent]
    public sealed class StrategyBirdAgent : MonoBehaviour
    {
        private const float FlightSpeed = 4.2f;
        private const float FleeFlightSpeed = 6.2f;
        private const float SwimSpeed = 0.55f;
        private const float ThreatCheckInterval = 0.20f;
        private const float AlertRadius = 4.2f;
        private const float FleeRadius = 2.4f;
        private const float NoisyAlertRadius = 8.0f;
        private const float NoisyFleeRadius = 5.2f;
        private const float IdleAnimationRate = 5.5f;
        private const float PeckAnimationRate = 8.5f;
        private const float HopAnimationRate = 9.5f;
        private const float FlyAnimationRate = 14.0f;
        private const float LandAnimationRate = 10.0f;
        private const float SwimAnimationRate = 6.0f;
        private const float BirdGlobalScale = 0.78f;
        private const float FlightShadowScale = 0.6f;
        private const float FlyingSortOffset = 3600f;
        private const float LandedSortOffset = 16f;

        private static Sprite shadowSprite;

        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private StrategyBirdSpecies species;
        private StrategyBirdBehaviorState state;
        private StrategyBirdSpritePose appliedPose;
        private Vector2Int homeCell;
        private Vector2Int groundCell;
        private Vector3 groundWorld;
        private Vector3 flightStartGround;
        private Vector3 flightEndGround;
        private Vector3 swimTargetGround;
        private Vector3 lastThreatWorld;
        private int homeRadius;
        private int birdId;
        private int frame;
        private int appliedFrame = -1;
        private float waitTimer;
        private float stateTimer;
        private float threatCheckTimer;
        private float frameTimer;
        private float flightTimer;
        private float flightDuration = 1f;
        private float altitude;
        private float bobPhase;
        private bool hasAppliedPose;
        private bool hasSwimTarget;

        public StrategyBirdSpecies Species => species;
        public StrategyBirdBehaviorState State => state;
        public int BirdId => birdId;
        public Vector2Int HomeCell => homeCell;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyBirdSpecies birdSpecies,
            Vector2Int home,
            int radius,
            int identifier,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            map = mapController;
            population = populationController;
            species = birdSpecies;
            homeCell = home;
            groundCell = home;
            homeRadius = Mathf.Max(5, radius);
            birdId = identifier;
            groundWorld = new Vector3(spawnWorld.x, spawnWorld.y, -0.064f);
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);
            transform.position = groundWorld;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * GetSpeciesScale();
            state = species == StrategyBirdSpecies.Duck && IsCurrentCellWater()
                ? StrategyBirdBehaviorState.Swimming
                : StrategyBirdBehaviorState.Idle;
            waitTimer = Random.Range(0.45f, 1.6f);
            stateTimer = waitTimer;
            ApplySprite(state == StrategyBirdBehaviorState.Swimming ? StrategyBirdSpritePose.Swim : StrategyBirdSpritePose.Idle, Random.Range(0, StrategyBirdSpriteFactory.IdleFrameCount));
            EnsureShadowRenderer();
            UpdateWorldSorting();
        }

        private void Update()
        {
            if (map == null || spriteRenderer == null)
            {
                return;
            }

            UpdateThreatAwareness();
            switch (state)
            {
                case StrategyBirdBehaviorState.Pecking:
                    UpdatePecking();
                    break;
                case StrategyBirdBehaviorState.Hopping:
                    UpdateHopping();
                    break;
                case StrategyBirdBehaviorState.Flying:
                case StrategyBirdBehaviorState.Fleeing:
                    UpdateFlying();
                    break;
                case StrategyBirdBehaviorState.Landing:
                    UpdateLanding();
                    break;
                case StrategyBirdBehaviorState.Swimming:
                    UpdateSwimming();
                    break;
                default:
                    UpdateIdle();
                    break;
            }

            UpdateShadow();
        }

        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private void UpdateThreatAwareness()
        {
            if (state == StrategyBirdBehaviorState.Fleeing)
            {
                return;
            }

            threatCheckTimer -= Time.deltaTime;
            if (threatCheckTimer > 0f)
            {
                return;
            }

            threatCheckTimer = ThreatCheckInterval;
            if (!TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat))
            {
                return;
            }

            lastThreatWorld = threatWorld;
            float fleeDistance = noisyThreat ? NoisyFleeRadius : FleeRadius;
            float alertDistance = noisyThreat ? NoisyAlertRadius : AlertRadius;
            if (threatDistance <= fleeDistance || (threatDistance <= alertDistance && Random.value < 0.45f))
            {
                StartFleeing(threatWorld, noisyThreat);
            }
        }

        private void UpdateIdle()
        {
            waitTimer -= Time.deltaTime;
            AnimateIdle();
            if (waitTimer > 0f)
            {
                return;
            }

            PickRelaxedBehavior();
        }

        private void UpdatePecking()
        {
            stateTimer -= Time.deltaTime;
            AnimatePeck();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.95f));
            }
        }

        private void UpdateHopping()
        {
            stateTimer -= Time.deltaTime;
            AnimateHop();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.9f));
            }
        }

        private void UpdateFlying()
        {
            flightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(flightTimer / Mathf.Max(0.05f, flightDuration));
            groundWorld = Vector3.Lerp(flightStartGround, flightEndGround, SmoothStep(t));
            altitude = Mathf.Sin(t * Mathf.PI) * (state == StrategyBirdBehaviorState.Fleeing ? 1.35f : 0.92f) + 0.18f;
            transform.position = new Vector3(groundWorld.x, groundWorld.y + altitude, -0.064f);
            if (Mathf.Abs(flightEndGround.x - flightStartGround.x) > 0.02f)
            {
                spriteRenderer.flipX = flightEndGround.x < flightStartGround.x;
            }

            AnimateFly();
            if (t >= 1f)
            {
                altitude = 0f;
                groundWorld = flightEndGround;
                if (map.TryWorldToCell(groundWorld, out Vector2Int cell))
                {
                    groundCell = cell;
                }

                StartLanding();
            }
        }

        private void UpdateLanding()
        {
            stateTimer -= Time.deltaTime;
            altitude = Mathf.Max(0f, altitude - Time.deltaTime * 2.8f);
            transform.position = new Vector3(groundWorld.x, groundWorld.y + altitude, -0.064f);
            AnimateLand();
            if (stateTimer <= 0f)
            {
                altitude = 0f;
                transform.position = groundWorld;
                if (species == StrategyBirdSpecies.Duck && IsCurrentCellWater())
                {
                    StartSwimmingIdle();
                }
                else
                {
                    StartIdle(Random.Range(0.25f, 1.0f));
                }
            }
        }

        private void UpdateSwimming()
        {
            if (species != StrategyBirdSpecies.Duck)
            {
                StartIdle(Random.Range(0.2f, 0.8f));
                return;
            }

            if (!hasSwimTarget && stateTimer <= 0f && TryPickLandingCell(false, true, out Vector2Int targetCell))
            {
                swimTargetGround = GetJitteredCellWorld(targetCell, birdId + 1601, 0.18f);
                hasSwimTarget = true;
            }

            if (hasSwimTarget)
            {
                Vector3 previous = groundWorld;
                groundWorld = Vector3.MoveTowards(groundWorld, swimTargetGround, SwimSpeed * Time.deltaTime);
                transform.position = groundWorld;
                if (Mathf.Abs(groundWorld.x - previous.x) > 0.001f)
                {
                    spriteRenderer.flipX = groundWorld.x < previous.x;
                }

                if (Vector3.Distance(groundWorld, swimTargetGround) <= 0.04f)
                {
                    hasSwimTarget = false;
                    if (map.TryWorldToCell(groundWorld, out Vector2Int cell))
                    {
                        groundCell = cell;
                    }

                    stateTimer = Random.Range(0.4f, 1.5f);
                }
            }
            else
            {
                stateTimer -= Time.deltaTime;
            }

            AnimateSwim();
            if (!IsCurrentCellWater() && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.9f));
            }
        }

        private void PickRelaxedBehavior()
        {
            if (species == StrategyBirdSpecies.Duck && IsCurrentCellWater())
            {
                float duckRoll = Random.value;
                if (duckRoll < 0.58f)
                {
                    StartSwimmingIdle();
                    return;
                }

                if (duckRoll < 0.78f && TryPickLandingCell(false, false, out Vector2Int duckTarget))
                {
                    StartFlightTo(duckTarget, false, false);
                    return;
                }
            }

            float roll = Random.value;
            if (roll < 0.42f)
            {
                StartPecking();
                return;
            }

            if (roll < 0.64f)
            {
                StartHopping();
                return;
            }

            if (roll < 0.88f && TryPickLandingCell(false, false, out Vector2Int targetCell))
            {
                StartFlightTo(targetCell, false, false);
                return;
            }

            StartIdle(Random.Range(0.35f, 1.25f));
        }

        private void StartIdle(float duration)
        {
            altitude = 0f;
            hasSwimTarget = false;
            waitTimer = duration;
            SetState(StrategyBirdBehaviorState.Idle, false, false);
        }

        private void StartPecking()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(1.0f, 2.6f);
            SetState(StrategyBirdBehaviorState.Pecking, false, false);
        }

        private void StartHopping()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(0.45f, 0.95f);
            SetState(StrategyBirdBehaviorState.Hopping, false, false);
        }

        private void StartSwimmingIdle()
        {
            altitude = 0f;
            stateTimer = Random.Range(0.3f, 1.2f);
            SetState(StrategyBirdBehaviorState.Swimming, false, false);
        }

        private void StartLanding()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(0.25f, 0.48f);
            SetState(StrategyBirdBehaviorState.Landing, false, false);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            if (!TryPickLandingCell(true, false, out Vector2Int targetCell))
            {
                return;
            }

            StartFlightTo(targetCell, true, noisyThreat);
        }

        private void StartFlightTo(Vector2Int targetCell, bool fleeing, bool noisyThreat)
        {
            hasSwimTarget = false;
            flightStartGround = groundWorld;
            flightEndGround = GetJitteredCellWorld(targetCell, birdId + 1327, 0.28f);
            flightTimer = 0f;
            float speed = fleeing ? FleeFlightSpeed : FlightSpeed;
            flightDuration = Mathf.Clamp(Vector3.Distance(flightStartGround, flightEndGround) / speed, fleeing ? 0.35f : 0.55f, fleeing ? 1.6f : 2.6f);
            groundCell = targetCell;
            SetState(fleeing ? StrategyBirdBehaviorState.Fleeing : StrategyBirdBehaviorState.Flying, fleeing, noisyThreat);
        }

        private void SetState(StrategyBirdBehaviorState nextState, bool logImportant, bool noisyThreat)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            frame = 0;
            frameTimer = 0f;
            appliedFrame = -1;
            hasAppliedPose = false;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * GetSpeciesScale();

            if (logImportant)
            {
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "BirdFleeing",
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("bird", birdId),
                    StrategyDebugLogger.F("noisyThreat", noisyThreat),
                    StrategyDebugLogger.F("world", groundWorld));
            }
        }

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
