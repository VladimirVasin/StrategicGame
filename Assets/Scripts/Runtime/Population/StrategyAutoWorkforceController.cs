using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyAutoWorkforceController : MonoBehaviour
    {
        private const float TickInterval = 3f;
        private const float ManualOverrideSeconds = 45f;

        private readonly StrategyAutoWorkforceSettings settings = new();
        private readonly List<StrategyAutoWorkforceDemand> demands = new();
        private readonly List<StrategyResidentAgent> candidates = new();
        private readonly Dictionary<StrategyProfessionType, float> manualLocks = new();
        private readonly Dictionary<StrategyProfessionType, int> desiredProfessionTargets = new();
        private readonly Dictionary<StrategyProfessionType, int> coverageProfessionFloors = new();
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
            CleanupManualLocks();
            CollectFreeCandidates();
            CollectDemands();
            int surplusReleased = RebalanceOverstaffedProfessions();
            int released = surplusReleased;
            demands.Sort((left, right) => right.Score.CompareTo(left.Score));

            int assigned = AssignDemandsWithRebalance(ref released, out int demandReleased);
            int fallbackAssigned = AssignIdleAdultsToBestAvailableRoles();
            assigned += fallbackAssigned;

            lastStatus = assigned > 0
                ? "Assigned " + assigned + " worker" + (assigned == 1 ? string.Empty : "s")
                : demandReleased > 0
                    ? "Released " + demandReleased + " worker" + (demandReleased == 1 ? string.Empty : "s") + " for demand"
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
    }
}
