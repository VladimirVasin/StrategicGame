using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public interface IStrategyConstructionResourceSource
    {
        Vector2Int Origin { get; }
        Bounds FootprintBounds { get; }

        bool TryReserveConstructionPickup(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int amount);

        void ReleaseConstructionPickupReservation(StrategyResidentAgent builder);

        bool TryTakeReservedConstructionResource(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int maxAmount,
            out int amount);
    }
}
