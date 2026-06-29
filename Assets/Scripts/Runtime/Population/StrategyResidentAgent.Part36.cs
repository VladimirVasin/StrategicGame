using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int TrailPathBuildBudgetPerFrame = 32;
        private const float TrailPathBudgetLogIntervalSeconds = 6f;

        private static int trailPathBudgetFrame = -1;
        private static int trailPathBuildsThisFrame;
        private static int trailPathBudgetDeferralsSinceLog;
        private static float nextTrailPathBudgetLogTime;

        private readonly StrategyTrailPathfinder trailPathfinder = new();
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
                || !trails.RecordsFootfalls
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
            if (!suppressTrailRouteCapture)
            {
                ClearPendingTrailRoute();
            }

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

            if (!TryConsumeTrailPathBuildBudget())
            {
                return false;
            }

            if (!trailPathfinder.TryBuildPath(map, startCell, targetCell))
            {
                return false;
            }

            BuildTrailWorldPath(startCell, targetCell, trailPathfinder.RawCells, trailPathfinder.SmoothedCells);
            return path.Count > 0;
        }

        private static bool TryConsumeTrailPathBuildBudget()
        {
            int frame = Time.frameCount;
            if (trailPathBudgetFrame != frame)
            {
                trailPathBudgetFrame = frame;
                trailPathBuildsThisFrame = 0;
            }

            if (trailPathBuildsThisFrame < TrailPathBuildBudgetPerFrame)
            {
                trailPathBuildsThisFrame++;
                return true;
            }

            trailPathBudgetDeferralsSinceLog++;
            float now = Time.realtimeSinceStartup;
            if (now >= nextTrailPathBudgetLogTime)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentPathBuildBudgetDeferred",
                    StrategyDebugLogger.F("deferred", trailPathBudgetDeferralsSinceLog),
                    StrategyDebugLogger.F("budgetPerFrame", TrailPathBuildBudgetPerFrame));
                trailPathBudgetDeferralsSinceLog = 0;
                nextTrailPathBudgetLogTime = now + TrailPathBudgetLogIntervalSeconds;
            }

            return false;
        }

        private void BuildTrailWorldPath(
            Vector2Int startCell,
            Vector2Int targetCell,
            IReadOnlyList<Vector2Int> rawCells,
            IReadOnlyList<Vector2Int> smoothedCells)
        {
            if (rawCells == null || rawCells.Count <= 0 || smoothedCells == null || smoothedCells.Count <= 0)
            {
                ClearPendingTrailRoute();
                path.Clear();
                pathIndex = 0;
                return;
            }

            PrepareTrailRouteForBuiltPath(startCell, targetCell, smoothedCells);
            path.Clear();
            for (int i = 0; i < smoothedCells.Count; i++)
            {
                Vector2Int cell = smoothedCells[i];
                Vector3 center = map.GetCellCenterWorld(cell.x, cell.y);
                if (i == smoothedCells.Count - 1)
                {
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.08f));
            }

            pathIndex = 0;
        }
    }
}
