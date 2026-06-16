using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private bool IsWolfTravelCell(Vector2Int cell, bool allowStructureBuffer = false)
        {
            return wildlife != null
                ? wildlife.IsLandWildlifeTravelCell(cell, allowStructureBuffer)
                : StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, cell);
        }

        private bool IsWolfTargetCell(Vector2Int cell, bool allowStructureBuffer = false)
        {
            return wildlife != null
                ? wildlife.IsLandWildlifeTargetCell(cell, allowStructureBuffer)
                : StrategyWildlifeRiverCrossing.IsLandCell(map, cell);
        }
    }
}
