using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private bool TryMoveToProcessableWood(StrategyForestryTree tree)
        {
            if (tree == null || !TryFindTreeWorkCell(tree, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LumberMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "wood"),
                    StrategyDebugLogger.F("reason", tree == null ? "wood_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("treeCell", tree != null ? tree.Cell : Vector2Int.zero));
                return false;
            }

            activeTree = tree;
            activity = tree.HasLogsReady ? ResidentActivity.MovingToLogs : ResidentActivity.MovingToTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "LumberMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", tree.HasLogsReady ? "logs" : "fallen_trunk"),
                    StrategyDebugLogger.F("treeCell", tree.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeTree = null;
            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "LumberMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("target", tree.HasLogsReady ? "logs" : "fallen_trunk"),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("treeCell", tree.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        private bool TryMoveToPlantingCell(Vector2Int cell, bool applyFailureCooldown = true)
        {
            if (!TryFindPlantingWorkCell(cell, out Vector2Int workCell))
            {
                workplace?.RegisterRejectedPlantingCell(cell, "no_work_cell");
                if (applyFailureCooldown)
                {
                    lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                }

                if (applyFailureCooldown)
                {
                    StrategyDebugLogger.Info(
                        "Population",
                        "PlantMoveDeferred",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("residentId", residentId),
                        StrategyDebugLogger.F("plantCell", cell),
                        StrategyDebugLogger.F("reason", "no_reachable_work_cell"));
                }
                return false;
            }

            plantingCell = cell;
            activity = ResidentActivity.MovingToPlantTree;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.22f);
            StrategyDebugLogger.Info(
                "Population",
                "PlantMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("plantCell", cell),
                StrategyDebugLogger.F("workCell", workCell));
            return true;
        }

        private bool TryStartPlantingTask()
        {
            if (workplace == null || !workplace.HasStorageSpaceFor(StrategyForestryTree.SmallTreeLogs))
            {
                return false;
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (!workplace.TryFindPlantingCell(out Vector2Int cell))
                {
                    return false;
                }

                if (TryMoveToPlantingCell(cell, false))
                {
                    return true;
                }
            }

            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            return false;
        }

        private bool TryMoveToStoneDeposit(StrategyStoneDeposit deposit)
        {
            if (deposit == null || !TryFindStoneWorkCell(deposit, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "StoneMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", deposit == null ? "deposit_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("depositCell", deposit != null ? deposit.Cell : Vector2Int.zero));
                return false;
            }

            activeStoneDeposit = deposit;
            activity = ResidentActivity.MovingToStone;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "StoneMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("depositCell", deposit.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeStoneDeposit = null;
            activity = ResidentActivity.Idle;
            stoneWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "StoneMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("depositCell", deposit.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        private bool TryFindPlantingWorkCell(Vector2Int targetCell, out Vector2Int cell)
        {
            return TryFindReachableRingWorkCell(targetCell, out cell, 4);
        }

        private bool IsPlantingCellOccupiedByResident()
        {
            if (population == null || map == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || resident == this
                    || resident.IsPendingRefugee
                    || resident.IsSleepingInsideHome
                    || resident.IsHomeboundYoungChild
                    || resident.Activity == ResidentActivity.StayingInsideHome
                    || !resident.gameObject.activeInHierarchy
                    || !map.TryWorldToCell(resident.transform.position, out Vector2Int cell))
                {
                    continue;
                }

                if (cell == plantingCell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindTreeWorkCell(StrategyForestryTree tree, out Vector2Int cell)
        {
            return TryFindReachableRingWorkCell(tree.Cell, out cell);
        }

        private bool TryFindStoneWorkCell(StrategyStoneDeposit deposit, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            Vector2Int origin = deposit.Cell;
            Vector2Int footprint = deposit.Footprint;
            for (int radius = 1; radius <= 3; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
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

            cell = default;
            return false;
        }

        private bool TryFindGardenWorkCell(StrategyBuildingUpgrade garden, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 2; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < garden.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < garden.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == garden.Footprint.x + radius - 1
                            || y == garden.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = garden.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !IsUpgradeCell(candidate, garden))
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

            cell = default;
            return false;
        }

        private void StartGardenWork()
        {
            activity = ResidentActivity.WorkingGarden;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            gardenWorkTimer = Random.Range(2.8f, 4.7f);

            if (activeGarden != null && spriteRenderer != null)
            {
                spriteRenderer.flipX = transform.position.x > activeGarden.FootprintBounds.center.x;
                SyncReadabilityRenderers();
            }
        }

        private void UpdateGardenWork()
        {
            gardenWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            if (gardenWorkTimer > 0f)
            {
                return;
            }

            CompleteGardenWork();
            activity = GetRestingActivity();
            activeGarden = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            waitTimer = Random.Range(0.45f, 1.2f);
        }

    }
}
