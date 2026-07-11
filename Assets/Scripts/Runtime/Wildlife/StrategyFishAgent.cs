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
    public sealed partial class StrategyFishAgent : MonoBehaviour, IStrategyWorldInspectable
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
        private const float HookVisualLift = 0.10f;
        private const float ReelCatchDistance = 0.16f;
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
        private Vector3 hookStartWorld;
        private Vector3 reelTargetWorld;
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
        private float reelProgress;
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
        public Vector3 FishingHookWorld => state == StrategyFishBehaviorState.Hooked
            ? hookWorld
            : new Vector3(transform.position.x, transform.position.y + HookVisualLift, -0.11f);
        public float FishingReelProgress => reelProgress;
        public bool CanBeFished => IsAdult
            && !isCaught
            && !StrategySeasonalSurfaceController.IsWaterFrozenForGameplay
            && fishingReservationOwner == null
            && state != StrategyFishBehaviorState.Fleeing
            && state != StrategyFishBehaviorState.Hooked
            && state != StrategyFishBehaviorState.Caught;
        public bool CanBreed => IsLakeFish
            && IsAdult
            && !isCaught
            && !StrategySeasonalSurfaceController.IsWaterFrozenForGameplay
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
            reelProgress = 0f;
            hookStartWorld = transform.position;
            reelTargetWorld = transform.position;
            hookWorld = FishingHookWorld;
            isCaught = false;
            waitTimer = Random.Range(0.4f, 1.4f);
            stateTimer = waitTimer;
            threatCheckTimer = Random.Range(0f, ThreatCheckInterval);
            ApplySprite(StrategyFishSpritePose.Idle, Random.Range(0, StrategyFishSpriteFactory.IdleFrameCount));
            EnsureRippleRenderer();
            UpdateWorldSorting();
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            bool hasCell = TryGetCurrentCell(out Vector2Int currentCell);
            info = StrategyWorldInspectInfoFactory.CreateFish(
                this,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                currentCell,
                hasCell);
            return true;
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
            return this != null && map != null && map.TryWorldToCell(transform.position, out cell);
        }

        public void RetargetShoalCenter(Vector2Int center, int radius)
        {
            if (IsRiverFish)
            {
                return;
            }

            homeCell = center;
            homeRadius = Mathf.Max(4, radius);
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
            reelProgress = 0f;
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
            if (owner == null
                || fishingReservationOwner != owner
                || isCaught
                || StrategySeasonalSurfaceController.IsWaterFrozenForGameplay)
            {
                return false;
            }

            hookStartWorld = transform.position;
            reelTargetWorld = hookStartWorld;
            hookWorld = new Vector3(hookPosition.x, hookPosition.y, -0.11f);
            reelHits = 0;
            reelProgress = 0f;
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
            if (owner == null
                || fishingReservationOwner != owner
                || isCaught
                || state != StrategyFishBehaviorState.Hooked
                || StrategySeasonalSurfaceController.IsWaterFrozenForGameplay)
            {
                return false;
            }

            reelHits++;
            reelTargetWorld = new Vector3(pullWorld.x, pullWorld.y, transform.position.z);
            reelProgress = Mathf.Max(reelProgress, Mathf.Clamp01(reelHits / (float)ReelHitsRequired));
            float distanceToCatch = Vector2.Distance(transform.position, reelTargetWorld);
            if (reelProgress < 1f || distanceToCatch > ReelCatchDistance)
            {
                StrategyDebugLogger.Info(
                    "Fishing",
                    "FishReelPull",
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("shoal", shoalId),
                    StrategyDebugLogger.F("reelHits", reelHits),
                    StrategyDebugLogger.F("progress", reelProgress),
                    StrategyDebugLogger.F("world", transform.position),
                    StrategyDebugLogger.F("targetWorld", reelTargetWorld),
                    StrategyDebugLogger.F("distance", distanceToCatch));
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
            if (Time.timeScale <= 0f || map == null || spriteRenderer == null)
            {
                return;
            }

            UpdateAge();
            if (StrategySeasonalSurfaceController.IsWaterFrozenForGameplay)
            {
                UpdateFrozenWaterIdle();
                return;
            }

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

    }
}
