using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyGranary : MonoBehaviour
    {
        public const int MaxWorkers = 2;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer gameStockRenderer;
        private SpriteRenderer fishStockRenderer;
        private int gameStored;
        private int fishStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int GameStored => gameStored;
        public int FishStored => fishStored;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            EnsureStockRenderers();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
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
                    && resident.CanWork
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
                    && resident.CanWork
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
                || !resident.CanWork
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignGranaryWorkplace(this);
            StrategyDebugLogger.Info(
                "Granary",
                "WorkerAssigned",
                StrategyDebugLogger.F("granaryOrigin", Origin),
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
                    "Granary",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("granaryOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearGranaryWorkplace(this);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryReserveFoodSource(
            object owner,
            out StrategyResourceType resource,
            out StrategyHunterCamp gameSource,
            out StrategyFisherHut fishSource)
        {
            resource = StrategyResourceType.None;
            gameSource = null;
            fishSource = null;
            if (owner == null)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            StrategyHunterCamp bestGame = null;
            StrategyHunterCamp[] camps = Object.FindObjectsByType<StrategyHunterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyHunterCamp camp = camps[i];
                if (camp == null || camp.AvailableGame <= 0)
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestGame = camp;
                    resource = StrategyResourceType.Game;
                }
            }

            StrategyFisherHut bestFish = null;
            StrategyFisherHut[] huts = Object.FindObjectsByType<StrategyFisherHut>();
            for (int i = 0; i < huts.Length; i++)
            {
                StrategyFisherHut hut = huts[i];
                if (hut == null || hut.AvailableFish <= 0)
                {
                    continue;
                }

                float distance = (hut.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestFish = hut;
                    bestGame = null;
                    resource = StrategyResourceType.Fish;
                }
            }

            if (resource == StrategyResourceType.Game)
            {
                if (bestGame == null || !bestGame.TryReserveStoredGame(owner, out _))
                {
                    return false;
                }

                gameSource = bestGame;
                return true;
            }

            if (resource == StrategyResourceType.Fish)
            {
                if (bestFish == null || !bestFish.TryReserveStoredFish(owner, out _))
                {
                    return false;
                }

                fishSource = bestFish;
                return true;
            }

            return false;
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

        public void AddGame(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gameStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", gameStored));
        }

        public void AddFish(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            fishStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", fishStored));
        }

        public string GetHudStatusText()
        {
            CountAvailableSources(out int gameSources, out int fishSources);
            return "\u0420\u0430\u0431\u043e\u0447\u0438\u0435: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "\u0414\u0438\u0447\u044c: "
                + gameStored
                + "\n"
                + "\u0420\u044b\u0431\u0430: "
                + fishStored
                + "\n"
                + "\u0418\u0441\u0442\u043e\u0447\u043d\u0438\u043a\u0438: "
                + "\u0434\u0438\u0447\u044c "
                + gameSources
                + " / "
                + "\u0440\u044b\u0431\u0430 "
                + fishSources;
        }

        private void CountAvailableSources(out int gameSources, out int fishSources)
        {
            gameSources = 0;
            fishSources = 0;
            StrategyHunterCamp[] camps = Object.FindObjectsByType<StrategyHunterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                if (camps[i] != null && camps[i].AvailableGame > 0)
                {
                    gameSources++;
                }
            }

            StrategyFisherHut[] huts = Object.FindObjectsByType<StrategyFisherHut>();
            for (int i = 0; i < huts.Length; i++)
            {
                if (huts[i] != null && huts[i].AvailableFish > 0)
                {
                    fishSources++;
                }
            }
        }

        private void EnsureStockRenderers()
        {
            if (gameStockRenderer == null)
            {
                GameObject gameObject = new GameObject("Game Stock");
                gameObject.transform.SetParent(transform, false);
                gameStockRenderer = gameObject.AddComponent<SpriteRenderer>();
                gameStockRenderer.color = Color.white;
            }

            if (fishStockRenderer == null)
            {
                GameObject fishObject = new GameObject("Fish Stock");
                fishObject.transform.SetParent(transform, false);
                fishStockRenderer = fishObject.AddComponent<SpriteRenderer>();
                fishStockRenderer.color = Color.white;
            }

            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderers();
            if (gameStockRenderer != null)
            {
                gameStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryGameStockSprite(gameStored);
                gameStockRenderer.gameObject.SetActive(gameStored > 0 && gameStockRenderer.sprite != null);
            }

            if (fishStockRenderer != null)
            {
                fishStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryFishStockSprite(fishStored);
                fishStockRenderer.gameObject.SetActive(fishStored > 0 && fishStockRenderer.sprite != null);
            }

            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (gameStockRenderer != null)
            {
                Vector3 gameWorld = new Vector3(bounds.min.x + 0.42f, bounds.min.y + 0.35f, -0.13f);
                gameStockRenderer.transform.localPosition = transform.InverseTransformPoint(gameWorld);
                gameStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(gameStockRenderer, gameWorld, 1);
            }

            if (fishStockRenderer != null)
            {
                Vector3 fishWorld = new Vector3(bounds.max.x - 0.42f, bounds.min.y + 0.37f, -0.13f);
                fishStockRenderer.transform.localPosition = transform.InverseTransformPoint(fishWorld);
                fishStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(fishStockRenderer, fishWorld, 1);
            }
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearGranaryWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
