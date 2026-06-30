using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    internal sealed class StrategyNightLightTaskController : MonoBehaviour
    {
        private const int BaseWorkerCount = 2;
        private const int LightsPerExtraWorker = 5;
        private const int MaxWorkerCount = 8;
        private const float AssignmentRetryInterval = 1.25f;

        private readonly List<StrategyNightLightSource> sources = new();
        private readonly List<StrategyNightLightSource> availableSources = new();
        private readonly List<StrategyResidentAgent> eligibleResidents = new();
        private readonly List<StrategyResidentAgent> selectedWorkers = new();
        private readonly Dictionary<StrategyResidentAgent, Queue<StrategyNightLightSource>> queues = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private int availableBuildingSourceCount;
        private int availableRoadsideSourceCount;
        private int skippedSourceCount;
        private StrategyTimeOfDayPhase lastPhase = (StrategyTimeOfDayPhase)(-1);
        private int lastNightDayIndex = -1;
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
                if (snapshot.Phase == StrategyTimeOfDayPhase.Night
                    && snapshot.DayIndex != lastNightDayIndex)
                {
                    lastNightDayIndex = snapshot.DayIndex;
                    BeginNight(snapshot);
                }
                else if (snapshot.Phase != StrategyTimeOfDayPhase.Night)
                {
                    queues.Clear();
                }
            }

            if (snapshot.Phase != StrategyTimeOfDayPhase.Night || queues.Count == 0)
            {
                return;
            }

            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer > 0f)
            {
                return;
            }

            retryTimer = AssignmentRetryInterval;
            RetryIdleWorkers();
        }

        private void BeginNight(StrategyCalendarSnapshot snapshot)
        {
            RefreshCinematicSources();
            CollectAvailableSources();
            CollectEligibleResidents();
            queues.Clear();

            if (availableSources.Count == 0 || eligibleResidents.Count == 0)
            {
                StrategyDebugLogger.Info(
                    "NightLights",
                    "NightLightAssignmentSkipped",
                    StrategyDebugLogger.F("day", snapshot.DisplayDay),
                    StrategyDebugLogger.F("sources", availableSources.Count),
                    StrategyDebugLogger.F("buildingSources", availableBuildingSourceCount),
                    StrategyDebugLogger.F("roadsideSources", availableRoadsideSourceCount),
                    StrategyDebugLogger.F("skippedSources", skippedSourceCount),
                    StrategyDebugLogger.F("eligibleAdults", eligibleResidents.Count));
                return;
            }

            int assignedSourceCount = availableSources.Count;
            int eligibleAdultCount = eligibleResidents.Count;
            SelectWorkers();
            int workerCount = selectedWorkers.Count;
            BuildAssignments();
            RetryIdleWorkers();
            StrategyDebugLogger.Info(
                "NightLights",
                "NightLightAssignmentsCreated",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("sources", assignedSourceCount),
                StrategyDebugLogger.F("buildingSources", availableBuildingSourceCount),
                StrategyDebugLogger.F("roadsideSources", availableRoadsideSourceCount),
                StrategyDebugLogger.F("skippedSources", skippedSourceCount),
                StrategyDebugLogger.F("eligibleAdults", eligibleAdultCount),
                StrategyDebugLogger.F("workers", workerCount));
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

                source.ResetForNight();
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

        private void RetryIdleWorkers()
        {
            selectedWorkers.Clear();
            foreach (StrategyResidentAgent resident in queues.Keys)
            {
                if (resident != null && resident.CanAcceptNightLightTask)
                {
                    selectedWorkers.Add(resident);
                }
            }

            for (int i = 0; i < selectedWorkers.Count; i++)
            {
                TryStartNextTaskForResident(selectedWorkers[i]);
            }
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
                return true;
            }

            source.ReleaseReservation(resident);
            return false;
        }

        private static void RefreshCinematicSources()
        {
            StrategyCinematicVisualController visuals = Object.FindAnyObjectByType<StrategyCinematicVisualController>();
            visuals?.RefreshSceneLightingNow();
            EnsureRoadsideNightLightSources();
        }

        private static void EnsureRoadsideNightLightSources()
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            StrategyRoadsideLightSource[] roadsideLights = Object.FindObjectsByType<StrategyRoadsideLightSource>();
            for (int i = 0; i < roadsideLights.Length; i++)
            {
                StrategyRoadsideLightSource roadsideLight = roadsideLights[i];
                if (roadsideLight == null)
                {
                    continue;
                }

                if (!roadsideLight.TryGetComponent(out StrategyCinematicLightEmitter emitter))
                {
                    emitter = roadsideLight.gameObject.AddComponent<StrategyCinematicLightEmitter>();
                }

                emitter.ConfigureForRoadsideLight(roadsideLight);
                if (!roadsideLight.TryGetComponent(out StrategyNightLightSource source))
                {
                    source = roadsideLight.gameObject.AddComponent<StrategyNightLightSource>();
                }

                source.ConfigureForRoadside(roadsideLight);
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
