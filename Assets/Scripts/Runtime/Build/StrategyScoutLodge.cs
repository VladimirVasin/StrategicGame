using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyScoutLodge : MonoBehaviour
    {
        public const int MaxWorkers = 1;

        private const float RejectedTargetSeconds = 20f;
        private static readonly Dictionary<Vector2Int, StrategyResidentAgent> TargetReservations = new();

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly Dictionary<Vector2Int, float> rejectedTargets = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyFogOfWarController fog;
        private StrategyResidentAgent reservationOwner;
        private Vector2Int reservedTarget;
        private bool hasReservedTarget;
        private string missionStatus = "Awaiting assignment";

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null
            ? building.FootprintBounds
            : new Bounds(transform.position, Vector3.one);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetReservations()
        {
            TargetReservations.Clear();
        }

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            fog = fogController;
            missionStatus = workers.Count > 0 ? "Planning route" : "Awaiting assignment";
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("footprint", building != null ? building.Footprint : Vector2Int.zero),
                StrategyDebugLogger.F("maxWorkers", MaxWorkers));
        }

        public bool CanAssignNextAvailableWorker()
        {
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (CanAssignWorker(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableWorker(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                if (CanAssignWorker(residents[i]))
                {
                    candidates.Add(residents[i]);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            if (AssignWorker(assigned))
            {
                return true;
            }

            assigned = null;
            return false;
        }

        public bool AssignWorker(StrategyResidentAgent resident)
        {
            if (!CanAssignWorker(resident))
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignScoutWorkplace(this);
            if (resident.ScoutWorkplace != this)
            {
                workers.Remove(resident);
                return false;
            }

            HandleScoutWorkerAssigned();
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "WorkerAssigned",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", resident.FullName));
            return true;
        }

        public void UnassignWorkerAt(int index)
        {
            if (index < 0 || index >= workers.Count)
            {
                return;
            }

            StrategyResidentAgent worker = workers[index];
            HandleScoutWorkerRemoved(worker);
            workers.RemoveAt(index);
            if (worker != null)
            {
                worker.ClearScoutWorkplace(this);
                StrategyDebugLogger.Info(
                    "ScoutLodge",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("lodgeOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName));
            }

            missionStatus = workers.Count > 0 ? "Planning route" : "Awaiting assignment";
        }

        public void UnassignWorker(StrategyResidentAgent worker)
        {
            int index = workers.IndexOf(worker);
            if (index >= 0)
            {
                UnassignWorkerAt(index);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryReserveExplorationTarget(
            StrategyResidentAgent resident,
            out Vector2Int target)
        {
            target = default;
            if (resident == null || map == null || fog == null || !workers.Contains(resident))
            {
                return false;
            }

            if (hasReservedTarget
                && reservationOwner == resident
                && IsTargetReservedBy(resident, reservedTarget))
            {
                target = reservedTarget;
                return true;
            }

            ReleaseExplorationTarget(resident);
            PruneStaleReservations();
            PruneRejectedTargets();
            if (!map.TryWorldToCell(resident.transform.position, out Vector2Int startCell)
                || !StrategyScoutTargetSelector.TrySelectTarget(
                    map.Width,
                    map.Height,
                    startCell,
                    map.IsCellWalkable,
                    fog.IsCellExplored,
                    cell => IsTargetUnavailable(resident, cell),
                    out target))
            {
                missionStatus = "No reachable frontier";
                return false;
            }

            TargetReservations[target] = resident;
            reservationOwner = resident;
            reservedTarget = target;
            hasReservedTarget = true;
            missionStatus = "Planning route";
            return true;
        }

        public bool IsTargetReservedBy(StrategyResidentAgent resident, Vector2Int target)
        {
            return resident != null
                && hasReservedTarget
                && reservationOwner == resident
                && reservedTarget == target
                && TargetReservations.TryGetValue(target, out StrategyResidentAgent owner)
                && owner == resident;
        }

        public void NotifySurveyStarted(StrategyResidentAgent resident, Vector2Int target)
        {
            if (!IsTargetReservedBy(resident, target))
            {
                return;
            }

            missionStatus = "Charting frontier";
            fog?.RequestRefresh();
        }

        public void NotifyPointOfInterestTravelStarted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Travelling to point of interest";
            }
        }

        public void NotifyPointOfInterestInvestigationStarted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Investigating point of interest";
            }
        }

        public void NotifyPointOfInterestCompleted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Planning route";
            }
        }

        public void NotifyPointOfInterestInterrupted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Planning route";
            }
        }

        public void CompleteExplorationTarget(StrategyResidentAgent resident, Vector2Int target)
        {
            if (!IsTargetReservedBy(resident, target))
            {
                return;
            }

            ReleaseExplorationTarget(resident);
            missionStatus = "Planning route";
            fog?.RequestRefresh();
        }

        public void MarkTargetUnreachable(StrategyResidentAgent resident, Vector2Int target)
        {
            if (IsTargetReservedBy(resident, target))
            {
                rejectedTargets[target] = Time.time + RejectedTargetSeconds;
            }

            ReleaseExplorationTarget(resident);
            missionStatus = "Planning route";
        }

        public void ReleaseExplorationTarget(StrategyResidentAgent resident)
        {
            if (!hasReservedTarget || (resident != null && reservationOwner != resident))
            {
                return;
            }

            if (TargetReservations.TryGetValue(reservedTarget, out StrategyResidentAgent owner)
                && owner == reservationOwner)
            {
                TargetReservations.Remove(reservedTarget);
            }

            reservationOwner = null;
            reservedTarget = default;
            hasReservedTarget = false;
            missionStatus = workers.Count > 0 ? "Planning route" : "Awaiting assignment";
        }

        public string GetHudStatusText()
        {
            return BuildExpeditionHudStatusText();
        }

        public bool CanAssignWorker(StrategyResidentAgent resident)
        {
            return resident != null
                && workers.Count < MaxWorkers
                && !workers.Contains(resident)
                && resident.CanAcceptWorkAssignment
                && !resident.HasWorkplace
                && !resident.HasConstructionAssignment;
        }

        public bool CanAppointWorker(StrategyResidentAgent resident)
        {
            return resident != null
                && workers.Count < MaxWorkers
                && !workers.Contains(resident)
                && resident.CanAcceptWorkAssignment
                && !resident.IsHouseholder
                && !resident.HasConstructionAssignment;
        }

        public bool TryAppointWorker(StrategyResidentAgent resident)
        {
            if (!CanAppointWorker(resident))
            {
                return false;
            }

            if (resident.HasExternalWorkplace
                && !resident.TryReleaseExternalWorkAssignment())
            {
                return false;
            }

            return AssignWorker(resident);
        }

        public string GetAssignmentBlockReason(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "Resident unavailable";
            }

            if (workers.Contains(resident))
            {
                return "Already assigned to this Lodge";
            }

            if (workers.Count >= MaxWorkers)
            {
                return "Scout slot already filled";
            }

            if (!resident.IsAdult)
            {
                return "Only adults can become Scouts";
            }

            if (resident.IsPendingRefugee)
            {
                return "Has not joined the settlement";
            }

            if (resident.HasConstructionAssignment)
            {
                return "Assigned to construction";
            }

            if (resident.IsHouseholder)
            {
                return "Responsible for a household";
            }

            if (CanAppointWorker(resident))
            {
                return string.Empty;
            }

            if (!resident.CanAcceptWorkAssignment)
            {
                return "Currently " + StrategyResidentHudText.GetStatusText(resident);
            }

            return string.Empty;
        }

        private bool IsTargetUnavailable(StrategyResidentAgent resident, Vector2Int target)
        {
            if (rejectedTargets.TryGetValue(target, out float rejectedUntil))
            {
                if (rejectedUntil > Time.time)
                {
                    return true;
                }

                rejectedTargets.Remove(target);
            }

            return TargetReservations.TryGetValue(target, out StrategyResidentAgent owner)
                && owner != null
                && owner != resident;
        }

        private void PruneRejectedTargets()
        {
            List<Vector2Int> expired = null;
            foreach (KeyValuePair<Vector2Int, float> pair in rejectedTargets)
            {
                if (pair.Value > Time.time)
                {
                    continue;
                }

                expired ??= new List<Vector2Int>();
                expired.Add(pair.Key);
            }

            if (expired == null)
            {
                return;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                rejectedTargets.Remove(expired[i]);
            }
        }

        private static void PruneStaleReservations()
        {
            List<Vector2Int> stale = null;
            foreach (KeyValuePair<Vector2Int, StrategyResidentAgent> pair in TargetReservations)
            {
                if (pair.Value != null)
                {
                    continue;
                }

                stale ??= new List<Vector2Int>();
                stale.Add(pair.Key);
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                TargetReservations.Remove(stale[i]);
            }
        }

        private void OnDestroy()
        {
            HandleScoutLodgeDestroyed();
            ReleaseExplorationTarget(null);
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearScoutWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
