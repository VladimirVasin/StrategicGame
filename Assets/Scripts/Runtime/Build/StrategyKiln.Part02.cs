using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyKiln : IStrategyProductionLogisticsNode
    {
        public bool TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
        {
            int clayCapacity = GetAvailableInputClayCapacity();
            int coalCapacity = GetAvailableInputCoalCapacity();
            int clayCycles = (clayStored + reservedInputClay) / ClayPerWorkCycle;
            int coalCycles = coalStored + reservedInputCoal;
            bool needsClay = clayStored + reservedInputClay < ClayPerWorkCycle;
            bool needsCoal = coalStored + reservedInputCoal < CoalPerWorkCycle;

            if ((needsClay || clayCycles <= coalCycles) && clayCapacity > 0)
            {
                resource = StrategyResourceType.Clay;
                maxAmount = clayCapacity;
                return true;
            }

            if (coalCapacity > 0)
            {
                resource = StrategyResourceType.Coal;
                maxAmount = coalCapacity;
                return true;
            }

            resource = StrategyResourceType.None;
            maxAmount = 0;
            return false;
        }

        public bool CanAcceptInputClay(int amount)
        {
            return amount > 0 && GetAvailableInputClayCapacity() >= amount;
        }

        public bool CanAcceptInputCoal(int amount)
        {
            return amount > 0 && GetAvailableInputCoalCapacity() >= amount;
        }

        public bool TryReserveInputDelivery(StrategyResourceType resource, object owner, int maxAmount, out int amount)
        {
            amount = 0;
            if (resource == StrategyResourceType.Clay)
            {
                return TryReserveInput(ref inputClayReservationOwner, ref reservedInputClay, GetAvailableInputClayCapacity(), owner, maxAmount, resource, out amount);
            }

            if (resource == StrategyResourceType.Coal)
            {
                return TryReserveInput(ref inputCoalReservationOwner, ref reservedInputCoal, GetAvailableInputCoalCapacity(), owner, maxAmount, resource, out amount);
            }

            return false;
        }

        public bool TryAcceptInputDelivery(StrategyResourceType resource, object owner, int amount, out int accepted)
        {
            accepted = 0;
            if (resource == StrategyResourceType.Clay)
            {
                return TryAcceptInput(ref inputClayReservationOwner, ref reservedInputClay, ref clayStored, MaxInputClay, owner, amount, resource, out accepted);
            }

            if (resource == StrategyResourceType.Coal)
            {
                return TryAcceptInput(ref inputCoalReservationOwner, ref reservedInputCoal, ref coalStored, MaxInputCoal, owner, amount, resource, out accepted);
            }

            return false;
        }

        public void ReleaseInputDeliveryReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Clay && owner != null && inputClayReservationOwner == owner)
            {
                inputClayReservationOwner = null;
                reservedInputClay = 0;
            }
            else if (resource == StrategyResourceType.Coal && owner != null && inputCoalReservationOwner == owner)
            {
                inputCoalReservationOwner = null;
                reservedInputCoal = 0;
            }
        }

        public bool TryGetOutputPickupRequest(out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.Pottery;
            amount = AvailablePottery;
            return amount > 0;
        }

        public bool TryReserveOutputPickup(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Pottery && TryReserveStoredPottery(owner, out amount);
        }

        public bool TryTakeReservedOutput(StrategyResourceType resource, object owner, out int amount)
        {
            amount = 0;
            return resource == StrategyResourceType.Pottery && TryTakeReservedPottery(owner, out amount);
        }

        public void ReleaseOutputPickupReservation(StrategyResourceType resource, object owner)
        {
            if (resource == StrategyResourceType.Pottery)
            {
                ReleaseStoredPotteryReservation(owner);
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
            StrategyDebugLogger.Info("Logistics", "ProductionInputReserved", StrategyDebugLogger.F("node", "Kiln"), StrategyDebugLogger.F("origin", Origin), StrategyDebugLogger.F("resource", resource), StrategyDebugLogger.F("amount", amount));
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
            int capacity = Mathf.Min(Mathf.Max(0, maxStored - stored), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - StorageUsed - pendingPottery));
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
            StrategyDebugLogger.Info("Logistics", "ProductionInputDelivered", StrategyDebugLogger.F("node", "Kiln"), StrategyDebugLogger.F("origin", Origin), StrategyDebugLogger.F("resource", resource), StrategyDebugLogger.F("accepted", accepted), StrategyDebugLogger.F("rejected", amount - accepted));
            return true;
        }

        private bool TryReserveStoredPottery(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || potteryStored <= 0 || potteryReservationOwner != null && potteryReservationOwner != owner)
            {
                return false;
            }

            if (potteryReservationOwner == owner && reservedPottery > 0)
            {
                amount = reservedPottery;
                return true;
            }

            int available = AvailablePottery;
            if (available <= 0)
            {
                return false;
            }

            reservedPottery = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, available);
            potteryReservationOwner = owner;
            amount = reservedPottery;
            return true;
        }

        private bool TryTakeReservedPottery(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || potteryReservationOwner != owner || reservedPottery <= 0 || potteryStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedPottery, potteryStored);
            potteryStored -= amount;
            reservedPottery = 0;
            potteryReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        private void ReleaseStoredPotteryReservation(object owner)
        {
            if (owner != null && potteryReservationOwner == owner)
            {
                potteryReservationOwner = null;
                reservedPottery = 0;
            }
        }

        private int GetAvailableInputClayCapacity()
        {
            return Mathf.Min(Mathf.Max(0, MaxInputClay - clayStored - reservedInputClay), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed));
        }

        private int GetAvailableInputCoalCapacity()
        {
            return Mathf.Min(Mathf.Max(0, MaxInputCoal - coalStored - reservedInputCoal), Mathf.Max(0, StrategyProductionStorage.LocalCapacity - ReservedStorageUsed));
        }
    }
}
