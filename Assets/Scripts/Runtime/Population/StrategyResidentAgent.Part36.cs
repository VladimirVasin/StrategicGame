using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool hasLastTrailFootfallCell;
        private Vector2Int lastTrailFootfallCell;

        private void MoveAlongCurrentPathTarget(Vector3 targetWorld)
        {
            if (TryHandleMovingToCombatRangeBeforeStep())
            {
                return;
            }

            float moveSpeed = GetCurrentMoveSpeed() * GetTrailMovementSpeedMultiplier(targetWorld);
            Vector3 delta = movement.MoveTowards(targetWorld, moveSpeed, Time.deltaTime);

            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
                SyncReadabilityRenderers();
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                RecordTrailFootfallAtPosition(transform.position);
                if (ShouldUseNightTorchCarryVisual())
                {
                    AnimateNightTorchWalk();
                }
                else
                {
                    AnimateWalk();
                }
            }
            else
            {
                if (ShouldUseNightTorchCarryVisual())
                {
                    UseNightTorchCarrySprite();
                }
                else
                {
                    AnimateIdle();
                }
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
                ResidentActivity.MovingToNightLight => 0.60f,
                ResidentActivity.LightingNightLight => 0f,
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
            lastPathBuildStatus = StrategyNavigationStatus.Invalid;
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
                lastPathBuildStatus = StrategyNavigationStatus.Success;
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.08f));
                pathIndex = 0;
                return true;
            }

            StrategyResidentPerformanceCounters.RecordPathRequest();
            bool isScoutRoute = activity is ResidentActivity.MovingToScoutFrontier
                or ResidentActivity.MovingToPointOfInterest
                or ResidentActivity.ReturningToScoutLodge;
            StrategyNavigationStatus status = movement.TryBuildPath(
                startCell,
                targetCell,
                evaluatingPlannedTasks && !isScoutRoute
                    ? StrategyNavigationPriority.Normal
                    : StrategyNavigationPriority.Critical,
                evaluatingPlannedTasks);
            lastPathBuildStatus = status;
            if (status == StrategyNavigationStatus.Deferred)
            {
                pathBuildDeferredDuringDecision = true;
                return false;
            }

            bool pathBuilt = status == StrategyNavigationStatus.Success;
            StrategyResidentPerformanceCounters.RecordPathResult(pathBuilt);
            if (!pathBuilt)
            {
                return false;
            }

            BuildTrailWorldPath(
                startCell,
                targetCell,
                movement.NavigationRawCells,
                movement.NavigationSmoothedCells);
            return path.Count > 0;
        }

        private bool WasLastPathBuildDeferred => lastPathBuildStatus == StrategyNavigationStatus.Deferred;

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
            movement.BuildWorldPath(smoothedCells, -0.08f, map.CellSize * 0.18f);
        }
    }
}
