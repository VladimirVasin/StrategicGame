using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRefugeeArrivalController
    {
        private readonly List<Vector2Int> navigationRawCells = new();
        private readonly List<Vector2Int> navigationSmoothedCells = new();

        private bool TryBuildCampArrivalTargets(
            Vector2Int campCell,
            out HashSet<Vector2Int> arrivalTargets,
            out HashSet<Vector2Int> campReachableCells)
        {
            arrivalTargets = null;
            campReachableCells = null;
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= MaxCampGatherRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !candidates.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            HashSet<Vector2Int> assigned = new();
            HashSet<Vector2Int> bestTargets = null;
            HashSet<Vector2Int> bestReachable = null;
            int bestResidentScore = -1;
            int bestTargetCount = -1;
            float bestDistanceScore = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int start = candidates[i];
                if (assigned.Contains(start) || !BuildReachableSet(start, out HashSet<Vector2Int> reachable))
                {
                    continue;
                }

                HashSet<Vector2Int> componentTargets = new();
                float distanceScore = 0f;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    Vector2Int candidate = candidates[candidateIndex];
                    if (!reachable.Contains(candidate))
                    {
                        continue;
                    }

                    componentTargets.Add(candidate);
                    assigned.Add(candidate);
                    distanceScore += (candidate - campCell).sqrMagnitude;
                }

                if (componentTargets.Count <= 0)
                {
                    continue;
                }

                int residentScore = CountAcceptedResidentsInReachableSet(reachable);
                float averageDistance = distanceScore / Mathf.Max(1, componentTargets.Count);
                bool better = residentScore > bestResidentScore
                    || (residentScore == bestResidentScore && componentTargets.Count > bestTargetCount)
                    || (residentScore == bestResidentScore
                        && componentTargets.Count == bestTargetCount
                        && averageDistance < bestDistanceScore);
                if (!better)
                {
                    continue;
                }

                bestTargets = componentTargets;
                bestReachable = reachable;
                bestResidentScore = residentScore;
                bestTargetCount = componentTargets.Count;
                bestDistanceScore = averageDistance;
            }

            if (bestTargets == null || bestTargets.Count <= 0 || bestReachable == null)
            {
                return false;
            }

            arrivalTargets = bestTargets;
            campReachableCells = bestReachable;
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivalRouteTargetsPrepared",
                StrategyDebugLogger.F("campCell", campCell),
                StrategyDebugLogger.F("targetCount", arrivalTargets.Count),
                StrategyDebugLogger.F("residentScore", bestResidentScore));
            return true;
        }

        private bool BuildReachableSet(Vector2Int startCell, out HashSet<Vector2Int> reachable)
        {
            reachable = null;
            if (!map.IsCellWalkable(startCell))
            {
                return false;
            }

            reachable = new HashSet<Vector2Int>();
            Queue<Vector2Int> open = new();
            open.Enqueue(startCell);
            reachable.Add(startCell);

            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && reachable.Count < visitLimit)
            {
                Vector2Int current = open.Dequeue();
                TryVisitReachableNeighbor(current + Vector2Int.right, open, reachable);
                TryVisitReachableNeighbor(current + Vector2Int.left, open, reachable);
                TryVisitReachableNeighbor(current + Vector2Int.up, open, reachable);
                TryVisitReachableNeighbor(current + Vector2Int.down, open, reachable);
            }

            return reachable.Count > 0;
        }

        private void TryVisitReachableNeighbor(
            Vector2Int candidate,
            Queue<Vector2Int> open,
            HashSet<Vector2Int> reachable)
        {
            if (reachable.Contains(candidate) || !map.IsCellWalkable(candidate))
            {
                return;
            }

            reachable.Add(candidate);
            open.Enqueue(candidate);
        }

        private int CountAcceptedResidentsInReachableSet(HashSet<Vector2Int> reachable)
        {
            if (population == null || reachable == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || resident.IsPendingRefugee)
                {
                    continue;
                }

                if (map.TryWorldToCell(resident.transform.position, out Vector2Int residentCell)
                    && reachable.Contains(residentCell))
                {
                    count++;
                }
            }

            return count;
        }

        private List<Vector3> BuildDepartureRoute(StrategyResidentAgent resident, int memberIndex)
        {
            List<Vector3> route = new();
            if (resident != null
                && map.TryWorldToCell(resident.transform.position, out Vector2Int currentCell)
                && map.IsCellWalkable(currentCell)
                && TryBuildCellPath(currentCell, activeEntryCell, out List<Vector2Int> cellPath))
            {
                route.AddRange(ToWorldRoute(cellPath));
            }

            Vector3 offset = GetFormationOffset(memberIndex);
            Vector3 exitWorld = activeOutsideBaseWorld + offset;
            route.Add(exitWorld);
            exitTargets.Add(exitWorld);
            return route;
        }

        private bool TryBuildCellPath(Vector2Int startCell, Vector2Int targetCell, out List<Vector2Int> path)
        {
            path = null;
            if (!map.IsCellWalkable(startCell) || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path = new List<Vector2Int> { startCell };
                return true;
            }

            StrategyNavigationService navigation = StrategyNavigationService.Active;
            if (navigation == null)
            {
                return false;
            }

            StrategyNavigationStatus status = navigation.TryBuildPath(
                new StrategyNavigationQuery(
                    startCell,
                    targetCell,
                    StrategyNavigationMode.GroundCardinal,
                    Mathf.Max(256, map.Width * map.Height)),
                navigationRawCells,
                navigationSmoothedCells,
                false);
            if (status != StrategyNavigationStatus.Success)
            {
                return false;
            }

            path = new List<Vector2Int>(navigationSmoothedCells.Count + 1) { startCell };
            path.AddRange(navigationSmoothedCells);
            return path.Count > 1;
        }

        private List<Vector3> ToWorldRoute(IReadOnlyList<Vector2Int> cells)
        {
            List<Vector3> route = new();
            if (cells == null)
            {
                return route;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 world = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                route.Add(new Vector3(world.x, world.y, -0.08f));
            }

            return route;
        }

        private Vector3 GetFormationOffset(int index)
        {
            Vector2 axis = activeFormationAxis.sqrMagnitude > 0.001f
                ? activeFormationAxis.normalized
                : Vector2.right;
            float side = index switch
            {
                0 => -0.35f,
                1 => 0.35f,
                2 => -0.75f,
                3 => 0.75f,
                _ => 0f
            };
            float back = index <= 1 ? 0f : -0.42f - (index - 2) * 0.18f;
            Vector2 perpendicular = new Vector2(-axis.y, axis.x);
            Vector2 offset = (axis * back + perpendicular * side) * map.CellSize;
            return new Vector3(offset.x, offset.y, 0f);
        }

        private void PushPause()
        {
            if (pauseHeld)
            {
                return;
            }

            pauseHeld = true;
            timeScale?.PushPauseLock("RefugeeDecision");
        }

        private void ReleasePause()
        {
            if (!pauseHeld)
            {
                return;
            }

            pauseHeld = false;
            timeScale?.PopPauseLock("RefugeeDecision");
        }

        private void OnDisable()
        {
            CancelArrivalPreparation();
            ReleasePause();
        }
    }
}
