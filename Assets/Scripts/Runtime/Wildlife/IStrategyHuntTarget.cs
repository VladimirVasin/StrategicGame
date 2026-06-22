using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public interface IStrategyHuntTarget
    {
        bool IsAlive { get; }
        bool IsCarcass { get; }
        string HuntTargetKind { get; }
        Vector3 HuntWorldPosition { get; }

        bool TryGetCurrentCell(out Vector2Int cell);
        void ReleaseHuntReservation(object owner);
        bool ReceiveArrowHit(object owner, Vector3 hitWorld);
        bool ReactToHuntMiss(object owner, Vector3 threatWorld);
        bool ReceiveButcherHit(object owner, Vector3 hitWorld, out int gameAmount);
    }
}
