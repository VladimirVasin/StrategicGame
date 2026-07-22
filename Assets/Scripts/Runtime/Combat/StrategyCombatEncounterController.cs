using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCombatEncounterStatusKind
    {
        Ready = 0,
        ControllersUnavailable = 1,
        NoResidentAvailable = 2,
        StagingFailed = 3,
        StartFailed = 4,
        ResetComplete = 5,
        ResidentLost = 6,
        WolfDefeated = 7,
        WolfRetreating = 8,
        Running = 9
    }

    public readonly struct StrategyCombatEncounterStatus
    {
        public StrategyCombatEncounterStatus(
            StrategyCombatEncounterStatusKind kind,
            string residentName = "",
            int residentHealth = 0,
            int residentMaxHealth = 0,
            int wolfHealth = 0,
            int wolfMaxHealth = 0)
        {
            Kind = kind;
            ResidentName = residentName ?? string.Empty;
            ResidentHealth = residentHealth;
            ResidentMaxHealth = residentMaxHealth;
            WolfHealth = wolfHealth;
            WolfMaxHealth = wolfMaxHealth;
        }

        public StrategyCombatEncounterStatusKind Kind { get; }
        public string ResidentName { get; }
        public int ResidentHealth { get; }
        public int ResidentMaxHealth { get; }
        public int WolfHealth { get; }
        public int WolfMaxHealth { get; }
    }

    [DisallowMultipleComponent]
    public sealed class StrategyCombatEncounterController : MonoBehaviour
    {
        private const int WolfSpawnDistance = 6;

        private StrategyPopulationController population;
        private StrategyWildlifeController wildlife;
        private StrategyResidentAgent resident;
        private StrategyWolfAgent wolf;
        private bool running;

        public StrategyCombatEncounterStatus Status { get; private set; } =
            new(StrategyCombatEncounterStatusKind.Ready);
        public bool IsRunning => running;

        public void Configure(
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController)
        {
            population = populationController;
            wildlife = wildlifeController;
            if (!running)
            {
                SetStatus(population != null && wildlife != null
                    ? StrategyCombatEncounterStatusKind.Ready
                    : StrategyCombatEncounterStatusKind.ControllersUnavailable);
            }
        }

        public bool TryStartEncounter()
        {
            ResetEncounter(false);
            if (population == null || wildlife == null)
            {
                SetStatus(StrategyCombatEncounterStatusKind.ControllersUnavailable);
                return false;
            }

            resident = FindCombatResident();
            if (resident == null)
            {
                SetStatus(StrategyCombatEncounterStatusKind.NoResidentAvailable);
                return false;
            }

            if (!wildlife.TrySpawnCombatEncounterWolf(
                    resident,
                    WolfSpawnDistance,
                    out wolf))
            {
                SetStatus(StrategyCombatEncounterStatusKind.StagingFailed);
                resident = null;
                wolf = null;
                return false;
            }

            if (!resident.TryPrepareForCombatDuty())
            {
                wildlife.DespawnCombatEncounterWolf(wolf);
                SetStatus(StrategyCombatEncounterStatusKind.StagingFailed);
                resident = null;
                wolf = null;
                return false;
            }

            if (!resident.TryStartCombatEngagement(wolf)
                || !wildlife.TryBeginCombatEncounter(wolf, resident))
            {
                resident.CancelCombatEngagement(false);
                wildlife.DespawnCombatEncounterWolf(wolf);
                resident = null;
                wolf = null;
                SetStatus(StrategyCombatEncounterStatusKind.StartFailed);
                return false;
            }

            running = true;
            RefreshRunningStatus();
            StrategyDebugLogger.Info(
                "Combat",
                "CombatEncounterStarted",
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("residentHealth", resident.CurrentCombatHealth),
                StrategyDebugLogger.F("wolfHealth", wolf.CurrentCombatHealth));
            return true;
        }

        public void ResetEncounter(bool showStatus = true)
        {
            running = false;
            if (resident != null)
            {
                resident.CancelCombatEngagement(false);
            }

            if (wildlife != null && wolf != null)
            {
                wildlife.DespawnCombatEncounterWolf(wolf);
            }

            resident = null;
            wolf = null;
            if (showStatus)
            {
                SetStatus(StrategyCombatEncounterStatusKind.ResetComplete);
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            if (resident == null || !resident.IsCombatAlive)
            {
                running = false;
                if (wildlife != null && wolf != null)
                {
                    wildlife.DespawnCombatEncounterWolf(wolf);
                }

                resident = null;
                wolf = null;
                SetStatus(StrategyCombatEncounterStatusKind.ResidentLost);
                return;
            }

            if (wolf == null || !wolf.IsCombatAlive)
            {
                running = false;
                resident.CancelCombatEngagement(false);
                SetResidentOutcomeStatus(StrategyCombatEncounterStatusKind.WolfDefeated);
                return;
            }

            if (!wolf.CanBeCombatTargeted)
            {
                running = false;
                resident.CancelCombatEngagement(false);
                SetResidentOutcomeStatus(StrategyCombatEncounterStatusKind.WolfRetreating);
                return;
            }

            RefreshRunningStatus();
        }

        private StrategyResidentAgent FindCombatResident()
        {
            StrategyResidentAgent fallback = null;
            for (int i = 0; i < population.Residents.Count; i++)
            {
                StrategyResidentAgent candidate = population.Residents[i];
                if (candidate == null || !candidate.CanStartCombatEngagement)
                {
                    continue;
                }

                if (candidate.HunterWorkplace != null)
                {
                    return candidate;
                }

                fallback ??= candidate;
            }

            return fallback;
        }

        private void RefreshRunningStatus()
        {
            if (resident == null || wolf == null)
            {
                return;
            }

            Status = new StrategyCombatEncounterStatus(
                StrategyCombatEncounterStatusKind.Running,
                resident.FullName,
                resident.CurrentCombatHealth,
                resident.MaxCombatHealth,
                wolf.CurrentCombatHealth,
                wolf.MaxCombatHealth);
        }

        private void SetResidentOutcomeStatus(StrategyCombatEncounterStatusKind kind)
        {
            Status = new StrategyCombatEncounterStatus(
                kind,
                resident != null ? resident.FullName : string.Empty,
                resident != null ? resident.CurrentCombatHealth : 0,
                resident != null ? resident.MaxCombatHealth : 0);
        }

        private void SetStatus(StrategyCombatEncounterStatusKind kind)
        {
            Status = new StrategyCombatEncounterStatus(kind);
        }
    }
}
