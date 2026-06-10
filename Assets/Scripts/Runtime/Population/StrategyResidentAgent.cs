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
        private const float GardenWorkChance = 0.45f;
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
        private const float WalkAnimationFrameRate = 12f;
        private const float WoodcutAnimationFrameRate = 11.5f;
        private const float StonecutAnimationFrameRate = 10.5f;
        private const float ConstructionAnimationFrameRate = 12.5f;
        private const float SecondsPerYear = 120f;
        private const int AdultAgeYears = 18;
        private const int WoodcutImpactFrame = 5;
        private const int StonecutImpactFrame = 5;
        private const int ConstructionImpactFrame = 6;
        private const float MovingThresholdSqr = 0.000001f;
        private const float ReadabilityOutlineScale = 1.16f;
        private static Sprite readabilityShadowSprite;
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
            MovingHome,
            MovingToGarden,
            WorkingGarden,
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
            BuildingConstruction
        }

        private readonly List<int> childIds = new();
        private CityMapController map;
        private StrategyPlacedBuilding home;
        private StrategyLumberjackCamp workplace;
        private StrategyStonecutterCamp stoneWorkplace;
        private StrategyStorageYard storageWorkplace;
        private StrategyStorageYard builderWorkplace;
        private StrategyConstructionSite constructionSite;
        private StrategyStorageYard activeConstructionStorage;
        private Vector2Int idleOrigin;
        private Vector2Int idleFootprint = Vector2Int.one;
        private StrategyResidentGender gender;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer carriedLogsRenderer;
        private SpriteRenderer carriedStoneRenderer;
        private readonly List<Vector3> path = new();
        private ResidentActivity activity;
        private StrategyBuildingUpgrade activeGarden;
        private StrategyForestryTree activeTree;
        private StrategyLumberjackCamp activeLogSource;
        private StrategyStoneDeposit activeStoneDeposit;
        private StrategyStonecutterCamp activeStoneSource;
        private StrategyConstructionResourceKind activeConstructionResource;
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
        private float walkFrameTimer;
        private float workFrameTimer;
        private float bobPhase;
        private int walkFrame;
        private int appliedWalkFrame = -1;
        private int workFrame;
        private int appliedWorkFrame = -1;
        private int carriedLogAmount;
        private int carriedStoneAmount;
        private float ageYears = AdultAgeYears;
        private bool hasTarget;
        private bool usingWalkSprite;
        private bool usingWorkSprite;
        private bool constructionFutureHome;

        public StrategyPlacedBuilding Home => home;
        public StrategyLumberjackCamp Workplace => workplace;
        public StrategyStonecutterCamp StoneWorkplace => stoneWorkplace;
        public StrategyStorageYard StorageWorkplace => storageWorkplace;
        public StrategyStorageYard BuilderWorkplace => builderWorkplace;
        public StrategyConstructionSite ConstructionSite => constructionSite;
        public bool ConstructionWillBecomeHome => constructionFutureHome;
        public bool HasWorkplace => workplace != null || stoneWorkplace != null || storageWorkplace != null || builderWorkplace != null;
        public bool HasConstructionAssignment => constructionSite != null;
        public bool IsAdult => lifeStage == StrategyResidentLifeStage.Adult;
        public bool CanWork => IsAdult;
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
        }

        public void AddChildId(int childIdentifier)
        {
            if (childIdentifier > 0 && !childIds.Contains(childIdentifier))
            {
                childIds.Add(childIdentifier);
            }
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
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            activity = ResidentActivity.Idle;
            path.Clear();
            pathIndex = 0;
            hasTarget = false;
            gardenWorkTimer = 0f;
            gardenWorkCooldown = Random.Range(2.5f, 6.5f);
            waitTimer = Random.Range(0.05f, 0.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHomeAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("homeTool", home.Tool));
        }

        public void AssignHome(StrategyPlacedBuilding newHome, Vector3 targetWorld)
        {
            AssignHome(newHome);
            if (home == newHome)
            {
                StartMovingHome(targetWorld);
            }
        }

        public void AssignConstructionSite(StrategyConstructionSite site, bool willLiveThere)
        {
            if (site == null || constructionSite == site || builderWorkplace == null || !CanWork)
            {
                return;
            }

            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            constructionSite = site;
            constructionFutureHome = willLiveThere;
            activeConstructionStorage = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
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
            activeConstructionStorage = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionFutureHome = false;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
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

            if (constructionSite != null)
            {
                constructionSite.UnregisterBuilder(this);
            }

            if (IsConstructionActivity(activity))
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.70f);
            }

            constructionSite = null;
            activeConstructionStorage = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionFutureHome = false;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        public void AssignWorkplace(StrategyLumberjackCamp camp)
        {
            if (camp == null
                || workplace == camp
                || stoneWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || constructionSite != null
                || !CanWork)
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
                || storageWorkplace != null
                || builderWorkplace != null
                || constructionSite != null
                || !CanWork)
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

        public void AssignStorageWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || storageWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || builderWorkplace != null
                || constructionSite != null
                || !CanWork)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
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

        public void AssignBuilderWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || builderWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || storageWorkplace != null
                || constructionSite != null
                || !CanWork)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
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
            if (map == null)
            {
                return;
            }

            UpdateAge();

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

            if (activity == ResidentActivity.WorkingGarden)
            {
                UpdateGardenWork();
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

                if (TryStartConstructionTask())
                {
                    return;
                }

                if (TryStartGardenTask())
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
                    else if (activity == ResidentActivity.MovingToPlantTree)
                    {
                        StartPlantingTree();
                    }
                    else
                    {
                        activity = ResidentActivity.Idle;
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

        private bool TryStartGardenTask()
        {
            if (activity != ResidentActivity.Idle
                || home == null
                || HasWorkplace
                || constructionSite != null
                || !CanWork
                || gardenWorkCooldown > 0f
                || !home.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden)
                || Random.value > GardenWorkChance)
            {
                return false;
            }

            if (!TryFindGardenWorkCell(garden, out Vector2Int workCell))
            {
                gardenWorkCooldown = Random.Range(3f, 6f);
                return false;
            }

            activeGarden = garden;
            activity = ResidentActivity.MovingToGarden;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.25f);
                gardenWorkCooldown = Random.Range(9f, 15f);
                return true;
            }

            activeGarden = null;
            activity = ResidentActivity.Idle;
            gardenWorkCooldown = Random.Range(3f, 6f);
            return false;
        }

        private bool TryStartLumberTask()
        {
            if (activity != ResidentActivity.Idle
                || workplace == null
                || stoneWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
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
                || storageWorkplace != null
                || builderWorkplace != null
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
                || builderWorkplace != null
                || !CanWork
                || logisticsWorkCooldown > 0f)
            {
                return false;
            }

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
                        out StrategyStorageYard storage,
                        out StrategyConstructionResourceKind kind,
                        out Vector2Int pickupCell))
                {
                    waitTimer = Random.Range(0.45f, 1.1f);
                    return false;
                }

                if (!TryBuildPathTo(pickupCell))
                {
                    waitTimer = Random.Range(0.45f, 1.1f);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "BuilderPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                        StrategyDebugLogger.F("resource", kind),
                        StrategyDebugLogger.F("reason", "no_path"));
                    return false;
                }

                activeConstructionStorage = storage;
                activeConstructionResource = kind;
                activity = ResidentActivity.MovingToConstructionStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.14f);
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuilderPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("yardOrigin", storage != null ? storage.Origin : Vector2Int.zero),
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
            activity = ResidentActivity.Idle;
            activeGarden = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            waitTimer = Random.Range(0.45f, 1.2f);
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

            if (activeLogSource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageLogs;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeLogSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeLogSource.Origin),
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

            if (activeLogSource == null
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeLogSource != null)
                {
                    activeLogSource.ReleaseStoredLogsReservation(this);
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

            if (!activeLogSource.TryTakeReservedLogs(this, out carriedLogAmount))
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeLogSource.Origin));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeLogSource.Origin;
            activeLogSource = null;
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

            if (activeStoneSource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageStone;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeStoneSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "StonePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeStoneSource.Origin),
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

            if (activeStoneSource == null
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeStoneSource != null)
                {
                    activeStoneSource.ReleaseStoredStoneReservation(this);
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

            if (!activeStoneSource.TryTakeReservedStone(this, out carriedStoneAmount))
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "StonePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeStoneSource.Origin));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeStoneSource.Origin;
            activeStoneSource = null;
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

        private void StartPickingUpConstructionResource()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (constructionSite == null
                || activeConstructionStorage == null
                || activeConstructionResource == StrategyConstructionResourceKind.None)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = activeConstructionResource == StrategyConstructionResourceKind.Logs
                ? ResidentActivity.PickingUpConstructionLogs
                : ResidentActivity.PickingUpConstructionStone;
            lumberWorkTimer = Random.Range(ConstructionPickupSecondsMin, ConstructionPickupSecondsMax);
            FaceWorldPoint(activeConstructionStorage.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("yardOrigin", activeConstructionStorage.Origin),
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
                || activeConstructionStorage == null
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

            if (!activeConstructionStorage.TryTakeReservedConstructionResource(constructionSite, activeConstructionResource, 1, out int amount))
            {
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("yardOrigin", activeConstructionStorage.Origin),
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

            Vector2Int yardOrigin = activeConstructionStorage.Origin;
            activeConstructionStorage = null;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderResourcePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("yardOrigin", yardOrigin),
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

        private void CompleteConstructionDelivery()
        {
            activeConstructionStorage = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            waitTimer = constructionSite != null && constructionSite.ResourcesComplete
                ? Random.Range(0.05f, 0.22f)
                : Random.Range(0.20f, 0.55f);
        }

        private void ResetLumberWorkToIdle()
        {
            activeTree = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
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

            if (storeCarriedLogs && carriedLogAmount > 0 && storageWorkplace != null)
            {
                storageWorkplace.AddLogs(carriedLogAmount);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedLogsStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", carriedLogAmount),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
            }

            if (storeCarriedLogs && carriedStoneAmount > 0 && storageWorkplace != null)
            {
                storageWorkplace.AddResource(StrategyResourceType.Stone, carriedStoneAmount);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedStoneStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", carriedStoneAmount),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
            }

            activeLogSource = null;
            activeStoneSource = null;
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

        private void ResetConstructionWorkToIdle()
        {
            activeConstructionStorage = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
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
                    activity = ResidentActivity.Idle;
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
            if (!map.TryWorldToCell(transform.position, out Vector2Int startCell)
                || !map.IsCellWalkable(startCell)
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

        private void UpdateAge()
        {
            ageYears += Time.deltaTime / SecondsPerYear;
            if (lifeStage == StrategyResidentLifeStage.Child && ageYears >= AdultAgeYears)
            {
                GrowUp();
            }
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
                SyncReadabilityRenderers();
            }
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

            activeGarden.Owner.Resources.AddResource(activeGarden.ProducedResource, 1);
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
