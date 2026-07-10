using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyStarterCaravanCart : MonoBehaviour, IStrategyConstructionResourceSource, IStrategyResourceStoreOwner
    {
        private static readonly List<StrategyStarterCaravanCart> cartQuery = new();
        private static Vector3 cartSortWorld;

        private readonly Dictionary<object, int> constructionLogReservations = new();
        private readonly Dictionary<object, int> constructionStoneReservations = new();
        private readonly Dictionary<object, int> constructionPlankReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();
        private readonly Dictionary<object, HouseholdFoodReservation> householdFoodReservations = new();
        private readonly Dictionary<object, int> householdLogReservations = new();

        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyBuildPlacementController placement;
        private bool removing;

        public int LogsStored => logsStored;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int StoneStored => stoneStored;
        public int PlanksStored => planksStored;
        public int AvailableConstructionLogs => Mathf.Max(0, logsStored - CountReservations(constructionLogReservations) - CountReservations(householdLogReservations));
        public int AvailableConstructionStone => Mathf.Max(0, stoneStored - CountReservations(constructionStoneReservations));
        public int AvailableConstructionPlanks => Mathf.Max(0, planksStored - CountReservations(constructionPlankReservations));
        public float TotalFoodRationValue => GetTotalFoodRations(false);
        public float AvailableHouseholdRationValue => GetTotalFoodRations(true);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        private sealed class ConstructionPickupReservation
        {
            public object Owner;
            public StrategyConstructionResourceKind Kind;
            public int Amount;
        }

        private sealed class HouseholdFoodReservation
        {
            public StrategyResourceType Resource;
            public int Amount;
        }

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyBuildPlacementController placementController)
        {
            building = placedBuilding;
            ConfigureResourceStore();
            map = mapController;
            population = populationController;
            placement = placementController;
            StrategyDebugLogger.Info("StarterCaravan", "Configured", StrategyDebugLogger.F("origin", Origin));
        }

        public void InitializeStarterStock(int initialLogs, int initialStone, float starterFoodRations)
        {
            logsStored = Mathf.Max(0, initialLogs);
            stoneStored = Mathf.Max(0, initialStone);
            planksStored = 0;
            ClearFoodStock();
            AddStarterFoodRations(starterFoodRations);
            StrategyDebugLogger.Info(
                "StarterCaravan",
                "StarterStockInitialized",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("logs", logsStored),
                StrategyDebugLogger.F("stone", stoneStored),
                StrategyDebugLogger.F("targetFoodRations", starterFoodRations),
                StrategyDebugLogger.F("foodRations", TotalFoodRationValue),
                StrategyDebugLogger.F("food", GetFoodStockText()));
            TryTransferConstructionResourcesToNearestStorageYard();
            TryDespawnIfEmpty();
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

        public string GetHudStatusText()
        {
            return "Temporary starter stock"
                + "\n"
                + "Construction: Logs "
                + logsStored
                + " / Stone "
                + stoneStored
                + " / Planks "
                + planksStored
                + "\n"
                + "Available: Logs "
                + AvailableConstructionLogs
                + " / Stone "
                + AvailableConstructionStone
                + " / Planks "
                + AvailableConstructionPlanks
                + "\n"
                + "Food: "
                + GetFoodStockText()
                + "\n"
                + "Rations: "
                + TotalFoodRationValue.ToString("0.#")
                + " total / "
                + AvailableHouseholdRationValue.ToString("0.#")
                + " available";
        }

        private static List<StrategyStarterCaravanCart> GetActiveCarts()
        {
            StrategyPlacedBuilding.CopyActiveComponents(cartQuery);
            return cartQuery;
        }

        private static List<StrategyStarterCaravanCart> GetCartsSortedByDistance(Vector3 nearWorld)
        {
            StrategyPlacedBuilding.CopyActiveComponents(cartQuery);
            cartSortWorld = nearWorld;
            cartQuery.Sort(CompareCartsByDistance);
            return cartQuery;
        }

        private static int CompareCartsByDistance(StrategyStarterCaravanCart left, StrategyStarterCaravanCart right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            float leftDistance = (left.FootprintBounds.center - cartSortWorld).sqrMagnitude;
            float rightDistance = (right.FootprintBounds.center - cartSortWorld).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        }

        private static int CountReservations(Dictionary<object, int> reservations)
        {
            int total = 0;
            foreach (KeyValuePair<object, int> pair in reservations)
            {
                if (pair.Key != null && pair.Value > 0)
                {
                    total += pair.Value;
                }
            }

            return total;
        }

        private void TryDespawnIfEmpty()
        {
            if (removing
                || logsStored > 0
                || stoneStored > 0
                || planksStored > 0
                || HasFoodStock()
                || HasAnyReservations())
            {
                return;
            }

            removing = true;
            StrategyDebugLogger.Info("StarterCaravan", "Depleted", StrategyDebugLogger.F("origin", Origin));
            if (placement != null && building != null)
            {
                placement.DemolishBuilding(building);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool HasAnyReservations()
        {
            return CountReservations(constructionLogReservations) > 0
                || CountReservations(constructionStoneReservations) > 0
                || CountReservations(constructionPlankReservations) > 0
                || constructionPickupReservations.Count > 0
                || householdFoodReservations.Count > 0
                || CountReservations(householdLogReservations) > 0;
        }
    }
}
