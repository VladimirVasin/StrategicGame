using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyForagerCamp : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 11;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyForageResourceController forage;
        private SpriteRenderer stockRenderer;
        private object forageReservationOwner;
        private StrategyResourceType forageReservedResource = StrategyResourceType.None;
        private int forageReservedAmount;
        private int berriesStored;
        private int rootsStored;
        private int mushroomsStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int BerriesStored => berriesStored;
        public int RootsStored => rootsStored;
        public int MushroomsStored => mushroomsStored;
        public int TotalForageStored => berriesStored + rootsStored + mushroomsStored;
        public int AvailableForage => Mathf.Max(0, TotalForageStored - forageReservedAmount);
        public bool HasStorageSpace => StrategyProductionStorage.GetRemaining(TotalForageStored + forageReservedAmount) > 0;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyForageResourceController forageController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            forage = forageController != null ? forageController : StrategyForageResourceController.Active;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "ForagerCamp",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("workRadius", WorkRadius),
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
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
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
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            return AssignWorker(assigned);
        }

        public bool AssignWorker(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Count >= MaxWorkers
                || workers.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignForagerWorkplace(this);
            StrategyDebugLogger.Info(
                "ForagerCamp",
                "WorkerAssigned",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("worker", resident.FullName),
                StrategyDebugLogger.F("workerCount", workers.Count));
            return true;
        }

        public void UnassignWorkerAt(int index)
        {
            if (index < 0 || index >= workers.Count)
            {
                return;
            }

            StrategyResidentAgent worker = workers[index];
            workers.RemoveAt(index);
            if (worker != null)
            {
                StrategyDebugLogger.Info(
                    "ForagerCamp",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("campOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearForagerWorkplace(this);
            }
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
    }
}
