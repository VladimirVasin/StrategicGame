using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyAutoWorkforceController : MonoBehaviour
    {
        private const float TickInterval = 3f;
        private const float ManualOverrideSeconds = 45f;
        private const int MaxAssignmentsPerTick = 4;

        private readonly StrategyAutoWorkforceSettings settings = new();
        private readonly List<StrategyAutoWorkforceDemand> demands = new();
        private readonly List<StrategyResidentAgent> candidates = new();
        private readonly Dictionary<StrategyProfessionType, float> manualLocks = new();
        private readonly Dictionary<StrategyProfessionType, int> desiredProfessionTargets = new();
        private readonly Dictionary<StrategyProfessionType, int> coverageProfessionFloors = new();
        private StrategyConstructionSite[] cachedConstructionSites = System.Array.Empty<StrategyConstructionSite>();
        private StrategyStorageYard[] cachedStorageYards = System.Array.Empty<StrategyStorageYard>();
        private StrategyLumberjackCamp[] cachedLumberjackCamps = System.Array.Empty<StrategyLumberjackCamp>();
        private StrategyStonecutterCamp[] cachedStonecutterCamps = System.Array.Empty<StrategyStonecutterCamp>();
        private StrategyMine[] cachedMines = System.Array.Empty<StrategyMine>();
        private StrategyCoalPit[] cachedCoalPits = System.Array.Empty<StrategyCoalPit>();
        private StrategyClayPit[] cachedClayPits = System.Array.Empty<StrategyClayPit>();
        private StrategySawmill[] cachedSawmills = System.Array.Empty<StrategySawmill>();
        private StrategyKiln[] cachedKilns = System.Array.Empty<StrategyKiln>();
        private StrategyHunterCamp[] cachedHunterCamps = System.Array.Empty<StrategyHunterCamp>();
        private StrategyFisherHut[] cachedFisherHuts = System.Array.Empty<StrategyFisherHut>();
        private StrategyPopulationController population;
        private float tickTimer;
        private string lastStatus = "Auto workforce ready";

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

            tickTimer -= Time.deltaTime;
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
            RefreshWorksiteCache();
            CollectFreeCandidates();
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

            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "AutoWorkforceTick",
                StrategyDebugLogger.F("enabled", settings.Enabled),
                StrategyDebugLogger.F("demands", demands.Count),
                StrategyDebugLogger.F("freeAdults", candidates.Count),
                StrategyDebugLogger.F("released", released),
                StrategyDebugLogger.F("demandReleased", demandReleased),
                StrategyDebugLogger.F("fallbackAssigned", fallbackAssigned),
                StrategyDebugLogger.F("assigned", assigned),
                StrategyDebugLogger.F("durationMs", Mathf.RoundToInt((Time.realtimeSinceStartup - tickStartedAt) * 1000f)),
                StrategyDebugLogger.F("status", lastStatus));
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
            cachedConstructionSites = UnityEngine.Object.FindObjectsByType<StrategyConstructionSite>();
            cachedStorageYards = UnityEngine.Object.FindObjectsByType<StrategyStorageYard>();
            cachedLumberjackCamps = UnityEngine.Object.FindObjectsByType<StrategyLumberjackCamp>();
            cachedStonecutterCamps = UnityEngine.Object.FindObjectsByType<StrategyStonecutterCamp>();
            cachedMines = UnityEngine.Object.FindObjectsByType<StrategyMine>();
            cachedCoalPits = UnityEngine.Object.FindObjectsByType<StrategyCoalPit>();
            cachedClayPits = UnityEngine.Object.FindObjectsByType<StrategyClayPit>();
            cachedSawmills = UnityEngine.Object.FindObjectsByType<StrategySawmill>();
            cachedKilns = UnityEngine.Object.FindObjectsByType<StrategyKiln>();
            cachedHunterCamps = UnityEngine.Object.FindObjectsByType<StrategyHunterCamp>();
            cachedFisherHuts = UnityEngine.Object.FindObjectsByType<StrategyFisherHut>();
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

            if (typeof(T) == typeof(StrategyHunterCamp))
            {
                return (T[])(object)cachedHunterCamps;
            }

            if (typeof(T) == typeof(StrategyFisherHut))
            {
                return (T[])(object)cachedFisherHuts;
            }

            return System.Array.Empty<T>();
        }
    }
}
