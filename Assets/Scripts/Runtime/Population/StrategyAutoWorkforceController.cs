using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyAutoWorkforceController : MonoBehaviour
    {
        private const float TickInterval = 3f;
        private const float WorksiteCacheRefreshInterval = 8f;
        private const float DemandLogInterval = 12f;
        private const float ManualOverrideSeconds = 45f;
        private const float NoFreeAdultFullScanRetrySeconds = 18f;
        private const int SlowTickLogThresholdMs = 16;
        private const int MaxAssignmentsPerTick = 4;

        private readonly StrategyAutoWorkforceSettings settings = new();
        private readonly List<StrategyAutoWorkforceDemand> demands = new();
        private readonly List<StrategyResidentAgent> candidates = new();
        private readonly Dictionary<StrategyProfessionType, float> manualLocks = new();
        private readonly Dictionary<string, float> demandLogTimes = new();
        private readonly Dictionary<StrategyProfessionType, int> desiredProfessionTargets = new();
        private readonly Dictionary<StrategyProfessionType, int> coverageProfessionFloors = new();
        private readonly List<Component> demandSiteScratch = new();
        private StrategyConstructionSite[] cachedConstructionSites = System.Array.Empty<StrategyConstructionSite>();
        private StrategyStorageYard[] cachedStorageYards = System.Array.Empty<StrategyStorageYard>();
        private StrategyLumberjackCamp[] cachedLumberjackCamps = System.Array.Empty<StrategyLumberjackCamp>();
        private StrategyStonecutterCamp[] cachedStonecutterCamps = System.Array.Empty<StrategyStonecutterCamp>();
        private StrategyMine[] cachedMines = System.Array.Empty<StrategyMine>();
        private StrategyCoalPit[] cachedCoalPits = System.Array.Empty<StrategyCoalPit>();
        private StrategyClayPit[] cachedClayPits = System.Array.Empty<StrategyClayPit>();
        private StrategySawmill[] cachedSawmills = System.Array.Empty<StrategySawmill>();
        private StrategyKiln[] cachedKilns = System.Array.Empty<StrategyKiln>();
        private StrategyForge[] cachedForges = System.Array.Empty<StrategyForge>();
        private StrategyHunterCamp[] cachedHunterCamps = System.Array.Empty<StrategyHunterCamp>();
        private StrategyFisherHut[] cachedFisherHuts = System.Array.Empty<StrategyFisherHut>();
        private StrategyForagerCamp[] cachedForagerCamps = System.Array.Empty<StrategyForagerCamp>();
        private StrategyChickenCoop[] cachedChickenCoops = System.Array.Empty<StrategyChickenCoop>();
        private StrategyGranary[] cachedGranaries = System.Array.Empty<StrategyGranary>();
        private StrategyPlacedBuilding[] cachedPlacedBuildings = System.Array.Empty<StrategyPlacedBuilding>();
        private StrategyPopulationController population;
        private float tickTimer;
        private float nextWorksiteCacheRefreshTime;
        private float nextNoFreeAdultFullScanTime;
        private float nextAutoWorkforceStatusLogTime;
        private string lastStatus = "Auto workforce ready";
        private string lastLoggedTickStatus = string.Empty;
        private string lastLoggedTickReason = string.Empty;

        public StrategyAutoWorkforceSettings Settings => settings;
        public bool IsAutoAssignEnabled => settings.Enabled;
        public string LastStatus => lastStatus;

        public void Configure(StrategyPopulationController populationController)
        {
            population = populationController != null
                ? populationController
                : population ?? Object.FindAnyObjectByType<StrategyPopulationController>();
            tickTimer = 0f;
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "Configured",
                StrategyDebugLogger.F("enabled", settings.Enabled),
                StrategyDebugLogger.F("hasPopulation", population != null));
        }

        private void Awake()
        {
            Configure(null);
        }

        private void Update()
        {
            if (population == null)
            {
                population = Object.FindAnyObjectByType<StrategyPopulationController>();
            }

            if (population == null || !settings.Enabled)
            {
                return;
            }

            tickTimer -= Time.unscaledDeltaTime;
            if (tickTimer > 0f)
            {
                return;
            }

            tickTimer = TickInterval;
            RunAssignmentTick();
        }

        public void SetAutoAssignEnabled(bool enabled)
        {
            if (settings.Enabled == enabled)
            {
                return;
            }

            settings.SetEnabled(enabled);
            tickTimer = 0f;
            nextWorksiteCacheRefreshTime = 0f;
            ResetNoDonorSearchCooldown();
            ResetNoFreeAdultFullScanCooldown();
            lastStatus = enabled ? "Auto assign enabled" : "Auto assign disabled";
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "AutoAssignToggled",
                StrategyDebugLogger.F("enabled", enabled));
        }

        public int AdjustPriority(StrategyAutoWorkforceCategory category, int delta)
        {
            int value = settings.AdjustPriority(category, delta);
            tickTimer = 0f;
            nextWorksiteCacheRefreshTime = 0f;
            ResetNoDonorSearchCooldown();
            ResetNoFreeAdultFullScanCooldown();
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "PriorityChanged",
                StrategyDebugLogger.F("category", category),
                StrategyDebugLogger.F("priority", value));
            return value;
        }

        public void RegisterManualOverride(StrategyProfessionType profession)
        {
            manualLocks[profession] = Time.time + ManualOverrideSeconds;
            tickTimer = 0f;
            nextWorksiteCacheRefreshTime = 0f;
            ResetNoDonorSearchCooldown();
            ResetNoFreeAdultFullScanCooldown();
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "ManualOverrideRegistered",
                StrategyDebugLogger.F("profession", profession),
                StrategyDebugLogger.F("seconds", ManualOverrideSeconds));
        }

        public bool IsProfessionManualLocked(StrategyProfessionType profession)
        {
            return manualLocks.TryGetValue(profession, out float until) && until > Time.time;
        }

        private void RunAssignmentTick()
        {
            float tickStartedAt = Time.realtimeSinceStartup;
            CleanupManualLocks();
            CollectFreeCandidates();
            if (candidates.Count <= 0
                && (IsNoDonorSearchCooldownActive() || IsNoFreeAdultFullScanCooldownActive()))
            {
                string reason = IsNoDonorSearchCooldownActive()
                    ? "donor_retry_cooldown"
                    : "no_free_adult_full_scan_cooldown";
                lastStatus = "No free adults";
                LogAssignmentTick(tickStartedAt, demands.Count, candidates.Count, 0, 0, 0, 0, reason);
                return;
            }

            RefreshWorksiteCacheIfDue(false);
            CollectDemands();
            int disabledReleased = ReleaseDisabledProfessionWorkers();
            int demandShortfall = Mathf.Max(0, CountUnfilledDemand() - candidates.Count);
            int surplusReleased = demandShortfall > 0 ? RebalanceOverstaffedProfessions(demandShortfall) : 0;
            int released = disabledReleased + surplusReleased;
            demands.Sort((left, right) => right.Score.CompareTo(left.Score));

            int assignmentBudget = MaxAssignmentsPerTick;
            int assigned = AssignDemandsWithRebalance(ref released, out int demandReleased, ref assignmentBudget);
            bool allowOverTargetFallback = surplusReleased <= 0 && demandReleased <= 0;
            int fallbackAssigned = assignmentBudget > 0
                ? AssignIdleAdultsToBestAvailableRoles(allowOverTargetFallback, ref assignmentBudget)
                : 0;
            assigned += fallbackAssigned;

            lastStatus = assigned > 0
                ? "Assigned " + assigned + " worker" + (assigned == 1 ? string.Empty : "s")
                : demandReleased > 0
                    ? "Released " + demandReleased + " worker" + (demandReleased == 1 ? string.Empty : "s") + " for demand"
                    : disabledReleased > 0
                    ? "Released " + disabledReleased + " disabled worker" + (disabledReleased == 1 ? string.Empty : "s")
                    : surplusReleased > 0
                    ? "Released " + surplusReleased + " surplus worker" + (surplusReleased == 1 ? string.Empty : "s")
                    : candidates.Count > 0 ? "No enabled workforce slot" : "No free adults";
            if (candidates.Count <= 0 && released <= 0 && assigned <= 0)
            {
                RegisterNoFreeAdultFullScanCooldown();
            }

            LogAssignmentTick(tickStartedAt, demands.Count, candidates.Count, released, demandReleased, fallbackAssigned, assigned, string.Empty);
        }

        private void LogAssignmentTick(
            float tickStartedAt,
            int demandCount,
            int freeAdults,
            int released,
            int demandReleased,
            int fallbackAssigned,
            int assigned,
            string reason)
        {
            int durationMs = Mathf.RoundToInt((Time.realtimeSinceStartup - tickStartedAt) * 1000f);
            bool changed = lastStatus != lastLoggedTickStatus || reason != lastLoggedTickReason;
            bool hasAction = released > 0 || demandReleased > 0 || fallbackAssigned > 0 || assigned > 0;
            bool hasWorkContext = demandCount > 0 || freeAdults > 0;
            bool periodic = hasWorkContext && Time.realtimeSinceStartup >= nextAutoWorkforceStatusLogTime;
            if (!hasAction && !changed && !periodic && durationMs < SlowTickLogThresholdMs)
            {
                return;
            }

            lastLoggedTickStatus = lastStatus;
            lastLoggedTickReason = reason;
            nextAutoWorkforceStatusLogTime = Time.realtimeSinceStartup + DemandLogInterval;
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "AutoWorkforceTick",
                StrategyDebugLogger.F("enabled", settings.Enabled),
                StrategyDebugLogger.F("demands", demandCount),
                StrategyDebugLogger.F("freeAdults", freeAdults),
                StrategyDebugLogger.F("released", released),
                StrategyDebugLogger.F("demandReleased", demandReleased),
                StrategyDebugLogger.F("fallbackAssigned", fallbackAssigned),
                StrategyDebugLogger.F("assigned", assigned),
                StrategyDebugLogger.F("durationMs", durationMs),
                StrategyDebugLogger.F("status", lastStatus),
                StrategyDebugLogger.F("reason", reason));
        }

        private void CleanupManualLocks()
        {
            if (manualLocks.Count <= 0)
            {
                return;
            }

            List<StrategyProfessionType> expired = new();
            foreach (KeyValuePair<StrategyProfessionType, float> pair in manualLocks)
            {
                if (pair.Value <= Time.time)
                {
                    expired.Add(pair.Key);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                manualLocks.Remove(expired[i]);
            }
        }

        private void CollectFreeCandidates()
        {
            candidates.Clear();
            if (population == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    candidates.Add(resident);
                }
            }
        }

        private void RefreshWorksiteCache()
        {
            cachedConstructionSites = FindSceneObjects<StrategyConstructionSite>();
            cachedStorageYards = FindSceneObjects<StrategyStorageYard>();
            cachedLumberjackCamps = FindSceneObjects<StrategyLumberjackCamp>();
            cachedStonecutterCamps = FindSceneObjects<StrategyStonecutterCamp>();
            cachedMines = FindSceneObjects<StrategyMine>();
            cachedCoalPits = FindSceneObjects<StrategyCoalPit>();
            cachedClayPits = FindSceneObjects<StrategyClayPit>();
            cachedSawmills = FindSceneObjects<StrategySawmill>();
            cachedKilns = FindSceneObjects<StrategyKiln>();
            cachedForges = FindSceneObjects<StrategyForge>();
            cachedHunterCamps = FindSceneObjects<StrategyHunterCamp>();
            cachedFisherHuts = FindSceneObjects<StrategyFisherHut>();
            cachedForagerCamps = FindSceneObjects<StrategyForagerCamp>();
            cachedChickenCoops = FindSceneObjects<StrategyChickenCoop>();
            cachedGranaries = FindSceneObjects<StrategyGranary>();
            cachedPlacedBuildings = FindSceneObjects<StrategyPlacedBuilding>();
        }

        private static T[] FindSceneObjects<T>()
            where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
        }

        private void RefreshWorksiteCacheIfDue(bool force)
        {
            float now = Time.unscaledTime;
            if (!force && now < nextWorksiteCacheRefreshTime)
            {
                return;
            }

            RefreshWorksiteCache();
            nextWorksiteCacheRefreshTime = now + WorksiteCacheRefreshInterval;
        }

        private bool IsNoFreeAdultFullScanCooldownActive()
        {
            return Time.realtimeSinceStartup < nextNoFreeAdultFullScanTime;
        }

        private void RegisterNoFreeAdultFullScanCooldown()
        {
            nextNoFreeAdultFullScanTime = Time.realtimeSinceStartup + NoFreeAdultFullScanRetrySeconds;
        }

        private void ResetNoFreeAdultFullScanCooldown()
        {
            nextNoFreeAdultFullScanTime = 0f;
        }

        private T[] GetCachedSites<T>()
            where T : Component
        {
            if (typeof(T) == typeof(StrategyConstructionSite))
            {
                return (T[])(object)cachedConstructionSites;
            }

            if (typeof(T) == typeof(StrategyStorageYard))
            {
                return (T[])(object)cachedStorageYards;
            }

            if (typeof(T) == typeof(StrategyLumberjackCamp))
            {
                return (T[])(object)cachedLumberjackCamps;
            }

            if (typeof(T) == typeof(StrategyStonecutterCamp))
            {
                return (T[])(object)cachedStonecutterCamps;
            }

            if (typeof(T) == typeof(StrategyMine))
            {
                return (T[])(object)cachedMines;
            }

            if (typeof(T) == typeof(StrategyCoalPit))
            {
                return (T[])(object)cachedCoalPits;
            }

            if (typeof(T) == typeof(StrategyClayPit))
            {
                return (T[])(object)cachedClayPits;
            }

            if (typeof(T) == typeof(StrategySawmill))
            {
                return (T[])(object)cachedSawmills;
            }

            if (typeof(T) == typeof(StrategyKiln))
            {
                return (T[])(object)cachedKilns;
            }

            if (typeof(T) == typeof(StrategyForge))
            {
                return (T[])(object)cachedForges;
            }

            if (typeof(T) == typeof(StrategyHunterCamp))
            {
                return (T[])(object)cachedHunterCamps;
            }

            if (typeof(T) == typeof(StrategyFisherHut))
            {
                return (T[])(object)cachedFisherHuts;
            }

            if (typeof(T) == typeof(StrategyForagerCamp))
            {
                return (T[])(object)cachedForagerCamps;
            }

            if (typeof(T) == typeof(StrategyGranary))
            {
                return (T[])(object)cachedGranaries;
            }

            return System.Array.Empty<T>();
        }
    }
}
