using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyIronResourceController : MonoBehaviour
    {
        private readonly List<StrategyIronDeposit> deposits = new();
        private CityMapController map;

        public static StrategyIronResourceController Active { get; private set; }
        public IReadOnlyList<StrategyIronDeposit> Deposits => deposits;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            Active = this;
        }

        public void RegisterGeneratedDeposit(
            GameObject depositObject,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyIronDepositKind kind,
            int ironAmount)
        {
            if (depositObject == null || map == null)
            {
                return;
            }

            StrategyIronDeposit deposit = depositObject.GetComponent<StrategyIronDeposit>();
            if (deposit == null)
            {
                deposit = depositObject.AddComponent<StrategyIronDeposit>();
            }

            deposit.Configure(this, map, cell, footprint, kind, ironAmount);
        }

        public void RegisterDeposit(StrategyIronDeposit deposit)
        {
            if (deposit == null || deposits.Contains(deposit))
            {
                return;
            }

            deposits.Add(deposit);
        }

        public void UnregisterDeposit(StrategyIronDeposit deposit)
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

        public int CountAvailableDeposits(Vector2Int origin, int radius)
        {
            PruneNulls();
            int count = 0;
            int radiusSqr = radius * radius;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit deposit = deposits[i];
                if (deposit != null
                    && !deposit.IsReserved
                    && !deposit.IsDepleted
                    && (deposit.Cell - origin).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountAvailableDepositsInFootprint(Vector2Int origin, Vector2Int footprint)
        {
            PruneNulls();
            int count = 0;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit deposit = deposits[i];
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

        public bool TryFindIronDeposit(Vector2Int origin, int radius, out StrategyIronDeposit deposit)
        {
            PruneNulls();
            deposit = null;
            int radiusSqr = radius * radius;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit candidate = deposits[i];
                if (candidate == null || candidate.IsReserved || candidate.IsDepleted)
                {
                    continue;
                }

                int distance = (candidate.Cell - origin).sqrMagnitude;
                if (distance > radiusSqr || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                deposit = candidate;
            }

            return deposit != null;
        }

        public bool TryFindIronDepositInFootprint(Vector2Int origin, Vector2Int footprint, out StrategyIronDeposit deposit)
        {
            PruneNulls();
            deposit = null;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit candidate = deposits[i];
                if (candidate == null
                    || candidate.IsReserved
                    || candidate.IsDepleted
                    || !Overlaps(origin, footprint, candidate.Cell, candidate.Footprint))
                {
                    continue;
                }

                deposit = candidate;
                return true;
            }

            return false;
        }

        public bool HasAvailableDepositAtCell(Vector2Int cell)
        {
            PruneNulls();
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit deposit = deposits[i];
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
