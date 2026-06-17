using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryBuildPathToConstructionDropoffCell(
            StrategyConstructionSite site,
            out Vector2Int dropoffCell,
            out int checkedCells)
        {
            dropoffCell = default;
            checkedCells = 0;
            if (map == null || site == null || !site.TryCollectDropoffCells(constructionWorkCellCandidates))
            {
                return false;
            }

            while (constructionWorkCellCandidates.Count > 0)
            {
                int index = GetConstructionWorkCellIndex(site, constructionWorkCellCandidates);
                Vector2Int candidate = constructionWorkCellCandidates[index];
                constructionWorkCellCandidates.RemoveAt(index);
                checkedCells++;
                dropoffCell = candidate;
                if (TryBuildPathTo(candidate))
                {
                    return true;
                }
            }

            path.Clear();
            pathIndex = 0;
            return false;
        }
    }
}
