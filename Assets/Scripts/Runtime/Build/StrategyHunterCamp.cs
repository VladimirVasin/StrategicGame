using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyHunterCamp : MonoBehaviour, IStrategyResourceStoreOwner
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 11;
        private const float RejectedRabbitTargetCooldownSeconds = 18f;
        private const float RejectedRabbitTargetLogCooldownSeconds = 4f;

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly Dictionary<StrategyRabbitAgent, float> rejectedRabbitTargetUntil = new();
        private readonly Dictionary<Vector2Int, float> rejectedRabbitCellUntil = new();
        private readonly List<StrategyRabbitAgent> rejectedRabbitTargetCleanup = new();
        private readonly List<Vector2Int> rejectedRabbitCellCleanup = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyWildlifeController wildlife;
        private SpriteRenderer stockRenderer;
        private object gameReservationOwner;
        private float nextRejectedRabbitTargetLogTime;
        private int reservedGame;
        private readonly StrategyResourceStore resourceStore = new();
        private ref int gameStored => ref resourceStore.GetAmountRef(StrategyResourceType.Game);

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int GameStored => gameStored;
        public int AvailableGame => Mathf.Max(0, gameStored - reservedGame);
        public bool HasStorageSpace => HasStorageSpaceFor(1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.LocalCapacity);
            map = mapController;
            population = populationController;
            wildlife = wildlifeController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "HunterCamp",
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
            resident.AssignHunterWorkplace(this);
            StrategyDebugLogger.Info(
                "HunterCamp",
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
                    "HunterCamp",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("campOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearHunterWorkplace(this);
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

        public bool TryReserveRabbitTarget(object owner, out StrategyRabbitAgent rabbit)
        {
            rabbit = null;
            if (wildlife == null)
            {
                wildlife = StrategyWildlifeController.Active;
            }

            PruneRejectedRabbitTargets();
            return HasStorageSpace
                && wildlife != null
                && wildlife.TryReserveRabbitForHunt(Origin, WorkRadius, owner, out rabbit, IsRabbitTargetAllowed);
        }

        public void RegisterRejectedRabbitTarget(StrategyRabbitAgent rabbit, Vector2Int cell, string reason)
        {
            float until = Time.time + RejectedRabbitTargetCooldownSeconds;
            if (rabbit != null)
            {
                rejectedRabbitTargetUntil[rabbit] = until;
            }

            rejectedRabbitCellUntil[cell] = until;
            if (Time.time < nextRejectedRabbitTargetLogTime)
            {
                return;
            }

            nextRejectedRabbitTargetLogTime = Time.time + RejectedRabbitTargetLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "HunterCamp",
                "RabbitTargetTemporarilyRejected",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("rabbitCell", cell),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cooldownSeconds", RejectedRabbitTargetCooldownSeconds));
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            return false;
        }

        public bool TryReserveStoredGame(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || gameStored <= 0)
            {
                return false;
            }

            if (gameReservationOwner != null && gameReservationOwner != owner)
            {
                return false;
            }

            if (gameReservationOwner == owner && reservedGame > 0)
            {
                amount = reservedGame;
                return true;
            }

            int available = AvailableGame;
            if (available <= 0)
            {
                return false;
            }

            int carryLimit = owner is StrategyResidentAgent { IsHouseholder: true } ? 1 : StrategyProductionStorage.HaulerCarryLimit;
            reservedGame = Mathf.Min(carryLimit, available);
            gameReservationOwner = owner;
            amount = reservedGame;
            StrategyDebugLogger.Info(
                "HunterCamp",
                "GameReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", gameStored),
                StrategyDebugLogger.F("available", AvailableGame),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }

        public bool TryTakeReservedGame(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || gameReservationOwner != owner
                || reservedGame <= 0
                || gameStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedGame, gameStored);
            gameStored -= amount;
            reservedGame = 0;
            gameReservationOwner = null;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "HunterCamp",
                "GameTakenFromStock",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", gameStored),
                StrategyDebugLogger.F("owner", owner));
            return amount > 0;
        }

        public void ReleaseStoredGameReservation(object owner)
        {
            if (owner == null || gameReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "HunterCamp",
                "GameReservationReleased",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", reservedGame),
                StrategyDebugLogger.F("owner", owner));
            gameReservationOwner = null;
            reservedGame = 0;
        }

        public void AddGame(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gameStored = StrategyProductionStorage.AddCapped(gameStored, gameStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "HunterCamp",
                "GameStored",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", gameStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(gameStored, amount);
        }

        public string GetHudStatusText()
        {
            int rabbits = wildlife != null ? wildlife.CountHuntableRabbits(Origin, WorkRadius) : 0;
            int deer = CanHuntDeer && wildlife != null ? wildlife.CountHuntableDeer(Origin, WorkRadius) : 0;
            return "Workers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "Game: "
                + StrategyProductionStorage.Format(gameStored)
                + (reservedGame > 0 ? " (reserved: " + reservedGame + ")" : string.Empty)
                + "\n"
                + "Rabbits: "
                + rabbits
                + "\n"
                + "Deer: "
                + (CanHuntDeer ? deer.ToString() : "locked");
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Game Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetHunterCampStockSprite(gameStored);
            stockRenderer.gameObject.SetActive(gameStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.35f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private bool IsRabbitTargetAllowed(StrategyRabbitAgent rabbit)
        {
            if (rabbit == null)
            {
                return false;
            }

            float now = Time.time;
            if (rejectedRabbitTargetUntil.TryGetValue(rabbit, out float rejectedUntil)
                && rejectedUntil > now)
            {
                return false;
            }

            return !rabbit.TryGetCurrentCell(out Vector2Int cell)
                || !rejectedRabbitCellUntil.TryGetValue(cell, out rejectedUntil)
                || rejectedUntil <= now;
        }

        private void PruneRejectedRabbitTargets()
        {
            float now = Time.time;
            rejectedRabbitTargetCleanup.Clear();
            foreach (KeyValuePair<StrategyRabbitAgent, float> pair in rejectedRabbitTargetUntil)
            {
                if (pair.Key == null || pair.Value <= now)
                {
                    rejectedRabbitTargetCleanup.Add(pair.Key);
                }
            }

            for (int i = 0; i < rejectedRabbitTargetCleanup.Count; i++)
            {
                rejectedRabbitTargetUntil.Remove(rejectedRabbitTargetCleanup[i]);
            }

            rejectedRabbitCellCleanup.Clear();
            foreach (KeyValuePair<Vector2Int, float> pair in rejectedRabbitCellUntil)
            {
                if (pair.Value <= now)
                {
                    rejectedRabbitCellCleanup.Add(pair.Key);
                }
            }

            for (int i = 0; i < rejectedRabbitCellCleanup.Count; i++)
            {
                rejectedRabbitCellUntil.Remove(rejectedRabbitCellCleanup[i]);
            }
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearHunterWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
