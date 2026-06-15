using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void StartPickingUpStorageLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeLogSource == null && activeLooseLogSource == null) || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageLogs;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLogSource != null ? activeLogSource.FootprintBounds : activeLooseLogSource.FootprintBounds;
            Vector2Int sourceOrigin = activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource.Origin;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void UpdatePickingUpStorageLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeLogSource == null && activeLooseLogSource == null)
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeLogSource != null)
                {
                    activeLogSource.ReleaseStoredLogsReservation(this);
                }

                if (activeLooseLogSource != null)
                {
                    activeLooseLogSource.ReleaseStorageReservation(this);
                }

                StrategyDebugLogger.Warn(
                    "Logistics",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_storage_path"),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            bool taken = activeLogSource != null
                ? activeLogSource.TryTakeReservedLogs(this, out carriedLogAmount)
                : activeLooseLogSource.TryTakeReservedForStorage(this, StrategyConstructionResourceKind.Logs, out carriedLogAmount);
            if (!taken)
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "LogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource != null ? activeLooseLogSource.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeLogSource != null ? activeLogSource.Origin : activeLooseLogSource.Origin;
            activeLogSource = null;
            activeLooseLogSource = null;
            activity = ResidentActivity.CarryingLogsToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedLogsVisible(true);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void StartDepositingStorageLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageLogs;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "StorageDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedLogAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStorageLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedLogAmount;
            if (storageWorkplace != null)
            {
                storageWorkplace.AddLogs(depositedAmount);
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            StrategyDebugLogger.Info(
                "Logistics",
                "LogsDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("yardStock", storageWorkplace != null ? storageWorkplace.LogsStored : -1));
            CompleteStorageDelivery();
        }

        private void StartPickingUpStorageStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeStoneSource == null && activeLooseStoneSource == null) || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageStone;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeStoneSource != null ? activeStoneSource.FootprintBounds : activeLooseStoneSource.FootprintBounds;
            Vector2Int sourceOrigin = activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource.Origin;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Logistics",
                "StonePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void UpdatePickingUpStorageStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeStoneSource == null && activeLooseStoneSource == null)
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (activeStoneSource != null)
                {
                    activeStoneSource.ReleaseStoredStoneReservation(this);
                }

                if (activeLooseStoneSource != null)
                {
                    activeLooseStoneSource.ReleaseStorageReservation(this);
                }

                StrategyDebugLogger.Warn(
                    "Logistics",
                    "StonePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_storage_path"),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            bool taken = activeStoneSource != null
                ? activeStoneSource.TryTakeReservedStone(this, out carriedStoneAmount)
                : activeLooseStoneSource.TryTakeReservedForStorage(this, StrategyConstructionResourceKind.Stone, out carriedStoneAmount);
            if (!taken)
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "StonePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource != null ? activeLooseStoneSource.Origin : Vector2Int.zero));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeStoneSource != null ? activeStoneSource.Origin : activeLooseStoneSource.Origin;
            activeStoneSource = null;
            activeLooseStoneSource = null;
            activity = ResidentActivity.CarryingStoneToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedStoneVisible(true);
            StrategyDebugLogger.Info(
                "Logistics",
                "StonePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void StartDepositingStorageStone()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageStone;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "StorageStoneDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedStoneAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingStorageStone()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedStoneVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedStoneAmount;
            if (storageWorkplace != null)
            {
                storageWorkplace.AddResource(StrategyResourceType.Stone, depositedAmount);
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            StrategyDebugLogger.Info(
                "Logistics",
                "StoneDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("yardStock", storageWorkplace != null ? storageWorkplace.StoneStored : -1));
            CompleteStorageDelivery();
        }

        private void StartPickingUpGranaryGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeGameSource == null && activeLooseFoodSource == null) || activeGranaryDeliveryTarget == null)
            {
                ResetGranaryWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpGranaryGame;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLooseFoodSource != null
                ? activeLooseFoodSource.FootprintBounds
                : activeGameSource.FootprintBounds;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Granary",
                "GamePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeLooseFoodSource != null ? activeLooseFoodSource.Origin : activeGameSource.Origin),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }

        private void UpdatePickingUpGranaryGame()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeGameSource == null && activeLooseFoodSource == null)
                || activeGranaryDeliveryTarget == null
                || !activeGranaryDeliveryTarget.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                activeGameSource?.ReleaseStoredGameReservation(this);
                activeLooseFoodSource?.ReleaseReservation(this);
                StrategyDebugLogger.Warn(
                    "Granary",
                    "GamePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_granary_path"),
                    StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
                ResetGranaryWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin;
            if (activeLooseFoodSource != null)
            {
                sourceOrigin = activeLooseFoodSource.Origin;
                if (!activeLooseFoodSource.TryTakeReserved(this, out StrategyResourceType resource, out carriedGameAmount)
                    || resource != StrategyResourceType.Game)
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "GamePickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "loose_take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    activeLooseFoodSource = null;
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeLooseFoodSource = null;
            }
            else if (!activeGameSource.TryTakeReservedGame(this, out carriedGameAmount))
            {
                StrategyDebugLogger.Warn(
                    "Granary",
                    "GamePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeGameSource.Origin));
                ResetGranaryWorkToIdle();
                return;
            }
            else
            {
                sourceOrigin = activeGameSource.Origin;
            }

            activeGameSource = null;
            activity = ResidentActivity.CarryingGameToGranary;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedGameVisible(true);
            StrategyDebugLogger.Info(
                "Granary",
                "GamePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }

        private void StartDepositingGranaryGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGranaryGame;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (activeGranaryDeliveryTarget != null)
            {
                FaceWorldPoint(activeGranaryDeliveryTarget.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Granary",
                "GameDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
        }
        private void UpdateDepositingGranaryGame()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedGameVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedGameAmount;
            if (activeGranaryDeliveryTarget != null)
            {
                activeGranaryDeliveryTarget.AddGame(depositedAmount);
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            StrategyDebugLogger.Info(
                "Granary",
                "GameDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("granaryStock", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.GameStored : -1));
            CompleteGranaryDelivery();
        }

        private void StartPickingUpGranaryFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeFishSource == null && activeLooseFoodSource == null) || activeGranaryDeliveryTarget == null)
            {
                ResetGranaryWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpGranaryFish;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLooseFoodSource != null
                ? activeLooseFoodSource.FootprintBounds
                : activeFishSource.FootprintBounds;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Granary",
                "FishPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", activeLooseFoodSource != null ? activeLooseFoodSource.Origin : activeFishSource.Origin),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }
    }
}
