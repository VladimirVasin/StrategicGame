using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void UpdateDepositingForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            SetCarriedForageVisible(true);
            if (forageWorkTimer > 0f)
            {
                return;
            }

            StrategyResourceType depositedResource = carriedForageResource;
            int depositedAmount = carriedForageAmount;
            if (home != null && home.Resources != null && depositedAmount > 0)
            {
                home.Resources.AddResource(depositedResource, depositedAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageDeposited",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", depositedResource),
                    StrategyDebugLogger.F("amount", depositedAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.45f, 1.05f);
        }

        private bool TryBuildPathToHomeDropoff()
        {
            if (map == null || home == null || !TryFindHomeExitWorld(out Vector3 exitWorld))
            {
                return false;
            }

            return map.TryWorldToCell(exitWorld, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell);
        }

        private void CancelForageWork(bool storeCarried)
        {
            if (!IsForagingActivity(activity)
                && activeForageNode == null
                && activeLooseForageSource == null
                && carriedForageAmount <= 0)
            {
                return;
            }

            ResetForageWorkToIdle(storeCarried);
        }

        private void ResetForageWorkToIdle(bool storeCarried)
        {
            if (activeForageNode != null)
            {
                activeForageNode.Release(this);
                activeForageNode = null;
            }

            if (activeLooseForageSource != null)
            {
                activeLooseForageSource.ReleaseReservation(this);
                activeLooseForageSource = null;
            }

            if (storeCarried && carriedForageAmount > 0 && home != null && home.Resources != null)
            {
                home.Resources.AddResource(carriedForageResource, carriedForageAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedForageResource),
                    StrategyDebugLogger.F("amount", carriedForageAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.30f, 0.85f);
        }

        private void StartChoppingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree != null && activeTree.CanBeBucked)
            {
                StartBuckingTree();
                return;
            }

            if (activeTree != null && activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (activeTree == null || !activeTree.CanBeChopped)
            {
                if (activeTree != null)
                {
                    activeTree.Release(this);
                }

                activeTree = null;
                activity = ResidentActivity.Idle;
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                waitTimer = Random.Range(0.35f, 0.85f);
                return;
            }

            activity = ResidentActivity.ChoppingTree;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeTree.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "ChopStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("treeCell", activeTree.Cell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateChoppingTree()
        {
            if (activeTree == null)
            {
                ResetLumberWorkToIdle();
                return;
            }

            if (activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (activeTree.CanBeBucked)
            {
                StartBuckingTree();
                return;
            }

            if (activeTree.IsFalling)
            {
                AnimateWoodcutHold();
                return;
            }

            if (!activeTree.CanBeChopped)
            {
                activeTree.Release(this);
                activeTree = null;
                activity = ResidentActivity.Idle;
                lumberWorkCooldown = Random.Range(2.0f, 4.0f);
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
                return;
            }

            AnimateChoppingWork();
        }

        private void StartBuckingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree == null || !activeTree.CanBeBucked)
            {
                ResetLumberWorkToIdle();
                return;
            }

            activity = ResidentActivity.BuckingTree;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeTree.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "BuckStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("treeCell", activeTree.Cell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateBuckingTree()
        {
            if (activeTree == null)
            {
                ResetLumberWorkToIdle();
                return;
            }

            if (activeTree.HasLogsReady)
            {
                StartCollectingLogs();
                return;
            }

            if (!activeTree.CanBeBucked)
            {
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            AnimateBuckingWork();
        }

        private void StartCollectingLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeTree == null || !activeTree.HasLogsReady || workplace == null)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", activeTree == null ? "logs_missing" : workplace == null ? "workplace_missing" : "logs_not_ready"));
                ResetLumberWorkToIdle();
                return;
            }

            Vector2Int logsCell = activeTree.Cell;
            if (!workplace.HasStorageSpaceFor(StrategyForestryTree.LogsPerTree))
            {
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            if (!workplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("logsCell", logsCell),
                    StrategyDebugLogger.F("campOrigin", workplace.Origin));
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            if (!activeTree.TryTakeLogs(this, out carriedLogAmount))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("logsCell", logsCell));
                activeTree.Release(this);
                activeTree = null;
                ResetLumberWorkToIdle();
                return;
            }

            activeTree = null;
            activity = ResidentActivity.CarryingLogs;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedLogsVisible(true);
            StrategyDebugLogger.Info(
                "Population",
                "LogsCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("logsCell", logsCell),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", workplace.Origin));
        }

        private void StartDepositingLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingLogs;
            lumberWorkTimer = Random.Range(LumberDepositSecondsMin, LumberDepositSecondsMax);
            if (workplace != null)
            {
                FaceWorldPoint(workplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Population",
                "LogsDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.2f, 3.4f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedLogAmount;
            if (workplace != null)
            {
                workplace.AddLogs(depositedAmount);
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            StrategyDebugLogger.Info(
                "Population",
                "LogsDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", workplace != null ? workplace.LogsStored : -1));
            CompleteLumberDelivery();
        }

        private void StartMiningStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeStoneDeposit == null || activeStoneDeposit.IsDepleted || stoneWorkplace == null)
            {
                ResetStoneWorkToIdle();
                return;
            }

            activity = ResidentActivity.MiningStone;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeStoneDeposit.transform.position);
            StrategyDebugLogger.Info(
                "Population",
                "StoneMiningStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("depositCell", activeStoneDeposit.Cell),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace.Origin));
        }

        private void UpdateMiningStone()
        {
            if (activeStoneDeposit == null || activeStoneDeposit.IsDepleted || stoneWorkplace == null)
            {
                ResetStoneWorkToIdle();
                return;
            }

            if (!stoneWorkplace.HasStorageSpace)
            {
                ResetStoneWorkToIdle();
                return;
            }

            AnimateStonecutWork();
        }

        private void StartCarryingMinedStone(int amount)
        {
            if (amount <= 0)
            {
                ResetStoneWorkToIdle();
                return;
            }

            if (stoneWorkplace == null
                || !stoneWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (stoneWorkplace != null)
                {
                    stoneWorkplace.AddStone(amount);
                }

                StrategyDebugLogger.Warn(
                    "Population",
                    "StoneCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"));
                carriedStoneAmount = 0;
                activeStoneDeposit = null;
                ResetStoneWorkToIdle();
                return;
            }

            carriedStoneAmount = amount;
            activeStoneDeposit = null;
            activity = ResidentActivity.CarryingStone;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedStoneVisible(true);
            StrategyDebugLogger.Info(
                "Population",
                "StoneCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace.Origin));
        }

        private void StartDepositingStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStone;
            lumberWorkTimer = Random.Range(LumberDepositSecondsMin, LumberDepositSecondsMax);
            if (stoneWorkplace != null)
            {
                FaceWorldPoint(stoneWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Population",
                "StoneDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.2f, 3.4f);
            SetCarriedStoneVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedStoneAmount;
            if (stoneWorkplace != null)
            {
                stoneWorkplace.AddStone(depositedAmount);
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            StrategyDebugLogger.Info(
                "Population",
                "StoneDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", stoneWorkplace != null ? stoneWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", stoneWorkplace != null ? stoneWorkplace.StoneStored : -1));
            CompleteStoneDelivery();
        }
    }
}
