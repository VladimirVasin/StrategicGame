using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float PathCostEpsilon = 0.0001f;
        private const float DiagonalPathCost = 1.41421356f;

        private static readonly TrailPathDirection[] TrailPathDirections =
        {
            new TrailPathDirection(new Vector2Int(1, 0), 1f),
            new TrailPathDirection(new Vector2Int(-1, 0), 1f),
            new TrailPathDirection(new Vector2Int(0, 1), 1f),
            new TrailPathDirection(new Vector2Int(0, -1), 1f),
            new TrailPathDirection(new Vector2Int(1, 1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(1, -1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(-1, 1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(-1, -1), DiagonalPathCost)
        };

        private bool hasLastTrailFootfallCell;
        private Vector2Int lastTrailFootfallCell;

        private void MoveAlongCurrentPathTarget(Vector3 targetWorld)
        {
            float moveSpeed = GetCurrentMoveSpeed() * GetTrailMovementSpeedMultiplier(targetWorld);
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, moveSpeed * Time.deltaTime);
            Vector3 delta = transform.position - previous;

            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
                SyncReadabilityRenderers();
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                RecordTrailFootfallAtPosition(transform.position);
                AnimateWalk();
            }
            else
            {
                AnimateIdle();
            }
        }

        private float GetTrailMovementSpeedMultiplier(Vector3 targetWorld)
        {
            StrategyTrailController trails = StrategyTrailController.Active;
            if (trails == null || map == null)
            {
                return 1f;
            }

            if (map.TryWorldToCell(transform.position, out Vector2Int currentCell)
                && trails.GetMoveSpeedMultiplier(currentCell) > 1f)
            {
                return trails.SpeedMultiplier;
            }

            return map.TryWorldToCell(targetWorld, out Vector2Int targetCell)
                ? trails.GetMoveSpeedMultiplier(targetCell)
                : 1f;
        }

        private void RecordTrailFootfallAtPosition(Vector3 worldPosition)
        {
            StrategyTrailController trails = StrategyTrailController.Active;
            if (trails == null
                || map == null
                || deathRequested
                || hiddenInsideHome
                || hiddenUnderground
                || IsPendingRefugee
                || !map.TryWorldToCell(worldPosition, out Vector2Int cell))
            {
                return;
            }

            if (hasLastTrailFootfallCell && lastTrailFootfallCell == cell)
            {
                return;
            }

            hasLastTrailFootfallCell = true;
            lastTrailFootfallCell = cell;
            trails.RecordFootfall(cell, GetTrailFootfallWeight());
        }

        private float GetTrailFootfallWeight()
        {
            return activity switch
            {
                ResidentActivity.Idle => 0.12f,
                ResidentActivity.TendingHousehold => 0.18f,
                ResidentActivity.MovingHome => 0.45f,
                ResidentActivity.MovingToFuneral => 0.45f,
                ResidentActivity.MovingToBurial => 0.45f,
                ResidentActivity.MourningCorpse => 0f,
                ResidentActivity.BuryingGrave => 0f,
                ResidentActivity.WaitingAtFuneral => 0f,
                _ => 1f
            };
        }

        private bool TryBuildTrailAwarePathTo(Vector2Int targetCell)
        {
            if (map == null
                || !TryGetPathStartCell(out Vector2Int startCell)
                || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.08f));
                pathIndex = 0;
                return true;
            }

            List<PathQueueNode> open = new();
            Dictionary<Vector2Int, float> costs = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> closed = new();
            costs[startCell] = 0f;
            PushPathNode(open, new PathQueueNode(startCell, 0f, EstimateTrailPathCost(startCell, targetCell)));

            int visited = 0;
            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited < visitLimit)
            {
                PathQueueNode node = PopPathNode(open);
                Vector2Int current = node.Cell;
                if (closed.Contains(current)
                    || !costs.TryGetValue(current, out float currentCost)
                    || node.Cost > currentCost + PathCostEpsilon)
                {
                    continue;
                }

                if (current == targetCell)
                {
                    BuildSmoothedTrailWorldPath(startCell, targetCell, cameFrom);
                    return path.Count > 0;
                }

                closed.Add(current);
                visited++;
                ExpandTrailPathNode(current, currentCost, targetCell, open, costs, cameFrom, closed);
            }

            return false;
        }

        private void ExpandTrailPathNode(
            Vector2Int current,
            float currentCost,
            Vector2Int targetCell,
            List<PathQueueNode> open,
            Dictionary<Vector2Int, float> costs,
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            HashSet<Vector2Int> closed)
        {
            for (int i = 0; i < TrailPathDirections.Length; i++)
            {
                TrailPathDirection direction = TrailPathDirections[i];
                Vector2Int next = current + direction.Delta;
                if (closed.Contains(next) || !CanUseTrailPathStep(current, direction))
                {
                    continue;
                }

                float newCost = currentCost + GetTrailPathStepCost(next, direction.Cost);
                if (costs.TryGetValue(next, out float oldCost) && newCost >= oldCost - PathCostEpsilon)
                {
                    continue;
                }

                costs[next] = newCost;
                cameFrom[next] = current;
                float priority = newCost + EstimateTrailPathCost(next, targetCell);
                PushPathNode(open, new PathQueueNode(next, newCost, priority));
            }
        }

        private bool CanUseTrailPathStep(Vector2Int current, TrailPathDirection direction)
        {
            Vector2Int next = current + direction.Delta;
            if (!map.IsCellWalkable(next))
            {
                return false;
            }

            if (!direction.IsDiagonal)
            {
                return true;
            }

            Vector2Int horizontal = current + new Vector2Int(direction.Delta.x, 0);
            Vector2Int vertical = current + new Vector2Int(0, direction.Delta.y);
            return map.IsCellWalkable(horizontal) && map.IsCellWalkable(vertical);
        }

        private static float EstimateTrailPathCost(Vector2Int from, Vector2Int to)
        {
            float minStepCost = StrategyTrailController.Active != null
                ? StrategyTrailController.Active.PathCostMultiplier
                : 1f;
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            int diagonal = Mathf.Min(dx, dy);
            int straight = Mathf.Max(dx, dy) - diagonal;
            return (straight + diagonal * DiagonalPathCost) * minStepCost;
        }

        private static float GetTrailPathStepCost(Vector2Int cell, float baseCost)
        {
            StrategyTrailController trails = StrategyTrailController.Active;
            return baseCost * (trails != null ? trails.GetPathCostMultiplier(cell) : 1f);
        }

        private void BuildSmoothedTrailWorldPath(
            Vector2Int startCell,
            Vector2Int targetCell,
            Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            if (!TryReconstructTrailCells(startCell, targetCell, cameFrom, out List<Vector2Int> cells))
            {
                path.Clear();
                pathIndex = 0;
                return;
            }

            SmoothTrailCells(startCell, cells);
            path.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                if (i == cells.Count - 1)
                {
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.08f));
            }

            pathIndex = 0;
        }

        private bool TryReconstructTrailCells(
            Vector2Int startCell,
            Vector2Int targetCell,
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            out List<Vector2Int> cells)
        {
            cells = new List<Vector2Int>();
            Vector2Int current = targetCell;
            while (current != startCell)
            {
                cells.Add(current);
                if (!cameFrom.TryGetValue(current, out current))
                {
                    return false;
                }
            }

            cells.Reverse();
            return cells.Count > 0;
        }

        private void SmoothTrailCells(Vector2Int startCell, List<Vector2Int> cells)
        {
            List<Vector2Int> smoothed = new();
            Vector2Int anchor = startCell;
            int index = 0;
            while (index < cells.Count)
            {
                int best = index;
                for (int candidate = cells.Count - 1; candidate >= index; candidate--)
                {
                    if (HasWalkableTrailLine(anchor, cells[candidate]))
                    {
                        best = candidate;
                        break;
                    }
                }

                anchor = cells[best];
                smoothed.Add(anchor);
                index = best + 1;
            }

            cells.Clear();
            cells.AddRange(smoothed);
        }

        private bool HasWalkableTrailLine(Vector2Int from, Vector2Int to)
        {
            Vector2Int current = from;
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            int stepX = from.x < to.x ? 1 : -1;
            int stepY = from.y < to.y ? 1 : -1;
            int error = dx - dy;

            while (current != to)
            {
                int doubledError = error * 2;
                Vector2Int next = current;
                if (doubledError > -dy)
                {
                    error -= dy;
                    next.x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    next.y += stepY;
                }

                TrailPathDirection direction = new TrailPathDirection(next - current, next.x != current.x && next.y != current.y ? DiagonalPathCost : 1f);
                if (!CanUseTrailPathStep(current, direction))
                {
                    return false;
                }

                current = next;
            }

            return true;
        }

        private static void PushPathNode(List<PathQueueNode> heap, PathQueueNode node)
        {
            heap.Add(node);
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!HasHigherPriority(node, heap[parent]))
                {
                    break;
                }

                heap[index] = heap[parent];
                index = parent;
            }

            heap[index] = node;
        }

        private static PathQueueNode PopPathNode(List<PathQueueNode> heap)
        {
            PathQueueNode result = heap[0];
            PathQueueNode last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count <= 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                if (left >= heap.Count)
                {
                    break;
                }

                int best = right < heap.Count && HasHigherPriority(heap[right], heap[left]) ? right : left;
                if (!HasHigherPriority(heap[best], last))
                {
                    break;
                }

                heap[index] = heap[best];
                index = best;
            }

            heap[index] = last;
            return result;
        }

        private static bool HasHigherPriority(PathQueueNode a, PathQueueNode b)
        {
            return a.Priority < b.Priority - PathCostEpsilon
                || (Mathf.Abs(a.Priority - b.Priority) <= PathCostEpsilon && a.Cost < b.Cost);
        }

        private readonly struct PathQueueNode
        {
            public PathQueueNode(Vector2Int cell, float cost, float priority)
            {
                Cell = cell;
                Cost = cost;
                Priority = priority;
            }

            public Vector2Int Cell { get; }
            public float Cost { get; }
            public float Priority { get; }
        }

        private readonly struct TrailPathDirection
        {
            public TrailPathDirection(Vector2Int delta, float cost)
            {
                Delta = delta;
                Cost = cost;
                IsDiagonal = delta.x != 0 && delta.y != 0;
            }

            public Vector2Int Delta { get; }
            public float Cost { get; }
            public bool IsDiagonal { get; }
        }
    }
}
