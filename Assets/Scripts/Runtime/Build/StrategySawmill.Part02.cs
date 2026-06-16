using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySawmill : IStrategyProductionLogisticsNode
    {
        public bool TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
        {
            resource = StrategyResourceType.Logs;
            maxAmount = GetAvailableInputLogCapacity();
            return maxAmount > 0;
        }

        public bool TryReserveInputDelivery(StrategyResourceType resource, object owner, int maxAmount, out int amount)
        {
            amount = 0;
            if (resource != StrategyResourceType.Logs
                || owner == null
                || maxAmount <= 0
                || inputLogsReservationOwner != null && inputLogsReservationOwner != owner)
            {
                return false;
            }

            if (inputLogsReservationOwner == owner && reservedInputLogs > 0)
            {
                amount = reservedInputLogs;
                return true;
            }

            amount = Mathf.Min(maxAmount, GetAvailableInputLogCapacity());
            if (amount <= 0)
            {
                return false;
            }

            inputLogsReservationOwner = owner;
            reservedInputLogs = amount;
            StrategyDebugLogger.Info(
                "Logistics",
                "ProductionInputReserved",
                StrategyDebugLogger.F("node", "Sawmill"),
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public bool TryAcceptInputDelivery(StrategyResourceType resource, object owner, int amount, out int accepted)
        {
            accepted = 0;
            if (resource != StrategyResourceType.Logs
                || owner == null
                || inputLogsReservationOwner != owner
                || reservedInputLogs <= 0
                || amount <= 0)
            {
                return false;
            }

            int requested = Mathf.Min(amount, reservedInputLogs);
            int actualCapacity = Mathf.Min(
                Mathf.Max(0, MaxInputLogs - logsStored),
                Mathf.Max(0, StrategyProductionStorage.LocalCapacity - logsStored - planksStored - pendingPlanks));
            accepted = Mathf.Min(requested, actualCapacity);
            reservedInputLogs = Mathf.Max(0, reservedInputLogs - requested);
            if (reservedInputLogs <= 0)
            {
                inputLogsReservationOwner = null;
            }

            if (accepted <= 0)
            {
                return false;
            }

            logsStored += accepted;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Logistics",
                "ProductionInputDelivered",
                StrategyDebugLogger.F("node", "Sawmill"),
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("accepted", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("logsStored", logsStored));
            return true;
        }

        public void ReleaseInputDeliveryReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Logs && owner != null && inputLogsReservationOwner == owner)
            {
                inputLogsReservationOwner = null;
                reservedInputLogs = 0;
            }
        }

        public bool TryGetOutputPickupRequest(out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.Planks;
            amount = AvailablePlanks;
            return amount > 0;
        }

        public bool TryReserveOutputPickup(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Planks && TryReserveStoredPlanks(owner, out amount);
        }

        public bool TryTakeReservedOutput(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Planks && TryTakeReservedPlanks(owner, out amount);
        }

        public void ReleaseOutputPickupReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Planks)
            {
                ReleaseStoredPlanksReservation(owner);
            }
        }

        private int GetAvailableInputLogCapacity()
        {
            int logSpace = Mathf.Max(0, MaxInputLogs - logsStored - reservedInputLogs);
            int storageSpace = Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed);
            return Mathf.Min(logSpace, storageSpace);
        }
    }
}
