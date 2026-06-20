using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyClayResourceController : MonoBehaviour
    {
        private readonly List<StrategyClayDeposit> deposits = new();
        private CityMapController map;

        public static StrategyClayResourceController Active { get; private set; }
        public IReadOnlyList<StrategyClayDeposit> Deposits => deposits;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            Active = this;
        }

        public void RegisterGeneratedDeposit(
            GameObject depositObject,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyClayDepositKind kind,
            int clayAmount)
        {
            if (depositObject == null || map == null)
            {
                return;
            }

            StrategyClayDeposit deposit = depositObject.GetComponent<StrategyClayDeposit>();
            if (deposit == null)
            {
                deposit = depositObject.AddComponent<StrategyClayDeposit>();
            }

            deposit.Configure(this, map, cell, footprint, kind, clayAmount);
        }

        public void RegisterDeposit(StrategyClayDeposit deposit)
        {
            if (deposit == null || deposits.Contains(deposit))
            {
                return;
            }

            deposits.Add(deposit);
        }

        public void UnregisterDeposit(StrategyClayDeposit deposit)
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
                StrategyClayDeposit deposit = deposits[i];
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

        public bool TryFindClayDepositInFootprint(Vector2Int origin, Vector2Int footprint, out StrategyClayDeposit deposit)
        {
            PruneNulls();
            deposit = null;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyClayDeposit candidate = deposits[i];
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

        public bool HasAvailableDepositAtCell(Vector2Int cell)
        {
            PruneNulls();
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyClayDeposit deposit = deposits[i];
                if (deposit != null
                    && !deposit.IsDepleted
                    && Overlaps(cell, Vector2Int.one, deposit.Cell, deposit.Footprint))
                {
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
