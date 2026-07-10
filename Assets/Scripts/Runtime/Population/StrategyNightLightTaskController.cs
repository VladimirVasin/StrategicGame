using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    internal sealed partial class StrategyNightLightTaskController : MonoBehaviour
    {
        private const int BaseWorkerCount = 2;
        private const int LightsPerExtraWorker = 5;
        private const int MaxWorkerCount = 8;
        private const float AssignmentRetryInterval = 1.25f;
        private const float DuskTorchStartProgress = 1f / 3f, DuskTorchDispatchEndProgress = 0.92f;

        private readonly List<StrategyNightLightSource> sources = new();
        private readonly List<StrategyNightLightSource> availableSources = new();
        private readonly List<StrategyResidentAgent> eligibleResidents = new();
        private readonly List<StrategyResidentAgent> selectedWorkers = new();
        private readonly Dictionary<StrategyResidentAgent, Queue<StrategyNightLightSource>> queues = new();
        private readonly Dictionary<StrategyResidentAgent, float> workerStartProgress = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private int availableBuildingSourceCount;
        private int availableRoadsideSourceCount;
        private int skippedSourceCount;
        private StrategyTimeOfDayPhase lastPhase = (StrategyTimeOfDayPhase)(-1);
        private int lastEveningDayIndex = -1, lastNightDayIndex = -1;
        private int lastSkippedAssignmentDayIndex = -1;
        private int lastLightStateResetDayIndex = -1;
        private float retryTimer;

        public static StrategyNightLightTaskController Active { get; private set; }

        public void Configure(CityMapController mapController, StrategyPopulationController populationController)
        {
            map = mapController;
            population = populationController;
            Active = this;
            lastPhase = StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase;
            retryTimer = Random.Range(0f, AssignmentRetryInterval);
            StrategyDebugLogger.Info("NightLights", "Configured");
        }

        public bool TryStartNextTaskForResident(StrategyResidentAgent resident)
        {
            if (resident == null || !queues.TryGetValue(resident, out Queue<StrategyNightLightSource> queue))
            {
                return false;
            }

            while (queue.Count > 0)
            {
                StrategyNightLightSource source = queue.Dequeue();
                if (!TryStartTask(resident, source))
                {
                    continue;
                }

                return true;
            }

            queues.Remove(resident);
            workerStartProgress.Remove(resident);
            resident.SetEveningNightTorchActive(false);
            return false;
        }

        private void Update()
        {
            if (map == null || population == null)
            {
                return;
            }

            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (snapshot.Phase != lastPhase)
            {
                lastPhase = snapshot.Phase;
                if (snapshot.Phase != StrategyTimeOfDayPhase.Dusk
                    && snapshot.Phase != StrategyTimeOfDayPhase.Night)
                {
                    ClearAssignments();
                    ClearAmbientTorchCarriers();
                }
            }

            if (snapshot.Phase == StrategyTimeOfDayPhase.Dusk)
            {
                UpdateDuskTorchDuty(snapshot);
                return;
            }

            if (snapshot.Phase != StrategyTimeOfDayPhase.Night)
            {
                return;
            }

            UpdateAmbientTorchCarriers(snapshot);
            if (snapshot.DayIndex != lastNightDayIndex)
            {
                lastNightDayIndex = snapshot.DayIndex;
                if (snapshot.DayIndex != lastEveningDayIndex)
                {
                    BeginTorchDuty(snapshot, true);
                }
            }

            RetryQueuedWorkers(snapshot);
        }

        private void UpdateDuskTorchDuty(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.PhaseProgress < DuskTorchStartProgress)
            {
                return;
            }

            UpdateAmbientTorchCarriers(snapshot);
            if (snapshot.DayIndex != lastEveningDayIndex)
            {
                retryTimer -= Time.unscaledDeltaTime;
                if (retryTimer > 0f)
                {
                    return;
                }

                retryTimer = AssignmentRetryInterval;
                BeginTorchDuty(snapshot, false);
            }

            UpdateEveningTorchCarriers(snapshot);
        }

        private void RetryQueuedWorkers(StrategyCalendarSnapshot snapshot)
        {
            if (queues.Count == 0)
            {
                return;
            }

            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer > 0f)
            {
                return;
            }

            retryTimer = AssignmentRetryInterval;
            RetryReadyWorkers(snapshot);
        }

        private void BeginTorchDuty(StrategyCalendarSnapshot snapshot, bool immediateDispatch)
        {
            RefreshCinematicSources();
            ResetSourcesForEvening(snapshot);
            CollectAvailableSources();
            CollectEligibleResidents();
            ClearAssignments();

            if (availableSources.Count == 0 || eligibleResidents.Count == 0)
            {
                LogAssignmentSkipped(snapshot);
                return;
            }

            lastEveningDayIndex = snapshot.DayIndex;
            int assignedSourceCount = availableSources.Count;
            int eligibleAdultCount = eligibleResidents.Count;
            SelectWorkers();
            int workerCount = selectedWorkers.Count;
            BuildAssignments();
            BuildWorkerStartProgress(snapshot, immediateDispatch);
            if (immediateDispatch)
            {
                RetryReadyWorkers(snapshot);
            }
            else
            {
                UpdateEveningTorchCarriers(snapshot);
            }

            StrategyDebugLogger.Info(
                "NightLights",
                "NightLightAssignmentsCreated",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("phase", snapshot.PhaseLabel),
                StrategyDebugLogger.F("phaseProgress", snapshot.PhaseProgress),
                StrategyDebugLogger.F("sources", assignedSourceCount),
                StrategyDebugLogger.F("buildingSources", availableBuildingSourceCount),
                StrategyDebugLogger.F("roadsideSources", availableRoadsideSourceCount),
                StrategyDebugLogger.F("skippedSources", skippedSourceCount),
                StrategyDebugLogger.F("eligibleAdults", eligibleAdultCount),
                StrategyDebugLogger.F("workers", workerCount),
                StrategyDebugLogger.F("staggered", !immediateDispatch));
        }

        private void ResetSourcesForEvening(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.DayIndex == lastLightStateResetDayIndex)
            {
                return;
            }

            StrategyNightLightSource.CopyActiveSources(sources);
            for (int i = 0; i < sources.Count; i++)
            {
                StrategyNightLightSource source = sources[i];
                if (source != null)
                {
                    source.ResetForNight();
                }
            }

            lastLightStateResetDayIndex = snapshot.DayIndex;
        }

        private void CollectAvailableSources()
        {
            availableSources.Clear();
            availableBuildingSourceCount = 0;
            availableRoadsideSourceCount = 0;
            skippedSourceCount = 0;
            StrategyNightLightSource.CopyActiveSources(sources);
            for (int i = 0; i < sources.Count; i++)
            {
                StrategyNightLightSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                if (source.IsLit || source.IsReserved)
                {
                    continue;
                }

                if (!source.TryRefreshWorkCell(map))
                {
                    skippedSourceCount++;
                    continue;
                }

                availableSources.Add(source);
                if (source.SourceKind == StrategyNightLightSourceKind.Roadside)
                {
                    availableRoadsideSourceCount++;
                }
                else
                {
                    availableBuildingSourceCount++;
                }
            }
        }

        private void CollectEligibleResidents()
        {
            eligibleResidents.Clear();
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.CanAcceptNightLightTask)
                {
                    eligibleResidents.Add(resident);
                }
            }
        }

        private void SelectWorkers()
        {
            selectedWorkers.Clear();
            Shuffle(eligibleResidents);
            int desired = Mathf.Clamp(
                BaseWorkerCount + availableSources.Count / LightsPerExtraWorker,
                1,
                Mathf.Min(MaxWorkerCount, eligibleResidents.Count));
            for (int i = 0; i < desired; i++)
            {
                selectedWorkers.Add(eligibleResidents[i]);
            }
        }

        private void BuildAssignments()
        {
            for (int i = 0; i < selectedWorkers.Count; i++)
            {
                queues[selectedWorkers[i]] = new Queue<StrategyNightLightSource>();
            }

            int workerIndex = 0;
            while (availableSources.Count > 0 && selectedWorkers.Count > 0)
            {
                StrategyResidentAgent worker = selectedWorkers[workerIndex % selectedWorkers.Count];
                if (TryTakeNearestSource(worker, out StrategyNightLightSource source))
                {
                    queues[worker].Enqueue(source);
                }

                workerIndex++;
            }
        }

        private void BuildWorkerStartProgress(StrategyCalendarSnapshot snapshot, bool immediateDispatch)
        {
            workerStartProgress.Clear();
            if (immediateDispatch
                || snapshot.Phase != StrategyTimeOfDayPhase.Dusk
                || selectedWorkers.Count <= 0)
            {
                return;
            }

            float start = Mathf.Clamp(
                Mathf.Max(DuskTorchStartProgress, snapshot.PhaseProgress),
                DuskTorchStartProgress,
                DuskTorchDispatchEndProgress);
            float end = Mathf.Max(start, DuskTorchDispatchEndProgress);
            for (int i = 0; i < selectedWorkers.Count; i++)
            {
                StrategyResidentAgent worker = selectedWorkers[i];
                float t = selectedWorkers.Count <= 1 ? 0f : i / (float)(selectedWorkers.Count - 1);
                float jitter = selectedWorkers.Count <= 1 ? 0f : Random.Range(-0.035f, 0.035f);
                workerStartProgress[worker] = Mathf.Clamp(Mathf.Lerp(start, end, t) + jitter, start, 0.985f);
            }
        }

        private bool TryTakeNearestSource(
            StrategyResidentAgent worker,
            out StrategyNightLightSource source)
        {
            source = null;
            if (worker == null || availableSources.Count == 0)
            {
                return false;
            }

            Vector3 anchor = worker.Home != null ? worker.Home.FootprintBounds.center : worker.transform.position;
            float bestDistance = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < availableSources.Count; i++)
            {
                StrategyNightLightSource candidate = availableSources[i];
                if (candidate == null)
                {
                    continue;
                }

                float distance = (candidate.WorldPosition - anchor).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                availableSources.Clear();
                return false;
            }

            source = availableSources[bestIndex];
            availableSources.RemoveAt(bestIndex);
            return true;
        }

        private void RetryReadyWorkers(StrategyCalendarSnapshot snapshot)
        {
            selectedWorkers.Clear();
            foreach (StrategyResidentAgent resident in queues.Keys)
            {
                if (resident != null
                    && resident.CanAcceptNightLightTask
                    && IsWorkerStartReady(resident, snapshot))
                {
                    selectedWorkers.Add(resident);
                }
            }

            for (int i = 0; i < selectedWorkers.Count; i++)
            {
                TryStartNextTaskForResident(selectedWorkers[i]);
            }
        }

        private void UpdateEveningTorchCarriers(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.Phase != StrategyTimeOfDayPhase.Dusk)
            {
                return;
            }

            selectedWorkers.Clear();
            foreach (StrategyResidentAgent resident in queues.Keys)
            {
                if (resident != null && IsWorkerStartReady(resident, snapshot))
                {
                    selectedWorkers.Add(resident);
                }
            }

            for (int i = 0; i < selectedWorkers.Count; i++)
            {
                selectedWorkers[i].SetEveningNightTorchActive(true);
            }
        }

        private bool IsWorkerStartReady(StrategyResidentAgent resident, StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.Phase == StrategyTimeOfDayPhase.Night)
            {
                return true;
            }

            return snapshot.Phase == StrategyTimeOfDayPhase.Dusk
                && (!workerStartProgress.TryGetValue(resident, out float progress)
                    || snapshot.PhaseProgress >= progress);
        }

        private bool TryStartTask(StrategyResidentAgent resident, StrategyNightLightSource source)
        {
            if (resident == null
                || source == null
                || source.IsLit
                || !source.HasWorkCell
                || !source.TryReserve(resident))
            {
                return false;
            }

            if (resident.TryStartNightLightTask(source, source.WorkCell))
            {
                resident.SetEveningNightTorchActive(false);
                return true;
            }

            source.ReleaseReservation(resident);
            return false;
        }

        private void ClearAssignments()
        {
            foreach (StrategyResidentAgent resident in queues.Keys)
            {
                resident?.SetEveningNightTorchActive(false);
            }

            queues.Clear();
            workerStartProgress.Clear();
        }

        private void LogAssignmentSkipped(StrategyCalendarSnapshot snapshot)
        {
            if (lastSkippedAssignmentDayIndex == snapshot.DayIndex)
            {
                return;
            }

            lastSkippedAssignmentDayIndex = snapshot.DayIndex;
            StrategyDebugLogger.Info(
                "NightLights",
                "NightLightAssignmentSkipped",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("phase", snapshot.PhaseLabel),
                StrategyDebugLogger.F("phaseProgress", snapshot.PhaseProgress),
                StrategyDebugLogger.F("sources", availableSources.Count),
                StrategyDebugLogger.F("buildingSources", availableBuildingSourceCount),
                StrategyDebugLogger.F("roadsideSources", availableRoadsideSourceCount),
                StrategyDebugLogger.F("skippedSources", skippedSourceCount),
                StrategyDebugLogger.F("eligibleAdults", eligibleResidents.Count));
        }

    }
}
