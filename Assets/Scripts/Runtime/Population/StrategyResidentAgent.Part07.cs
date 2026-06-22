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

        private bool TryMoveToPlantingCell(Vector2Int cell)
        {
            if (!TryFindPlantingWorkCell(cell, out Vector2Int workCell))
            {
                workplace?.RegisterRejectedPlantingCell(cell, "no_work_cell");
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Population",
                    "PlantMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("plantCell", cell),
                    StrategyDebugLogger.F("reason", "no_work_cell"));
                return false;
            }

            plantingCell = cell;
            activity = ResidentActivity.MovingToPlantTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "PlantMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("plantCell", cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activity = ResidentActivity.Idle;
            workplace?.RegisterRejectedPlantingCell(cell, "no_path");
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "PlantMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", cell),
                StrategyDebugLogger.F("workCell", workCell),
                StrategyDebugLogger.F("reason", "no_path"));
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
            return TryFindReachableRingWorkCell(targetCell, out cell);
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

        private void StartGatheringForage()
        {
            if (activeForageNode == null || home == null || !activeForageNode.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.GatheringForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageGatherSecondsMin, ForageGatherSecondsMax);
            FaceWorldPoint(activeForageNode.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageGathering",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeForageNode.ResourceType),
                StrategyDebugLogger.F("nodeCell", activeForageNode.Cell));
        }

        private void UpdateGatheringForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateForageWork(
                activeForageNode != null ? activeForageNode.ResourceType : StrategyResourceType.Berries,
                false);
            if (activeForageNode != null)
            {
                FaceWorldPoint(activeForageNode.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeForageNode == null
                || !activeForageNode.TryGather(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = IsAdult ? amount : Mathf.Min(1, amount);
            activeForageNode = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToHomeDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartPickingUpLooseForage()
        {
            if (activeLooseForageSource == null
                || home == null
                || !activeLooseForageSource.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.PickingUpLooseForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(0.35f, 0.75f);
            FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForagePickup",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeLooseForageSource.Resource),
                StrategyDebugLogger.F("pileOrigin", activeLooseForageSource.Origin));
        }

        private void UpdatePickingUpLooseForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateForageWork(
                activeLooseForageSource != null ? activeLooseForageSource.Resource : StrategyResourceType.Berries,
                true);
            if (activeLooseForageSource != null)
            {
                FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeLooseForageSource == null
                || !activeLooseForageSource.TryTakeReserved(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = amount;
            activeLooseForageSource = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToHomeDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartDepositingForage()
        {
            if (home == null || home.Resources == null || carriedForageAmount <= 0)
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.DepositingForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageDepositSecondsMin, ForageDepositSecondsMax);
            FaceWorldPoint(home.FootprintBounds.center);
            SetCarriedForageVisible(true);
        }
    }
}
