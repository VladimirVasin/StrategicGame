using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyResidentGender
    {
        Male,
        Female
    }

    public enum StrategyResidentLifeStage
    {
        Child,
        Adult
    }

    [DisallowMultipleComponent]
    public sealed class StrategyResidentAgent : MonoBehaviour
    {
        private const float MoveSpeed = 0.85f;
        private const float TargetReachDistance = 0.04f;
        private const int IdleRadius = 4;
        private const float GardenWorkCooldownMin = 7.0f;
        private const float GardenWorkCooldownMax = 11.0f;
        private const float GardenWorkRetryCooldownMin = 1.4f;
        private const float GardenWorkRetryCooldownMax = 3.0f;
        private const float LumberChopSecondsMin = 3.0f;
        private const float LumberChopSecondsMax = 5.0f;
        private const float LumberPlantSecondsMin = 2.2f;
        private const float LumberPlantSecondsMax = 3.6f;
        private const float LumberDepositSecondsMin = 0.8f;
        private const float LumberDepositSecondsMax = 1.3f;
        private const float LogisticsPickupSecondsMin = 0.45f;
        private const float LogisticsPickupSecondsMax = 0.85f;
        private const float LogisticsDepositSecondsMin = 0.65f;
        private const float LogisticsDepositSecondsMax = 1.05f;
        private const float ConstructionPickupSecondsMin = 0.42f;
        private const float ConstructionPickupSecondsMax = 0.82f;
        private const float ConstructionDepositSecondsMin = 0.55f;
        private const float ConstructionDepositSecondsMax = 0.95f;
        private const int ConstructionPickupPathFailureLimit = 5;
        private const float HuntingDepositSecondsMin = 0.65f;
        private const float HuntingDepositSecondsMax = 1.05f;
        private const float FishingDepositSecondsMin = 0.65f;
        private const float FishingDepositSecondsMax = 1.05f;
        private const float ForageGatherSecondsMin = 2.0f;
        private const float ForageGatherSecondsMax = 3.8f;
        private const float ForageDepositSecondsMin = 0.45f;
        private const float ForageDepositSecondsMax = 0.85f;
        private const float WalkAnimationFrameRate = 12f;
        private const float WoodcutAnimationFrameRate = 11.5f;
        private const float StonecutAnimationFrameRate = 10.5f;
        private const float ConstructionAnimationFrameRate = 12.5f;
        private const float BowAnimationFrameRate = 12.0f;
        private const float ButcherAnimationFrameRate = 10.5f;
        private const float FishingAnimationFrameRate = 10.5f;
        private const float CryingAnimationFrameRate = 6.5f;
        private const float SecondsPerYear = 100f;
        private const int AdultAgeYears = 16;
        private const int HomeboundChildAgeYears = 3;
        private const int ForagingChildMinimumAge = 7;
        private const int WoodcutImpactFrame = 5;
        private const int StonecutImpactFrame = 5;
        private const int ConstructionImpactFrame = 6;
        private const int BowReleaseFrame = 7;
        private const int ButcherImpactFrame = 5;
        private const int FishingHookFrame = 5;
        private const int FishingReelFrame = 10;
        private const float HuntingShotRange = 4.3f;
        private const float FuneralWaitingAutoReleaseSeconds = 90f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float ReadabilityOutlineScale = 1.16f;
        private static Sprite readabilityShadowSprite;
        private static Sprite fishingLineSprite;
        private static Sprite fishingBobberSprite;
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public enum ResidentActivity
        {
            Idle,
            TendingHousehold,
            StayingInsideHome,
            MovingHome,
            ArrivingAsRefugee,
            LeavingSettlement,
            MovingToGarden,
            WorkingGarden,
            MovingToForage,
            GatheringForage,
            MovingToLooseForagePickup,
            PickingUpLooseForage,
            CarryingForage,
            DepositingForage,
            MovingToTree,
            ChoppingTree,
            BuckingTree,
            MovingToLogs,
            CarryingLogs,
            DepositingLogs,
            MovingToStoragePickup,
            PickingUpStorageLogs,
            CarryingLogsToStorage,
            DepositingStorageLogs,
            MovingToPlantTree,
            PlantingTree,
            MovingToStone,
            MiningStone,
            CarryingStone,
            DepositingStone,
            MovingToStorageStonePickup,
            PickingUpStorageStone,
            CarryingStoneToStorage,
            DepositingStorageStone,
            MovingToConstructionStorage,
            PickingUpConstructionLogs,
            PickingUpConstructionStone,
            CarryingConstructionLogs,
            CarryingConstructionStone,
            DepositingConstructionResource,
            MovingToConstructionSite,
            BuildingConstruction,
            MovingToHuntingRange,
            AimingBow,
            WaitingForHuntHit,
            MovingToHuntCarcass,
            ButcheringRabbit,
            CarryingGame,
            DepositingGame,
            MovingToFishingSpot,
            CastingFishingLine,
            WaitingForFishBite,
            ReelingFish,
            CarryingFish,
            DepositingFish,
            MovingToGranaryGamePickup,
            PickingUpGranaryGame,
            CarryingGameToGranary,
            DepositingGranaryGame,
            MovingToGranaryFishPickup,
            PickingUpGranaryFish,
            CarryingFishToGranary,
            DepositingGranaryFish,
            ReturningLogsToStorage,
            ReturningStoneToStorage,
            ReturningGameToGranary,
            ReturningFishToGranary,
            MovingToFuneral,
            MourningCorpse,
            CarryingCorpseToCemetery,
            MovingToBurial,
            BuryingGrave,
            WaitingAtFuneral
        }

        private readonly List<int> childIds = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyPlacedBuilding home;
        private StrategyLumberjackCamp workplace;
        private StrategyStonecutterCamp stoneWorkplace;
        private StrategyHunterCamp hunterWorkplace;
        private StrategyFisherHut fisherWorkplace;
        private StrategyStorageYard storageWorkplace;
        private StrategyStorageYard builderWorkplace;
        private StrategyGranary granaryWorkplace;
        private StrategyConstructionSite constructionSite;
        private StrategyConstructionSite carriedConstructionReturnSite;
        private IStrategyConstructionResourceSource activeConstructionSource;
        private StrategyStorageYard returnStorageYard;
        private StrategyGranary returnGranary;
        private Vector2Int idleOrigin;
        private Vector2Int idleFootprint = Vector2Int.one;
        private StrategyResidentGender gender;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer carriedLogsRenderer;
        private SpriteRenderer carriedStoneRenderer;
        private SpriteRenderer carriedGameRenderer;
        private SpriteRenderer carriedFishRenderer;
        private SpriteRenderer carriedForageRenderer;
        private SpriteRenderer fishingLineRenderer;
        private SpriteRenderer fishingBobberRenderer;
        private StrategyResidentFootstepAudio footstepAudio;
        private readonly List<Vector3> path = new();
        private ResidentActivity activity;
        private StrategyBuildingUpgrade activeGarden;
        private StrategyForageNode activeForageNode;
        private StrategyLooseCarriedResourcePile activeLooseForageSource;
        private StrategyForestryTree activeTree;
        private StrategyLumberjackCamp activeLogSource;
        private StrategyLooseConstructionResourcePile activeLooseLogSource;
        private StrategyStoneDeposit activeStoneDeposit;
        private StrategyStonecutterCamp activeStoneSource;
        private StrategyLooseConstructionResourcePile activeLooseStoneSource;
        private StrategyHunterCamp activeGameSource;
        private StrategyFisherHut activeFishSource;
        private StrategyLooseCarriedResourcePile activeLooseFoodSource;
        private StrategyRabbitAgent activeHuntTarget;
        private StrategyFishAgent activeFishTarget;
        private StrategyConstructionResourceKind activeConstructionResource;
        private StrategyConstructionResourceKind carriedConstructionReturnResource = StrategyConstructionResourceKind.None;
        private StrategyResidentLifeStage lifeStage = StrategyResidentLifeStage.Adult;
        private Vector2Int plantingCell;
        private int residentId;
        private int fatherId;
        private int motherId;
        private int pathIndex;
        private float waitTimer;
        private float gardenWorkCooldown;
        private float gardenWorkTimer;
        private float lumberWorkCooldown;
        private float lumberWorkTimer;
        private float stoneWorkCooldown;
        private float logisticsWorkCooldown;
        private float huntingWorkCooldown;
        private float fishingWorkCooldown;
        private float huntingWorkTimer;
        private float fishingWorkTimer;
        private float fishingBiteTimer;
        private float forageWorkTimer;
        private float funeralTimer;
        private float walkFrameTimer;
        private float workFrameTimer;
        private float bobPhase;
        private int walkFrame;
        private int appliedWalkFrame = -1;
        private int workFrame;
        private int appliedWorkFrame = -1;
        private int lastMortalityAgeChecked;
        private int carriedLogAmount;
        private int carriedStoneAmount;
        private int carriedGameAmount;
        private int carriedFishAmount;
        private StrategyResourceType carriedForageResource = StrategyResourceType.None;
        private int carriedForageAmount;
        private int constructionPickupPathFailures;
        private float ageYears = AdultAgeYears;
        private bool hasTarget;
        private bool usingWalkSprite;
        private bool usingWorkSprite;
        private bool constructionFutureHome;
        private bool bowShotReleased;
        private bool fishingLineCast;
        private bool deathRequested;
        private bool hiddenInsideHome;
        private bool returnCarriedResourcesImmediately;

        public StrategyPlacedBuilding Home => home;
        public StrategyLumberjackCamp Workplace => workplace;
        public StrategyStonecutterCamp StoneWorkplace => stoneWorkplace;
        public StrategyHunterCamp HunterWorkplace => hunterWorkplace;
        public StrategyFisherHut FisherWorkplace => fisherWorkplace;
        public StrategyStorageYard StorageWorkplace => storageWorkplace;
        public StrategyStorageYard BuilderWorkplace => builderWorkplace;
        public StrategyGranary GranaryWorkplace => granaryWorkplace;
        public StrategyConstructionSite ConstructionSite => constructionSite;
        public bool ConstructionWillBecomeHome => constructionFutureHome;
        public bool IsHouseholder => home != null && home.Householder == this;
        public bool HasExternalWorkplace => workplace != null
            || stoneWorkplace != null
            || hunterWorkplace != null
            || fisherWorkplace != null
            || storageWorkplace != null
            || builderWorkplace != null
            || granaryWorkplace != null;
        public bool HasWorkplace => HasExternalWorkplace || IsHouseholder;
        public bool HasConstructionAssignment => constructionSite != null;
        public bool IsAdult => lifeStage == StrategyResidentLifeStage.Adult;
        public bool CanWork => IsAdult && !IsPendingRefugee;
        public bool IsFuneralDutyActive => IsFuneralActivity(activity);
        public bool IsHouseholdForaging => IsForagingActivity(activity);
        public bool CanAcceptWorkAssignment => CanWork && !IsFuneralDutyActive && !IsHouseholdForaging;
        public bool IsHomeboundYoungChild => lifeStage == StrategyResidentLifeStage.Child
            && ageYears < HomeboundChildAgeYears
            && home != null
            && !IsPendingRefugee;
        public bool IsPendingRefugee { get; private set; }
        public bool IsRefugeeTraveling => activity == ResidentActivity.ArrivingAsRefugee
            || activity == ResidentActivity.LeavingSettlement;
        public StrategyResidentGender Gender => gender;
        public StrategyResidentLifeStage LifeStage => lifeStage;
        public int ResidentId => residentId;
        public int FatherId => fatherId;
        public int MotherId => motherId;
        public IReadOnlyList<int> ChildIds => childIds;
        public float AgeYears => ageYears;
        public int DisplayAgeYears => Mathf.FloorToInt(ageYears);
        public int VisualVariant { get; private set; }
        public string FullName { get; private set; }
        public string FamilyName { get; private set; }
        public ResidentActivity Activity => activity;
        public Bounds SelectionBounds => spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, new Vector3(0.55f, 0.75f, 0f));

        public bool CanStartHouseholdForagingForHome(StrategyPlacedBuilding targetHome)
        {
            return targetHome != null
                && home == targetHome
                && targetHome.Tool == StrategyBuildTool.House
                && !deathRequested
                && !IsPendingRefugee
                && !IsHouseholder
                && !HasExternalWorkplace
                && constructionSite == null
                && !IsFuneralDutyActive
                && !hiddenInsideHome
                && !IsHomeboundYoungChild
                && (IsAdult || ageYears >= ForagingChildMinimumAge)
                && activity == ResidentActivity.Idle
                && !hasTarget
                && activeForageNode == null
                && activeLooseForageSource == null
                && carriedForageAmount <= 0;
        }

        public bool TryStartHouseholdForaging(StrategyForageNode node, Vector2Int workCell)
        {
            if (node == null || !node.IsReservedBy(this) || !CanStartHouseholdForagingForHome(home))
            {
                return false;
            }

            activeForageNode = node;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activeGarden = null;
            activity = ResidentActivity.MovingToForage;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.04f, 0.18f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", node.ResourceType),
                    StrategyDebugLogger.F("nodeCell", node.Cell));
                return true;
            }

            node.Release(this);
            activeForageNode = null;
            activity = ResidentActivity.Idle;
            waitTimer = Random.Range(0.35f, 0.85f);
            StrategyDebugLogger.Warn(
                "Forage",
                "ResidentForagePathRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", node.ResourceType),
                StrategyDebugLogger.F("nodeCell", node.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        public bool TryStartHouseholdLooseForagePickup(
            StrategyLooseCarriedResourcePile pile,
            Vector2Int pickupCell)
        {
            if (pile == null || !pile.IsReservedBy(this) || !CanStartHouseholdForagingForHome(home))
            {
                return false;
            }

            StrategyResourceType resource = pile.Resource;
            if (resource != StrategyResourceType.Berries
                && resource != StrategyResourceType.Roots
                && resource != StrategyResourceType.Mushrooms)
            {
                return false;
            }

            activeLooseForageSource = pile;
            activeForageNode = null;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activeGarden = null;
            activity = ResidentActivity.MovingToLooseForagePickup;
            if (TryBuildPathTo(pickupCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.04f, 0.18f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentLooseForagePickupStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("pileOrigin", pile.Origin),
                    StrategyDebugLogger.F("pickupCell", pickupCell));
                return true;
            }

            pile.ReleaseReservation(this);
            activeLooseForageSource = null;
            activity = ResidentActivity.Idle;
            waitTimer = Random.Range(0.35f, 0.85f);
            StrategyDebugLogger.Warn(
                "Forage",
                "ResidentLooseForagePathRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("pileOrigin", pile.Origin),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return false;
        }

        public void Configure(
            CityMapController mapController,
            StrategyPlacedBuilding homeBuilding,
            StrategyResidentGender residentGender,
            int visualVariant,
            string fullName,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            Vector2Int initialIdleOrigin,
            Vector2Int initialIdleFootprint,
            int residentIdentifier = 0,
            float initialAgeYears = AdultAgeYears,
            StrategyResidentLifeStage initialLifeStage = StrategyResidentLifeStage.Adult,
            int fatherIdentifier = 0,
            int motherIdentifier = 0,
            string residentFamilyName = null)
        {
            map = mapController;
            population = GetComponentInParent<StrategyPopulationController>();
            home = homeBuilding;
            idleOrigin = initialIdleOrigin;
            idleFootprint = new Vector2Int(
                Mathf.Max(1, initialIdleFootprint.x),
                Mathf.Max(1, initialIdleFootprint.y));
            gender = residentGender;
            VisualVariant = visualVariant;
            FullName = string.IsNullOrWhiteSpace(fullName)
                ? GetFallbackName(residentGender, visualVariant)
                : fullName;
            FamilyName = string.IsNullOrWhiteSpace(residentFamilyName)
                ? ExtractFamilyName(FullName)
                : residentFamilyName;
            residentId = residentIdentifier;
            fatherId = fatherIdentifier;
            motherId = motherIdentifier;
            ageYears = Mathf.Max(0f, initialAgeYears);
            lastMortalityAgeChecked = Mathf.FloorToInt(ageYears);
            lifeStage = initialLifeStage == StrategyResidentLifeStage.Child && ageYears < AdultAgeYears
                ? StrategyResidentLifeStage.Child
                : StrategyResidentLifeStage.Adult;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.08f);
            transform.localScale = Vector3.one;
            UseIdleSprite();
            UpdateWorldSorting();
            waitTimer = Random.Range(0.35f, 1.1f);
            gardenWorkCooldown = Random.Range(2.5f, 6.5f);
            lumberWorkCooldown = Random.Range(1.5f, 4.5f);
            stoneWorkCooldown = Random.Range(1.5f, 4.5f);
            logisticsWorkCooldown = Random.Range(1.0f, 3.0f);
            home?.TryRegisterResident(this);
            EnsureReadabilityRenderers();
            SyncReadabilityRenderers();
            EnsureClickCollider();
            EnsureFootstepAudio();
            if (IsHomeboundYoungChild)
            {
                EnterHomeboundChildState(false);
            }
        }

        public void PrepareForDeath()
        {
            if (deathRequested)
            {
                return;
            }

            deathRequested = true;
            DropCarriedResourcesOnDeath();
            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(false);
            activeGarden = null;
            home?.UnregisterResident(this);
            home = null;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            SetFishingLineVisible(false);

            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void DropCarriedResourcesOnDeath()
        {
            int droppedLogs = carriedLogAmount;
            int droppedStone = carriedStoneAmount;
            int droppedGame = carriedGameAmount;
            int droppedFish = carriedFishAmount;
            int droppedForage = carriedForageAmount;
            StrategyResourceType droppedForageResource = carriedForageResource;
            if (droppedLogs <= 0
                && droppedStone <= 0
                && droppedGame <= 0
                && droppedFish <= 0
                && droppedForage <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                if (droppedLogs > 0 || droppedStone > 0)
                {
                    StrategyLooseConstructionResourcePile.Create(
                        map,
                        cell,
                        transform.position,
                        droppedLogs,
                        droppedStone);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "CarriedConstructionResourcesDroppedOnDeath",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("origin", cell),
                        StrategyDebugLogger.F("logs", droppedLogs),
                        StrategyDebugLogger.F("stone", droppedStone),
                        StrategyDebugLogger.F("reservation", "cleared"));
                }

                DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Game, droppedGame);
                DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Fish, droppedFish);
                DropLooseCarriedResourceOnDeath(cell, droppedForageResource, droppedForage);
            }
            else
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourcesLostOnDeath",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("logs", droppedLogs),
                    StrategyDebugLogger.F("stone", droppedStone),
                    StrategyDebugLogger.F("game", droppedGame),
                    StrategyDebugLogger.F("fish", droppedFish),
                    StrategyDebugLogger.F("forageResource", droppedForageResource),
                    StrategyDebugLogger.F("forage", droppedForage),
                    StrategyDebugLogger.F("reason", "no_map_cell"));
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            ClearCarriedConstructionReturnReservation();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
        }

        private void DropLooseCarriedResourceOnDeath(Vector2Int cell, StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            StrategyLooseCarriedResourcePile.Create(
                map,
                cell,
                transform.position,
                resource,
                amount);
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourceDroppedOnDeath",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("origin", cell),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("reservation", "cleared"));
        }

        public bool TryStartFuneralMove(Vector3 targetWorld, ResidentActivity funeralMoveActivity)
        {
            if (map == null
                || deathRequested
                || IsPendingRefugee
                || IsHomeboundYoungChild
                || !IsFuneralMoveActivity(funeralMoveActivity))
            {
                return false;
            }

            returnCarriedResourcesImmediately = true;
            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(true);
            returnCarriedResourcesImmediately = false;
            activeGarden = null;
            funeralTimer = 0f;

            bool hasGridPath = map.TryWorldToCell(targetWorld, out Vector2Int targetCell)
                && TryBuildPathTo(targetCell);
            if (!hasGridPath)
            {
                path.Clear();
                pathIndex = 0;
                hasTarget = false;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "ResidentFuneralMoveFailed",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("activity", funeralMoveActivity),
                    StrategyDebugLogger.F("targetWorld", targetWorld),
                    StrategyDebugLogger.F("reason", "no_walkable_path"));
                return false;
            }

            activity = funeralMoveActivity;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            UseIdleSprite();

            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("activity", funeralMoveActivity),
                StrategyDebugLogger.F("targetWorld", targetWorld));
            return hasTarget;
        }

        public void StartFuneralMourning(float seconds)
        {
            StartTimedFuneralActivity(ResidentActivity.MourningCorpse, seconds);
        }

        public void StartFuneralBurial(float seconds)
        {
            StartTimedFuneralActivity(ResidentActivity.BuryingGrave, seconds);
        }

        public void EndFuneralDuty()
        {
            if (!IsFuneralActivity(activity))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            funeralTimer = 0f;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.9f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            UseIdleSprite();
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralDutyEnded",
                StrategyDebugLogger.F("resident", FullName));
        }

        private void StartTimedFuneralActivity(ResidentActivity funeralActivity, float seconds)
        {
            if (deathRequested || IsPendingRefugee || IsHomeboundYoungChild)
            {
                return;
            }

            activity = funeralActivity;
            funeralTimer = Mathf.Max(0.5f, seconds);
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        public void AddChildId(int childIdentifier)
        {
            if (childIdentifier > 0 && !childIds.Contains(childIdentifier))
            {
                childIds.Add(childIdentifier);
            }
        }

        public void SetPendingRefugee(bool pending)
        {
            IsPendingRefugee = pending;
            if (!pending && activity == ResidentActivity.ArrivingAsRefugee)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
            }
        }

        public void SetCampIdleOrigin(Vector2Int origin)
        {
            idleOrigin = origin;
            idleFootprint = Vector2Int.one;
            if (home == null && !IsRefugeeTraveling)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.85f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }
        }

        public bool FollowRefugeePath(IReadOnlyList<Vector3> worldPath, bool leaving)
        {
            path.Clear();
            if (worldPath != null)
            {
                for (int i = 0; i < worldPath.Count; i++)
                {
                    Vector3 point = worldPath[i];
                    path.Add(new Vector3(point.x, point.y, -0.08f));
                }
            }

            pathIndex = 0;
            hasTarget = path.Count > 0;
            activity = leaving ? ResidentActivity.LeavingSettlement : ResidentActivity.ArrivingAsRefugee;
            waitTimer = 0f;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            if (!hasTarget)
            {
                activity = ResidentActivity.Idle;
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
            }

            return hasTarget;
        }

        public void AssignHome(StrategyPlacedBuilding newHome)
        {
            if (newHome == null || home == newHome)
            {
                return;
            }

            if (!newHome.CanAcceptResident(this))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ResidentHomeAssignRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", newHome.Origin),
                    StrategyDebugLogger.F("reason", "resident_capacity"));
                return;
            }

            home?.UnregisterResident(this);
            home = newHome;
            home.TryRegisterResident(this);

            idleOrigin = home.Origin;
            idleFootprint = home.Footprint;
            activeGarden = null;
            gardenWorkTimer = 0f;
            gardenWorkCooldown = Random.Range(2.5f, 6.5f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHomeAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("homeTool", home.Tool));

            if (IsHomeboundYoungChild)
            {
                EnterHomeboundChildState(true);
            }
        }

        public void AssignHome(StrategyPlacedBuilding newHome, Vector3 targetWorld)
        {
            AssignHome(newHome);
            if (home == newHome && CanStartHomeMoveNow())
            {
                StartMovingHome(targetWorld);
            }
        }

        public void PrepareHouseholderHomeDuty()
        {
            StrategyLumberjackCamp previousLumber = workplace;
            StrategyStonecutterCamp previousStone = stoneWorkplace;
            StrategyHunterCamp previousHunter = hunterWorkplace;
            StrategyFisherHut previousFisher = fisherWorkplace;
            StrategyStorageYard previousStorage = storageWorkplace;
            StrategyStorageYard previousBuilder = builderWorkplace;
            StrategyGranary previousGranary = granaryWorkplace;
            bool hadExternalWork = HasExternalWorkplace || constructionSite != null;

            previousLumber?.UnassignWorker(this);
            previousStone?.UnassignWorker(this);
            previousHunter?.UnassignWorker(this);
            previousFisher?.UnassignWorker(this);
            previousStorage?.UnassignWorker(this);
            previousBuilder?.UnassignBuilder(this);
            previousGranary?.UnassignWorker(this);
            ClearConstructionSite(null);
            CancelForageWork(true);

            if (home != null)
            {
                idleOrigin = home.Origin;
                idleFootprint = home.Footprint;
            }

            if (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
            {
                activity = ResidentActivity.TendingHousehold;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Mathf.Min(waitTimer, Random.Range(0.10f, 0.35f));
                UseIdleSprite();
            }

            gardenWorkCooldown = Mathf.Min(gardenWorkCooldown, Random.Range(0.35f, 1.25f));
            if (hadExternalWork)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholderExternalWorkCleared",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            }
        }

        private bool CanStartHomeMoveNow()
        {
            return constructionSite == null
                && activity == ResidentActivity.Idle
                && !hasTarget;
        }

        public void AssignConstructionSite(StrategyConstructionSite site, bool willLiveThere)
        {
            if (site == null
                || constructionSite == site
                || builderWorkplace == null
                || IsReturningCarriedResourceActivity(activity)
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(true);
            constructionSite = site;
            constructionFutureHome = willLiveThere;
            ClearCarriedConstructionReturnReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.08f, 0.32f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentConstructionAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteTool", site.Tool),
                StrategyDebugLogger.F("siteOrigin", site.Origin),
                StrategyDebugLogger.F("futureHome", willLiveThere));
        }

        public void NotifyConstructionCompleted(StrategyConstructionSite site)
        {
            if (site != null && constructionSite != site)
            {
                return;
            }

            constructionSite = null;
            ReleaseActiveConstructionPickupReservation();
            ClearCarriedConstructionReturnReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            constructionFutureHome = false;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.12f, 0.38f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentConstructionCompleted",
                StrategyDebugLogger.F("resident", FullName));
        }

        public void ClearConstructionSite(StrategyConstructionSite site)
        {
            if (site != null && constructionSite != site)
            {
                return;
            }

            bool hadCarriedResources = carriedLogAmount > 0
                || carriedStoneAmount > 0
                || carriedGameAmount > 0
                || carriedFishAmount > 0;
            CaptureCarriedConstructionReturnReservation();

            if (constructionSite != null)
            {
                constructionSite.UnregisterBuilder(this);
            }

            ReleaseActiveConstructionPickupReservation();
            if (IsConstructionActivity(activity))
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.70f);
            }

            constructionSite = null;
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            constructionFutureHome = false;
            if (hadCarriedResources && TryStartCarriedResourceReturn("construction_assignment_cleared"))
            {
                return;
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        public void ExtractCarriedConstructionResources(
            StrategyConstructionSite site,
            out int logs,
            out int stone)
        {
            logs = 0;
            stone = 0;
            if (site != null && constructionSite != site)
            {
                return;
            }

            logs = carriedLogAmount;
            stone = carriedStoneAmount;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            ClearCarriedConstructionReturnReservation();
        }

        public void ClearHome(StrategyPlacedBuilding removedHome)
        {
            if (removedHome != null && home != removedHome)
            {
                return;
            }

            if (hiddenInsideHome)
            {
                hiddenInsideHome = false;
                SetWorldPresenceVisible(true);
            }

            CancelForageWork(false);
            home = null;
            activeGarden = null;
            gardenWorkTimer = 0f;
            gardenWorkCooldown = Random.Range(2.0f, 5.0f);
            if (activity == ResidentActivity.TendingHousehold
                || activity == ResidentActivity.MovingToGarden
                || activity == ResidentActivity.WorkingGarden
                || activity == ResidentActivity.StayingInsideHome
                || activity == ResidentActivity.MovingHome)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.35f, 0.95f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                idleOrigin = currentCell;
            }

            idleFootprint = Vector2Int.one;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHomeCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("removedHomeOrigin", removedHome != null ? removedHome.Origin : Vector2Int.zero));
        }

        public void AssignWorkplace(StrategyLumberjackCamp camp)
        {
            if (camp == null
                || workplace == camp
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            workplace = camp;
            lumberWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void AssignStoneWorkplace(StrategyStonecutterCamp camp)
        {
            if (camp == null
                || stoneWorkplace == camp
                || workplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelStoneWork();
            stoneWorkplace = camp;
            stoneWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStoneWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void AssignHunterWorkplace(StrategyHunterCamp camp)
        {
            if (camp == null
                || hunterWorkplace == camp
                || workplace != null
                || stoneWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            hunterWorkplace = camp;
            huntingWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHunterWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void ClearWorkplace(StrategyLumberjackCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && workplace != camp)
            {
                return;
            }

            StrategyLumberjackCamp previousWorkplace = workplace;
            workplace = null;
            CancelLumberWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearStoneWorkplace(StrategyStonecutterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && stoneWorkplace != camp)
            {
                return;
            }

            StrategyStonecutterCamp previousWorkplace = stoneWorkplace;
            stoneWorkplace = null;
            CancelStoneWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStoneWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearHunterWorkplace(StrategyHunterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && hunterWorkplace != camp)
            {
                return;
            }

            StrategyHunterCamp previousWorkplace = hunterWorkplace;
            CancelHunterWork(true);
            hunterWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHunterWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignFisherWorkplace(StrategyFisherHut hut)
        {
            if (hut == null
                || fisherWorkplace == hut
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            fisherWorkplace = hut;
            fishingWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentFisherWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("hutOrigin", hut.Origin));
        }

        public void ClearFisherWorkplace(StrategyFisherHut hut)
        {
            if (this == null)
            {
                return;
            }

            if (hut != null && fisherWorkplace != hut)
            {
                return;
            }

            StrategyFisherHut previousWorkplace = fisherWorkplace;
            CancelFisherWork(true);
            fisherWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentFisherWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("hutOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignStorageWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || storageWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelGranaryWork(true);
            storageWorkplace = yard;
            logisticsWorkCooldown = Random.Range(0.35f, 1.45f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStorageWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yard.Origin));
        }

        public void ClearStorageWorkplace(StrategyStorageYard yard)
        {
            if (this == null)
            {
                return;
            }

            if (yard != null && storageWorkplace != yard)
            {
                return;
            }

            StrategyStorageYard previousWorkplace = storageWorkplace;
            CancelStorageWork(true);
            storageWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStorageWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignGranaryWorkplace(StrategyGranary granary)
        {
            if (granary == null
                || granaryWorkplace == granary
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            granaryWorkplace = granary;
            logisticsWorkCooldown = Random.Range(0.35f, 1.45f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGranaryWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("granaryOrigin", granary.Origin));
        }

        public void ClearGranaryWorkplace(StrategyGranary granary)
        {
            if (this == null)
            {
                return;
            }

            if (granary != null && granaryWorkplace != granary)
            {
                return;
            }

            StrategyGranary previousWorkplace = granaryWorkplace;
            CancelGranaryWork(true);
            granaryWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGranaryWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("granaryOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignBuilderWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || builderWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            builderWorkplace = yard;
            idleOrigin = yard.Origin;
            idleFootprint = new Vector2Int(3, 2);
            waitTimer = Random.Range(0.20f, 0.90f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentBuilderWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yard.Origin));
        }

        public void ClearBuilderWorkplace(StrategyStorageYard yard)
        {
            if (this == null)
            {
                return;
            }

            if (yard != null && builderWorkplace != yard)
            {
                return;
            }

            StrategyStorageYard previousWorkplace = builderWorkplace;
            ClearConstructionSite(null);
            builderWorkplace = null;
            if (home != null)
            {
                idleOrigin = home.Origin;
                idleFootprint = home.Footprint;
            }

            StrategyDebugLogger.Info(
                "Population",
                "ResidentBuilderWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        private void Update()
        {
            if (map == null || deathRequested)
            {
                return;
            }

            if (UpdateAge())
            {
                return;
            }

            if (IsHomeboundYoungChild)
            {
                UpdateHomeboundChild();
                return;
            }

            if (hiddenInsideHome)
            {
                ReleaseHomeboundChild();
            }

            if (gardenWorkCooldown > 0f)
            {
                gardenWorkCooldown -= Time.deltaTime;
            }

            if (lumberWorkCooldown > 0f)
            {
                lumberWorkCooldown -= Time.deltaTime;
            }

            if (stoneWorkCooldown > 0f)
            {
                stoneWorkCooldown -= Time.deltaTime;
            }

            if (logisticsWorkCooldown > 0f)
            {
                logisticsWorkCooldown -= Time.deltaTime;
            }

            if (huntingWorkCooldown > 0f)
            {
                huntingWorkCooldown -= Time.deltaTime;
            }

            if (fishingWorkCooldown > 0f)
            {
                fishingWorkCooldown -= Time.deltaTime;
            }

            if (IsStationaryFuneralActivity(activity))
            {
                UpdateFuneralActivity();
                return;
            }

            if (activity == ResidentActivity.WorkingGarden)
            {
                UpdateGardenWork();
                return;
            }

            if (activity == ResidentActivity.GatheringForage)
            {
                UpdateGatheringForage();
                return;
            }

            if (activity == ResidentActivity.PickingUpLooseForage)
            {
                UpdatePickingUpLooseForage();
                return;
            }

            if (activity == ResidentActivity.DepositingForage)
            {
                UpdateDepositingForage();
                return;
            }

            if (activity == ResidentActivity.ChoppingTree)
            {
                UpdateChoppingTree();
                return;
            }

            if (activity == ResidentActivity.BuckingTree)
            {
                UpdateBuckingTree();
                return;
            }

            if (activity == ResidentActivity.DepositingLogs)
            {
                UpdateDepositingLogs();
                return;
            }

            if (activity == ResidentActivity.MiningStone)
            {
                UpdateMiningStone();
                return;
            }

            if (activity == ResidentActivity.DepositingStone)
            {
                UpdateDepositingStone();
                return;
            }

            if (activity == ResidentActivity.PickingUpStorageLogs)
            {
                UpdatePickingUpStorageLogs();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageLogs)
            {
                UpdateDepositingStorageLogs();
                return;
            }

            if (activity == ResidentActivity.PickingUpStorageStone)
            {
                UpdatePickingUpStorageStone();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageStone)
            {
                UpdateDepositingStorageStone();
                return;
            }

            if (activity == ResidentActivity.PickingUpGranaryGame)
            {
                UpdatePickingUpGranaryGame();
                return;
            }

            if (activity == ResidentActivity.DepositingGranaryGame)
            {
                UpdateDepositingGranaryGame();
                return;
            }

            if (activity == ResidentActivity.PickingUpGranaryFish)
            {
                UpdatePickingUpGranaryFish();
                return;
            }

            if (activity == ResidentActivity.DepositingGranaryFish)
            {
                UpdateDepositingGranaryFish();
                return;
            }

            if (activity == ResidentActivity.PickingUpConstructionLogs
                || activity == ResidentActivity.PickingUpConstructionStone)
            {
                UpdatePickingUpConstructionResource();
                return;
            }

            if (activity == ResidentActivity.DepositingConstructionResource)
            {
                UpdateDepositingConstructionResource();
                return;
            }

            if (activity == ResidentActivity.BuildingConstruction)
            {
                UpdateBuildingConstruction();
                return;
            }

            if (activity == ResidentActivity.AimingBow)
            {
                UpdateAimingBow();
                return;
            }

            if (activity == ResidentActivity.WaitingForHuntHit)
            {
                UpdateWaitingForHuntHit();
                return;
            }

            if (activity == ResidentActivity.ButcheringRabbit)
            {
                UpdateButcheringRabbit();
                return;
            }

            if (activity == ResidentActivity.DepositingGame)
            {
                UpdateDepositingGame();
                return;
            }

            if (activity == ResidentActivity.CastingFishingLine)
            {
                UpdateCastingFishingLine();
                return;
            }

            if (activity == ResidentActivity.WaitingForFishBite)
            {
                UpdateWaitingForFishBite();
                return;
            }

            if (activity == ResidentActivity.ReelingFish)
            {
                UpdateReelingFish();
                return;
            }

            if (activity == ResidentActivity.DepositingFish)
            {
                UpdateDepositingFish();
                return;
            }

            if (activity == ResidentActivity.PlantingTree)
            {
                UpdatePlantingTree();
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                AnimateIdle();
                return;
            }

            if (!hasTarget || pathIndex >= path.Count)
            {
                if (IsReturningCarriedResourceActivity(activity))
                {
                    if (!TryStartCarriedResourceReturn("resource_return_retry", true))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }

                if (TryStartGardenTask())
                {
                    return;
                }

                if (TryStartLumberTask())
                {
                    return;
                }

                if (TryStartStoneTask())
                {
                    return;
                }

                if (TryStartStorageTask())
                {
                    return;
                }

                if (TryStartGranaryTask())
                {
                    return;
                }

                if (TryStartConstructionTask())
                {
                    return;
                }

                if (TryStartHunterTask())
                {
                    return;
                }

                if (TryStartFisherTask())
                {
                    return;
                }

                PickNextIdleTarget();
                return;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    hasTarget = false;
                    if (activity == ResidentActivity.MovingToGarden)
                    {
                        StartGardenWork();
                    }
                    else if (activity == ResidentActivity.MovingToForage)
                    {
                        StartGatheringForage();
                    }
                    else if (activity == ResidentActivity.MovingToLooseForagePickup)
                    {
                        StartPickingUpLooseForage();
                    }
                    else if (activity == ResidentActivity.CarryingForage)
                    {
                        StartDepositingForage();
                    }
                    else if (activity == ResidentActivity.MovingToTree)
                    {
                        StartChoppingTree();
                    }
                    else if (activity == ResidentActivity.MovingToLogs)
                    {
                        StartCollectingLogs();
                    }
                    else if (activity == ResidentActivity.CarryingLogs)
                    {
                        StartDepositingLogs();
                    }
                    else if (activity == ResidentActivity.MovingToStone)
                    {
                        StartMiningStone();
                    }
                    else if (activity == ResidentActivity.CarryingStone)
                    {
                        StartDepositingStone();
                    }
                    else if (activity == ResidentActivity.MovingToStoragePickup)
                    {
                        StartPickingUpStorageLogs();
                    }
                    else if (activity == ResidentActivity.CarryingLogsToStorage)
                    {
                        StartDepositingStorageLogs();
                    }
                    else if (activity == ResidentActivity.MovingToStorageStonePickup)
                    {
                        StartPickingUpStorageStone();
                    }
                    else if (activity == ResidentActivity.CarryingStoneToStorage)
                    {
                        StartDepositingStorageStone();
                    }
                    else if (activity == ResidentActivity.MovingToConstructionStorage)
                    {
                        StartPickingUpConstructionResource();
                    }
                    else if (activity == ResidentActivity.CarryingConstructionLogs
                        || activity == ResidentActivity.CarryingConstructionStone)
                    {
                        StartDepositingConstructionResource();
                    }
                    else if (activity == ResidentActivity.MovingToConstructionSite)
                    {
                        StartBuildingConstruction();
                    }
                    else if (activity == ResidentActivity.MovingToHuntingRange)
                    {
                        StartAimingBow();
                    }
                    else if (activity == ResidentActivity.MovingToHuntCarcass)
                    {
                        StartButcheringRabbit();
                    }
                    else if (activity == ResidentActivity.CarryingGame)
                    {
                        StartDepositingGame();
                    }
                    else if (activity == ResidentActivity.MovingToFishingSpot)
                    {
                        StartCastingFishingLine();
                    }
                    else if (activity == ResidentActivity.CarryingFish)
                    {
                        StartDepositingFish();
                    }
                    else if (activity == ResidentActivity.MovingToGranaryGamePickup)
                    {
                        StartPickingUpGranaryGame();
                    }
                    else if (activity == ResidentActivity.CarryingGameToGranary)
                    {
                        StartDepositingGranaryGame();
                    }
                    else if (activity == ResidentActivity.MovingToGranaryFishPickup)
                    {
                        StartPickingUpGranaryFish();
                    }
                    else if (activity == ResidentActivity.CarryingFishToGranary)
                    {
                        StartDepositingGranaryFish();
                    }
                    else if (IsReturningCarriedResourceActivity(activity))
                    {
                        CompleteCarriedResourceReturn();
                    }
                    else if (activity == ResidentActivity.MovingToPlantTree)
                    {
                        StartPlantingTree();
                    }
                    else if (IsFuneralMoveActivity(activity))
                    {
                        activity = ResidentActivity.WaitingAtFuneral;
                        funeralTimer = FuneralWaitingAutoReleaseSeconds;
                        waitTimer = 0f;
                        UseIdleSprite();
                    }
                    else
                    {
                        activity = GetRestingActivity();
                        waitTimer = Random.Range(0.35f, 1.1f);
                        UseIdleSprite();
                    }
                }

                return;
            }

            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, MoveSpeed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
                SyncReadabilityRenderers();
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                AnimateWalk();
            }
            else
            {
                AnimateIdle();
            }
        }

        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private ResidentActivity GetRestingActivity()
        {
            return IsHouseholder && !HasExternalWorkplace && constructionSite == null
                ? ResidentActivity.TendingHousehold
                : ResidentActivity.Idle;
        }

        private bool CanStartGardenDuty()
        {
            return activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold;
        }

        private bool TryStartGardenTask()
        {
            if (!CanStartGardenDuty()
                || home == null
                || !IsHouseholder
                || constructionSite != null
                || !CanWork
                || gardenWorkCooldown > 0f
                || !home.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden))
            {
                return false;
            }

            if (HasExternalWorkplace)
            {
                PrepareHouseholderHomeDuty();
                if (HasExternalWorkplace)
                {
                    gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
                    StrategyDebugLogger.Warn(
                        "Population",
                        "HouseholderGardenBlocked",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("homeOrigin", home.Origin),
                        StrategyDebugLogger.F("reason", "external_workplace"));
                    return false;
                }
            }

            if (!TryFindGardenWorkCell(garden, out Vector2Int workCell))
            {
                gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
                StrategyDebugLogger.Warn(
                    "Population",
                    "HouseholderGardenBlocked",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                    StrategyDebugLogger.F("reason", "no_work_cell"));
                return false;
            }

            activeGarden = garden;
            activity = ResidentActivity.MovingToGarden;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.25f);
                gardenWorkCooldown = Random.Range(GardenWorkCooldownMin, GardenWorkCooldownMax);
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholderGardenWorkStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeGarden = null;
            activity = GetRestingActivity();
            gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
            StrategyDebugLogger.Warn(
                "Population",
                "HouseholderGardenBlocked",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                StrategyDebugLogger.F("workCell", workCell),
                StrategyDebugLogger.F("reason", "no_path"));
            return false;
        }

        private bool TryStartLumberTask()
        {
            if (activity != ResidentActivity.Idle
                || workplace == null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || lumberWorkCooldown > 0f)
            {
                return false;
            }

            if (workplace.TryReserveProcessableWood(this, out StrategyForestryTree wood)
                && TryMoveToProcessableWood(wood))
            {
                return true;
            }

            wood?.Release(this);

            if (workplace.TryReserveMatureTree(this, out StrategyForestryTree tree)
                && TryMoveToTree(tree))
            {
                return true;
            }

            tree?.Release(this);

            if (workplace.TryFindPlantingCell(out Vector2Int cell)
                && TryMoveToPlantingCell(cell))
            {
                return true;
            }

            lumberWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryStartStoneTask()
        {
            if (activity != ResidentActivity.Idle
                || stoneWorkplace == null
                || workplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || stoneWorkCooldown > 0f)
            {
                return false;
            }

            if (stoneWorkplace.TryReserveStoneDeposit(this, out StrategyStoneDeposit deposit)
                && TryMoveToStoneDeposit(deposit))
            {
                return true;
            }

            deposit?.Release(this);
            stoneWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryStartStorageTask()
        {
            if (activity != ResidentActivity.Idle
                || storageWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || logisticsWorkCooldown > 0f)
            {
                return false;
            }

            bool stoneFirst = storageWorkplace.ShouldPrioritizeStonePickup();
            if (stoneFirst && TryStartStorageStonePickup())
            {
                return true;
            }

            if (TryStartStorageLogPickup())
            {
                return true;
            }

            if (!stoneFirst && TryStartStorageStonePickup())
            {
                return true;
            }

            logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
            return false;
        }

        private bool TryStartStorageLogPickup()
        {
            if (storageWorkplace.TryReserveLogSource(this, out StrategyLumberjackCamp source))
            {
                if (!source.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    source.ReleaseStoredLogsReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", source.Origin),
                        StrategyDebugLogger.F("resource", "logs"),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeLogSource = source;
                activity = ResidentActivity.MovingToStoragePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", "logs"),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            if (StrategyLooseConstructionResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    StrategyConstructionResourceKind.Logs,
                    out StrategyLooseConstructionResourcePile looseLogSource))
            {
                if (!looseLogSource.TryFindPickupCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    looseLogSource.ReleaseStorageReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseLogSource.Origin),
                        StrategyDebugLogger.F("resource", "logs"),
                        StrategyDebugLogger.F("reason", "no_loose_pickup_path"));
                    return false;
                }

                activeLooseLogSource = looseLogSource;
                activity = ResidentActivity.MovingToStoragePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseLogSource.Origin),
                    StrategyDebugLogger.F("source", "loose_construction_resources"),
                    StrategyDebugLogger.F("resource", "logs"),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            return false;
        }

        private bool TryStartStorageStonePickup()
        {
            if (storageWorkplace.TryReserveStoneSource(this, out StrategyStonecutterCamp stoneSource))
            {
                if (!stoneSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    stoneSource.ReleaseStoredStoneReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", stoneSource.Origin),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeStoneSource = stoneSource;
                activity = ResidentActivity.MovingToStorageStonePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", stoneSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            if (StrategyLooseConstructionResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    StrategyConstructionResourceKind.Stone,
                    out StrategyLooseConstructionResourcePile looseStoneSource))
            {
                if (!looseStoneSource.TryFindPickupCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    looseStoneSource.ReleaseStorageReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseStoneSource.Origin),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                        StrategyDebugLogger.F("reason", "no_loose_pickup_path"));
                    return false;
                }

                activeLooseStoneSource = looseStoneSource;
                activity = ResidentActivity.MovingToStorageStonePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseStoneSource.Origin),
                    StrategyDebugLogger.F("source", "loose_construction_resources"),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            return false;
        }

        private bool TryStartGranaryTask()
        {
            if (activity != ResidentActivity.Idle
                || granaryWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || !CanWork
                || logisticsWorkCooldown > 0f)
            {
                return false;
            }

            if (StrategyLooseCarriedResourcePile.TryReserveNearestForGranary(
                    granaryWorkplace,
                    this,
                    out StrategyLooseCarriedResourcePile looseFoodSource,
                    out StrategyResourceType looseResource,
                    out Vector2Int loosePickupCell))
            {
                if (!TryBuildPathTo(loosePickupCell))
                {
                    looseFoodSource.ReleaseReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseFoodSource.Origin),
                        StrategyDebugLogger.F("resource", looseResource),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeLooseFoodSource = looseFoodSource;
                activity = looseResource == StrategyResourceType.Game
                    ? ResidentActivity.MovingToGranaryGamePickup
                    : ResidentActivity.MovingToGranaryFishPickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "LooseFoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseFoodSource.Origin),
                    StrategyDebugLogger.F("resource", looseResource),
                    StrategyDebugLogger.F("pickupCell", loosePickupCell),
                    StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
                return true;
            }

            if (!granaryWorkplace.TryReserveFoodSource(
                    this,
                    out StrategyResourceType resource,
                    out StrategyHunterCamp gameSource,
                    out StrategyFisherHut fishSource))
            {
                logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
                return false;
            }

            if (resource == StrategyResourceType.Game)
            {
                if (gameSource == null
                    || !gameSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    gameSource?.ReleaseStoredGameReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", gameSource != null ? gameSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeGameSource = gameSource;
                activity = ResidentActivity.MovingToGranaryGamePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", gameSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
                return true;
            }

            if (resource == StrategyResourceType.Fish)
            {
                if (fishSource == null
                    || !fishSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    fishSource?.ReleaseStoredFishReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", fishSource != null ? fishSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeFishSource = fishSource;
                activity = ResidentActivity.MovingToGranaryFishPickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", fishSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
                return true;
            }

            logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
            return false;
        }

        private bool TryStartConstructionTask()
        {
            if (activity != ResidentActivity.Idle || constructionSite == null || builderWorkplace == null || !CanWork)
            {
                return false;
            }

            if (constructionSite.IsCompleted)
            {
                NotifyConstructionCompleted(constructionSite);
                return false;
            }

            if (!constructionSite.ResourcesComplete)
            {
                if (!constructionSite.TryFindResourcePickup(
                        this,
                        out IStrategyConstructionResourceSource source,
                        out StrategyConstructionResourceKind kind,
                        out Vector2Int pickupCell))
                {
                    waitTimer = Random.Range(0.45f, 1.1f);
                    return false;
                }

                if (!TryBuildPathTo(pickupCell))
                {
                    Vector2Int startCell = Vector2Int.zero;
                    bool hasStartCell = map != null && map.TryWorldToCell(transform.position, out startCell);
                    bool startWalkable = hasStartCell && map.IsCellWalkable(startCell);
                    bool pickupWalkable = map != null && map.IsCellWalkable(pickupCell);
                    constructionPickupPathFailures++;
                    waitTimer = Random.Range(0.45f, 1.1f);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "BuilderPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                        StrategyDebugLogger.F("resource", kind),
                        StrategyDebugLogger.F("startCell", hasStartCell ? startCell : Vector2Int.zero),
                        StrategyDebugLogger.F("startWalkable", startWalkable),
                        StrategyDebugLogger.F("pickupCell", pickupCell),
                        StrategyDebugLogger.F("pickupWalkable", pickupWalkable),
                        StrategyDebugLogger.F("failureCount", constructionPickupPathFailures),
                        StrategyDebugLogger.F("reason", "no_path"));
                    if (constructionPickupPathFailures >= ConstructionPickupPathFailureLimit)
                    {
                        StrategyConstructionSite failedSite = constructionSite;
                        Vector2Int failedOrigin = failedSite != null ? failedSite.Origin : Vector2Int.zero;
                        ClearConstructionSite(failedSite);
                        waitTimer = Random.Range(2.0f, 4.2f);
                        StrategyDebugLogger.Warn(
                            "Construction",
                            "BuilderConstructionAssignmentDropped",
                            StrategyDebugLogger.F("resident", FullName),
                            StrategyDebugLogger.F("siteOrigin", failedOrigin),
                            StrategyDebugLogger.F("reason", "pickup_path_repeatedly_failed"));
                    }

                    return false;
                }

                if (source == null || !source.TryReserveConstructionPickup(constructionSite, this, kind, 1))
                {
                    hasTarget = false;
                    path.Clear();
                    pathIndex = 0;
                    waitTimer = Random.Range(0.45f, 1.1f);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "BuilderPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                        StrategyDebugLogger.F("sourceOrigin", source != null ? source.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", kind),
                        StrategyDebugLogger.F("pickupCell", pickupCell),
                        StrategyDebugLogger.F("reason", "reserve_failed"));
                    return false;
                }

                constructionPickupPathFailures = 0;
                activeConstructionSource = source;
                activeConstructionResource = kind;
                activity = ResidentActivity.MovingToConstructionStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.14f);
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuilderPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("sourceOrigin", source != null ? source.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", kind),
                    StrategyDebugLogger.F("pickupCell", pickupCell));
                return true;
            }

            if (!constructionSite.TryFindBuildWorkCell(out Vector2Int workCell))
            {
                waitTimer = Random.Range(0.45f, 1.1f);
                return false;
            }

            if (!TryBuildPathTo(workCell))
            {
                waitTimer = Random.Range(0.45f, 1.1f);
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderWorkMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("workCell", workCell),
                    StrategyDebugLogger.F("reason", "no_path"));
                return false;
            }

            activity = ResidentActivity.MovingToConstructionSite;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.14f);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderWorkMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("workCell", workCell));
            return true;
        }

        private bool TryStartHunterTask()
        {
            if (activity != ResidentActivity.Idle
                || hunterWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || huntingWorkCooldown > 0f)
            {
                return false;
            }

            if (hunterWorkplace.TryReserveRabbitTarget(this, out StrategyRabbitAgent rabbit)
                && TryMoveToHuntingTarget(rabbit))
            {
                return true;
            }

            rabbit?.ReleaseHuntReservation(this);
            activeHuntTarget = null;
            huntingWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryMoveToHuntingTarget(StrategyRabbitAgent rabbit)
        {
            if (rabbit == null || !rabbit.IsAlive || rabbit.IsCarcass)
            {
                return false;
            }

            activeHuntTarget = rabbit;
            float directDistance = Vector2.Distance(transform.position, rabbit.transform.position);
            if (directDistance <= HuntingShotRange && map != null && map.TryWorldToCell(transform.position, out Vector2Int currentCell) && map.IsCellWalkable(currentCell))
            {
                StartAimingBow();
                return true;
            }

            if (!rabbit.TryGetCurrentCell(out Vector2Int targetCell))
            {
                activeHuntTarget = null;
                return false;
            }

            for (int radius = 1; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = targetCell + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                        float shotDistance = Vector2.Distance(candidateWorld, rabbit.transform.position);
                        if (shotDistance <= HuntingShotRange)
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                while (candidates.Count > 0)
                {
                    int index = Random.Range(0, candidates.Count);
                    Vector2Int candidate = candidates[index];
                    candidates.RemoveAt(index);
                    if (!TryBuildPathTo(candidate))
                    {
                        continue;
                    }

                    activity = ResidentActivity.MovingToHuntingRange;
                    hasTarget = true;
                    waitTimer = Random.Range(0.04f, 0.18f);
                    StrategyDebugLogger.Info(
                        "Hunting",
                        "HuntMoveStarted",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("rabbitCell", targetCell),
                        StrategyDebugLogger.F("workCell", candidate),
                        StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
                    return true;
                }
            }

            activeHuntTarget = null;
            activity = ResidentActivity.Idle;
            huntingWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Hunting",
                "HuntMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitCell", targetCell),
                StrategyDebugLogger.F("reason", "no_shot_path"));
            return false;
        }

        private bool TryStartFisherTask()
        {
            if (activity != ResidentActivity.Idle
                || fisherWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || fishingWorkCooldown > 0f)
            {
                return false;
            }

            if (fisherWorkplace.TryReserveFishTarget(this, out StrategyFishAgent fish)
                && TryMoveToFishingTarget(fish))
            {
                return true;
            }

            fish?.ReleaseFishingReservation(this);
            activeFishTarget = null;
            fishingWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryMoveToFishingTarget(StrategyFishAgent fish)
        {
            if (fish == null || fish.IsCaught || fisherWorkplace == null)
            {
                return false;
            }

            activeFishTarget = fish;
            if (!fisherWorkplace.TryFindFishingCell(fish, out Vector2Int fishingCell)
                || !TryBuildPathTo(fishingCell))
            {
                activeFishTarget = null;
                activity = ResidentActivity.Idle;
                fishingWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Fishing",
                    "FishingMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("fishWorld", fish != null ? fish.transform.position : Vector3.zero),
                    StrategyDebugLogger.F("reason", "no_shore_path"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
                return false;
            }

            activity = ResidentActivity.MovingToFishingSpot;
            hasTarget = true;
            waitTimer = Random.Range(0.04f, 0.18f);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishingMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishingCell", fishingCell),
                StrategyDebugLogger.F("fishWorld", fish.transform.position),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
            return true;
        }

        private bool TryMoveToTree(StrategyForestryTree tree)
        {
            if (tree == null || !TryFindTreeWorkCell(tree, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LumberMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "tree"),
                    StrategyDebugLogger.F("reason", tree == null ? "tree_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("treeCell", tree != null ? tree.Cell : Vector2Int.zero));
                return false;
            }

            activeTree = tree;
            activity = ResidentActivity.MovingToTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "LumberMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "tree"),
                    StrategyDebugLogger.F("treeCell", tree.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeTree = null;
            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "LumberMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("target", "tree"),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("treeCell", tree.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        private bool TryMoveToProcessableWood(StrategyForestryTree tree)
        {
            if (tree == null || !TryFindTreeWorkCell(tree, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LumberMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "wood"),
                    StrategyDebugLogger.F("reason", tree == null ? "wood_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("treeCell", tree != null ? tree.Cell : Vector2Int.zero));
                return false;
            }

            activeTree = tree;
            activity = tree.HasLogsReady ? ResidentActivity.MovingToLogs : ResidentActivity.MovingToTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "LumberMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", tree.HasLogsReady ? "logs" : "fallen_trunk"),
                    StrategyDebugLogger.F("treeCell", tree.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeTree = null;
            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "LumberMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("target", tree.HasLogsReady ? "logs" : "fallen_trunk"),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("treeCell", tree.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        private bool TryMoveToPlantingCell(Vector2Int cell)
        {
            if (!TryFindPlantingWorkCell(cell, out Vector2Int workCell))
            {
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Population",
                    "PlantMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("plantCell", cell),
                    StrategyDebugLogger.F("reason", "no_work_cell"));
                return false;
            }

            plantingCell = cell;
            activity = ResidentActivity.MovingToPlantTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "PlantMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("plantCell", cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "PlantMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", cell),
                StrategyDebugLogger.F("workCell", workCell),
                StrategyDebugLogger.F("reason", "no_path"));
            return false;
        }

        private bool TryMoveToStoneDeposit(StrategyStoneDeposit deposit)
        {
            if (deposit == null || !TryFindStoneWorkCell(deposit, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "StoneMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", deposit == null ? "deposit_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("depositCell", deposit != null ? deposit.Cell : Vector2Int.zero));
                return false;
            }

            activeStoneDeposit = deposit;
            activity = ResidentActivity.MovingToStone;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "StoneMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("depositCell", deposit.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeStoneDeposit = null;
            activity = ResidentActivity.Idle;
            stoneWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "StoneMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("depositCell", deposit.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        private bool TryFindPlantingWorkCell(Vector2Int targetCell, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 2; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = targetCell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool TryFindTreeWorkCell(StrategyForestryTree tree, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 2; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = tree.Cell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool TryFindStoneWorkCell(StrategyStoneDeposit deposit, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            Vector2Int origin = deposit.Cell;
            Vector2Int footprint = deposit.Footprint;
            for (int radius = 1; radius <= 3; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool TryFindGardenWorkCell(StrategyBuildingUpgrade garden, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 2; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < garden.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < garden.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == garden.Footprint.x + radius - 1
                            || y == garden.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = garden.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !IsUpgradeCell(candidate, garden))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private void StartGardenWork()
        {
            activity = ResidentActivity.WorkingGarden;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            gardenWorkTimer = Random.Range(2.8f, 4.7f);

            if (activeGarden != null && spriteRenderer != null)
            {
                spriteRenderer.flipX = transform.position.x > activeGarden.FootprintBounds.center.x;
                SyncReadabilityRenderers();
            }
        }

        private void UpdateGardenWork()
        {
            gardenWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            if (gardenWorkTimer > 0f)
            {
                return;
            }

            CompleteGardenWork();
            activity = GetRestingActivity();
            activeGarden = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            waitTimer = Random.Range(0.45f, 1.2f);
        }

        private void StartGatheringForage()
        {
            if (activeForageNode == null || home == null || !activeForageNode.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.GatheringForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageGatherSecondsMin, ForageGatherSecondsMax);
            FaceWorldPoint(activeForageNode.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageGathering",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeForageNode.ResourceType),
                StrategyDebugLogger.F("nodeCell", activeForageNode.Cell));
        }

        private void UpdateGatheringForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            if (activeForageNode != null)
            {
                FaceWorldPoint(activeForageNode.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeForageNode == null
                || !activeForageNode.TryGather(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = IsAdult ? amount : Mathf.Min(1, amount);
            activeForageNode = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToHomeDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartPickingUpLooseForage()
        {
            if (activeLooseForageSource == null
                || home == null
                || !activeLooseForageSource.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.PickingUpLooseForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(0.35f, 0.75f);
            FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForagePickup",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeLooseForageSource.Resource),
                StrategyDebugLogger.F("pileOrigin", activeLooseForageSource.Origin));
        }

        private void UpdatePickingUpLooseForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            if (activeLooseForageSource != null)
            {
                FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeLooseForageSource == null
                || !activeLooseForageSource.TryTakeReserved(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = amount;
            activeLooseForageSource = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToHomeDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartDepositingForage()
        {
            if (home == null || home.Resources == null || carriedForageAmount <= 0)
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.DepositingForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageDepositSecondsMin, ForageDepositSecondsMax);
            FaceWorldPoint(home.FootprintBounds.center);
            SetCarriedForageVisible(true);
        }

        private void UpdateDepositingForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            SetCarriedForageVisible(true);
            if (forageWorkTimer > 0f)
            {
                return;
            }

            StrategyResourceType depositedResource = carriedForageResource;
            int depositedAmount = carriedForageAmount;
            if (home != null && home.Resources != null && depositedAmount > 0)
            {
                home.Resources.AddResource(depositedResource, depositedAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageDeposited",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", depositedResource),
                    StrategyDebugLogger.F("amount", depositedAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.45f, 1.05f);
        }

        private bool TryBuildPathToHomeDropoff()
        {
            if (map == null || home == null || !TryFindHomeExitWorld(out Vector3 exitWorld))
            {
                return false;
            }

            return map.TryWorldToCell(exitWorld, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell);
        }

        private void CancelForageWork(bool storeCarried)
        {
            if (!IsForagingActivity(activity)
                && activeForageNode == null
                && activeLooseForageSource == null
                && carriedForageAmount <= 0)
            {
                return;
            }

            ResetForageWorkToIdle(storeCarried);
        }

        private void ResetForageWorkToIdle(bool storeCarried)
        {
            if (activeForageNode != null)
            {
                activeForageNode.Release(this);
                activeForageNode = null;
            }

            if (activeLooseForageSource != null)
            {
                activeLooseForageSource.ReleaseReservation(this);
                activeLooseForageSource = null;
            }

            if (storeCarried && carriedForageAmount > 0 && home != null && home.Resources != null)
            {
                home.Resources.AddResource(carriedForageResource, carriedForageAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedForageResource),
                    StrategyDebugLogger.F("amount", carriedForageAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.30f, 0.85f);
        }

        private void StartChoppingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree != null && activeTree.CanBeBucked)
            {
                StartBuckingTree();
                return;
            }

            if (activeTree != null && activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (activeTree == null || !activeTree.CanBeChopped)
            {
                if (activeTree != null)
                {
                    activeTree.Release(this);
                }

                activeTree = null;
                activity = ResidentActivity.Idle;
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                waitTimer = Random.Range(0.35f, 0.85f);
                return;
            }

            activity = ResidentActivity.ChoppingTree;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeTree.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "ChopStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("treeCell", activeTree.Cell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateChoppingTree()
        {
            if (activeTree == null)
            {
                ResetLumberWorkToIdle();
                return;
            }

            if (activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (activeTree.CanBeBucked)
            {
                StartBuckingTree();
                return;
            }

            if (activeTree.IsFalling)
            {
                AnimateWoodcutHold();
                return;
            }

            if (!activeTree.CanBeChopped)
            {
                activeTree.Release(this);
                activeTree = null;
                activity = ResidentActivity.Idle;
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
                return;
            }

            AnimateChoppingWork();
        }

        private void StartBuckingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree == null || !activeTree.CanBeBucked)
            {
                ResetLumberWorkToIdle();
                return;
            }

            activity = ResidentActivity.BuckingTree;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeTree.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "BuckStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("treeCell", activeTree.Cell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateBuckingTree()
        {
            if (activeTree == null)
            {
                ResetLumberWorkToIdle();
                return;
            }

            if (activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (!activeTree.CanBeBucked)
            {
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            AnimateBuckingWork();
        }

        private void StartCollectingLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree == null || !activeTree.HasLogsReady || workplace == null)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", activeTree == null ? "logs_missing" : workplace == null ? "workplace_missing" : "logs_not_ready"));
                ResetLumberWorkToIdle();
                return;
            }

            Vector2Int logsCell = activeTree.Cell;
            if (!workplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("logsCell", logsCell),
                    StrategyDebugLogger.F("campOrigin", workplace.Origin));
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            if (!activeTree.TryTakeLogs(this, out carriedLogAmount))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("logsCell", logsCell));
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            activeTree = null;
            activity = ResidentActivity.CarryingLogs;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedLogsVisible(true);
            StrategyDebugLogger.Info(
                "Population",
                "LogsCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("logsCell", logsCell),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", workplace.Origin));
        }

        private void StartDepositingLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingLogs;
            lumberWorkTimer = Random.Range(LumberDepositSecondsMin, LumberDepositSecondsMax);
            if (workplace != null)
            {
                FaceWorldPoint(workplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Population",
                "LogsDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.2f, 3.4f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedLogAmount;
            if (workplace != null)
            {
                workplace.AddLogs(depositedAmount);
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            StrategyDebugLogger.Info(
                "Population",
                "LogsDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", workplace != null ? workplace.LogsStored : -1));
            CompleteLumberDelivery();
        }

        private void StartMiningStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeStoneDeposit == null || activeStoneDeposit.IsDepleted || stoneWorkplace == null)
            {
                ResetStoneWorkToIdle();
                return;
            }

            activity = ResidentActivity.MiningStone;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeStoneDeposit.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "StoneMiningStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("depositCell", activeStoneDeposit.Cell),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace.Origin));
        }

        private void UpdateMiningStone()
        {
            if (activeStoneDeposit == null || activeStoneDeposit.IsDepleted || stoneWorkplace == null)
            {
                ResetStoneWorkToIdle();
                return;
            }

            AnimateStonecutWork();
        }

        private void StartCarryingMinedStone(int amount)
        {
            if (amount <= 0)
            {
                ResetStoneWorkToIdle();
                return;
            }

            if (stoneWorkplace == null
                || !stoneWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (stoneWorkplace != null)
                {
                    stoneWorkplace.AddStone(amount);
                }

                StrategyDebugLogger.Warn(
                    "Population",
                    "StoneCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"));
                carriedStoneAmount = 0;
                activeStoneDeposit = null;
                ResetStoneWorkToIdle();
                return;
            }

            carriedStoneAmount = amount;
            activeStoneDeposit = null;
            activity = ResidentActivity.CarryingStone;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedStoneVisible(true);
            StrategyDebugLogger.Info(
                "Population",
                "StoneCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace.Origin));
        }

        private void StartDepositingStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStone;
            lumberWorkTimer = Random.Range(LumberDepositSecondsMin, LumberDepositSecondsMax);
            if (stoneWorkplace != null)
            {
                FaceWorldPoint(stoneWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Population",
                "StoneDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.2f, 3.4f);
            SetCarriedStoneVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedStoneAmount;
            if (stoneWorkplace != null)
            {
                stoneWorkplace.AddStone(depositedAmount);
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            StrategyDebugLogger.Info(
                "Population",
                "StoneDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", stoneWorkplace != null ? stoneWorkplace.StoneStored : -1));
            CompleteStoneDelivery();
        }

        private void StartPickingUpStorageLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeLogSource == null && activeLooseLogSource == null) || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageLogs;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLogSource != null ? activeLogSource.FootprintBounds : activeLooseLogSource.FootprintBounds;
            Vector2Int sourceOrigin = activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource.Origin;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void UpdatePickingUpStorageLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeLogSource == null && activeLooseLogSource == null)
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeLogSource != null)
                {
                    activeLogSource.ReleaseStoredLogsReservation(this);
                }

                if (activeLooseLogSource != null)
                {
                    activeLooseLogSource.ReleaseStorageReservation(this);
                }

                StrategyDebugLogger.Warn(
                    "Logistics",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_storage_path"),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            bool taken = activeLogSource != null
                ? activeLogSource.TryTakeReservedLogs(this, out carriedLogAmount)
                : activeLooseLogSource.TryTakeReservedForStorage(this, StrategyConstructionResourceKind.Logs, out carriedLogAmount);
            if (!taken)
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource != null ? activeLooseLogSource.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource.Origin;
            activeLogSource = null;
            activeLooseLogSource = null;
            activity = ResidentActivity.CarryingLogsToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedLogsVisible(true);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void StartDepositingStorageLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageLogs;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "StorageDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStorageLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedLogAmount;
            if (storageWorkplace != null)
            {
                storageWorkplace.AddLogs(depositedAmount);
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("yardStock", storageWorkplace != null ? storageWorkplace.LogsStored : -1));
            CompleteStorageDelivery();
        }

        private void StartPickingUpStorageStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeStoneSource == null && activeLooseStoneSource == null) || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageStone;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeStoneSource != null ? activeStoneSource.FootprintBounds : activeLooseStoneSource.FootprintBounds;
            Vector2Int sourceOrigin = activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource.Origin;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "StonePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void UpdatePickingUpStorageStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeStoneSource == null && activeLooseStoneSource == null)
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeStoneSource != null)
                {
                    activeStoneSource.ReleaseStoredStoneReservation(this);
                }

                if (activeLooseStoneSource != null)
                {
                    activeLooseStoneSource.ReleaseStorageReservation(this);
                }

                StrategyDebugLogger.Warn(
                    "Logistics",
                    "StonePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_storage_path"),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            bool taken = activeStoneSource != null
                ? activeStoneSource.TryTakeReservedStone(this, out carriedStoneAmount)
                : activeLooseStoneSource.TryTakeReservedForStorage(this, StrategyConstructionResourceKind.Stone, out carriedStoneAmount);
            if (!taken)
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "StonePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource != null ? activeLooseStoneSource.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource.Origin;
            activeStoneSource = null;
            activeLooseStoneSource = null;
            activity = ResidentActivity.CarryingStoneToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedStoneVisible(true);
            StrategyDebugLogger.Info(
                "Logistics",
                "StonePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void StartDepositingStorageStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageStone;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "StorageStoneDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStorageStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedStoneVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedStoneAmount;
            if (storageWorkplace != null)
            {
                storageWorkplace.AddResource(StrategyResourceType.Stone, depositedAmount);
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            StrategyDebugLogger.Info(
                "Logistics",
                "StoneDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("yardStock", storageWorkplace != null ? storageWorkplace.StoneStored : -1));
            CompleteStorageDelivery();
        }

        private void StartPickingUpGranaryGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeGameSource == null && activeLooseFoodSource == null) || granaryWorkplace == null)
            {
                ResetGranaryWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpGranaryGame;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLooseFoodSource != null
                ? activeLooseFoodSource.FootprintBounds
                : activeGameSource.FootprintBounds;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Granary",
                "GamePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeLooseFoodSource != null ? activeLooseFoodSource.Origin : activeGameSource.Origin),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
        }

        private void UpdatePickingUpGranaryGame()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeGameSource == null && activeLooseFoodSource == null)
                || granaryWorkplace == null
                || !granaryWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                activeGameSource?.ReleaseStoredGameReservation(this);
                activeLooseFoodSource?.ReleaseReservation(this);
                StrategyDebugLogger.Warn(
                    "Granary",
                    "GamePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_granary_path"),
                    StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero));
                ResetGranaryWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin;
            if (activeLooseFoodSource != null)
            {
                sourceOrigin = activeLooseFoodSource.Origin;
                if (!activeLooseFoodSource.TryTakeReserved(this, out StrategyResourceType resource, out carriedGameAmount)
                    || resource != StrategyResourceType.Game)
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "GamePickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "loose_take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    activeLooseFoodSource = null;
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeLooseFoodSource = null;
            }
            else if (!activeGameSource.TryTakeReservedGame(this, out carriedGameAmount))
            {
                StrategyDebugLogger.Warn(
                    "Granary",
                    "GamePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeGameSource.Origin));
                ResetGranaryWorkToIdle();
                return;
            }
            else
            {
                sourceOrigin = activeGameSource.Origin;
            }

            activeGameSource = null;
            activity = ResidentActivity.CarryingGameToGranary;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedGameVisible(true);
            StrategyDebugLogger.Info(
                "Granary",
                "GamePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
        }

        private void StartDepositingGranaryGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGranaryGame;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (granaryWorkplace != null)
            {
                FaceWorldPoint(granaryWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Granary",
                "GameDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGranaryGame()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedGameVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedGameAmount;
            if (granaryWorkplace != null)
            {
                granaryWorkplace.AddGame(depositedAmount);
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            StrategyDebugLogger.Info(
                "Granary",
                "GameDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("granaryStock", granaryWorkplace != null ? granaryWorkplace.GameStored : -1));
            CompleteGranaryDelivery();
        }

        private void StartPickingUpGranaryFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeFishSource == null && activeLooseFoodSource == null) || granaryWorkplace == null)
            {
                ResetGranaryWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpGranaryFish;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLooseFoodSource != null
                ? activeLooseFoodSource.FootprintBounds
                : activeFishSource.FootprintBounds;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Granary",
                "FishPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeLooseFoodSource != null ? activeLooseFoodSource.Origin : activeFishSource.Origin),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
        }

        private void UpdatePickingUpGranaryFish()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeFishSource == null && activeLooseFoodSource == null)
                || granaryWorkplace == null
                || !granaryWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                activeFishSource?.ReleaseStoredFishReservation(this);
                activeLooseFoodSource?.ReleaseReservation(this);
                StrategyDebugLogger.Warn(
                    "Granary",
                    "FishPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_granary_path"),
                    StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero));
                ResetGranaryWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin;
            if (activeLooseFoodSource != null)
            {
                sourceOrigin = activeLooseFoodSource.Origin;
                if (!activeLooseFoodSource.TryTakeReserved(this, out StrategyResourceType resource, out carriedFishAmount)
                    || resource != StrategyResourceType.Fish)
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FishPickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "loose_take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    activeLooseFoodSource = null;
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeLooseFoodSource = null;
            }
            else if (!activeFishSource.TryTakeReservedFish(this, out carriedFishAmount))
            {
                StrategyDebugLogger.Warn(
                    "Granary",
                    "FishPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeFishSource.Origin));
                ResetGranaryWorkToIdle();
                return;
            }
            else
            {
                sourceOrigin = activeFishSource.Origin;
            }

            activeFishSource = null;
            activity = ResidentActivity.CarryingFishToGranary;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedFishVisible(true);
            StrategyDebugLogger.Info(
                "Granary",
                "FishPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace.Origin));
        }

        private void StartDepositingGranaryFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGranaryFish;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (granaryWorkplace != null)
            {
                FaceWorldPoint(granaryWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Granary",
                "FishDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGranaryFish()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedFishVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedFishAmount;
            if (granaryWorkplace != null)
            {
                granaryWorkplace.AddFish(depositedAmount);
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            StrategyDebugLogger.Info(
                "Granary",
                "FishDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("granaryOrigin", granaryWorkplace != null ? granaryWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("granaryStock", granaryWorkplace != null ? granaryWorkplace.FishStored : -1));
            CompleteGranaryDelivery();
        }

        private void StartPickingUpConstructionResource()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (constructionSite == null
                || activeConstructionSource == null
                || activeConstructionResource == StrategyConstructionResourceKind.None)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = activeConstructionResource == StrategyConstructionResourceKind.Logs
                ? ResidentActivity.PickingUpConstructionLogs
                : ResidentActivity.PickingUpConstructionStone;
            lumberWorkTimer = Random.Range(ConstructionPickupSecondsMin, ConstructionPickupSecondsMax);
            FaceWorldPoint(activeConstructionSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("sourceOrigin", activeConstructionSource.Origin),
                StrategyDebugLogger.F("resource", activeConstructionResource));
        }

        private void UpdatePickingUpConstructionResource()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (constructionSite == null
                || activeConstructionSource == null
                || activeConstructionResource == StrategyConstructionResourceKind.None
                || !constructionSite.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite != null ? constructionSite.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"));
                ResetConstructionWorkToIdle();
                return;
            }

            if (!activeConstructionSource.TryTakeReservedConstructionResource(constructionSite, this, activeConstructionResource, 1, out int amount))
            {
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("sourceOrigin", activeConstructionSource.Origin),
                    StrategyDebugLogger.F("resource", activeConstructionResource),
                    StrategyDebugLogger.F("reason", "take_failed"));
                ResetConstructionWorkToIdle();
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Logs)
            {
                carriedLogAmount = amount;
                SetCarriedLogsVisible(true);
                activity = ResidentActivity.CarryingConstructionLogs;
            }
            else
            {
                carriedStoneAmount = amount;
                SetCarriedStoneVisible(true);
                activity = ResidentActivity.CarryingConstructionStone;
            }

            CaptureCarriedConstructionReturnReservation();
            Vector2Int sourceOrigin = activeConstructionSource.Origin;
            activeConstructionSource = null;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderResourcePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("resource", activeConstructionResource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell));
        }

        private void StartDepositingConstructionResource()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (constructionSite == null)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = ResidentActivity.DepositingConstructionResource;
            lumberWorkTimer = Random.Range(ConstructionDepositSecondsMin, ConstructionDepositSecondsMax);
            FaceWorldPoint(constructionSite.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("resource", activeConstructionResource),
                StrategyDebugLogger.F("logs", carriedLogAmount),
                StrategyDebugLogger.F("stone", carriedStoneAmount));
        }

        private void UpdateDepositingConstructionResource()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.3f);
            SetCarriedLogsVisible(carriedLogAmount > 0);
            SetCarriedStoneVisible(carriedStoneAmount > 0);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (constructionSite != null)
            {
                if (activeConstructionResource == StrategyConstructionResourceKind.Logs && carriedLogAmount > 0)
                {
                    constructionSite.AddDeliveredResource(StrategyConstructionResourceKind.Logs, carriedLogAmount);
                }
                else if (activeConstructionResource == StrategyConstructionResourceKind.Stone && carriedStoneAmount > 0)
                {
                    constructionSite.AddDeliveredResource(StrategyConstructionResourceKind.Stone, carriedStoneAmount);
                }
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            ClearCarriedConstructionReturnReservation();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            CompleteConstructionDelivery();
        }

        private void StartBuildingConstruction()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (constructionSite == null || !constructionSite.ResourcesComplete)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = ResidentActivity.BuildingConstruction;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(constructionSite.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderWorkStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin));
        }

        private void UpdateBuildingConstruction()
        {
            if (constructionSite == null || constructionSite.IsCompleted)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            if (!constructionSite.ResourcesComplete)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            AnimateConstructionWork();
        }

        private void StartAimingBow()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeHuntTarget == null || !activeHuntTarget.IsAlive || activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.AimingBow;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            bowShotReleased = false;
            huntingWorkTimer = 0.35f;
            FaceWorldPoint(activeHuntTarget.transform.position);
            StrategyDebugLogger.Info(
                "Hunting",
                "BowAimingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitWorld", activeHuntTarget.transform.position),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateAimingBow()
        {
            if (activeHuntTarget == null)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (activeHuntTarget.IsCarcass)
            {
                TryMoveToHuntCarcass();
                return;
            }

            if (!activeHuntTarget.IsAlive)
            {
                activity = ResidentActivity.WaitingForHuntHit;
                huntingWorkTimer = 1.2f;
                return;
            }

            FaceWorldPoint(activeHuntTarget.transform.position);
            AnimateBowWork();
            if (!bowShotReleased)
            {
                return;
            }

            huntingWorkTimer -= Time.deltaTime;
            if (huntingWorkTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingForHuntHit;
            huntingWorkTimer = 1.8f;
        }

        private void UpdateWaitingForHuntHit()
        {
            if (activeHuntTarget == null)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (activeHuntTarget.IsCarcass)
            {
                TryMoveToHuntCarcass();
                return;
            }

            if (activeHuntTarget.IsAlive)
            {
                huntingWorkTimer -= Time.deltaTime;
                ApplyBowFrame(9);
                if (huntingWorkTimer <= 0f)
                {
                    ResetHunterWorkToIdle(true);
                }

                return;
            }

            ApplyBowFrame(10);
        }

        private void TryMoveToHuntCarcass()
        {
            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (!activeHuntTarget.TryGetCurrentCell(out Vector2Int carcassCell))
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (!TryBuildPathTo(carcassCell))
            {
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int candidate = carcassCell + CardinalDirections[i];
                    if (map.IsCellWalkable(candidate) && TryBuildPathTo(candidate))
                    {
                        activity = ResidentActivity.MovingToHuntCarcass;
                        hasTarget = true;
                        waitTimer = Random.Range(0.04f, 0.18f);
                        return;
                    }
                }

                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.MovingToHuntCarcass;
            hasTarget = true;
            waitTimer = Random.Range(0.04f, 0.18f);
            StrategyDebugLogger.Info(
                "Hunting",
                "CarcassMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("carcassCell", carcassCell),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void StartButcheringRabbit()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.ButcheringRabbit;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeHuntTarget.transform.position);
            StrategyDebugLogger.Info(
                "Hunting",
                "ButcheringStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitWorld", activeHuntTarget.transform.position),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateButcheringRabbit()
        {
            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            FaceWorldPoint(activeHuntTarget.transform.position);
            AnimateButcherWork();
        }

        private void StartCarryingGame(int amount)
        {
            if (amount <= 0)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (hunterWorkplace == null
                || !hunterWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (hunterWorkplace != null)
                {
                    hunterWorkplace.AddGame(amount);
                }

                activeHuntTarget = null;
                carriedGameAmount = 0;
                ResetHunterWorkToIdle(false);
                StrategyDebugLogger.Warn(
                    "Hunting",
                    "GameCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
                return;
            }

            carriedGameAmount = amount;
            activeHuntTarget = null;
            activity = ResidentActivity.CarryingGame;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedGameVisible(true);
            StrategyDebugLogger.Info(
                "Hunting",
                "GameCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void StartDepositingGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGame;
            huntingWorkTimer = Random.Range(HuntingDepositSecondsMin, HuntingDepositSecondsMax);
            if (hunterWorkplace != null)
            {
                FaceWorldPoint(hunterWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Hunting",
                "GameDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGame()
        {
            huntingWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedGameVisible(true);
            if (huntingWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedGameAmount;
            if (hunterWorkplace != null)
            {
                hunterWorkplace.AddGame(depositedAmount);
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            StrategyDebugLogger.Info(
                "Hunting",
                "GameDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", hunterWorkplace != null ? hunterWorkplace.GameStored : -1));
            CompleteHunterDelivery();
        }

        private void StartCastingFishingLine()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.CastingFishingLine;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            fishingLineCast = false;
            fishingWorkTimer = 0.72f;
            fishingBiteTimer = Random.Range(0.65f, 1.35f);
            FaceWorldPoint(activeFishTarget.transform.position);
            SetFishingLineVisible(true);
            StrategyDebugLogger.Info(
                "Fishing",
                "CastingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateCastingFishingLine()
        {
            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            AnimateFishingWork();
            SyncFishingLineRenderer();
            fishingWorkTimer -= Time.deltaTime;
            if (!fishingLineCast || fishingWorkTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingForFishBite;
            fishingBiteTimer = Random.Range(0.75f, 1.65f);
            StrategyDebugLogger.Info(
                "Fishing",
                "WaitingForBite",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position));
        }

        private void UpdateWaitingForFishBite()
        {
            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            if (!activeFishTarget.IsHooked)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            ApplyFishingFrame(7 + (Time.frameCount / 10) % 2);
            SetFishingLineVisible(true);
            fishingBiteTimer -= Time.deltaTime;
            if (fishingBiteTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.ReelingFish;
            workFrame = 8;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            StrategyDebugLogger.Info(
                "Fishing",
                "ReelingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position));
        }

        private void UpdateReelingFish()
        {
            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            if (!activeFishTarget.IsHooked)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            SetFishingLineVisible(true);
            AnimateFishingWork();
        }

        private void StartCarryingFish(int amount)
        {
            if (amount <= 0)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            SetFishingLineVisible(false);
            if (fisherWorkplace == null
                || !fisherWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (fisherWorkplace != null)
                {
                    fisherWorkplace.AddFish(amount);
                }

                activeFishTarget = null;
                carriedFishAmount = 0;
                ResetFisherWorkToIdle(false);
                StrategyDebugLogger.Warn(
                    "Fishing",
                    "FishCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
                return;
            }

            carriedFishAmount = amount;
            activeFishTarget = null;
            activity = ResidentActivity.CarryingFish;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedFishVisible(true);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void StartDepositingFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingFish;
            fishingWorkTimer = Random.Range(FishingDepositSecondsMin, FishingDepositSecondsMax);
            if (fisherWorkplace != null)
            {
                FaceWorldPoint(fisherWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Fishing",
                "FishDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingFish()
        {
            fishingWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedFishVisible(true);
            if (fishingWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedFishAmount;
            if (fisherWorkplace != null)
            {
                fisherWorkplace.AddFish(depositedAmount);
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("hutStock", fisherWorkplace != null ? fisherWorkplace.FishStored : -1));
            CompleteFisherDelivery();
        }

        private void CompleteLumberDelivery()
        {
            activeTree = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();

            if (workplace != null
                && workplace.TryFindPlantingCell(out Vector2Int cell)
                && TryMoveToPlantingCell(cell))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(4.0f, 8.0f);
            waitTimer = Random.Range(0.45f, 1.1f);
        }

        private void CompleteStoneDelivery()
        {
            activeStoneDeposit = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            stoneWorkCooldown = Random.Range(3.4f, 6.8f);
            waitTimer = Random.Range(0.45f, 1.1f);
        }

        private void CompleteStorageDelivery()
        {
            activeLogSource = null;
            activeStoneSource = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            logisticsWorkCooldown = Random.Range(2.2f, 4.8f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteGranaryDelivery()
        {
            activeGameSource = null;
            activeFishSource = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            logisticsWorkCooldown = Random.Range(2.2f, 4.8f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteConstructionDelivery()
        {
            activeConstructionSource = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            waitTimer = constructionSite != null && constructionSite.ResourcesComplete
                ? Random.Range(0.05f, 0.22f)
                : Random.Range(0.20f, 0.55f);
        }

        private void CompleteHunterDelivery()
        {
            activeHuntTarget = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            huntingWorkCooldown = Random.Range(3.5f, 7.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteFisherDelivery()
        {
            activeFishTarget = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetFishingLineVisible(false);
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            fishingWorkCooldown = Random.Range(3.5f, 7.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private bool HasAnyCarriedResource()
        {
            return carriedLogAmount > 0
                || carriedStoneAmount > 0
                || carriedGameAmount > 0
                || carriedFishAmount > 0;
        }

        private void CaptureCarriedConstructionReturnReservation()
        {
            if (constructionSite == null || constructionSite.IsCompleted)
            {
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Logs && carriedLogAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Logs;
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Stone && carriedStoneAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Stone;
                return;
            }

            if (carriedLogAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Logs;
                return;
            }

            if (carriedStoneAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Stone;
            }
        }

        private int GetRestorableCarriedConstructionReservationAmount(
            StrategyConstructionResourceKind resource,
            int amount,
            out StrategyConstructionSite site)
        {
            site = null;
            if (amount <= 0
                || carriedConstructionReturnSite == null
                || carriedConstructionReturnResource != resource
                || resource == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            StrategyConstructionSite candidate = carriedConstructionReturnSite;
            if (candidate == null || candidate.IsCompleted)
            {
                ClearCarriedConstructionReturnReservation();
                return 0;
            }

            int needed = resource == StrategyConstructionResourceKind.Logs
                ? candidate.NeededLogs
                : candidate.NeededStone;
            if (needed <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return 0;
            }

            site = candidate;
            return Mathf.Min(amount, needed);
        }

        private void ClearCarriedConstructionReturnReservation()
        {
            carriedConstructionReturnSite = null;
            carriedConstructionReturnResource = StrategyConstructionResourceKind.None;
        }

        private void ClearEmptyCarriedResourceReturn(string reason)
        {
            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            ClearCarriedConstructionReturnReservation();
            if (IsReturningCarriedResourceActivity(activity))
            {
                activity = GetRestingActivity();
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.25f, 0.70f);
            StrategyDebugLogger.Info(
                "Logistics",
                "EmptyCarriedResourceReturnCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("reason", reason));
        }

        private bool TryStartCarriedResourceReturn(string reason, bool restartCurrentReturn = false)
        {
            if (deathRequested)
            {
                return false;
            }

            if (!HasAnyCarriedResource())
            {
                if (IsReturningCarriedResourceActivity(activity) || restartCurrentReturn)
                {
                    ClearEmptyCarriedResourceReturn(reason);
                }
                else
                {
                    ClearCarriedConstructionReturnReservation();
                }

                return false;
            }

            if (IsReturningCarriedResourceActivity(activity) && !restartCurrentReturn)
            {
                return true;
            }

            if (restartCurrentReturn)
            {
                returnStorageYard = null;
                returnGranary = null;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
            }

            if (carriedLogAmount > 0)
            {
                return TryStartMaterialReturn(StrategyConstructionResourceKind.Logs, reason);
            }

            if (carriedStoneAmount > 0)
            {
                return TryStartMaterialReturn(StrategyConstructionResourceKind.Stone, reason);
            }

            if (carriedGameAmount > 0)
            {
                return TryStartFoodReturn(StrategyResourceType.Game, reason);
            }

            if (carriedFishAmount > 0)
            {
                return TryStartFoodReturn(StrategyResourceType.Fish, reason);
            }

            return false;
        }

        private bool TryStartMaterialReturn(StrategyConstructionResourceKind resource, string reason)
        {
            if (resource != StrategyConstructionResourceKind.Logs
                && resource != StrategyConstructionResourceKind.Stone)
            {
                return false;
            }

            int amount = resource == StrategyConstructionResourceKind.Logs
                ? carriedLogAmount
                : carriedStoneAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyStorageYard.TryFindNearestDropoff(transform.position, out StrategyStorageYard yard, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = yard;
                returnGranary = null;
                activity = resource == StrategyConstructionResourceKind.Logs
                    ? ResidentActivity.ReturningLogsToStorage
                    : ResidentActivity.ReturningStoneToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedLogsVisible(carriedLogAmount > 0);
                SetCarriedStoneVisible(carriedStoneAmount > 0);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedMaterialImmediately(resource, reason, "no_reachable_storage");
        }

        private bool TryStartFoodReturn(StrategyResourceType resource, string reason)
        {
            if (resource != StrategyResourceType.Game && resource != StrategyResourceType.Fish)
            {
                return false;
            }

            int amount = resource == StrategyResourceType.Game ? carriedGameAmount : carriedFishAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyGranary.TryFindNearestDropoff(transform.position, out StrategyGranary granary, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = null;
                returnGranary = granary;
                activity = resource == StrategyResourceType.Game
                    ? ResidentActivity.ReturningGameToGranary
                    : ResidentActivity.ReturningFishToGranary;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedGameVisible(carriedGameAmount > 0);
                SetCarriedFishVisible(carriedFishAmount > 0);
                SetFishingLineVisible(false);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedFoodReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedFoodImmediately(resource, reason, "no_reachable_granary");
        }

        private void CompleteCarriedResourceReturn()
        {
            ResidentActivity completedActivity = activity;
            int amount = 0;
            object resource = StrategyConstructionResourceKind.None;
            Vector2Int storageOrigin = Vector2Int.zero;

            if (completedActivity == ResidentActivity.ReturningLogsToStorage)
            {
                amount = carriedLogAmount;
                resource = StrategyConstructionResourceKind.Logs;
                if (returnStorageYard == null)
                {
                    if (!StoreCarriedMaterialImmediately(
                        StrategyConstructionResourceKind.Logs,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnStorageYard.Origin;
                    StoreReturnedMaterialAtYard(returnStorageYard, StrategyConstructionResourceKind.Logs, amount);
                }

                carriedLogAmount = 0;
                SetCarriedLogsVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningStoneToStorage)
            {
                amount = carriedStoneAmount;
                resource = StrategyConstructionResourceKind.Stone;
                if (returnStorageYard == null)
                {
                    if (!StoreCarriedMaterialImmediately(
                        StrategyConstructionResourceKind.Stone,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnStorageYard.Origin;
                    StoreReturnedMaterialAtYard(returnStorageYard, StrategyConstructionResourceKind.Stone, amount);
                }

                carriedStoneAmount = 0;
                SetCarriedStoneVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningGameToGranary)
            {
                amount = carriedGameAmount;
                resource = StrategyResourceType.Game;
                if (returnGranary == null)
                {
                    if (!StoreCarriedFoodImmediately(
                        StrategyResourceType.Game,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnGranary.Origin;
                    returnGranary.AddGame(amount);
                }

                carriedGameAmount = 0;
                SetCarriedGameVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningFishToGranary)
            {
                amount = carriedFishAmount;
                resource = StrategyResourceType.Fish;
                if (returnGranary == null)
                {
                    if (!StoreCarriedFoodImmediately(
                        StrategyResourceType.Fish,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnGranary.Origin;
                    returnGranary.AddFish(amount);
                }

                carriedFishAmount = 0;
                SetCarriedFishVisible(false);
                SetFishingLineVisible(false);
            }

            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();

            if (amount <= 0)
            {
                ClearEmptyCarriedResourceReturn("completed_without_resource");
                return;
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));

            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);

            if (HasAnyCarriedResource() && TryStartCarriedResourceReturn("remaining_carried_resource"))
            {
                return;
            }
        }

        private void StoreReturnedMaterialAtYard(
            StrategyStorageYard yard,
            StrategyConstructionResourceKind resource,
            int amount)
        {
            if (yard == null || amount <= 0)
            {
                return;
            }

            int reservedAmount = GetRestorableCarriedConstructionReservationAmount(resource, amount, out StrategyConstructionSite site);
            if (reservedAmount > 0)
            {
                yard.ReturnReservedConstructionResource(site, resource, reservedAmount);
            }

            int regularAmount = amount - reservedAmount;
            if (regularAmount > 0)
            {
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    yard.AddLogs(regularAmount);
                }
                else if (resource == StrategyConstructionResourceKind.Stone)
                {
                    yard.AddResource(StrategyResourceType.Stone, regularAmount);
                }
            }

            ClearCarriedConstructionReturnReservation();
        }

        private void RestoreReturnedMaterialReservationOnPile(
            StrategyLooseConstructionResourcePile pile,
            StrategyConstructionResourceKind resource,
            int amount)
        {
            if (pile == null || amount <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return;
            }

            int reservedAmount = GetRestorableCarriedConstructionReservationAmount(resource, amount, out StrategyConstructionSite site);
            if (reservedAmount > 0)
            {
                pile.TryRestoreConstructionReservation(site, resource, reservedAmount);
            }

            ClearCarriedConstructionReturnReservation();
        }

        private bool StoreCarriedMaterialImmediately(
            StrategyConstructionResourceKind resource,
            string reason,
            string fallbackReason)
        {
            int amount = resource == StrategyConstructionResourceKind.Logs
                ? carriedLogAmount
                : carriedStoneAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                StoreReturnedMaterialAtYard(yard, resource, amount);
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    carriedLogAmount = 0;
                    SetCarriedLogsVisible(false);
                }
                else
                {
                    carriedStoneAmount = 0;
                    SetCarriedStoneVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                int logs = resource == StrategyConstructionResourceKind.Logs ? amount : 0;
                int stone = resource == StrategyConstructionResourceKind.Stone ? amount : 0;
                StrategyLooseConstructionResourcePile pile = StrategyLooseConstructionResourcePile.Create(map, cell, transform.position, logs, stone);
                RestoreReturnedMaterialReservationOnPile(pile, resource, amount);
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    carriedLogAmount = 0;
                    SetCarriedLogsVisible(false);
                }
                else
                {
                    carriedStoneAmount = 0;
                    SetCarriedStoneVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private bool StoreCarriedFoodImmediately(
            StrategyResourceType resource,
            string reason,
            string fallbackReason)
        {
            int amount = resource == StrategyResourceType.Game ? carriedGameAmount : carriedFishAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyGranary.TryFindNearestGranary(transform.position, out StrategyGranary granary))
            {
                if (resource == StrategyResourceType.Game)
                {
                    granary.AddGame(amount);
                    carriedGameAmount = 0;
                    SetCarriedGameVisible(false);
                }
                else
                {
                    granary.AddFish(amount);
                    carriedFishAmount = 0;
                    SetCarriedFishVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedFoodStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (resource == StrategyResourceType.Game && hunterWorkplace != null)
            {
                hunterWorkplace.AddGame(amount);
                carriedGameAmount = 0;
                SetCarriedGameVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Hunting",
                    "CarriedGameStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "hunter_camp"),
                    StrategyDebugLogger.F("campOrigin", hunterWorkplace.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (resource == StrategyResourceType.Fish && fisherWorkplace != null)
            {
                fisherWorkplace.AddFish(amount);
                carriedFishAmount = 0;
                SetCarriedFishVisible(false);
                SetFishingLineVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Fishing",
                    "CarriedFishStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "fisher_hut"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void ResetAfterImmediateCarriedResourceStore()
        {
            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.25f, 0.70f);
        }

        private void ScheduleCarriedResourceReturnRetry()
        {
            if (!HasAnyCarriedResource())
            {
                ClearEmptyCarriedResourceReturn("retry_without_resource");
                return;
            }

            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourceReturnRetryScheduled",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("logs", carriedLogAmount),
                StrategyDebugLogger.F("stone", carriedStoneAmount),
                StrategyDebugLogger.F("game", carriedGameAmount),
                StrategyDebugLogger.F("fish", carriedFishAmount));
        }

        private void ResetLumberWorkToIdle()
        {
            activeTree = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (carriedLogAmount > 0 && TryStartCarriedResourceReturn("lumber_work_reset"))
            {
                return;
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetStoneWorkToIdle()
        {
            if (activeStoneDeposit != null)
            {
                activeStoneDeposit.Release(this);
            }

            activeStoneDeposit = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (carriedStoneAmount > 0 && TryStartCarriedResourceReturn("stone_work_reset"))
            {
                return;
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            stoneWorkCooldown = Random.Range(2.0f, 4.0f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetStorageWorkToIdle(bool storeCarriedLogs = false)
        {
            if (activeLogSource != null)
            {
                activeLogSource.ReleaseStoredLogsReservation(this);
            }

            if (activeStoneSource != null)
            {
                activeStoneSource.ReleaseStoredStoneReservation(this);
            }

            if (activeLooseLogSource != null)
            {
                activeLooseLogSource.ReleaseStorageReservation(this);
            }

            if (activeLooseStoneSource != null)
            {
                activeLooseStoneSource.ReleaseStorageReservation(this);
            }

            activeLogSource = null;
            activeStoneSource = null;
            activeLooseLogSource = null;
            activeLooseStoneSource = null;
            if (storeCarriedLogs
                && (carriedLogAmount > 0 || carriedStoneAmount > 0)
                && TryStartCarriedResourceReturn("storage_work_cancelled"))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            logisticsWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetGranaryWorkToIdle(bool storeCarriedFood = false)
        {
            if (activeGameSource != null)
            {
                activeGameSource.ReleaseStoredGameReservation(this);
            }

            if (activeFishSource != null)
            {
                activeFishSource.ReleaseStoredFishReservation(this);
            }

            if (activeLooseFoodSource != null)
            {
                activeLooseFoodSource.ReleaseReservation(this);
            }

            activeGameSource = null;
            activeFishSource = null;
            activeLooseFoodSource = null;
            if (storeCarriedFood
                && (carriedGameAmount > 0 || carriedFishAmount > 0)
                && TryStartCarriedResourceReturn("granary_work_cancelled"))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            logisticsWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetConstructionWorkToIdle()
        {
            CaptureCarriedConstructionReturnReservation();
            ReleaseActiveConstructionPickupReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            if ((carriedLogAmount > 0 || carriedStoneAmount > 0)
                && TryStartCarriedResourceReturn("construction_work_cancelled"))
            {
                return;
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ReleaseActiveConstructionPickupReservation()
        {
            if (activeConstructionSource != null)
            {
                activeConstructionSource.ReleaseConstructionPickupReservation(this);
            }
        }

        private void ResetHunterWorkToIdle(bool releaseReservation)
        {
            if (releaseReservation && activeHuntTarget != null)
            {
                activeHuntTarget.ReleaseHuntReservation(this);
            }

            activeHuntTarget = null;
            bowShotReleased = false;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (carriedGameAmount > 0 && TryStartCarriedResourceReturn("hunter_work_cancelled"))
            {
                return;
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            UseIdleSprite();
            huntingWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetFisherWorkToIdle(bool releaseReservation)
        {
            if (releaseReservation && activeFishTarget != null)
            {
                activeFishTarget.ReleaseFishingReservation(this);
            }

            activeFishTarget = null;
            fishingLineCast = false;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (carriedFishAmount > 0 && TryStartCarriedResourceReturn("fisher_work_cancelled"))
            {
                return;
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            SetFishingLineVisible(false);
            UseIdleSprite();
            fishingWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void StartPlantingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.PlantingTree;
            lumberWorkTimer = Random.Range(LumberPlantSecondsMin, LumberPlantSecondsMax);
            if (map != null)
            {
                FaceWorldPoint(map.GetCellCenterWorld(plantingCell.x, plantingCell.y));
            }

            StrategyDebugLogger.Info(
                "Population",
                "PlantingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", plantingCell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdatePlantingTree()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.4f, 4.5f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            bool planted = workplace != null && workplace.TryPlantTree(plantingCell);
            StrategyDebugLogger.Info(
                "Population",
                planted ? "TreePlanted" : "TreePlantFailed",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", plantingCell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            lumberWorkCooldown = Random.Range(5.0f, 9.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void PickNextIdleTarget()
        {
            for (int attempt = 0; attempt < 18; attempt++)
            {
                int minX = idleOrigin.x - IdleRadius;
                int maxX = idleOrigin.x + idleFootprint.x + IdleRadius - 1;
                int minY = idleOrigin.y - IdleRadius;
                int maxY = idleOrigin.y + idleFootprint.y + IdleRadius - 1;
                Vector2Int cell = new Vector2Int(
                    Random.Range(minX, maxX + 1),
                    Random.Range(minY, maxY + 1));

                if (!map.IsCellWalkable(cell))
                {
                    continue;
                }

                if (TryBuildPathTo(cell))
                {
                    activity = GetRestingActivity();
                    hasTarget = true;
                    waitTimer = Random.Range(0.15f, 0.55f);
                    return;
                }
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (map == null
                || !TryGetPathStartCell(out Vector2Int startCell)
                || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.08f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited.Count < visitLimit)
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
                    if (visited.Contains(next) || !map.IsCellWalkable(next))
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

        private bool TryGetPathStartCell(out Vector2Int startCell)
        {
            startCell = default;
            if (map == null || !map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            if (map.IsCellWalkable(currentCell))
            {
                startCell = currentCell;
                return true;
            }

            Vector2Int bestCell = currentCell;
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int radius = 1; radius <= 4; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = currentCell + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                        float distance = (candidateWorld - transform.position).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestCell = candidate;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            Vector3 recoveryWorld = map.GetCellCenterWorld(bestCell.x, bestCell.y);
            transform.position = new Vector3(recoveryWorld.x, recoveryWorld.y, transform.position.z);
            startCell = bestCell;
            StrategyDebugLogger.Warn(
                "Resident",
                "PathStartRecovered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fromCell", currentCell),
                StrategyDebugLogger.F("toCell", bestCell));
            return true;
        }

        private void StartMovingHome(Vector3 targetWorld)
        {
            bool hasGridPath = map != null
                && map.TryWorldToCell(targetWorld, out Vector2Int targetCell)
                && TryBuildPathTo(targetCell);

            if (!hasGridPath)
            {
                BuildDirectWorldPath(targetWorld);
            }

            activity = ResidentActivity.MovingHome;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
        }

        private void UpdateHomeboundChild()
        {
            if (!hiddenInsideHome || activity != ResidentActivity.StayingInsideHome)
            {
                EnterHomeboundChildState(!hiddenInsideHome);
            }

            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            footstepAudio?.ResetStepPhase();
        }

        private void EnterHomeboundChildState(bool log)
        {
            if (home == null)
            {
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.45f, 1.15f);
            activity = ResidentActivity.StayingInsideHome;
            activeGarden = null;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            SetFishingLineVisible(false);
            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetWorldPresenceVisible(false);
            hiddenInsideHome = true;

            if (log)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentChildStayedInsideHome",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("age", ageYears),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }
        }

        private void ReleaseHomeboundChild()
        {
            hiddenInsideHome = false;
            SetWorldPresenceVisible(true);
            transform.position = GetHomeExitWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.45f, 1.15f);
            UseIdleSprite();
            UpdateWorldSorting();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentChildLeftHome",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", ageYears),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private Vector3 GetHomeInteriorWorld()
        {
            if (map != null && home != null)
            {
                Bounds homeBounds = map.GetCellRectWorld(home.Origin, home.Footprint);
                return new Vector3(homeBounds.center.x, homeBounds.center.y, -0.08f);
            }

            return new Vector3(transform.position.x, transform.position.y, -0.08f);
        }

        private Vector3 GetHomeExitWorld()
        {
            if (TryFindHomeExitWorld(out Vector3 exitWorld))
            {
                return exitWorld;
            }

            return GetHomeInteriorWorld();
        }

        private bool TryFindHomeExitWorld(out Vector3 exitWorld)
        {
            if (map == null || home == null)
            {
                exitWorld = default;
                return false;
            }

            Vector2Int origin = home.Origin;
            Vector2Int footprint = home.Footprint;
            Vector3 chosen = default;
            int found = 0;
            for (int radius = 1; radius <= IdleRadius; radius++)
            {
                int minX = origin.x - radius;
                int maxX = origin.x + footprint.x + radius - 1;
                int minY = origin.y - radius;
                int maxY = origin.y + footprint.y + radius - 1;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (x != minX && x != maxX && y != minY && y != maxY)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 center = map.GetCellCenterWorld(candidate.x, candidate.y);
                        Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                        center.x += jitter.x;
                        center.y += jitter.y;
                        center.z = -0.08f;
                        found++;
                        if (Random.Range(0, found) == 0)
                        {
                            chosen = center;
                        }
                    }
                }

                if (found > 0)
                {
                    exitWorld = chosen;
                    return true;
                }
            }

            exitWorld = default;
            return false;
        }

        private void SetWorldPresenceVisible(bool visible)
        {
            if (visible)
            {
                EnsureClickCollider();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = visible;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = visible;
            }

            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = visible && !deathRequested;
            }

            if (!visible)
            {
                SetCarriedLogsVisible(false);
                SetCarriedStoneVisible(false);
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
                SetCarriedForageVisible(false);
                SetFishingLineVisible(false);
            }
        }

        private void BuildDirectWorldPath(Vector3 targetWorld)
        {
            path.Clear();
            path.Add(new Vector3(targetWorld.x, targetWorld.y, -0.08f));
            pathIndex = 0;
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

                path.Add(new Vector3(center.x, center.y, -0.08f));
            }

            pathIndex = 0;
        }

        private bool UpdateAge()
        {
            ageYears += Time.deltaTime / SecondsPerYear;
            if (lifeStage == StrategyResidentLifeStage.Child && ageYears >= AdultAgeYears)
            {
                GrowUp();
            }

            int currentAge = DisplayAgeYears;
            if (currentAge <= lastMortalityAgeChecked)
            {
                return false;
            }

            for (int age = lastMortalityAgeChecked + 1; age <= currentAge; age++)
            {
                if (population != null && population.TryResolveAnnualMortality(this, age))
                {
                    return true;
                }
            }

            lastMortalityAgeChecked = currentAge;
            return false;
        }

        private void GrowUp()
        {
            lifeStage = StrategyResidentLifeStage.Adult;
            ageYears = Mathf.Max(ageYears, AdultAgeYears);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            UseIdleSprite();
            EnsureClickCollider();
            home?.EnsureHouseholder();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGrownUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", DisplayAgeYears));
        }

        private void EnsureClickCollider()
        {
            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                circle = gameObject.AddComponent<CircleCollider2D>();
            }

            circle.isTrigger = true;
            circle.offset = IsAdult ? new Vector2(0f, 0.36f) : new Vector2(0f, 0.25f);
            circle.radius = IsAdult ? 0.28f : 0.21f;
        }

        private void AnimateIdle()
        {
            UseIdleSprite();
            footstepAudio?.ResetStepPhase();
            SyncReadabilityRenderers();
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 5f) * 0.035f;
            transform.localScale = new Vector3(1f, pulse, 1f);
        }

        private void AnimateWalk()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWorkSprite = false;
            appliedWorkFrame = -1;

            if (!usingWalkSprite)
            {
                usingWalkSprite = true;
                walkFrame = 0;
                walkFrameTimer = 0f;
                appliedWalkFrame = -1;
            }

            walkFrameTimer += Time.deltaTime * WalkAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(walkFrameTimer);
            if (frameSteps > 0)
            {
                walkFrame = (walkFrame + frameSteps) % StrategyResidentSpriteFactory.WalkFrameCount;
                walkFrameTimer -= frameSteps;
            }

            if (appliedWalkFrame != walkFrame)
            {
                spriteRenderer.sprite = StrategyResidentSpriteFactory.GetWalkSprite(gender, VisualVariant, lifeStage, walkFrame);
                appliedWalkFrame = walkFrame;
                footstepAudio?.PlayWalkFrame(walkFrame, lifeStage);
                SyncReadabilityRenderers();
            }
        }

        private void EnsureFootstepAudio()
        {
            if (footstepAudio != null)
            {
                return;
            }

            footstepAudio = GetComponent<StrategyResidentFootstepAudio>();
            if (footstepAudio == null)
            {
                footstepAudio = gameObject.AddComponent<StrategyResidentFootstepAudio>();
            }

            footstepAudio.Configure(this);
        }

        private void AnimateGardenWork()
        {
            UseIdleSprite();
            SyncReadabilityRenderers();
            float swing = Mathf.Sin((Time.time + bobPhase) * 8.5f);
            float bend = Mathf.Abs(swing);
            transform.localRotation = Quaternion.Euler(0f, 0f, swing * 5.5f);
            transform.localScale = new Vector3(1f + bend * 0.035f, 0.94f - bend * 0.045f, 1f);
        }

        private void AnimateChoppingWork()
        {
            AnimateWoodcutWork(false);
        }

        private void AnimateBuckingWork()
        {
            AnimateWoodcutWork(true);
        }

        private void AnimateWoodcutWork(bool bucking)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * WoodcutAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.WoodcutFrameCount;
                    if (workFrame == WoodcutImpactFrame)
                    {
                        if (activeTree != null)
                        {
                            if (bucking)
                            {
                                activeTree.ReceiveBuckHit(transform.position);
                            }
                            else
                            {
                                activeTree.ReceiveChopHit(transform.position);
                            }
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyWoodcutFrame(workFrame);
        }

        private void AnimateWoodcutHold()
        {
            if (activeTree != null)
            {
                FaceWorldPoint(activeTree.transform.position);
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            ApplyWoodcutFrame(7);
        }

        private void ApplyWoodcutFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetWoodcutSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateStonecutWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (activeStoneDeposit != null)
            {
                FaceWorldPoint(activeStoneDeposit.transform.position);
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * StonecutAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.StonecutFrameCount;
                    if (workFrame == StonecutImpactFrame && activeStoneDeposit != null)
                    {
                        activeStoneDeposit.ReceivePickHit(this, transform.position, out int minedAmount);
                        if (minedAmount > 0)
                        {
                            ApplyStonecutFrame(workFrame);
                            StartCarryingMinedStone(minedAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyStonecutFrame(workFrame);
        }

        private void ApplyStonecutFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetStonecutSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateConstructionWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (constructionSite != null)
            {
                FaceWorldPoint(constructionSite.FootprintBounds.center);
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * ConstructionAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.ConstructionFrameCount;
                    if (workFrame == ConstructionImpactFrame && constructionSite != null)
                    {
                        constructionSite.ReceiveBuildHit(this, transform.position);
                        if (constructionSite == null || constructionSite.IsCompleted)
                        {
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyConstructionFrame(workFrame);
        }

        private void ApplyConstructionFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetConstructionSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateBowWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * BowAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.BowFrameCount;
                    if (workFrame == BowReleaseFrame && !bowShotReleased && activeHuntTarget != null)
                    {
                        bowShotReleased = true;
                        huntingWorkTimer = 0.28f;
                        StrategyHuntingArrowProjectile.Launch(GetBowWorldPosition(), activeHuntTarget, this);
                        StrategyDebugLogger.Info(
                            "Hunting",
                            "ArrowReleased",
                            StrategyDebugLogger.F("resident", FullName),
                            StrategyDebugLogger.F("rabbitWorld", activeHuntTarget.transform.position));
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyBowFrame(workFrame);
        }

        private void ApplyBowFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetBowSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateButcherWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * ButcherAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.ButcherFrameCount;
                    if (workFrame == ButcherImpactFrame && activeHuntTarget != null)
                    {
                        activeHuntTarget.ReceiveButcherHit(this, transform.position, out int gameAmount);
                        if (gameAmount > 0)
                        {
                            ApplyButcherFrame(workFrame);
                            StartCarryingGame(gameAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyButcherFrame(workFrame);
        }

        private void ApplyButcherFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetButcherSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateFishingWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * FishingAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.FishingFrameCount;
                    if (activity == ResidentActivity.CastingFishingLine && workFrame == FishingHookFrame && !fishingLineCast)
                    {
                        fishingLineCast = true;
                        bool hooked = activeFishTarget != null && activeFishTarget.ReceiveFishingHook(this, GetFishingBobberWorld());
                        if (!hooked)
                        {
                            ResetFisherWorkToIdle(false);
                            return;
                        }

                        StrategyDebugLogger.Info(
                            "Fishing",
                            "LineCast",
                            StrategyDebugLogger.F("resident", FullName),
                            StrategyDebugLogger.F("fishWorld", activeFishTarget != null ? activeFishTarget.transform.position : Vector3.zero));
                    }
                    else if (activity == ResidentActivity.ReelingFish && workFrame == FishingReelFrame && activeFishTarget != null)
                    {
                        activeFishTarget.ReceiveReelPull(this, GetFishingReelTargetWorld(), out int fishAmount);
                        if (fishAmount > 0)
                        {
                            ApplyFishingFrame(workFrame);
                            StartCarryingFish(fishAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyFishingFrame(workFrame);
            SyncFishingLineRenderer();
        }

        private void ApplyFishingFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetFishingSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private Vector3 GetFishingBobberWorld()
        {
            if (activeFishTarget != null)
            {
                if (activeFishTarget.IsHooked)
                {
                    return activeFishTarget.FishingHookWorld;
                }

                Vector3 fishWorld = activeFishTarget.transform.position;
                float bob = Mathf.Sin((Time.time + bobPhase) * 8.0f) * 0.035f;
                return new Vector3(fishWorld.x, fishWorld.y + 0.12f + bob, -0.11f);
            }

            float side = spriteRenderer != null && spriteRenderer.flipX ? -0.85f : 0.85f;
            return new Vector3(transform.position.x + side, transform.position.y + 0.10f, -0.11f);
        }

        private Vector3 GetFishingRodTipWorld()
        {
            if (spriteRenderer == null)
            {
                return new Vector3(transform.position.x, transform.position.y + 0.52f, -0.11f);
            }

            return new Vector3(
                transform.position.x + (spriteRenderer.flipX ? -0.20f : 0.20f),
                transform.position.y + 0.52f,
                -0.11f);
        }

        private Vector3 GetFishingReelTargetWorld()
        {
            Vector3 fishWorld = activeFishTarget != null
                ? activeFishTarget.transform.position
                : GetFishingBobberWorld();
            Vector3 towardFish = fishWorld - transform.position;
            towardFish.z = 0f;
            if (towardFish.sqrMagnitude < 0.001f)
            {
                float side = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                towardFish = new Vector3(side, 0f, 0f);
            }

            towardFish.Normalize();
            Vector3 target = transform.position + towardFish * 0.48f;
            return new Vector3(target.x, target.y + 0.06f, -0.068f);
        }

        private Vector3 GetBowWorldPosition()
        {
            float side = spriteRenderer != null && spriteRenderer.flipX ? -0.22f : 0.22f;
            return new Vector3(transform.position.x + side, transform.position.y + 0.42f, -0.11f);
        }

        private void AnimateLumberWork(float frequency, float angle)
        {
            UseIdleSprite();
            SyncReadabilityRenderers();
            float swing = Mathf.Sin((Time.time + bobPhase) * frequency);
            float force = Mathf.Abs(swing);
            transform.localRotation = Quaternion.Euler(0f, 0f, swing * angle);
            transform.localScale = new Vector3(1f + force * 0.045f, 0.93f - force * 0.050f, 1f);
        }

        private void UseIdleSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Sprite idleSprite = StrategyResidentSpriteFactory.GetSprite(gender, VisualVariant, lifeStage);
            if (usingWalkSprite || usingWorkSprite || spriteRenderer.sprite != idleSprite)
            {
                spriteRenderer.sprite = idleSprite;
                SyncReadabilityRenderers();
            }

            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            walkFrame = 0;
            walkFrameTimer = 0f;
            workFrame = 0;
            workFrameTimer = 0f;
        }

        private void CompleteGardenWork()
        {
            if (activeGarden == null
                || activeGarden.Owner == null
                || activeGarden.Owner.Resources == null
                || activeGarden.ProducedResource == StrategyResourceType.None)
            {
                return;
            }

            activeGarden.BoostGardenGrowthFromWork();
            StrategyDebugLogger.Info(
                "Population",
                "HouseholderGardenWorkCompleted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("gardenOrigin", activeGarden.Origin),
                StrategyDebugLogger.F("crop", activeGarden.ProducedResource));
        }

        private void CancelLumberWork()
        {
            if (this == null)
            {
                return;
            }

            if (activeTree != null)
            {
                activeTree.Release(this);
            }

            activeTree = null;

            if (activity == ResidentActivity.MovingToTree
                || activity == ResidentActivity.ChoppingTree
                || activity == ResidentActivity.BuckingTree
                || activity == ResidentActivity.MovingToLogs
                || activity == ResidentActivity.CarryingLogs
                || activity == ResidentActivity.DepositingLogs
                || activity == ResidentActivity.MovingToPlantTree
                || activity == ResidentActivity.PlantingTree)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.75f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                if (carriedLogAmount > 0 && TryStartCarriedResourceReturn("lumber_work_cancelled"))
                {
                    return;
                }

                carriedLogAmount = 0;
                SetCarriedLogsVisible(false);
                UseIdleSprite();
            }
        }

        private void CancelStoneWork()
        {
            if (this == null)
            {
                return;
            }

            if (activeStoneDeposit != null)
            {
                activeStoneDeposit.Release(this);
            }

            activeStoneDeposit = null;

            if (activity == ResidentActivity.MovingToStone
                || activity == ResidentActivity.MiningStone
                || activity == ResidentActivity.CarryingStone
                || activity == ResidentActivity.DepositingStone)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.75f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                if (carriedStoneAmount > 0 && TryStartCarriedResourceReturn("stone_work_cancelled"))
                {
                    return;
                }

                carriedStoneAmount = 0;
                SetCarriedStoneVisible(false);
                UseIdleSprite();
            }
        }

        private void CancelStorageWork(bool storeCarriedLogs)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToStoragePickup
                || activity == ResidentActivity.PickingUpStorageLogs
                || activity == ResidentActivity.CarryingLogsToStorage
                || activity == ResidentActivity.DepositingStorageLogs
                || activity == ResidentActivity.MovingToStorageStonePickup
                || activity == ResidentActivity.PickingUpStorageStone
                || activity == ResidentActivity.CarryingStoneToStorage
                || activity == ResidentActivity.DepositingStorageStone)
            {
                ResetStorageWorkToIdle(storeCarriedLogs);
            }
            else if (activeLogSource != null)
            {
                activeLogSource.ReleaseStoredLogsReservation(this);
                activeLogSource = null;
            }
            else if (activeStoneSource != null)
            {
                activeStoneSource.ReleaseStoredStoneReservation(this);
                activeStoneSource = null;
            }
        }

        private void CancelGranaryWork(bool storeCarriedFood)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToGranaryGamePickup
                || activity == ResidentActivity.PickingUpGranaryGame
                || activity == ResidentActivity.CarryingGameToGranary
                || activity == ResidentActivity.DepositingGranaryGame
                || activity == ResidentActivity.MovingToGranaryFishPickup
                || activity == ResidentActivity.PickingUpGranaryFish
                || activity == ResidentActivity.CarryingFishToGranary
                || activity == ResidentActivity.DepositingGranaryFish)
            {
                ResetGranaryWorkToIdle(storeCarriedFood);
            }
            else if (activeGameSource != null)
            {
                activeGameSource.ReleaseStoredGameReservation(this);
                activeGameSource = null;
            }
            else if (activeFishSource != null)
            {
                activeFishSource.ReleaseStoredFishReservation(this);
                activeFishSource = null;
            }
        }

        private void CancelHunterWork(bool storeCarriedGame)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToHuntingRange
                || activity == ResidentActivity.AimingBow
                || activity == ResidentActivity.WaitingForHuntHit
                || activity == ResidentActivity.MovingToHuntCarcass
                || activity == ResidentActivity.ButcheringRabbit
                || activity == ResidentActivity.CarryingGame
                || activity == ResidentActivity.DepositingGame)
            {
                ResetHunterWorkToIdle(true);
                return;
            }

            if (activeHuntTarget != null)
            {
                activeHuntTarget.ReleaseHuntReservation(this);
                activeHuntTarget = null;
            }

            if (storeCarriedGame
                && carriedGameAmount > 0
                && TryStartCarriedResourceReturn("hunter_work_cancelled"))
            {
                return;
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
        }

        private void CancelFisherWork(bool storeCarriedFish)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToFishingSpot
                || activity == ResidentActivity.CastingFishingLine
                || activity == ResidentActivity.WaitingForFishBite
                || activity == ResidentActivity.ReelingFish
                || activity == ResidentActivity.CarryingFish
                || activity == ResidentActivity.DepositingFish)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            if (activeFishTarget != null)
            {
                activeFishTarget.ReleaseFishingReservation(this);
                activeFishTarget = null;
            }

            if (storeCarriedFish
                && carriedFishAmount > 0
                && TryStartCarriedResourceReturn("fisher_work_cancelled"))
            {
                return;
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            SetFishingLineVisible(false);
        }

        private void UpdateFuneralActivity()
        {
            AnimateFuneralPose();
            if (activity == ResidentActivity.WaitingAtFuneral)
            {
                funeralTimer -= Time.deltaTime;
                if (funeralTimer <= 0f)
                {
                    StrategyDebugLogger.Warn(
                        "Funeral",
                        "ResidentFuneralDutyAutoReleased",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "waiting_timeout"));
                    EndFuneralDuty();
                }

                return;
            }

            funeralTimer -= Time.deltaTime;
            if (funeralTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingAtFuneral;
            funeralTimer = FuneralWaitingAutoReleaseSeconds;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private void AnimateFuneralPose()
        {
            footstepAudio?.ResetStepPhase();

            float pulse = Mathf.Sin((Time.time + bobPhase) * 4.2f) * 0.018f;
            if (activity == ResidentActivity.MourningCorpse
                || activity == ResidentActivity.WaitingAtFuneral)
            {
                ApplyCryingFrame();
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + bobPhase) * 4.6f) * 2.4f);
                transform.localScale = new Vector3(1f, 0.93f + pulse, 1f);
                return;
            }

            UseIdleSprite();
            SyncReadabilityRenderers();
            if (activity == ResidentActivity.BuryingGrave)
            {
                float dig = Mathf.Sin((Time.time + bobPhase) * 11f);
                transform.localRotation = Quaternion.Euler(0f, 0f, dig * 4.5f);
                transform.localScale = new Vector3(1.02f, 0.92f + pulse, 1f);
                return;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + bobPhase) * 3.1f) * 1.6f);
            transform.localScale = new Vector3(1f, 0.94f + pulse, 1f);
        }

        private void ApplyCryingFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            usingWalkSprite = false;
            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * CryingAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.CryFrameCount;
                workFrameTimer -= frameSteps;
            }

            if (appliedWorkFrame != workFrame)
            {
                spriteRenderer.sprite = StrategyResidentSpriteFactory.GetCryingSprite(
                    gender,
                    VisualVariant,
                    lifeStage,
                    workFrame);
                appliedWorkFrame = workFrame;
                SyncReadabilityRenderers();
            }
        }

        private static bool IsFuneralActivity(ResidentActivity residentActivity)
        {
            return IsFuneralMoveActivity(residentActivity)
                || IsStationaryFuneralActivity(residentActivity);
        }

        private static bool IsFuneralMoveActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToFuneral
                || residentActivity == ResidentActivity.CarryingCorpseToCemetery
                || residentActivity == ResidentActivity.MovingToBurial;
        }

        private static bool IsStationaryFuneralActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MourningCorpse
                || residentActivity == ResidentActivity.BuryingGrave
                || residentActivity == ResidentActivity.WaitingAtFuneral;
        }

        private static bool IsConstructionActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToConstructionStorage
                || residentActivity == ResidentActivity.PickingUpConstructionLogs
                || residentActivity == ResidentActivity.PickingUpConstructionStone
                || residentActivity == ResidentActivity.CarryingConstructionLogs
                || residentActivity == ResidentActivity.CarryingConstructionStone
                || residentActivity == ResidentActivity.DepositingConstructionResource
                || residentActivity == ResidentActivity.MovingToConstructionSite
                || residentActivity == ResidentActivity.BuildingConstruction;
        }

        private static bool IsForagingActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToForage
                || residentActivity == ResidentActivity.GatheringForage
                || residentActivity == ResidentActivity.MovingToLooseForagePickup
                || residentActivity == ResidentActivity.PickingUpLooseForage
                || residentActivity == ResidentActivity.CarryingForage
                || residentActivity == ResidentActivity.DepositingForage;
        }

        private static bool IsReturningCarriedResourceActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.ReturningLogsToStorage
                || residentActivity == ResidentActivity.ReturningStoneToStorage
                || residentActivity == ResidentActivity.ReturningGameToGranary
                || residentActivity == ResidentActivity.ReturningFishToGranary;
        }

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.flipX = transform.position.x > world.x;
            SyncReadabilityRenderers();
        }

        private void SetCarriedLogsVisible(bool visible)
        {
            if (!visible || carriedLogAmount <= 0)
            {
                if (carriedLogsRenderer != null)
                {
                    carriedLogsRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedLogsRenderer();
            if (carriedLogsRenderer == null)
            {
                return;
            }

            carriedLogsRenderer.gameObject.SetActive(true);
            SyncCarriedLogsRenderer();
        }

        private void SetCarriedStoneVisible(bool visible)
        {
            if (!visible || carriedStoneAmount <= 0)
            {
                if (carriedStoneRenderer != null)
                {
                    carriedStoneRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedStoneRenderer();
            if (carriedStoneRenderer == null)
            {
                return;
            }

            carriedStoneRenderer.gameObject.SetActive(true);
            SyncCarriedStoneRenderer();
        }

        private void SetCarriedGameVisible(bool visible)
        {
            if (!visible || carriedGameAmount <= 0)
            {
                if (carriedGameRenderer != null)
                {
                    carriedGameRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedGameRenderer();
            if (carriedGameRenderer == null)
            {
                return;
            }

            carriedGameRenderer.gameObject.SetActive(true);
            SyncCarriedGameRenderer();
        }

        private void SetCarriedFishVisible(bool visible)
        {
            if (!visible || carriedFishAmount <= 0)
            {
                if (carriedFishRenderer != null)
                {
                    carriedFishRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedFishRenderer();
            if (carriedFishRenderer == null)
            {
                return;
            }

            carriedFishRenderer.gameObject.SetActive(true);
            SyncCarriedFishRenderer();
        }

        private void SetCarriedForageVisible(bool visible)
        {
            if (!visible || carriedForageAmount <= 0 || carriedForageResource == StrategyResourceType.None)
            {
                if (carriedForageRenderer != null)
                {
                    carriedForageRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedForageRenderer();
            if (carriedForageRenderer == null)
            {
                return;
            }

            carriedForageRenderer.gameObject.SetActive(true);
            SyncCarriedForageRenderer();
        }

        private void SetFishingLineVisible(bool visible)
        {
            if (!visible)
            {
                if (fishingLineRenderer != null)
                {
                    fishingLineRenderer.gameObject.SetActive(false);
                }

                if (fishingBobberRenderer != null)
                {
                    fishingBobberRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureFishingRenderers();
            if (fishingLineRenderer == null || fishingBobberRenderer == null)
            {
                return;
            }

            fishingLineRenderer.gameObject.SetActive(true);
            fishingBobberRenderer.gameObject.SetActive(true);
            SyncFishingLineRenderer();
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
                GameObject shadowObject = new GameObject("Resident Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
                shadowObject.transform.localScale = new Vector3(1.05f, 0.68f, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.02f, 0.03f, 0.025f, 0.34f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Resident Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.02f, 0.04f, 0.03f, 0.70f);
            }
        }

        private void EnsureCarriedLogsRenderer()
        {
            if (spriteRenderer == null || carriedLogsRenderer != null)
            {
                return;
            }

            GameObject logsObject = new GameObject("Carried Logs");
            logsObject.transform.SetParent(transform, false);
            carriedLogsRenderer = logsObject.AddComponent<SpriteRenderer>();
            carriedLogsRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedLogsSprite();
            carriedLogsRenderer.color = Color.white;
            carriedLogsRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedStoneRenderer()
        {
            if (spriteRenderer == null || carriedStoneRenderer != null)
            {
                return;
            }

            GameObject stoneObject = new GameObject("Carried Stone");
            stoneObject.transform.SetParent(transform, false);
            carriedStoneRenderer = stoneObject.AddComponent<SpriteRenderer>();
            carriedStoneRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedStoneSprite();
            carriedStoneRenderer.color = Color.white;
            carriedStoneRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedGameRenderer()
        {
            if (spriteRenderer == null || carriedGameRenderer != null)
            {
                return;
            }

            GameObject gameObject = new GameObject("Carried Game");
            gameObject.transform.SetParent(transform, false);
            carriedGameRenderer = gameObject.AddComponent<SpriteRenderer>();
            carriedGameRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedGameSprite();
            carriedGameRenderer.color = Color.white;
            carriedGameRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedFishRenderer()
        {
            if (spriteRenderer == null || carriedFishRenderer != null)
            {
                return;
            }

            GameObject fishObject = new GameObject("Carried Fish");
            fishObject.transform.SetParent(transform, false);
            carriedFishRenderer = fishObject.AddComponent<SpriteRenderer>();
            carriedFishRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedFishSprite();
            carriedFishRenderer.color = Color.white;
            carriedFishRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedForageRenderer()
        {
            if (spriteRenderer == null || carriedForageRenderer != null)
            {
                return;
            }

            GameObject forageObject = new GameObject("Carried Forage");
            forageObject.transform.SetParent(transform, false);
            carriedForageRenderer = forageObject.AddComponent<SpriteRenderer>();
            carriedForageRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(carriedForageResource);
            carriedForageRenderer.color = Color.white;
            carriedForageRenderer.gameObject.SetActive(false);
        }

        private void EnsureFishingRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (fishingLineSprite == null)
            {
                fishingLineSprite = CreateFishingLineSprite();
            }

            if (fishingBobberSprite == null)
            {
                fishingBobberSprite = CreateFishingBobberSprite();
            }

            if (fishingLineRenderer == null)
            {
                GameObject lineObject = new GameObject("Fishing Line");
                lineObject.transform.SetParent(transform, false);
                fishingLineRenderer = lineObject.AddComponent<SpriteRenderer>();
                fishingLineRenderer.sprite = fishingLineSprite;
                fishingLineRenderer.color = new Color(0.82f, 0.88f, 0.82f, 0.72f);
                fishingLineRenderer.gameObject.SetActive(false);
            }

            if (fishingBobberRenderer == null)
            {
                GameObject bobberObject = new GameObject("Fishing Bobber");
                bobberObject.transform.SetParent(transform, false);
                fishingBobberRenderer = bobberObject.AddComponent<SpriteRenderer>();
                fishingBobberRenderer.sprite = fishingBobberSprite;
                fishingBobberRenderer.color = Color.white;
                fishingBobberRenderer.gameObject.SetActive(false);
            }
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

            SyncCarriedLogsRenderer();
            SyncCarriedStoneRenderer();
            SyncCarriedGameRenderer();
            SyncCarriedFishRenderer();
            SyncCarriedForageRenderer();
            SyncFishingLineRenderer();
        }

        private void UpdateWorldSorting()
        {
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            SyncReadabilityRenderers();
        }

        private void SyncCarriedLogsRenderer()
        {
            if (spriteRenderer == null || carriedLogsRenderer == null)
            {
                return;
            }

            carriedLogsRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedLogsSprite();
            carriedLogsRenderer.flipX = spriteRenderer.flipX;
            carriedLogsRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.14f : 0.14f;
            carriedLogsRenderer.transform.localPosition = new Vector3(side, 0.44f, -0.02f);
            carriedLogsRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedStoneRenderer()
        {
            if (spriteRenderer == null || carriedStoneRenderer == null)
            {
                return;
            }

            carriedStoneRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedStoneSprite();
            carriedStoneRenderer.flipX = spriteRenderer.flipX;
            carriedStoneRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedStoneRenderer.transform.localPosition = new Vector3(side, 0.38f, -0.02f);
            carriedStoneRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedGameRenderer()
        {
            if (spriteRenderer == null || carriedGameRenderer == null)
            {
                return;
            }

            carriedGameRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedGameSprite();
            carriedGameRenderer.flipX = spriteRenderer.flipX;
            carriedGameRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedGameRenderer.transform.localPosition = new Vector3(side, 0.40f, -0.02f);
            carriedGameRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedFishRenderer()
        {
            if (spriteRenderer == null || carriedFishRenderer == null)
            {
                return;
            }

            carriedFishRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedFishSprite();
            carriedFishRenderer.flipX = spriteRenderer.flipX;
            carriedFishRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedFishRenderer.transform.localPosition = new Vector3(side, 0.40f, -0.02f);
            carriedFishRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedForageRenderer()
        {
            if (spriteRenderer == null || carriedForageRenderer == null)
            {
                return;
            }

            carriedForageRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(carriedForageResource);
            carriedForageRenderer.flipX = spriteRenderer.flipX;
            carriedForageRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.12f : 0.12f;
            carriedForageRenderer.transform.localPosition = new Vector3(side, 0.43f, -0.02f);
            carriedForageRenderer.transform.localScale = Vector3.one;
        }

        private void SyncFishingLineRenderer()
        {
            if (spriteRenderer == null
                || fishingLineRenderer == null
                || fishingBobberRenderer == null
                || !fishingLineRenderer.gameObject.activeSelf)
            {
                return;
            }

            Vector3 rodWorld = GetFishingRodTipWorld();
            Vector3 bobberWorld = GetFishingBobberWorld();
            Vector3 midpoint = (rodWorld + bobberWorld) * 0.5f;
            Vector3 delta = bobberWorld - rodWorld;
            float distance = Mathf.Max(0.05f, delta.magnitude);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            fishingLineRenderer.transform.localPosition = transform.InverseTransformPoint(midpoint);
            fishingLineRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            fishingLineRenderer.transform.localScale = new Vector3(distance, 1f, 1f);
            fishingLineRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
            fishingBobberRenderer.transform.localPosition = transform.InverseTransformPoint(bobberWorld);
            fishingBobberRenderer.transform.localRotation = Quaternion.identity;
            fishingBobberRenderer.transform.localScale = Vector3.one;
            fishingBobberRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
        }

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 36;
            const int height = 16;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Resident Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.46f;
            float radiusY = height * 0.34f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = (dx * dx) + (dy * dy);
                    if (distance > 1f)
                    {
                        continue;
                    }

                    float alpha = Mathf.Lerp(0.12f, 0.62f, 1f - distance);
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateFishingLineSprite()
        {
            const int width = 32;
            const int height = 2;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fishing Line Sprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            Color line = new Color(0.86f, 0.91f, 0.84f, 0.72f);
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, 0, line);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateFishingBobberSprite()
        {
            const int width = 12;
            const int height = 14;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fishing Bobber Sprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            Color outline = new Color32(54, 42, 34, 255);
            Color red = new Color32(205, 48, 42, 255);
            Color white = new Color32(239, 232, 199, 255);
            for (int y = -4; y <= 4; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    if (x * x * 16 + y * y * 9 <= 144)
                    {
                        int px = width / 2 + x;
                        int py = height / 2 + y;
                        texture.SetPixel(px, py, y >= 0 ? red : white);
                    }
                }
            }

            for (int x = 3; x <= 9; x++)
            {
                texture.SetPixel(x, 2, outline);
                texture.SetPixel(x, 11, outline);
            }

            for (int y = 3; y <= 10; y++)
            {
                texture.SetPixel(3, y, outline);
                texture.SetPixel(9, y, outline);
            }

            texture.SetPixel(width / 2, 12, outline);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static bool IsUpgradeCell(Vector2Int cell, StrategyBuildingUpgrade upgrade)
        {
            return cell.x >= upgrade.Origin.x
                && cell.x < upgrade.Origin.x + upgrade.Footprint.x
                && cell.y >= upgrade.Origin.y
                && cell.y < upgrade.Origin.y + upgrade.Footprint.y;
        }

        private static string GetFallbackName(StrategyResidentGender residentGender, int visualVariant)
        {
            return residentGender == StrategyResidentGender.Male
                ? "Settler " + (visualVariant + 1)
                : "Settler " + (visualVariant + 1);
        }

        private static string ExtractFamilyName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            string[] parts = fullName.Split(' ');
            return parts.Length > 1 ? parts[parts.Length - 1] : string.Empty;
        }
    }
}
