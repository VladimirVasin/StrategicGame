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
    public sealed partial class StrategyDeerAgent : MonoBehaviour, IStrategyWorldInspectable
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
        private StrategyWildlifeController wildlife;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer swimRippleRenderer;
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
        public bool IsPredatorReserved => predatorReservationOwner != null;
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
            StrategyWildlifeController wildlifeController,
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
            wildlife = wildlifeController;
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
    }
}
