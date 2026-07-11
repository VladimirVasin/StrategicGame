using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {
        private bool lastPathBuildDeferred;

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            lastPathBuildDeferred = false;
            if (!map.TryWorldToCell(transform.position, out Vector2Int startCell)
                || !IsRabbitWalkCell(startCell, true)
                || !IsRabbitWalkCell(targetCell, landOnly: true))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.072f));
                pathIndex = 0;
                return true;
            }

            StrategyNavigationService navigation = StrategyNavigationService.Active;
            if (navigation == null)
            {
                return false;
            }

            bool allowStructureBuffer = wildlife != null && wildlife.IsLandWildlifeStructureBufferCell(startCell);
            StrategyNavigationStatus status = navigation.TryBuildPath(
                new StrategyNavigationQuery(
                    startCell,
                    targetCell,
                    StrategyNavigationMode.WildlifeLand,
                    360,
                    wildlife,
                    allowStructureBuffer),
                navigationRawCells,
                navigationSmoothedCells);
            if (status == StrategyNavigationStatus.Deferred)
            {
                lastPathBuildDeferred = true;
                return false;
            }

            if (status != StrategyNavigationStatus.Success)
            {
                return false;
            }

            BuildWorldPath(navigationSmoothedCells);
            return path.Count > 0;
        }
    }
}
