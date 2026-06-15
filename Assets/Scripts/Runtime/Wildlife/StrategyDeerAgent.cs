using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyDeerSex
    {
        Female,
        Male
    }

    public enum StrategyDeerBehaviorState
    {
        Idle,
        Walking,
        Grazing,
        Alert,
        Fleeing,
        Resting
    }

    public enum StrategyDeerLifeStage
    {
        Fawn,
        Adult
    }

    [DisallowMultipleComponent]
    public sealed class StrategyDeerAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float WalkSpeed = 0.78f;
        private const float FleeSpeed = 2.35f;
        private const float TargetReachDistance = 0.045f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float ThreatCheckInterval = 0.16f;
        private const float AlertRadius = 5.2f;
        private const float FleeRadius = 2.85f;
        private const float NoisyAlertRadius = 8.0f;
        private const float NoisyFleeRadius = 5.7f;
        private const float IdleAnimationRate = 5.5f;
        private const float WalkAnimationRate = 10.5f;
        private const float GrazeAnimationRate = 8.0f;
        private const float AlertAnimationRate = 7.5f;
        private const float FleeAnimationRate = 15.0f;
        private const float RestAnimationRate = 4.0f;
        private const float ReadabilityOutlineScale = 1.11f;
        private const float ReadabilityEffectScale = 0.6f;
        private const float DeerGlobalScale = 0.88f;
        private const float FawnMaturitySeconds = 240f;
        private const float FawnStartScale = 0.56f;
        private const float FawnMatureScale = 0.94f;

        private static Sprite readabilityShadowSprite;
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
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private StrategyDeerSex sex;
        private StrategyDeerBehaviorState state;
        private StrategyDeerLifeStage lifeStage = StrategyDeerLifeStage.Adult;
        private StrategyDeerSpritePose appliedPose;
        private Vector2Int homeCell;
        private Vector3 lastThreatWorld;
        private int pathIndex;
        private int homeRadius;
        private int herdId;
        private int frame;
        private int appliedFrame = -1;
        private float waitTimer;
        private float stateTimer;
        private float threatCheckTimer;
        private float frameTimer;
        private float bobPhase;
        private float ageSeconds;
        private float visualScale = 1f;
        private object predatorReservationOwner;
        private bool hasTarget;
        private bool hasThreat;
        private bool hasAppliedPose;
        private bool isAlive = true;

        public StrategyDeerSex Sex => sex;
        public StrategyDeerBehaviorState State => state;
        public StrategyDeerLifeStage LifeStage => lifeStage;
        public bool IsAdult => lifeStage == StrategyDeerLifeStage.Adult;
        public bool IsAlive => isAlive;
        public bool CanBeWolfPrey => IsAdult && isAlive && predatorReservationOwner == null;
        public bool CanBreed => IsAdult
            && isAlive
            && predatorReservationOwner == null
            && sex == StrategyDeerSex.Female
            && state != StrategyDeerBehaviorState.Alert
            && state != StrategyDeerBehaviorState.Fleeing;
        public int HerdId => herdId;
        public Vector2Int HomeCell => homeCell;
        public int HomeRadius => homeRadius;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyDeerSex deerSex,
            Vector2Int herdCenterCell,
            int herdHomeRadius,
            int herdIdentifier,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            StrategyDeerLifeStage deerLifeStage = StrategyDeerLifeStage.Adult,
            float initialAgeSeconds = 0f)
        {
            map = mapController;
            population = populationController;
            sex = deerSex;
            lifeStage = deerLifeStage;
            homeCell = herdCenterCell;
            homeRadius = Mathf.Max(4, herdHomeRadius);
            herdId = herdIdentifier;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);
            ageSeconds = lifeStage == StrategyDeerLifeStage.Fawn
                ? Mathf.Clamp(initialAgeSeconds, 0f, FawnMaturitySeconds)
                : FawnMaturitySeconds;
            UpdateVisualScale();

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.075f);
            transform.localRotation = Quaternion.identity;
            SetAnimatedScale(1f, 1f);
            state = StrategyDeerBehaviorState.Idle;
            waitTimer = Random.Range(0.35f, 1.4f);
            stateTimer = waitTimer;
            ApplySprite(StrategyDeerSpritePose.Idle, Random.Range(0, StrategyDeerSpriteFactory.IdleFrameCount));
            EnsureReadabilityRenderers();
            UpdateWorldSorting();
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            bool hasCell = TryGetCurrentCell(out Vector2Int currentCell);
            string body = "Sex: "
                + Sex
                + "\nStage: "
                + LifeStage
                + "\nState: "
                + State
                + "\nHerd: "
                + HerdId
                + "\nWolf prey: "
                + (CanBeWolfPrey ? "yes" : "no");
            info = new StrategyWorldInspectInfo(
                Sex == StrategyDeerSex.Male ? "Stag" : "Doe",
                "Wildlife",
                body,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                currentCell,
                hasCell);
            return true;
        }

        public bool TryGetCurrentCell(out Vector2Int cell)
        {
            cell = default;
            return map != null && map.TryWorldToCell(transform.position, out cell);
        }

        public void RetargetHerdCenter(Vector2Int center, int radius)
        {
            homeCell = center;
            homeRadius = Mathf.Max(4, radius);
        }

        public bool TryReserveForPredator(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (predatorReservationOwner == owner)
            {
                return isAlive;
            }

            if (!CanBeWolfPrey)
            {
                return false;
            }

            predatorReservationOwner = owner;
            hasTarget = false;
            hasThreat = true;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = transform.position + Vector3.left;
            stateTimer = Random.Range(0.7f, 1.4f);
            SetState(StrategyDeerBehaviorState.Alert, true, true);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerPredatorReserved",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position));
            return true;
        }

        public void ReleasePredatorReservation(object owner)
        {
            if (owner == null || predatorReservationOwner != owner)
            {
                return;
            }

            predatorReservationOwner = null;
            if (isAlive)
            {
                StartFleeing(transform.position + Vector3.left, true);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerPredatorReservationReleased",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position));
        }

        public bool KillByPredator(object owner, Vector3 attackWorld)
        {
            if (owner == null || predatorReservationOwner != owner || !isAlive)
            {
                return false;
            }

            isAlive = false;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = attackWorld;
            state = StrategyDeerBehaviorState.Resting;
            transform.localRotation = Quaternion.Euler(0f, 0f, spriteRenderer != null && spriteRenderer.flipX ? 11f : -11f);
            SetAnimatedScale(1.04f, 0.72f);
            ApplySprite(StrategyDeerSpritePose.Rest, 0);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerKilledByPredator",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("attackWorld", attackWorld));
            return true;
        }

        public bool ConsumePredatorKill(object owner)
        {
            if (owner == null || predatorReservationOwner != owner || isAlive)
            {
                return false;
            }

            predatorReservationOwner = null;
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerConsumedByPredator",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
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

            if (!isAlive)
            {
                return;
            }

            UpdateAge();
            UpdateThreatAwareness();

            switch (state)
            {
                case StrategyDeerBehaviorState.Walking:
                    UpdateWalking();
                    break;
                case StrategyDeerBehaviorState.Grazing:
                    UpdateGrazing();
                    break;
                case StrategyDeerBehaviorState.Alert:
                    UpdateAlert();
                    break;
                case StrategyDeerBehaviorState.Fleeing:
                    UpdateFleeing();
                    break;
                case StrategyDeerBehaviorState.Resting:
                    UpdateResting();
                    break;
                default:
                    UpdateIdle();
                    break;
            }
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
                hasThreat = false;
                return;
            }

            hasThreat = true;
            lastThreatWorld = threatWorld;
            float fleeDistance = noisyThreat ? NoisyFleeRadius : FleeRadius;
            float alertDistance = noisyThreat ? NoisyAlertRadius : AlertRadius;

            if (threatDistance <= fleeDistance)
            {
                StartFleeing(threatWorld, noisyThreat);
                return;
            }

            if (threatDistance <= alertDistance && state != StrategyDeerBehaviorState.Fleeing)
            {
                StartAlert(threatWorld, noisyThreat);
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

        private void UpdateWalking()
        {
            if (!hasTarget || pathIndex >= path.Count)
            {
                StartIdle(Random.Range(0.35f, 1.15f));
                return;
            }

            if (MoveAlongPath(WalkSpeed, false))
            {
                StartIdle(Random.Range(0.25f, 0.9f));
            }
        }

        private void UpdateGrazing()
        {
            stateTimer -= Time.deltaTime;
            AnimateGraze();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.85f));
            }
        }

        private void UpdateAlert()
        {
            stateTimer -= Time.deltaTime;
            FaceWorldPoint(lastThreatWorld);
            AnimateAlert();
            if (!hasThreat && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.28f, 0.95f));
                return;
            }

            if (stateTimer <= -1.0f)
            {
                PickRelaxedBehavior();
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
                    StartAlert(lastThreatWorld, false);
                    return;
                }
            }

            if (MoveAlongPath(FleeSpeed, true) && stateTimer <= 0f)
            {
                StartAlert(lastThreatWorld, false);
            }
        }

        private void UpdateResting()
        {
            stateTimer -= Time.deltaTime;
            AnimateRest();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.35f, 1.05f));
            }
        }

        private void PickRelaxedBehavior()
        {
            float roll = Random.value;
            if (roll < 0.48f)
            {
                StartGrazing();
                return;
            }

            if (roll < 0.84f && TryPickWalkTarget())
            {
                SetState(StrategyDeerBehaviorState.Walking, false, false);
                return;
            }

            if (roll < 0.94f)
            {
                StartResting();
                return;
            }

            StartIdle(Random.Range(0.45f, 1.25f));
        }

        private void StartIdle(float duration)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = duration;
            SetState(StrategyDeerBehaviorState.Idle, false, false);
        }

        private void StartGrazing()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(2.4f, 5.8f);
            SetState(StrategyDeerBehaviorState.Grazing, false, false);
        }

        private void StartResting()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(2.2f, 4.7f);
            SetState(StrategyDeerBehaviorState.Resting, false, false);
        }

        private void StartAlert(Vector3 threatWorld, bool noisyThreat)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.5f, 3.2f) : Random.Range(0.9f, 2.2f);
            SetState(StrategyDeerBehaviorState.Alert, true, noisyThreat);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(2.1f, 3.8f) : Random.Range(1.5f, 2.8f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyDeerBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyDeerBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyDeerBehaviorState nextState, bool logImportant, bool noisyThreat)
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
                StrategyDebugLogger.Info(
                    "Wildlife",
                    nextState == StrategyDeerBehaviorState.Fleeing ? "DeerFleeing" : "DeerAlert",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("herd", herdId),
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
                    AnimateWalk();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private bool TryPickWalkTarget()
        {
            for (int attempt = 0; attempt < 22; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));

                if (!IsRelaxedDeerTarget(cell))
                {
                    continue;
                }

                if (TryBuildPathTo(cell))
                {
                    hasTarget = path.Count > 0;
                    return hasTarget;
                }
            }

            return false;
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
                away = Random.insideUnitCircle.normalized;
            }
            else
            {
                away.Normalize();
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 42; attempt++)
            {
                int distance = Random.Range(5, 12);
                Vector2 randomArc = Random.insideUnitCircle * 3.2f;
                Vector2 candidateOffset = away * distance + randomArc;
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
                float terrainScore = GetTerrainPreference(candidate);
                float score = threatDistance * 1.35f + directionScore * 5.0f + terrainScore - homeDistance * 0.16f;
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
                || !IsDeerWalkCell(startCell)
                || !IsDeerWalkCell(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.075f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 640)
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
                    if (visited.Contains(next) || !IsDeerWalkCell(next))
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
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.24f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.075f));
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
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }

        private bool IsRelaxedDeerTarget(Vector2Int cell)
        {
            return IsDeerWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius
                && GetTerrainPreference(cell) >= 0f;
        }

        private bool IsFleeTarget(Vector2Int cell)
        {
            return IsDeerWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius + 14
                && GetTerrainPreference(cell) > -2f;
        }

        private bool IsDeerWalkCell(Vector2Int cell)
        {
            return map != null && map.IsCellWalkable(cell);
        }

        private float GetTerrainPreference(Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 4f,
                CityMapCellKind.Grass => 2.5f,
                CityMapCellKind.Forest => 1.35f,
                CityMapCellKind.Dirt => 0.15f,
                CityMapCellKind.Shore => -0.5f,
                _ => -10f
            };
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(IdleAnimationRate, StrategyDeerSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyDeerSpritePose.Idle, frame);
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 4.5f) * 0.025f;
            SetAnimatedScale(1f, pulse);
        }

        private void AnimateWalk()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(WalkAnimationRate, StrategyDeerSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyDeerSpritePose.Walk, frame);
        }

        private void AnimateGraze()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(GrazeAnimationRate, StrategyDeerSpriteFactory.GrazeFrameCount);
            ApplySprite(StrategyDeerSpritePose.Graze, frame);
        }

        private void AnimateAlert()
        {
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 8.5f) * 0.018f;
            SetAnimatedScale(1f + (pulse - 1f) * 0.4f, pulse);
            AdvanceLoopingFrame(AlertAnimationRate, StrategyDeerSpriteFactory.AlertFrameCount);
            ApplySprite(StrategyDeerSpritePose.Alert, frame);
        }

        private void AnimateFlee()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(FleeAnimationRate, StrategyDeerSpriteFactory.RunFrameCount);
            ApplySprite(StrategyDeerSpritePose.Run, frame);
        }

        private void AnimateRest()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(RestAnimationRate, StrategyDeerSpriteFactory.RestFrameCount);
            ApplySprite(StrategyDeerSpritePose.Rest, frame);
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

        private void ApplySprite(StrategyDeerSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (hasAppliedPose && appliedPose == pose && appliedFrame == spriteFrame)
            {
                return;
            }

            StrategyDeerSex spriteSex = lifeStage == StrategyDeerLifeStage.Fawn ? StrategyDeerSex.Female : sex;
            spriteRenderer.sprite = pose switch
            {
                StrategyDeerSpritePose.Walk => StrategyDeerSpriteFactory.GetWalkSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Graze => StrategyDeerSpriteFactory.GetGrazeSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Alert => StrategyDeerSpriteFactory.GetAlertSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Run => StrategyDeerSpriteFactory.GetRunSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Rest => StrategyDeerSpriteFactory.GetRestSprite(spriteSex, spriteFrame),
                _ => StrategyDeerSpriteFactory.GetIdleSprite(spriteSex, spriteFrame)
            };

            appliedPose = pose;
            appliedFrame = spriteFrame;
            hasAppliedPose = true;
            SyncReadabilityRenderers();
        }

        private void UpdateAge()
        {
            if (lifeStage != StrategyDeerLifeStage.Fawn)
            {
                return;
            }

            ageSeconds += Time.deltaTime;
            if (ageSeconds >= FawnMaturitySeconds)
            {
                lifeStage = StrategyDeerLifeStage.Adult;
                ageSeconds = FawnMaturitySeconds;
                UpdateVisualScale();
                hasAppliedPose = false;
                appliedFrame = -1;
                ApplySprite(appliedPose, frame);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "DeerGrownUp",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("herd", herdId),
                    StrategyDebugLogger.F("world", transform.position));
                return;
            }

            UpdateVisualScale();
        }

        private void UpdateVisualScale()
        {
            visualScale = lifeStage == StrategyDeerLifeStage.Fawn
                ? Mathf.Lerp(FawnStartScale, FawnMatureScale, Mathf.Clamp01(ageSeconds / FawnMaturitySeconds))
                : 1f;
        }

        private void SetAnimatedScale(float x, float y)
        {
            transform.localScale = new Vector3(visualScale * DeerGlobalScale * x, visualScale * DeerGlobalScale * y, 1f);
        }

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
