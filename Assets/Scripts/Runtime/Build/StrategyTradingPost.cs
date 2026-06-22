using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTradingPost : MonoBehaviour
    {
        private StrategyPlacedBuilding building;
        private CityMapController map;

        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(StrategyPlacedBuilding placedBuilding, CityMapController mapController)
        {
            building = placedBuilding;
            map = mapController;
            StrategyDebugLogger.Info(
                "Trade",
                "TradingPostConfigured",
                StrategyDebugLogger.F("origin", Origin));
        }

        public bool TryFindCaravanStopCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 4; radius++)
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
            StrategyTradeCaravanController controller = StrategyTradeCaravanController.Active;
            if (controller == null)
            {
                return "Trade controller unavailable.";
            }

            return controller.GetPostStatusText(this);
        }
    }
}
