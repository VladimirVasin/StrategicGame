using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private StrategyFoundingStartState foundingStartState;

        public void SetFoundingStartState(StrategyFoundingStartState state)
        {
            if (hasStarterCamp)
            {
                return;
            }

            foundingStartState = state;
        }

        private bool TryUseFoundingCampCell(out Vector2Int cell)
        {
            cell = default;
            if (foundingStartState == null
                || !foundingStartState.HasCampCell
                || map == null)
            {
                return false;
            }

            Vector2Int selected = foundingStartState.CampCell;
            if (!map.TryGetCell(selected.x, selected.y, out CityMapCell mapCell)
                || mapCell.IsWater
                || mapCell.IsShore
                || !map.IsCellWalkable(selected))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "FoundingCampRejected",
                    StrategyDebugLogger.F("cell", selected),
                    StrategyDebugLogger.F("reason", "invalid_or_unwalkable"));
                return false;
            }

            cell = selected;
            StrategyDebugLogger.Info(
                "Population",
                "FoundingCampAccepted",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("profile", foundingStartState.Preferences?.ProfileId ?? "legacy"));
            return true;
        }

        private bool TryFindCampSpawnCell(
            HashSet<Vector2Int> usedCells,
            int spawnSlot,
            out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= CampSpawnRadius; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate)
                            && !usedCells.Contains(candidate)
                            && !IsInsideFoundingCaravanReservation(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[StableIndex(candidates.Count, spawnSlot + radius * 31)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsInsideFoundingCaravanReservation(Vector2Int cell)
        {
            return foundingStartState != null
                && foundingStartState.HasCaravanOrigin
                && new RectInt(
                    foundingStartState.CaravanOrigin,
                    StrategyStartSiteSelector.CaravanReservedFootprint).Contains(cell);
        }
    }
}
