using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyFishBehaviorState
    {
        Idle,
        Swimming,
        Feeding,
        Fleeing,
        Turning,
        Hooked,
        Caught
    }

    public enum StrategyFishLifeStage
    {
        Fry,
        Adult
    }

    public enum StrategyFishHabitatKind
    {
        Lake,
        River
    }

    [DisallowMultipleComponent]
    public sealed class StrategyFishAgent : MonoBehaviour
    {
        private const float SwimSpeed = 0.92f;
        private const float RiverSwimSpeed = 1.08f;
        private const float FleeSpeed = 2.25f;
        private const float TargetReachDistance = 0.04f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float ThreatCheckInterval = 0.18f;
        private const float AlertRadius = 4.6f;
        private const float FleeRadius = 2.6f;
        private const float NoisyAlertRadius = 8.8f;
        private const float NoisyFleeRadius = 5.6f;
        private const float IdleAnimationRate = 5.0f;
        private const float SwimAnimationRate = 9.5f;
        private const float FeedAnimationRate = 7.5f;
        private const float FleeAnimationRate = 16.0f;
        private const float TurnAnimationRate = 11.0f;
        private const float HookedAnimationRate = 18.0f;
        private const float FishGlobalScale = 0.82f;
        private const float SurfaceRippleScale = 0.6f;
        private const float FryMaturitySeconds = 90f;
        private const float FryStartScale = 0.42f;
        private const float FryMatureScale = 0.78f;
        private const int FishYield = 2;
        private const int ReelHitsRequired = 4;

        private static Sprite rippleSprite;
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private readonly List<Vector3> path = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer rippleRenderer;
        private StrategyFishSpecies species;
        private StrategyFishBehaviorState state;
        private StrategyFishLifeStage lifeStage = StrategyFishLifeStage.Adult;
        private StrategyFishHabitatKind habitatKind = StrategyFishHabitatKind.Lake;
        private StrategyFishSpritePose appliedPose;
        private Vector2Int homeCell;
        private Vector3 lastThreatWorld;
        private Vector3 hookWorld;
        private object fishingReservationOwner;
        private int pathIndex;
        private int homeRadius;
        private int shoalId;
        private int waterRegionId = -1;
        private int frame;
        private int appliedFrame = -1;
        private int reelHits;
        private float waitTimer;
        private float stateTimer;
        private float threatCheckTimer;
        private float frameTimer;
        private float bobPhase;
        private float ageSeconds;
        private float visualScale = 1f;
        private float riverSpeedMultiplier = 1f;
        private bool hasTarget;
        private bool hasAppliedPose;
        private bool isCaught;
        private bool riverRouteActive;

        public StrategyFishSpecies Species => species;
        public StrategyFishBehaviorState State => state;
        public StrategyFishLifeStage LifeStage => lifeStage;
        public StrategyFishHabitatKind HabitatKind => habitatKind;
        public bool IsAdult => lifeStage == StrategyFishLifeStage.Adult;
        public bool IsLakeFish => habitatKind == StrategyFishHabitatKind.Lake;
        public bool IsRiverFish => habitatKind == StrategyFishHabitatKind.River;
        public bool IsHooked => state == StrategyFishBehaviorState.Hooked;
        public bool IsCaught => isCaught;
        public bool CanBeFished => IsAdult
            && !isCaught
            && fishingReservationOwner == null
            && state != StrategyFishBehaviorState.Fleeing
            && state != StrategyFishBehaviorState.Hooked
            && state != StrategyFishBehaviorState.Caught;
        public bool CanBreed => IsLakeFish
            && IsAdult
            && !isCaught
            && fishingReservationOwner == null
            && state != StrategyFishBehaviorState.Fleeing
            && state != StrategyFishBehaviorState.Hooked
            && state != StrategyFishBehaviorState.Caught;
        public int ShoalId => shoalId;
        public int WaterRegionId => waterRegionId;
        public Vector2Int HomeCell => homeCell;
        public int HomeRadius => homeRadius;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyFishSpecies fishSpecies,
            Vector2Int shoalCenterCell,
            int shoalHomeRadius,
            int shoalIdentifier,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            StrategyFishLifeStage fishLifeStage = StrategyFishLifeStage.Adult,
            float initialAgeSeconds = 0f,
            StrategyFishHabitatKind fishHabitatKind = StrategyFishHabitatKind.Lake,
            int fishWaterRegionId = -1)
        {
            map = mapController;
            population = populationController;
            species = fishSpecies;
            lifeStage = fishLifeStage;
            habitatKind = fishHabitatKind;
            homeCell = shoalCenterCell;
            homeRadius = Mathf.Max(4, shoalHomeRadius);
            shoalId = shoalIdentifier;
            waterRegionId = fishWaterRegionId;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);
            riverRouteActive = false;
            riverSpeedMultiplier = 1f;
            ageSeconds = lifeStage == StrategyFishLifeStage.Fry
                ? Mathf.Clamp(initialAgeSeconds, 0f, FryMaturitySeconds)
                : FryMaturitySeconds;
            UpdateVisualScale();

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.068f);
            transform.localRotation = Quaternion.identity;
            SetAnimatedScale(1f, 1f);
            state = StrategyFishBehaviorState.Idle;
            fishingReservationOwner = null;
            reelHits = 0;
            isCaught = false;
            waitTimer = Random.Range(0.4f, 1.4f);
            stateTimer = waitTimer;
            ApplySprite(StrategyFishSpritePose.Idle, Random.Range(0, StrategyFishSpriteFactory.IdleFrameCount));
            EnsureRippleRenderer();
            UpdateWorldSorting();
        }

        public void ConfigureRiverRoute(IReadOnlyList<Vector3> routeWorldPoints, float speedMultiplier)
        {
            if (routeWorldPoints == null || routeWorldPoints.Count <= 0)
            {
                return;
            }

            habitatKind = StrategyFishHabitatKind.River;
            riverRouteActive = true;
            riverSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.72f, 1.34f);
            path.Clear();
            for (int i = 0; i < routeWorldPoints.Count; i++)
            {
                Vector3 point = routeWorldPoints[i];
                path.Add(new Vector3(point.x, point.y, -0.068f));
            }

            pathIndex = 0;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            SetState(StrategyFishBehaviorState.Swimming, false, false);
        }

        public bool TryGetCurrentCell(out Vector2Int cell)
        {
            cell = default;
            return map != null && map.TryWorldToCell(transform.position, out cell);
        }

        public bool TryReserveForFishing(object owner)
        {
            if (owner == null || !CanBeFished)
            {
                return false;
            }

            fishingReservationOwner = owner;
            hasTarget = false;
            if (!riverRouteActive)
            {
                path.Clear();
                pathIndex = 0;
            }

            waitTimer = Random.Range(0.25f, 0.85f);
            SetState(StrategyFishBehaviorState.Idle, false, false);
            return true;
        }

        public void ReleaseFishingReservation(object owner)
        {
            if (owner == null || fishingReservationOwner != owner)
            {
                return;
            }

            fishingReservationOwner = null;
            reelHits = 0;
            if (isCaught)
            {
                return;
            }

            if (riverRouteActive && pathIndex < path.Count)
            {
                hasTarget = true;
                SetState(StrategyFishBehaviorState.Swimming, false, false);
                return;
            }

            if (state == StrategyFishBehaviorState.Hooked)
            {
                StartIdle(Random.Range(0.3f, 0.9f));
            }
        }

        public bool ReceiveFishingHook(object owner, Vector3 hookPosition)
        {
            if (owner == null || fishingReservationOwner != owner || isCaught)
            {
                return false;
            }

            hookWorld = new Vector3(hookPosition.x, hookPosition.y, transform.position.z);
            reelHits = 0;
            hasTarget = false;
            if (!riverRouteActive)
            {
                path.Clear();
                pathIndex = 0;
            }

            SetState(StrategyFishBehaviorState.Hooked, true, false);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishHooked",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("hookWorld", hookWorld));
            return true;
        }

        public bool ReceiveReelPull(object owner, Vector3 pullWorld, out int fishAmount)
        {
            fishAmount = 0;
            if (owner == null || fishingReservationOwner != owner || isCaught || state != StrategyFishBehaviorState.Hooked)
            {
                return false;
            }

            reelHits++;
            hookWorld = new Vector3(pullWorld.x, pullWorld.y, transform.position.z);
            if (reelHits < ReelHitsRequired)
            {
                StrategyDebugLogger.Info(
                    "Fishing",
                    "FishReelPull",
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("shoal", shoalId),
                    StrategyDebugLogger.F("reelHits", reelHits),
                    StrategyDebugLogger.F("world", transform.position));
                return false;
            }

            isCaught = true;
            fishAmount = FishYield;
            SetState(StrategyFishBehaviorState.Caught, true, false);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishCaught",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("yield", fishAmount),
                StrategyDebugLogger.F("world", transform.position));
            Destroy(gameObject);
            return true;
        }

        private void Update()
        {
            if (map == null || spriteRenderer == null)
            {
                return;
            }

            UpdateAge();
            if (state == StrategyFishBehaviorState.Hooked)
            {
                UpdateHooked();
                UpdateRipple();
                return;
            }

            if (state == StrategyFishBehaviorState.Caught || isCaught)
            {
                AnimateHooked();
                UpdateRipple();
                return;
            }

            if (fishingReservationOwner != null)
            {
                AnimateIdle();
                UpdateRipple();
                return;
            }

            if (!riverRouteActive)
            {
                UpdateThreatAwareness();
            }

            switch (state)
            {
                case StrategyFishBehaviorState.Swimming:
                    UpdateSwimming();
                    break;
                case StrategyFishBehaviorState.Feeding:
                    UpdateFeeding();
                    break;
                case StrategyFishBehaviorState.Fleeing:
                    UpdateFleeing();
                    break;
                case StrategyFishBehaviorState.Turning:
                    UpdateTurning();
                    break;
                case StrategyFishBehaviorState.Hooked:
                    UpdateHooked();
                    break;
                default:
                    UpdateIdle();
                    break;
            }

            UpdateRipple();
        }

        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private void UpdateThreatAwareness()
        {
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

            if (threatDistance <= fleeDistance)
            {
                StartFleeing(threatWorld, noisyThreat);
                return;
            }

            if (threatDistance <= alertDistance && state != StrategyFishBehaviorState.Fleeing && TryPickSwimTarget(true))
            {
                SetState(StrategyFishBehaviorState.Swimming, false, noisyThreat);
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

        private void UpdateSwimming()
        {
            if (!hasTarget || pathIndex >= path.Count)
            {
                if (riverRouteActive)
                {
                    CompleteRiverRoute();
                    return;
                }

                StartIdle(Random.Range(0.2f, 0.9f));
                return;
            }

            float speed = riverRouteActive ? RiverSwimSpeed * riverSpeedMultiplier : SwimSpeed;
            if (MoveAlongPath(speed, false))
            {
                if (riverRouteActive)
                {
                    CompleteRiverRoute();
                    return;
                }

                StartIdle(Random.Range(0.2f, 0.8f));
            }
        }

        private void UpdateFeeding()
        {
            stateTimer -= Time.deltaTime;
            AnimateFeed();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.9f));
            }
        }

        private void UpdateFleeing()
        {
            stateTimer -= Time.deltaTime;
            if (!hasTarget || pathIndex >= path.Count)
            {
                if (stateTimer > 0f && TryPickFleeTarget(lastThreatWorld))
                {
                    hasTarget = true;
                }
                else
                {
                    StartIdle(Random.Range(0.25f, 0.85f));
                    return;
                }
            }

            if (MoveAlongPath(FleeSpeed, true) && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.85f));
            }
        }

        private void UpdateTurning()
        {
            stateTimer -= Time.deltaTime;
            AnimateTurn();
            if (stateTimer <= 0f)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
                StartIdle(Random.Range(0.2f, 0.75f));
            }
        }

        private void UpdateHooked()
        {
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, hookWorld, FleeSpeed * 0.55f * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            float jitterX = Mathf.Sin((Time.time + bobPhase) * 18f) * 0.025f;
            float jitterY = Mathf.Cos((Time.time + bobPhase) * 15f) * 0.018f;
            transform.position = new Vector3(transform.position.x + jitterX * Time.deltaTime, transform.position.y + jitterY * Time.deltaTime, -0.068f);
            AnimateHooked();
        }

        private void PickRelaxedBehavior()
        {
            float roll = Random.value;
            if (roll < 0.24f)
            {
                StartFeeding();
                return;
            }

            if (roll < 0.90f && TryPickSwimTarget(false))
            {
                SetState(StrategyFishBehaviorState.Swimming, false, false);
                return;
            }

            if (roll < 0.96f)
            {
                StartTurning();
                return;
            }

            StartIdle(Random.Range(0.35f, 1.25f));
        }

        private void StartIdle(float duration)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = duration;
            SetState(StrategyFishBehaviorState.Idle, false, false);
        }

        private void StartFeeding()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.2f, 3.4f);
            SetState(StrategyFishBehaviorState.Feeding, false, false);
        }

        private void StartTurning()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(0.35f, 0.7f);
            SetState(StrategyFishBehaviorState.Turning, false, false);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.6f, 2.8f) : Random.Range(1.0f, 2.0f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyFishBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyFishBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyFishBehaviorState nextState, bool logImportant, bool noisyThreat)
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
            SetAnimatedScale(1f, 1f);

            if (logImportant)
            {
                string eventName = nextState == StrategyFishBehaviorState.Hooked
                    ? "FishHookedState"
                    : nextState == StrategyFishBehaviorState.Fleeing
                        ? "FishFleeing"
                        : "FishStateChanged";
                StrategyDebugLogger.Info(
                    "Wildlife",
                    eventName,
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("shoal", shoalId),
                    StrategyDebugLogger.F("noisyThreat", noisyThreat),
                    StrategyDebugLogger.F("world", transform.position));
            }
        }

        private bool MoveAlongPath(float speed, bool fleeing)
        {
            if (pathIndex >= path.Count)
            {
                hasTarget = false;
                return true;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    hasTarget = false;
                    return true;
                }

                targetWorld = path[pathIndex];
            }

            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, speed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                if (fleeing)
                {
                    AnimateFlee();
                }
                else
                {
                    AnimateSwim();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private void CompleteRiverRoute()
        {
            if (isCaught)
            {
                return;
            }

            isCaught = true;
            StrategyDebugLogger.Info(
                "Wildlife",
                "RiverFishDespawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("world", transform.position));
            Destroy(gameObject);
        }

        private bool TryPickSwimTarget(bool awayFromThreat)
        {
            if (!TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            Vector2 away = Vector2.zero;
            if (awayFromThreat)
            {
                away = (Vector2)transform.position - (Vector2)lastThreatWorld;
                if (away.sqrMagnitude > 0.01f)
                {
                    away.Normalize();
                }
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 28; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));
                if (!IsRelaxedFishTarget(cell))
                {
                    continue;
                }

                Vector3 cellWorld = map.GetCellCenterWorld(cell.x, cell.y);
                float score = GetWaterPreference(cell) - Vector2Int.Distance(cell, homeCell) * 0.08f;
                if (awayFromThreat && away.sqrMagnitude > 0f)
                {
                    Vector2 direction = (Vector2)cellWorld - (Vector2)transform.position;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        score += Vector2.Dot(direction.normalized, away) * 3.0f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                    found = true;
                }
            }

            if (!found || !TryBuildPathTo(bestCell))
            {
                return false;
            }

            hasTarget = path.Count > 0;
            return hasTarget;
        }

        private bool TryPickFleeTarget(Vector3 threatWorld)
        {
            if (!map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            Vector2 currentWorld = transform.position;
            Vector2 away = currentWorld - (Vector2)threatWorld;
            if (away.sqrMagnitude < 0.01f)
            {
                away = Random.insideUnitCircle;
                if (away.sqrMagnitude < 0.01f)
                {
                    away = Vector2.right;
                }
            }

            away.Normalize();

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 42; attempt++)
            {
                int distance = Random.Range(4, 11);
                Vector2 lateral = new Vector2(-away.y, away.x) * Random.Range(-3.0f, 3.0f);
                Vector2 candidateOffset = away * distance + lateral + Random.insideUnitCircle * 1.8f;
                Vector2Int candidate = currentCell + new Vector2Int(
                    Mathf.RoundToInt(candidateOffset.x),
                    Mathf.RoundToInt(candidateOffset.y));

                if (!IsFleeTarget(candidate))
                {
                    continue;
                }

                Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                float threatDistance = Vector2.Distance(candidateWorld, threatWorld);
                float homeDistance = Vector2Int.Distance(candidate, homeCell);
                float directionScore = Vector2.Dot(((Vector2)candidateWorld - currentWorld).normalized, away);
                float score = threatDistance * 1.35f + directionScore * 4.0f + GetWaterPreference(candidate) - homeDistance * 0.10f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found || !TryBuildPathTo(bestCell))
            {
                return false;
            }

            hasTarget = path.Count > 0;
            return hasTarget;
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (!map.TryWorldToCell(transform.position, out Vector2Int startCell)
                || !IsFishWaterCell(startCell)
                || !IsFishWaterCell(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.068f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 460)
            {
                Vector2Int current = open.Dequeue();
                if (current == targetCell)
                {
                    BuildWorldPath(startCell, targetCell, cameFrom);
                    return path.Count > 0;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !IsFishWaterCell(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            return false;
        }

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
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.22f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.068f));
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
                || activity == StrategyResidentAgent.ResidentActivity.CastingFishingLine
                || activity == StrategyResidentAgent.ResidentActivity.ReelingFish
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }

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
