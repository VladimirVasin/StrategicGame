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
    public sealed partial class StrategyRabbitAgent : MonoBehaviour, IStrategyWorldInspectable, IStrategyHuntTarget
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
        private StrategyWildlifeController wildlife;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer swimRippleRenderer;
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
        public bool IsHuntReserved => huntReservationOwner != null;
        public bool IsPredatorReserved => predatorReservationOwner != null;
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
            StrategyWildlifeController wildlifeController,
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
            wildlife = wildlifeController;
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
            threatCheckTimer = Random.Range(0f, ThreatCheckInterval);
            ApplySprite(StrategyRabbitSpritePose.Idle, Random.Range(0, StrategyRabbitSpriteFactory.IdleFrameCount));
            EnsureReadabilityRenderers();
            UpdateWorldSorting();
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            bool hasCell = TryGetCurrentCell(out Vector2Int currentCell);
            info = StrategyWorldInspectInfoFactory.CreateRabbit(
                this,
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
    }
}
