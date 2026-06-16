using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public interface IStrategyProductionLogisticsNode
    {
        Vector2Int Origin { get; }
        Bounds FootprintBounds { get; }

        bool TryFindDropoffCell(out Vector2Int cell);
        bool TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount);
        bool TryReserveInputDelivery(StrategyResourceType resource, object owner, int maxAmount, out int amount);
        bool TryAcceptInputDelivery(StrategyResourceType resource, object owner, int amount, out int accepted);
        void ReleaseInputDeliveryReservation(StrategyResourceType resource, object owner);
        bool TryGetOutputPickupRequest(out StrategyResourceType resource, out int amount);
        bool TryReserveOutputPickup(StrategyResourceType resource, object owner, out int amount);
        bool TryTakeReservedOutput(StrategyResourceType resource, object owner, out int amount);
        void ReleaseOutputPickupReservation(StrategyResourceType resource, object owner);
    }
}
