using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        public bool TryPlaceStarterCaravanCartAt(
            Vector2Int origin,
            Vector2Int nearCell,
            int initialLogs,
            int initialStone,
            float starterFoodRations)
        {
            if (map == null)
            {
                return false;
            }

            for (int i = 0; i < placedBuildings.Count; i++)
            {
                StrategyPlacedBuilding placed = placedBuildings[i];
                if (placed != null && placed.Tool == StrategyBuildTool.StarterCaravanCart)
                {
                    return true;
                }
            }

            StrategyBuildToolInfo toolInfo = CreateStarterCaravanToolInfo();
            if (!CanPlaceStarterSupplyOrigin(origin, nearCell, toolInfo))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ReservedStarterCaravanOriginRejected",
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("nearCell", nearCell));
                return false;
            }

            return PlaceStarterCaravanCart(
                toolInfo,
                origin,
                nearCell,
                initialLogs,
                initialStone,
                starterFoodRations);
        }

        private static StrategyBuildToolInfo CreateStarterCaravanToolInfo()
        {
            return new StrategyBuildToolInfo(
                StrategyBuildTool.StarterCaravanCart,
                "Caravan Cart",
                new StrategyConstructionResourceCost(0, 0),
                new Color(0.72f, 0.54f, 0.30f),
                new Vector2Int(3, 2));
        }

        private bool PlaceStarterCaravanCart(
            StrategyBuildToolInfo toolInfo,
            Vector2Int origin,
            Vector2Int nearCell,
            int initialLogs,
            int initialStone,
            float starterFoodRations)
        {
            StrategyPlacedBuilding building = PlaceTool(toolInfo, origin);
            StrategyStarterCaravanCart cart = building != null
                ? building.GetComponent<StrategyStarterCaravanCart>()
                : null;
            if (cart == null)
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "StarterCaravanCartRejected",
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("reason", "cart_missing"));
                return false;
            }

            cart.InitializeStarterStock(initialLogs, initialStone, starterFoodRations);
            StrategyDebugLogger.Info(
                "Build",
                "StarterCaravanCartPlaced",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("nearCell", nearCell),
                StrategyDebugLogger.F("initialLogs", initialLogs),
                StrategyDebugLogger.F("initialStone", initialStone),
                StrategyDebugLogger.F("targetFoodRations", starterFoodRations));
            return true;
        }
    }
}
