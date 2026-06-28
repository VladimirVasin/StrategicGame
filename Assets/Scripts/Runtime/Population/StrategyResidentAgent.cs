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
    public sealed partial class StrategyResidentAgent : MonoBehaviour
    {
        private const float MoveSpeed = 0.85f;
        private const float ActiveMoveSpeedMultiplier = 1.15f;
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
        private const float HouseholdFoodPickupCooldownMin = 3.0f;
        private const float HouseholdFoodPickupCooldownMax = 6.5f;
        private const float HouseholdFoodPickupRetryCooldownMin = 2.0f;
        private const float HouseholdFoodPickupRetryCooldownMax = 4.5f;
        private const float HouseholdFoodReserveDays = 1.35f;
        private const float WalkAnimationFrameRate = 12f;
        private const float WoodcutAnimationFrameRate = 11.5f;
        private const float StonecutAnimationFrameRate = 10.5f;
        private const float CoalMineAnimationFrameRate = 9.5f;
        private const float ConstructionAnimationFrameRate = 12.5f;
        private const float BowAnimationFrameRate = 12.0f;
        private const float ButcherAnimationFrameRate = 10.5f;
        private const float FishingAnimationFrameRate = 10.5f;
        private const float CryingAnimationFrameRate = 6.5f;
        private const float SecondsPerYear = 100f;
        private const int AdultAgeYears = 16;
        private const int HomeboundChildAgeYears = 3;
        private const float AdultDailyRationNeed = 1.0f;
        private const float OlderChildDailyRationNeed = 0.7f;
        private const float YoungChildDailyRationNeed = 0.4f;
        private const float ToddlerDailyRationNeed = 0.25f;
        private const float NutritionDebtRecoveryPerFedDay = 0.75f;
        private const float MaxNutritionDebt = 7.0f;
        private const int MaxHungryDays = 14;
        private const int WoodcutImpactFrame = 5;
        private const int StonecutImpactFrame = 5;
        private const int ConstructionImpactFrame = 6;
        private const int BowReleaseFrame = 7;
        private const int ButcherImpactFrame = 5;
        private const int FishingHookFrame = 5;
        private const int FishingReelFrame = 10;
        private const float HuntingMinimumShotRange = 2.0f;
        private const float HuntingPreferredShotRange = 2.8f;
        private const float HuntingShotRange = 4.3f;
        private const float HuntingMissChance = 0.20f;
        private const int MaxHuntingStandPathChecks = 18;
        private const float HuntMoveRejectedLogCooldownSeconds = 4f;
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

        private readonly List<int> childIds = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyPlacedBuilding home;
        private StrategyLumberjackCamp workplace;
        private StrategyStonecutterCamp stoneWorkplace;
        private StrategyHunterCamp hunterWorkplace;
        private StrategyFisherHut fisherWorkplace;
        private StrategyForagerCamp foragerWorkplace;
        private StrategyMine mineWorkplace;
        private StrategyStorageYard storageWorkplace;
        private StrategyStorageYard builderWorkplace;
        private StrategyGranary granaryWorkplace;
        private StrategyConstructionSite constructionSite;
        private StrategyConstructionSite carriedConstructionReturnSite;
        private IStrategyConstructionResourceSource activeConstructionSource;
        private IStrategyProductionLogisticsNode activeProductionInputTarget;
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
        private SpriteRenderer carriedIronRenderer;
        private SpriteRenderer carriedGameRenderer;
        private SpriteRenderer carriedFishRenderer;
        private SpriteRenderer carriedForageRenderer;
        private SpriteRenderer fishingLineRenderer;
        private SpriteRenderer fishingBobberRenderer;
        private StrategyResidentFootstepAudio footstepAudio;
        private readonly List<Vector3> path = new();
        private readonly List<Vector2Int> constructionWorkCellCandidates = new();
        private ResidentActivity activity;
        private StrategyBuildingUpgrade activeGarden;
        private StrategyForageNode activeForageNode;
        private StrategyForagerCamp activeForagerCamp;
        private StrategyLooseCarriedResourcePile activeLooseForageSource;
        private StrategyForestryTree activeTree;
        private StrategyLumberjackCamp activeLogSource;
        private StrategyLooseConstructionResourcePile activeLooseLogSource;
        private StrategyStoneDeposit activeStoneDeposit;
        private StrategyStonecutterCamp activeStoneSource;
        private StrategyLooseConstructionResourcePile activeLooseStoneSource;
        private StrategyMine activeMine;
        private StrategyIronDeposit activeIronDeposit;
        private StrategyMine activeIronSource;
        private StrategyHunterCamp activeGameSource;
        private StrategyFisherHut activeFishSource;
        private StrategyForagerCamp activeForageFoodSource;
        private StrategyChickenCoop activeEggFoodSource;
        private StrategyLooseCarriedResourcePile activeLooseFoodSource;
        private StrategyGranary activeGranaryDeliveryTarget;
        private StrategyGranary activeHouseholdFoodGranary;
        private IStrategyHuntTarget activeHuntTarget;
        private StrategyFishAgent activeFishTarget;
        private StrategyConstructionResourceKind activeConstructionResource;
        private StrategyConstructionResourceKind carriedConstructionReturnResource = StrategyConstructionResourceKind.None;
        private StrategyResourceType activeProductionInputResource = StrategyResourceType.None;
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
        private float mineWorkCooldown;
        private float mineWorkTimer;
        private float logisticsWorkCooldown;
        private float huntingWorkCooldown;
        private float fishingWorkCooldown;
        private float foragerWorkCooldown;
        private float householdFoodWorkCooldown;
        private float nextHuntMoveRejectedLogTime;
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
        private int lastNutritionDayIndex = -1;
        private int carriedLogAmount;
        private int carriedStoneAmount;
        private int carriedIronAmount;
        private int carriedGameAmount;
        private int carriedFishAmount;
        private StrategyResourceType carriedHouseholdFoodResource = StrategyResourceType.None;
        private StrategyResourceType carriedForageResource = StrategyResourceType.None;
        private int carriedForageAmount;
        private int constructionPickupPathFailures;
        private float ageYears = AdultAgeYears;
        private float nutritionDebt;
        private int daysHungry;
        private bool hasTarget;
        private bool usingWalkSprite;
        private bool usingWorkSprite;
        private bool constructionFutureHome;
        private bool bowShotReleased;
        private bool fishingLineCast;
        private bool deathRequested;
        private bool hiddenInsideHome;
        private bool sleepingInsideHome;
        private bool returningHomeToSleep;
        private bool hiddenUnderground;
        private bool returnCarriedResourcesImmediately;
        private bool silentFuneralDuty;

        public StrategyPlacedBuilding Home => home;
        public StrategyLumberjackCamp Workplace => workplace;
        public StrategyStonecutterCamp StoneWorkplace => stoneWorkplace;
        public StrategyHunterCamp HunterWorkplace => hunterWorkplace;
        public StrategyFisherHut FisherWorkplace => fisherWorkplace;
        public StrategyForagerCamp ForagerWorkplace => foragerWorkplace;
        public StrategyMine MineWorkplace => mineWorkplace;
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
            || foragerWorkplace != null
            || mineWorkplace != null
            || coalPitWorkplace != null
            || clayPitWorkplace != null
            || sawmillWorkplace != null
            || kilnWorkplace != null
            || forgeWorkplace != null
            || storageWorkplace != null
            || builderWorkplace != null
            || granaryWorkplace != null;
        public bool HasWorkplace => HasExternalWorkplace || IsHouseholder;
        public bool HasConstructionAssignment => constructionSite != null;
        public bool IsAdult => lifeStage == StrategyResidentLifeStage.Adult;
        public bool CanWork => IsAdult && !IsPendingRefugee;
        public bool IsFuneralDutyActive => IsFuneralActivity(activity);
        public bool IsHouseholdForaging => IsForagingActivity(activity);
        public bool IsHouseholdFoodDuty => IsHouseholdFoodActivity(activity);
        public bool CanAcceptWorkAssignment => CanWork && !IsFuneralDutyActive && !IsHouseholdForaging && !IsHouseholdFoodDuty;
        public bool IsHomeboundYoungChild => lifeStage == StrategyResidentLifeStage.Child
            && ageYears < HomeboundChildAgeYears
            && home != null
            && !IsPendingRefugee;
        public bool IsSleepingInsideHome => sleepingInsideHome;
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
        public float DailyRationNeed => GetDailyRationNeed();
        public float NutritionDebt => nutritionDebt;
        public int DaysHungry => daysHungry;
        public int LastNutritionDayIndex => lastNutritionDayIndex;
        public int NutritionSeverityLevel => GetNutritionSeverityLevel();
        public bool IsHungry => NutritionSeverityLevel > 0;
        public bool IsStarving => NutritionSeverityLevel >= 3;
        public bool IsBirthBlockedByHunger => daysHungry >= 2 || nutritionDebt >= 1.5f;
        public float NutritionMortalityMultiplier => GetNutritionMortalityMultiplier();
        public string NutritionStatusText => GetNutritionStatusText();
        public int VisualVariant { get; private set; }
        public string FullName { get; private set; }
        public string FamilyName { get; private set; }
        public ResidentActivity Activity => activity;
        public Bounds SelectionBounds => spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, new Vector3(0.55f, 0.75f, 0f));

        public void ApplyDailyRation(float requiredRations, float suppliedRations, int dayIndex)
        {
            if (deathRequested || IsPendingRefugee)
            {
                return;
            }

            float required = Mathf.Max(0.01f, requiredRations);
            float supplied = Mathf.Clamp(suppliedRations, 0f, required);
            float deficit = required - supplied;
            int previousDaysHungry = daysHungry;
            float previousDebt = nutritionDebt;

            if (deficit <= 0.01f)
            {
                nutritionDebt = Mathf.Max(0f, nutritionDebt - NutritionDebtRecoveryPerFedDay);
                daysHungry = nutritionDebt <= 0.05f
                    ? 0
                    : Mathf.Max(0, daysHungry - 1);
            }
            else
            {
                float missingShare = Mathf.Clamp01(deficit / required);
                nutritionDebt = Mathf.Min(MaxNutritionDebt, nutritionDebt + missingShare);
                daysHungry = Mathf.Min(MaxHungryDays, daysHungry + 1);
            }

            lastNutritionDayIndex = dayIndex;
            if (previousDaysHungry != daysHungry
                || Mathf.Abs(previousDebt - nutritionDebt) > 0.01f)
            {
                StrategyDebugLogger.Info(
                    "Food",
                    "ResidentNutritionUpdated",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("day", dayIndex),
                    StrategyDebugLogger.F("required", required),
                    StrategyDebugLogger.F("supplied", supplied),
                    StrategyDebugLogger.F("deficit", deficit),
                    StrategyDebugLogger.F("nutritionDebt", nutritionDebt),
                    StrategyDebugLogger.F("daysHungry", daysHungry),
                    StrategyDebugLogger.F("status", NutritionStatusText));
            }
        }

        private float GetDailyRationNeed()
        {
            if (IsAdult)
            {
                return AdultDailyRationNeed;
            }

            if (ageYears >= 7f)
            {
                return OlderChildDailyRationNeed;
            }

            if (ageYears >= HomeboundChildAgeYears)
            {
                return YoungChildDailyRationNeed;
            }

            return ToddlerDailyRationNeed;
        }

        private int GetNutritionSeverityLevel()
        {
            if (nutritionDebt <= 0.05f && daysHungry <= 0)
            {
                return 0;
            }

            if (nutritionDebt < 1.5f && daysHungry < 2)
            {
                return 1;
            }

            if (nutritionDebt < 3.0f && daysHungry < 4)
            {
                return 2;
            }

            if (nutritionDebt < 5.0f && daysHungry < 7)
            {
                return 3;
            }

            return 4;
        }

        private float GetNutritionMortalityMultiplier()
        {
            return NutritionSeverityLevel switch
            {
                0 => 1f,
                1 => 1f,
                2 => 1.2f,
                3 => 1.8f,
                _ => 3.2f
            };
        }

        private string GetNutritionStatusText()
        {
            return NutritionSeverityLevel switch
            {
                0 => "fed",
                1 => "short rations",
                2 => "hungry",
                3 => "starving",
                _ => "severe starvation"
            };
        }

        public bool CanStartHouseholdForagingForHome(StrategyPlacedBuilding targetHome)
        {
            return false;
        }

        private float GetCurrentMoveSpeed()
        {
            return activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold
                ? MoveSpeed
                : MoveSpeed * ActiveMoveSpeedMultiplier;
        }
    }
}
