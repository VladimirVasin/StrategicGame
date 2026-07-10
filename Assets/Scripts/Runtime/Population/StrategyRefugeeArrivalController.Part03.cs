using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRefugeeArrivalController
    {
        private const float DynamicArrivalBaseChance = 0.08f;
        private const float DynamicArrivalChancePerAdultDeficit = 0.07f;
        private const float DynamicArrivalMaxChance = 0.75f;
        private const int DynamicConstructionSlotsPerActiveSite = 1;

        private int lastDynamicArrivalRollDayIndex = int.MinValue;

        private void UpdateFirstArrivalGate()
        {
            if (!IsFirstArrivalScheduleReady())
            {
                return;
            }

            arrivalTimer -= Time.deltaTime * GetArrivalIntensity();
            if (arrivalTimer > 0f)
            {
                return;
            }

            TryStartArrival(true);
        }

        private void UpdateRecurringArrivalGate()
        {
            TryRollDynamicArrival();
            if (state != RefugeeArrivalState.Waiting)
            {
                return;
            }

            arrivalTimer -= Time.deltaTime * GetArrivalIntensity();
            if (arrivalTimer <= 0f)
            {
                TryStartArrival(false);
            }
        }

        private void TryRollDynamicArrival()
        {
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (!firstArrivalTriggered || lastDynamicArrivalRollDayIndex == calendar.DayIndex)
            {
                return;
            }

            lastDynamicArrivalRollDayIndex = calendar.DayIndex;
            RefugeeWorkforceDemandSnapshot demand = CollectDynamicWorkforceDemand();
            float intensity = GetArrivalIntensity();
            float dailyNeed = population != null ? population.GetTotalDailyRationNeed() : 0f;
            float availableFoodDays = dailyNeed > 0.01f
                ? StrategyResourceQueryService.GetFoodRations(
                    StrategyResourceStoreScope.Settlement
                    | StrategyResourceStoreScope.TemporarySettlement
                    | StrategyResourceStoreScope.Household) / dailyNeed
                : StrategyFirstYearBalance.ComfortableRefugeeFoodDays;
            float foodMultiplier = StrategyFirstYearBalance.GetRefugeeFoodMultiplier(availableFoodDays);
            float chance = GetDynamicArrivalChance(demand.AdultDeficit) * intensity * foodMultiplier;
            if (chance <= 0f)
            {
                string reason = demand.AdultDeficit <= 0 ? "no_adult_deficit" : "population_slowdown";
                LogDynamicArrivalRoll(calendar, demand, intensity, chance, -1f, false, reason);
                return;
            }

            float roll = Random.value;
            bool shouldStart = roll <= chance;
            bool started = shouldStart && TryStartArrival(false);
            string result = started ? "started" : shouldStart ? "start_failed" : "chance_failed";
            LogDynamicArrivalRoll(calendar, demand, intensity, chance, roll, started, result);
        }

        private RefugeeWorkforceDemandSnapshot CollectDynamicWorkforceDemand()
        {
            int cappedWorkSlots = 0;
            int filledCappedSlots = 0;
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyLumberjackCamp>(),
                StrategyLumberjackCamp.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyStonecutterCamp>(),
                StrategyStonecutterCamp.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyMine>(),
                StrategyMine.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyCoalPit>(),
                StrategyCoalPit.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyClayPit>(),
                StrategyClayPit.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategySawmill>(),
                StrategySawmill.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyKiln>(),
                StrategyKiln.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyForge>(),
                StrategyForge.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyHunterCamp>(),
                StrategyHunterCamp.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyFisherHut>(),
                StrategyFisherHut.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyForagerCamp>(),
                StrategyForagerCamp.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);
            AddCappedWorkerSlots(
                Object.FindObjectsByType<StrategyGranary>(),
                StrategyGranary.MaxWorkers,
                site => site.WorkerCount,
                ref cappedWorkSlots,
                ref filledCappedSlots);

            CountConstructionWorkSlots(out int constructionSlots, out int filledConstructionSlots, out int activeConstructionSites);
            int workSlots = cappedWorkSlots + constructionSlots;
            int filledWorkSlots = filledCappedSlots + filledConstructionSlots;
            return new RefugeeWorkforceDemandSnapshot(
                population != null ? population.AdultResidentCount : 0,
                cappedWorkSlots,
                filledCappedSlots,
                constructionSlots,
                filledConstructionSlots,
                activeConstructionSites,
                workSlots,
                filledWorkSlots);
        }

        private static void AddCappedWorkerSlots<T>(
            T[] sites,
            int maxWorkers,
            System.Func<T, int> getWorkerCount,
            ref int workSlots,
            ref int filledSlots)
            where T : UnityEngine.Object
        {
            if (sites == null || getWorkerCount == null)
            {
                return;
            }

            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                if (site == null)
                {
                    continue;
                }

                workSlots += maxWorkers;
                filledSlots += Mathf.Clamp(getWorkerCount(site), 0, maxWorkers);
            }
        }

        private static void CountConstructionWorkSlots(
            out int constructionSlots,
            out int filledConstructionSlots,
            out int activeConstructionSites)
        {
            constructionSlots = 0;
            filledConstructionSlots = 0;
            activeConstructionSites = 0;
            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                activeConstructionSites++;
                constructionSlots += DynamicConstructionSlotsPerActiveSite;
                filledConstructionSlots += Mathf.Clamp(site.BuilderCount, 0, DynamicConstructionSlotsPerActiveSite);
            }
        }

        private static float GetDynamicArrivalChance(int adultDeficit)
        {
            if (adultDeficit <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp(
                DynamicArrivalBaseChance + adultDeficit * DynamicArrivalChancePerAdultDeficit,
                0f,
                DynamicArrivalMaxChance);
        }

        private void LogDynamicArrivalRoll(
            StrategyCalendarSnapshot calendar,
            RefugeeWorkforceDemandSnapshot demand,
            float intensity,
            float chance,
            float roll,
            bool started,
            string result)
        {
            StrategyDebugLogger.Info(
                "Refugees",
                "DynamicArrivalRoll",
                StrategyDebugLogger.F("day", calendar.DisplayDay),
                StrategyDebugLogger.F("phase", calendar.PhaseLabel),
                StrategyDebugLogger.F("adults", demand.AdultCount),
                StrategyDebugLogger.F("workSlots", demand.WorkSlots),
                StrategyDebugLogger.F("openWorkSlots", demand.OpenWorkSlots),
                StrategyDebugLogger.F("adultDeficit", demand.AdultDeficit),
                StrategyDebugLogger.F("cappedSlots", demand.CappedWorkSlots),
                StrategyDebugLogger.F("constructionSlots", demand.ConstructionSlots),
                StrategyDebugLogger.F("activeConstructionSites", demand.ActiveConstructionSites),
                StrategyDebugLogger.F("intensity", intensity),
                StrategyDebugLogger.F("chance", chance),
                StrategyDebugLogger.F("roll", roll),
                StrategyDebugLogger.F("started", started),
                StrategyDebugLogger.F("result", result));
        }

        private void ScheduleNextArrival()
        {
            arrivalTimer = Random.Range(RepeatArrivalMinSeconds, RepeatArrivalMaxSeconds);
            state = RefugeeArrivalState.Waiting;
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivalScheduled",
                StrategyDebugLogger.F("initial", false),
                StrategyDebugLogger.F("population", population != null ? population.TotalResidentCount : 0),
                StrategyDebugLogger.F("intensity", GetArrivalIntensity()),
                StrategyDebugLogger.F("seconds", arrivalTimer));
        }

        private float GetArrivalIntensity()
        {
            int count = population != null ? population.TotalResidentCount : 0;
            if (count >= PopulationHardCap)
            {
                return 0f;
            }

            float populationMultiplier = count <= PopulationSlowdownStart
                ? 1f
                : Mathf.Clamp01((PopulationHardCap - count) / (float)(PopulationHardCap - PopulationSlowdownStart));
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            int housingSlots = population != null ? population.GetAvailableHousingSlots() : 0;
            return populationMultiplier
                * StrategyFirstYearBalance.GetRefugeeSeasonMultiplier(calendar.Season)
                * StrategyFirstYearBalance.GetRefugeeHousingMultiplier(housingSlots);
        }

        private void LogFirstArrivalWaiting()
        {
            loggedWaitingForFirstArrival = true;
            arrivalTimer = 0f;
            StrategyDebugLogger.Info(
                "Refugees",
                "FirstArrivalWaitingForSchedule",
                StrategyDebugLogger.F("targetDay", FirstArrivalDayIndex + 1),
                StrategyDebugLogger.F("targetPhase", StrategyTimeOfDayPhase.Dusk),
                StrategyDebugLogger.F("currentDay", StrategyDayNightCycleController.CurrentCalendarSnapshot.DisplayDay),
                StrategyDebugLogger.F("currentPhase", StrategyDayNightCycleController.CurrentCalendarSnapshot.PhaseLabel));
        }

        private static bool IsFirstArrivalScheduleReady()
        {
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (snapshot.DayIndex > FirstArrivalDayIndex)
            {
                return true;
            }

            return snapshot.DayIndex == FirstArrivalDayIndex
                && (snapshot.Phase == StrategyTimeOfDayPhase.Dusk
                    || snapshot.Phase == StrategyTimeOfDayPhase.Night);
        }

        private readonly struct RefugeeWorkforceDemandSnapshot
        {
            public RefugeeWorkforceDemandSnapshot(
                int adultCount,
                int cappedWorkSlots,
                int filledCappedSlots,
                int constructionSlots,
                int filledConstructionSlots,
                int activeConstructionSites,
                int workSlots,
                int filledWorkSlots)
            {
                AdultCount = adultCount;
                CappedWorkSlots = cappedWorkSlots;
                FilledCappedSlots = filledCappedSlots;
                ConstructionSlots = constructionSlots;
                FilledConstructionSlots = filledConstructionSlots;
                ActiveConstructionSites = activeConstructionSites;
                WorkSlots = workSlots;
                FilledWorkSlots = filledWorkSlots;
                OpenWorkSlots = Mathf.Max(0, workSlots - filledWorkSlots);
                AdultDeficit = Mathf.Max(0, workSlots - adultCount);
            }

            public int AdultCount { get; }
            public int CappedWorkSlots { get; }
            public int FilledCappedSlots { get; }
            public int ConstructionSlots { get; }
            public int FilledConstructionSlots { get; }
            public int ActiveConstructionSites { get; }
            public int WorkSlots { get; }
            public int FilledWorkSlots { get; }
            public int OpenWorkSlots { get; }
            public int AdultDeficit { get; }
        }
    }
}
