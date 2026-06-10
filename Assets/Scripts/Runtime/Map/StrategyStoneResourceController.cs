using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStoneResourceController : MonoBehaviour
    {
        private readonly List<StrategyStoneDeposit> deposits = new();
        private CityMapController map;

        public static StrategyStoneResourceController Active { get; private set; }
        public IReadOnlyList<StrategyStoneDeposit> Deposits => deposits;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            Active = this;
        }

        public void RegisterGeneratedDeposit(
            GameObject depositObject,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyStoneDepositKind kind,
            int stoneAmount)
        {
            if (depositObject == null || map == null)
            {
                return;
            }

            StrategyStoneDeposit deposit = depositObject.GetComponent<StrategyStoneDeposit>();
            if (deposit == null)
            {
                deposit = depositObject.AddComponent<StrategyStoneDeposit>();
            }

            deposit.Configure(this, map, cell, footprint, kind, stoneAmount);
        }

        public void RegisterDeposit(StrategyStoneDeposit deposit)
        {
            if (deposit == null || deposits.Contains(deposit))
            {
                return;
            }

            deposits.Add(deposit);
        }

        public void UnregisterDeposit(StrategyStoneDeposit deposit)
        {
            if (deposit == null)
            {
                return;
            }

            deposits.Remove(deposit);
        }

        public bool TryFindStoneDeposit(Vector2Int center, int radius, out StrategyStoneDeposit deposit)
        {
            PruneNulls();
            List<StrategyStoneDeposit> candidates = new();
            int radiusSqr = radius * radius;

            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyStoneDeposit candidate = deposits[i];
                if (candidate == null
                    || candidate.IsDepleted
                    || candidate.IsReserved
                    || (candidate.Cell - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            if (candidates.Count <= 0)
            {
                deposit = null;
                return false;
            }

            deposit = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        public int CountAvailableDeposits(Vector2Int center, int radius)
        {
            PruneNulls();
            int count = 0;
            int radiusSqr = radius * radius;

            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyStoneDeposit deposit = deposits[i];
                if (deposit != null
                    && !deposit.IsDepleted
                    && (deposit.Cell - center).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
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
    }
}
