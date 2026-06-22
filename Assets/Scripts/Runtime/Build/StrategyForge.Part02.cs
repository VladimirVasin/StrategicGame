using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForge : IStrategyProductionLogisticsNode
    {
        public bool TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
        {
            int ironCapacity = GetAvailableInputIronCapacity();
            int coalCapacity = GetAvailableInputCoalCapacity();
            int logsCapacity = GetAvailableInputLogsCapacity();
            int ironCycles = ironStored + reservedInputIron;
            int coalCycles = coalStored + reservedInputCoal;
            int logsCycles = logsStored + reservedInputLogs;

            if (ironCycles < IronPerWorkCycle && ironCapacity > 0)
            {
                resource = StrategyResourceType.Iron;
                maxAmount = ironCapacity;
                return true;
            }

            if (coalCycles < CoalPerWorkCycle && coalCapacity > 0)
            {
                resource = StrategyResourceType.Coal;
                maxAmount = coalCapacity;
                return true;
            }

            if (logsCycles < LogsPerWorkCycle && logsCapacity > 0)
            {
                resource = StrategyResourceType.Logs;
                maxAmount = logsCapacity;
                return true;
            }

            if (ironCapacity > 0 && ironCycles <= coalCycles && ironCycles <= logsCycles)
            {
                resource = StrategyResourceType.Iron;
                maxAmount = ironCapacity;
                return true;
            }

            if (coalCapacity > 0 && coalCycles <= logsCycles)
            {
                resource = StrategyResourceType.Coal;
                maxAmount = coalCapacity;
                return true;
            }

            if (logsCapacity > 0)
            {
                resource = StrategyResourceType.Logs;
                maxAmount = logsCapacity;
                return true;
            }

            resource = StrategyResourceType.None;
            maxAmount = 0;
            return false;
        }

        public bool CanAcceptInputIron(int amount)
        {
            return amount > 0 && GetAvailableInputIronCapacity() >= amount;
        }

        public bool CanAcceptInputCoal(int amount)
        {
            return amount > 0 && GetAvailableInputCoalCapacity() >= amount;
        }

        public bool CanAcceptInputLogs(int amount)
        {
            return amount > 0 && GetAvailableInputLogsCapacity() >= amount;
        }

        public bool TryReserveInputDelivery(StrategyResourceType resource, object owner, int maxAmount, out int amount)
        {
            amount = 0;
            if (resource == StrategyResourceType.Iron)
            {
                return TryReserveInput(ref inputIronReservationOwner, ref reservedInputIron, GetAvailableInputIronCapacity(), owner, maxAmount, resource, out amount);
            }

            if (resource == StrategyResourceType.Coal)
            {
                return TryReserveInput(ref inputCoalReservationOwner, ref reservedInputCoal, GetAvailableInputCoalCapacity(), owner, maxAmount, resource, out amount);
            }

            if (resource == StrategyResourceType.Logs)
            {
                return TryReserveInput(ref inputLogsReservationOwner, ref reservedInputLogs, GetAvailableInputLogsCapacity(), owner, maxAmount, resource, out amount);
            }

            return false;
        }

        public bool TryAcceptInputDelivery(StrategyResourceType resource, object owner, int amount, out int accepted)
        {
            accepted = 0;
            if (resource == StrategyResourceType.Iron)
            {
                return TryAcceptInput(ref inputIronReservationOwner, ref reservedInputIron, ref ironStored, MaxInputIron, owner, amount, resource, out accepted);
            }

            if (resource == StrategyResourceType.Coal)
            {
                return TryAcceptInput(ref inputCoalReservationOwner, ref reservedInputCoal, ref coalStored, MaxInputCoal, owner, amount, resource, out accepted);
            }

            if (resource == StrategyResourceType.Logs)
            {
                return TryAcceptInput(ref inputLogsReservationOwner, ref reservedInputLogs, ref logsStored, MaxInputLogs, owner, amount, resource, out accepted);
            }

            return false;
        }

        public void ReleaseInputDeliveryReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Iron && owner != null && inputIronReservationOwner == owner)
            {
                inputIronReservationOwner = null;
                reservedInputIron = 0;
            }
            else if (resource == StrategyResourceType.Coal && owner != null && inputCoalReservationOwner == owner)
            {
                inputCoalReservationOwner = null;
                reservedInputCoal = 0;
            }
            else if (resource == StrategyResourceType.Logs && owner != null && inputLogsReservationOwner == owner)
            {
                inputLogsReservationOwner = null;
                reservedInputLogs = 0;
            }
        }

        public bool TryGetOutputPickupRequest(out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.Tools;
            amount = AvailableTools;
            return amount > 0;
        }

        public bool TryReserveOutputPickup(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Tools && TryReserveStoredTools(owner, out amount);
        }

        public bool TryTakeReservedOutput(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Tools && TryTakeReservedTools(owner, out amount);
        }

        public void ReleaseOutputPickupReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Tools)
            {
                ReleaseStoredToolsReservation(owner);
            }
        }

        private bool TryReserveInput(
            ref object reservationOwner,
            ref int reservedAmount,
            int capacity,
            object owner,
            int maxAmount,
            StrategyResourceType resource,
            out int amount)
        {
            amount = 0;
            if (owner == null || maxAmount <= 0 || reservationOwner != null && reservationOwner != owner)
            {
                return false;
            }

            if (reservationOwner == owner && reservedAmount > 0)
            {
                amount = reservedAmount;
                return true;
            }

            amount = Mathf.Min(maxAmount, capacity);
            if (amount <= 0)
            {
                return false;
            }

            reservationOwner = owner;
            reservedAmount = amount;
            StrategyDebugLogger.Info("Logistics", "ProductionInputReserved", StrategyDebugLogger.F("node", "Forge"), StrategyDebugLogger.F("origin", Origin), StrategyDebugLogger.F("resource", resource), StrategyDebugLogger.F("amount", amount));
            return true;
        }

        private bool TryAcceptInput(
            ref object reservationOwner,
            ref int reservedAmount,
            ref int stored,
            int maxStored,
            object owner,
            int amount,
            StrategyResourceType resource,
            out int accepted)
        {
            accepted = 0;
            if (owner == null || reservationOwner != owner || reservedAmount <= 0 || amount <= 0)
            {
                return false;
            }

            int requested = Mathf.Min(amount, reservedAmount);
            int capacity = Mathf.Min(Mathf.Max(0, maxStored - stored), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - StorageUsed - pendingTools));
            accepted = Mathf.Min(requested, capacity);
            reservedAmount = Mathf.Max(0, reservedAmount - requested);
            if (reservedAmount <= 0)
            {
                reservationOwner = null;
            }

            if (accepted <= 0)
            {
                return false;
            }

            stored += accepted;
            UpdateStockVisual();
            PlayInputDeliveredEffect(resource, accepted);
            StrategyDebugLogger.Info("Logistics", "ProductionInputDelivered", StrategyDebugLogger.F("node", "Forge"), StrategyDebugLogger.F("origin", Origin), StrategyDebugLogger.F("resource", resource), StrategyDebugLogger.F("accepted", accepted), StrategyDebugLogger.F("rejected", amount - accepted));
            return true;
        }

        private bool TryReserveStoredTools(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || toolsStored <= 0 || toolsReservationOwner != null && toolsReservationOwner != owner)
            {
                return false;
            }

            if (toolsReservationOwner == owner && reservedTools > 0)
            {
                amount = reservedTools;
                return true;
            }

            int available = AvailableTools;
            if (available <= 0)
            {
                return false;
            }

            reservedTools = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, available);
            toolsReservationOwner = owner;
            amount = reservedTools;
            return true;
        }

        private bool TryTakeReservedTools(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || toolsReservationOwner != owner || reservedTools <= 0 || toolsStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedTools, toolsStored);
            toolsStored -= amount;
            reservedTools = 0;
            toolsReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        private void ReleaseStoredToolsReservation(object owner)
        {
            if (owner != null && toolsReservationOwner == owner)
            {
                toolsReservationOwner = null;
                reservedTools = 0;
            }
        }

        private int GetAvailableInputIronCapacity()
        {
            return Mathf.Min(Mathf.Max(0, MaxInputIron - ironStored - reservedInputIron), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed));
        }

        private int GetAvailableInputCoalCapacity()
        {
            return Mathf.Min(Mathf.Max(0, MaxInputCoal - coalStored - reservedInputCoal), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed));
        }

        private int GetAvailableInputLogsCapacity()
        {
            return Mathf.Min(Mathf.Max(0, MaxInputLogs - logsStored - reservedInputLogs), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed));
        }
    }
}
