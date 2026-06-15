using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyRabbitSex
    {
        Female,
        Male
    }

    public enum StrategyRabbitBehaviorState
    {
        Idle,
        Hopping,
        Nibbling,
        Alert,
        Fleeing,
        Grooming,
        Resting,
        Hunted,
        Hit,
        Dead,
        Carcass
    }

    public enum StrategyRabbitLifeStage
    {
        Kit,
        Adult
    }

    [DisallowMultipleComponent]
    public sealed class StrategyRabbitAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float HopSpeed = 1.05f;
        private const float FleeSpeed = 2.7f;
        private const float TargetReachDistance = 0.04f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float ThreatCheckInterval = 0.13f;
        private const float AlertRadius = 3.6f;
        private const float FleeRadius = 1.95f;
        private const float NoisyAlertRadius = 6.2f;
        private const float NoisyFleeRadius = 4.15f;
        private const float IdleAnimationRate = 6.2f;
        private const float HopAnimationRate = 11.5f;
        private const float NibbleAnimationRate = 9.0f;
        private const float AlertAnimationRate = 8.5f;
        private const float FleeAnimationRate = 17.5f;
        private const float GroomAnimationRate = 10.0f;
        private const float RestAnimationRate = 4.5f;
        private const float HitAnimationRate = 14.0f;
        private const float DeathAnimationRate = 10.0f;
        private const float ReadabilityOutlineScale = 1.14f;
        private const float ReadabilityEffectScale = 0.6f;
        private const float RabbitGlobalScale = 0.82f;
        private const float KitMaturitySeconds = 120f;
        private const float KitStartScale = 0.52f;
        private const float KitMatureScale = 0.86f;
        private const int GameYield = 2;
        private const int ButcherHitsRequired = 3;

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
        private StrategyRabbitSex sex;
        private StrategyRabbitBehaviorState state;
        private StrategyRabbitLifeStage lifeStage = StrategyRabbitLifeStage.Adult;
        private StrategyRabbitSpritePose appliedPose;
        private Vector2Int homeCell;
        private Vector3 lastThreatWorld;
        private int pathIndex;
        private int homeRadius;
        private int groupId;
        private int frame;
        private int appliedFrame = -1;
        private float waitTimer;
        private float stateTimer;
        private float threatCheckTimer;
        private float frameTimer;
        private float bobPhase;
        private float ageSeconds;
        private float visualScale = 1f;
        private object huntReservationOwner;
        private object predatorReservationOwner;
        private int butcherHits;
        private bool hasTarget;
        private bool hasThreat;
        private bool hasAppliedPose;
        private bool isAlive = true;
        private bool isCarcass;

        public StrategyRabbitSex Sex => sex;
        public StrategyRabbitBehaviorState State => state;
        public StrategyRabbitLifeStage LifeStage => lifeStage;
        public bool IsAdult => lifeStage == StrategyRabbitLifeStage.Adult;
        public bool IsAlive => isAlive;
        public bool IsCarcass => isCarcass;
        public bool CanBeHunted => IsAdult && isAlive && !isCarcass && huntReservationOwner == null && predatorReservationOwner == null;
        public bool CanBeWolfPrey => IsAdult && isAlive && !isCarcass && huntReservationOwner == null && predatorReservationOwner == null;
        public bool CanBreed => IsAdult
            && isAlive
            && !isCarcass
            && huntReservationOwner == null
            && predatorReservationOwner == null
            && sex == StrategyRabbitSex.Female
            && state != StrategyRabbitBehaviorState.Alert
            && state != StrategyRabbitBehaviorState.Fleeing;
        public int GroupId => groupId;
        public Vector2Int HomeCell => homeCell;
        public int HomeRadius => homeRadius;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyRabbitSex rabbitSex,
            Vector2Int groupCenterCell,
            int groupHomeRadius,
            int groupIdentifier,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            StrategyRabbitLifeStage rabbitLifeStage = StrategyRabbitLifeStage.Adult,
            float initialAgeSeconds = 0f)
        {
            map = mapController;
            population = populationController;
            sex = rabbitSex;
            lifeStage = rabbitLifeStage;
            homeCell = groupCenterCell;
            homeRadius = Mathf.Max(3, groupHomeRadius);
            groupId = groupIdentifier;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);
            ageSeconds = lifeStage == StrategyRabbitLifeStage.Kit
                ? Mathf.Clamp(initialAgeSeconds, 0f, KitMaturitySeconds)
                : KitMaturitySeconds;
            UpdateVisualScale();

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.072f);
            transform.localRotation = Quaternion.identity;
            SetAnimatedScale(1f, 1f);
            state = StrategyRabbitBehaviorState.Idle;
            waitTimer = Random.Range(0.25f, 1.05f);
            stateTimer = waitTimer;
            ApplySprite(StrategyRabbitSpritePose.Idle, Random.Range(0, StrategyRabbitSpriteFactory.IdleFrameCount));
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
                + "\nGroup: "
                + GroupId
                + "\nHuntable: "
                + (CanBeHunted ? "yes" : "no");
            info = new StrategyWorldInspectInfo(
                IsCarcass ? "Rabbit Carcass" : "Rabbit",
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

        public void RetargetGroupCenter(Vector2Int center, int radius)
        {
            homeCell = center;
            homeRadius = Mathf.Max(3, radius);
        }

        public bool TryReserveForHunt(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (huntReservationOwner == owner)
            {
                return isAlive || isCarcass;
            }

            if (!CanBeHunted)
            {
                return false;
            }

            huntReservationOwner = owner;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(0.45f, 0.95f);
            SetState(StrategyRabbitBehaviorState.Hunted, true, false);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitHuntReserved",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position));
            return true;
        }

        public void ReleaseHuntReservation(object owner)
        {
            if (owner == null || huntReservationOwner != owner)
            {
                return;
            }

            huntReservationOwner = null;
            if (isAlive)
            {
                StartAlert(transform.position + Vector3.left, false);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitHuntReservationReleased",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position));
        }

        public bool TryReserveForPredator(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (predatorReservationOwner == owner)
            {
                return isAlive && !isCarcass;
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
            stateTimer = Random.Range(0.35f, 0.8f);
            SetState(StrategyRabbitBehaviorState.Alert, true, true);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitPredatorReserved",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
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
                "RabbitPredatorReservationReleased",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position));
        }

        public bool KillByPredator(object owner, Vector3 attackWorld)
        {
            if (owner == null || predatorReservationOwner != owner || !isAlive || isCarcass)
            {
                return false;
            }

            isAlive = false;
            isCarcass = false;
            huntReservationOwner = null;
            butcherHits = 0;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = attackWorld;
            SetAnimatedScale(1f, 1f);
            SetState(StrategyRabbitBehaviorState.Hit, true, true);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitKilledByPredator",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
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
                "RabbitConsumedByPredator",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position));
            Destroy(gameObject);
            return true;
        }

        public bool ReceiveArrowHit(object owner, Vector3 hitWorld)
        {
            if (owner == null || huntReservationOwner != owner || !isAlive || isCarcass)
            {
                return false;
            }

            isAlive = false;
            isCarcass = false;
            butcherHits = 0;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = hitWorld;
            SetAnimatedScale(1f, 1f);
            SetState(StrategyRabbitBehaviorState.Hit, true, false);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitHit",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("hitWorld", hitWorld));
            return true;
        }

        public bool ReceiveButcherHit(object owner, Vector3 hitWorld, out int gameAmount)
        {
            gameAmount = 0;
            if (owner == null || huntReservationOwner != owner || isAlive || !isCarcass)
            {
                return false;
            }

            butcherHits++;
            FaceWorldPoint(hitWorld);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitButcherHit",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("hit", butcherHits),
                StrategyDebugLogger.F("required", ButcherHitsRequired),
                StrategyDebugLogger.F("world", transform.position));
            if (butcherHits < ButcherHitsRequired)
            {
                return false;
            }

            gameAmount = GameYield;
            huntReservationOwner = null;
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitButchered",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("yield", gameAmount),
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
            if (!isAlive)
            {
                UpdateHuntDeath();
                return;
            }

            if (huntReservationOwner != null)
            {
                UpdateHunted();
                return;
            }

            UpdateThreatAwareness();

            switch (state)
            {
                case StrategyRabbitBehaviorState.Hopping:
                    UpdateHopping();
                    break;
                case StrategyRabbitBehaviorState.Nibbling:
                    UpdateNibbling();
                    break;
                case StrategyRabbitBehaviorState.Alert:
                    UpdateAlert();
                    break;
                case StrategyRabbitBehaviorState.Fleeing:
                    UpdateFleeing();
                    break;
                case StrategyRabbitBehaviorState.Grooming:
                    UpdateGrooming();
                    break;
                case StrategyRabbitBehaviorState.Resting:
                    UpdateResting();
                    break;
                case StrategyRabbitBehaviorState.Hunted:
                    UpdateHunted();
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

            if (threatDistance <= alertDistance && state != StrategyRabbitBehaviorState.Fleeing)
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

        private void UpdateHopping()
        {
            if (!hasTarget || pathIndex >= path.Count)
            {
                StartIdle(Random.Range(0.18f, 0.75f));
                return;
            }

            if (MoveAlongPath(HopSpeed, false))
            {
                StartIdle(Random.Range(0.15f, 0.65f));
            }
        }

        private void UpdateNibbling()
        {
            stateTimer -= Time.deltaTime;
            AnimateNibble();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.75f));
            }
        }

        private void UpdateAlert()
        {
            stateTimer -= Time.deltaTime;
            FaceWorldPoint(lastThreatWorld);
            AnimateAlert();
            if (!hasThreat && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.8f));
                return;
            }

            if (stateTimer <= -0.8f)
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

        private void UpdateGrooming()
        {
            stateTimer -= Time.deltaTime;
            AnimateGroom();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.7f));
            }
        }

        private void UpdateResting()
        {
            stateTimer -= Time.deltaTime;
            AnimateRest();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.85f));
            }
        }

        private void UpdateHunted()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer -= Time.deltaTime;
            AnimateAlert();
            if (stateTimer <= -1.0f)
            {
                stateTimer = Random.Range(0.2f, 0.65f);
            }
        }

        private void UpdateHuntDeath()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (state == StrategyRabbitBehaviorState.Hit)
            {
                AnimateHit();
                if (frame >= StrategyRabbitSpriteFactory.HitFrameCount - 1)
                {
                    SetState(StrategyRabbitBehaviorState.Dead, false, false);
                }

                return;
            }

            if (state == StrategyRabbitBehaviorState.Dead)
            {
                AnimateDeath();
                if (frame >= StrategyRabbitSpriteFactory.DeathFrameCount - 1)
                {
                    isCarcass = true;
                    SetState(StrategyRabbitBehaviorState.Carcass, false, false);
                    ApplySprite(StrategyRabbitSpritePose.Carcass, 0);
                    SetAnimatedScale(1f, 1f);
                }

                return;
            }

            isCarcass = true;
            ApplySprite(StrategyRabbitSpritePose.Carcass, 0);
            SetAnimatedScale(1f, 1f);
        }

        private void PickRelaxedBehavior()
        {
            float roll = Random.value;
            if (roll < 0.40f)
            {
                StartNibbling();
                return;
            }

            if (roll < 0.76f && TryPickHopTarget())
            {
                SetState(StrategyRabbitBehaviorState.Hopping, false, false);
                return;
            }

            if (roll < 0.90f)
            {
                StartGrooming();
                return;
            }

            if (roll < 0.97f)
            {
                StartResting();
                return;
            }

            StartIdle(Random.Range(0.25f, 1.0f));
        }

        private void StartIdle(float duration)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = duration;
            SetState(StrategyRabbitBehaviorState.Idle, false, false);
        }

        private void StartNibbling()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.7f, 4.2f);
            SetState(StrategyRabbitBehaviorState.Nibbling, false, false);
        }

        private void StartGrooming()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.0f, 2.4f);
            SetState(StrategyRabbitBehaviorState.Grooming, false, false);
        }

        private void StartResting()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.8f, 4.0f);
            SetState(StrategyRabbitBehaviorState.Resting, false, false);
        }

        private void StartAlert(Vector3 threatWorld, bool noisyThreat)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.0f, 2.3f) : Random.Range(0.6f, 1.5f);
            SetState(StrategyRabbitBehaviorState.Alert, true, noisyThreat);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.5f, 2.8f) : Random.Range(1.0f, 2.0f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyRabbitBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyRabbitBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyRabbitBehaviorState nextState, bool logImportant, bool noisyThreat)
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
                    nextState == StrategyRabbitBehaviorState.Fleeing ? "RabbitFleeing" : "RabbitAlert",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("group", groupId),
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
                    AnimateHop();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private bool TryPickHopTarget()
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));

                if (!IsRelaxedRabbitTarget(cell))
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
            for (int attempt = 0; attempt < 38; attempt++)
            {
                int distance = Random.Range(4, 10);
                Vector2 lateral = new Vector2(-away.y, away.x) * Random.Range(-3.5f, 3.5f);
                Vector2 randomArc = Random.insideUnitCircle * 2.0f;
                Vector2 candidateOffset = away * distance + lateral + randomArc;
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
                float score = threatDistance * 1.45f + directionScore * 4.5f + terrainScore - homeDistance * 0.12f;
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
                || !IsRabbitWalkCell(startCell)
                || !IsRabbitWalkCell(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.072f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 360)
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
                    if (visited.Contains(next) || !IsRabbitWalkCell(next))
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
