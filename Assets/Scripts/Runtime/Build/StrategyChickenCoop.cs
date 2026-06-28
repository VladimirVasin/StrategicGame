using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyChickenCoop : MonoBehaviour
    {
        private const int ChickensPerCoop = 3;
        private const float EggCycleSeconds = 22f;
        private const float EggCycleJitterSeconds = 2.5f;

        private readonly List<StrategyChickenAgent> chickens = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer stockRenderer;
        private Transform chickenRoot;
        private object eggReservationOwner;
        private float productionTimer;
        private float productionCycleSeconds;
        private int reservedEggs;
        private int eggsStored;
        private int appliedFrame = -1;

        public int EggsStored => eggsStored;
        public int AvailableEggs => Mathf.Max(0, eggsStored - reservedEggs);
        public bool HasStorageSpace => StrategyProductionStorage.CanAccept(eggsStored, 1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Vector2Int Footprint => building != null ? building.Footprint : Vector2Int.one;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);
        public float ProductionProgress => productionCycleSeconds > 0f
            ? 1f - Mathf.Clamp01(productionTimer / productionCycleSeconds)
            : 0f;
        public float NextProductionSeconds => Mathf.Max(0f, productionTimer);

        public void Configure(StrategyPlacedBuilding placedBuilding, CityMapController mapController)
        {
            building = placedBuilding;
            map = mapController;
            spriteRenderer = GetComponent<SpriteRenderer>();
            StartProductionCycle(Random.Range(0.05f, 0.65f), false);
            EnsureStockRenderer();
            SpawnChickens();
            SyncChickenNightState(true);
            UpdateVisuals();
            StrategyDebugLogger.Info(
                "ChickenCoop",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("capacity", StrategyProductionStorage.LocalCapacity),
                StrategyDebugLogger.F("chickens", chickens.Count));
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

        public bool TryReserveStoredEggs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || eggsStored <= 0)
            {
                return false;
            }

            if (eggReservationOwner != null && eggReservationOwner != owner)
            {
                return false;
            }

            if (eggReservationOwner == owner && reservedEggs > 0)
            {
                amount = reservedEggs;
                return true;
            }

            int available = AvailableEggs;
            if (available <= 0)
            {
                return false;
            }

            int carryLimit = owner is StrategyResidentAgent { IsHouseholder: true } ? 1 : StrategyProductionStorage.HaulerCarryLimit;
            reservedEggs = Mathf.Min(carryLimit, available);
            eggReservationOwner = owner;
            amount = reservedEggs;
            StrategyDebugLogger.Info(
                "ChickenCoop",
                "EggsReserved",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", eggsStored),
                StrategyDebugLogger.F("available", AvailableEggs),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }

        public bool TryTakeReservedEggs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || eggReservationOwner != owner
                || reservedEggs <= 0
                || eggsStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedEggs, eggsStored);
            eggsStored -= amount;
            reservedEggs = 0;
            eggReservationOwner = null;
            UpdateVisuals();
            StrategyDebugLogger.Info(
                "ChickenCoop",
                "EggsTakenFromStock",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", eggsStored),
                StrategyDebugLogger.F("owner", owner));
            return amount > 0;
        }

        public void ReleaseStoredEggsReservation(object owner)
        {
            if (owner == null || eggReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "ChickenCoop",
                "EggReservationReleased",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("amount", reservedEggs),
                StrategyDebugLogger.F("owner", owner));
            eggReservationOwner = null;
            reservedEggs = 0;
        }

        public string GetHudStatusText()
        {
            return "Workers: none"
                + "\n"
                + "Eggs: "
                + StrategyProductionStorage.Format(eggsStored)
                + (reservedEggs > 0 ? " (reserved: " + reservedEggs + ")" : string.Empty)
                + "\n"
                + "Next egg: "
                + Mathf.CeilToInt(NextProductionSeconds)
                + "s";
        }

        private void Update()
        {
            if (building == null)
            {
                return;
            }

            SyncChickenNightState(false);

            if (productionTimer > 0f)
            {
                productionTimer -= Time.deltaTime;
            }

            if (productionTimer <= 0f && HasStorageSpace)
            {
                float overflowSeconds = -productionTimer;
                AddEggs(1);
                StartProductionCycle(0f, true);
                productionTimer = Mathf.Max(0.25f, productionTimer - overflowSeconds);
            }

            UpdateCoopSprite();
        }

        private void AddEggs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            eggsStored = StrategyProductionStorage.AddCapped(eggsStored, eggsStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateVisuals();
            StrategyDebugLogger.Info(
                "ChickenCoop",
                "EggsStored",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", eggsStored));
        }

        private void StartProductionCycle(float startProgress, bool logStart)
        {
            productionCycleSeconds = Random.Range(
                EggCycleSeconds - EggCycleJitterSeconds,
                EggCycleSeconds + EggCycleJitterSeconds);
            productionTimer = productionCycleSeconds * (1f - Mathf.Clamp01(startProgress));
            appliedFrame = -1;

            if (logStart)
            {
                StrategyDebugLogger.Info(
                    "ChickenCoop",
                    "EggCycleStarted",
                    StrategyDebugLogger.F("coopOrigin", Origin),
                    StrategyDebugLogger.F("cycleSeconds", productionCycleSeconds));
            }
        }

        private void SpawnChickens()
        {
            if (map == null || building == null)
            {
                return;
            }

            EnsureChickenRoot();
            HashSet<Vector2Int> usedCells = new();
            for (int i = 0; i < ChickensPerCoop; i++)
            {
                bool foundSpawnCell = TryFindChickenSpawnCell(usedCells, i, out Vector2Int spawnCell);
                Vector3 spawnWorld = foundSpawnCell
                    ? map.GetCellCenterWorld(spawnCell.x, spawnCell.y)
                    : GetFallbackChickenSpawnWorld(i);

                if (foundSpawnCell)
                {
                    usedCells.Add(spawnCell);
                }

                GameObject chickenObject = new GameObject("Chicken");
                chickenObject.transform.SetParent(chickenRoot, false);

                SpriteRenderer renderer = chickenObject.AddComponent<SpriteRenderer>();
                renderer.sprite = StrategyChickenSpriteFactory.GetSprite();

                StrategyChickenAgent chicken = chickenObject.AddComponent<StrategyChickenAgent>();
                chicken.Configure(map, this, spawnWorld, renderer);
                chickens.Add(chicken);
            }
        }

        private bool TryFindChickenSpawnCell(HashSet<Vector2Int> usedCells, int variant, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            Vector2Int origin = Origin;
            Vector2Int footprint = Footprint;
            for (int radius = 1; radius <= 4; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate)
                            && !IsCoopCell(candidate)
                            && !usedCells.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Mathf.Abs(origin.x * 19 + origin.y * 23 + variant * 5) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private Vector3 GetFallbackChickenSpawnWorld(int variant)
        {
            Bounds bounds = FootprintBounds;
            float angle = variant * (Mathf.PI * 2f / ChickensPerCoop);
            float radius = Mathf.Max(0.45f, map != null ? map.CellSize * 0.85f : 0.85f);
            return new Vector3(
                bounds.center.x + Mathf.Cos(angle) * radius,
                bounds.center.y + Mathf.Sin(angle) * radius,
                -0.09f);
        }

        private bool IsCoopCell(Vector2Int cell)
        {
            Vector2Int origin = Origin;
            Vector2Int footprint = Footprint;
            return cell.x >= origin.x
                && cell.x < origin.x + footprint.x
                && cell.y >= origin.y
                && cell.y < origin.y + footprint.y;
        }

        private void EnsureChickenRoot()
        {
            if (chickenRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Chickens");
            rootObject.transform.SetParent(transform, false);
            chickenRoot = rootObject.transform;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Egg Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
        }

        private void UpdateVisuals()
        {
            UpdateCoopSprite();
            UpdateStockVisual();
        }

        private void UpdateCoopSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            int frame = Mathf.Clamp(
                Mathf.FloorToInt(ProductionProgress * StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount),
                0,
                StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount - 1);
            if (frame == appliedFrame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(frame);
            appliedFrame = frame;
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            stockRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(StrategyResourceType.Eggs);
            stockRenderer.gameObject.SetActive(eggsStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.36f, bounds.min.y + 0.36f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }
    }
}
