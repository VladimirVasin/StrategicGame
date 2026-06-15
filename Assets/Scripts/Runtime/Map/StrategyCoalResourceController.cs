using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCoalResourceController : MonoBehaviour
    {
        private readonly List<StrategyCoalDeposit> deposits = new();
        private CityMapController map;

        public static StrategyCoalResourceController Active { get; private set; }
        public IReadOnlyList<StrategyCoalDeposit> Deposits => deposits;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            Active = this;
        }

        public void RegisterGeneratedDeposit(
            GameObject depositObject,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyCoalDepositKind kind,
            int coalAmount)
        {
            if (depositObject == null || map == null)
            {
                return;
            }

            StrategyCoalDeposit deposit = depositObject.GetComponent<StrategyCoalDeposit>();
            if (deposit == null)
            {
                deposit = depositObject.AddComponent<StrategyCoalDeposit>();
            }

            deposit.Configure(this, cell, footprint, kind, coalAmount);
        }

        public void RegisterDeposit(StrategyCoalDeposit deposit)
        {
            if (deposit == null || deposits.Contains(deposit))
            {
                return;
            }

            deposits.Add(deposit);
        }

        public void UnregisterDeposit(StrategyCoalDeposit deposit)
        {
            if (deposit == null)
            {
                return;
            }

            deposits.Remove(deposit);
        }

        public int CountKnownDeposits()
        {
            PruneNulls();
            return deposits.Count;
        }

        public int CountAvailableDepositsInFootprint(Vector2Int origin, Vector2Int footprint)
        {
            PruneNulls();
            int count = 0;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyCoalDeposit deposit = deposits[i];
                if (deposit != null
                    && !deposit.IsReserved
                    && !deposit.IsDepleted
                    && Overlaps(origin, footprint, deposit.Cell, deposit.Footprint))
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryFindCoalDepositInFootprint(Vector2Int origin, Vector2Int footprint, out StrategyCoalDeposit deposit)
        {
            PruneNulls();
            deposit = null;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyCoalDeposit candidate = deposits[i];
                if (candidate != null
                    && !candidate.IsDepleted
                    && Overlaps(origin, footprint, candidate.Cell, candidate.Footprint))
                {
                    deposit = candidate;
                    return true;
                }
            }

            return false;
        }

        private void PruneNulls()
        {
            for (int i = deposits.Count - 1; i >= 0; i--)
            {
                if (deposits[i] == null)
                {
                    deposits.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private static bool Overlaps(Vector2Int aOrigin, Vector2Int aSize, Vector2Int bOrigin, Vector2Int bSize)
        {
            return aOrigin.x < bOrigin.x + bSize.x
                && aOrigin.x + aSize.x > bOrigin.x
                && aOrigin.y < bOrigin.y + bSize.y
                && aOrigin.y + aSize.y > bOrigin.y;
        }
    }
}
