using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static readonly List<Vector2Int> pendingPointOfInterestReservations = new();

        internal static void ReservePendingPointOfInterestCells(CityMapController map)
        {
            pendingPointOfInterestReservations.Clear();
            if (map == null
                || pendingLoad == null
                || pendingLoad.mapWidth != map.Width
                || pendingLoad.mapHeight != map.Height
                || !ValidateSaveData(pendingLoad, out _))
            {
                return;
            }

            HashSet<Vector2Int> uniqueCells = new();
            for (int i = 0; i < pendingLoad.pointsOfInterest.Count; i++)
            {
                StrategyPointOfInterestSaveData point = pendingLoad.pointsOfInterest[i];
                if (point == null)
                {
                    continue;
                }

                uniqueCells.Add(new Vector2Int(point.cellX, point.cellY));
                if (!point.hasMineralSite || point.remainingMineralAmount <= 0)
                {
                    continue;
                }

                Vector2Int origin = new(point.mineralOriginX, point.mineralOriginY);
                Vector2Int footprint = StrategyPointOfInterestPlacement.ExtractionBlockFootprint;
                for (int y = 0; y < footprint.y; y++)
                {
                    for (int x = 0; x < footprint.x; x++)
                    {
                        uniqueCells.Add(origin + new Vector2Int(x, y));
                    }
                }
            }

            foreach (Vector2Int cell in uniqueCells)
            {
                map.SetCellsBuildable(cell, Vector2Int.one, false);
                pendingPointOfInterestReservations.Add(cell);
            }

            if (pendingPointOfInterestReservations.Count > 0)
            {
                StrategyDebugLogger.Info(
                    "PointOfInterest",
                    "PendingCellsReserved",
                    StrategyDebugLogger.F("cells", pendingPointOfInterestReservations.Count));
            }
        }

        internal static void ReleasePendingPointOfInterestCells(CityMapController map)
        {
            if (map != null)
            {
                for (int i = 0; i < pendingPointOfInterestReservations.Count; i++)
                {
                    map.SetCellsBuildable(
                        pendingPointOfInterestReservations[i],
                        Vector2Int.one,
                        true);
                }
            }

            pendingPointOfInterestReservations.Clear();
        }
    }
}
