using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCoalDepositKind
    {
        CoalDustGround,
        CoalSeam
    }

    [DisallowMultipleComponent]
    public sealed class StrategyCoalDeposit : MonoBehaviour, IStrategyWorldInspectable
    {
        private StrategyCoalResourceController controller;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private object reservedBy;
        private bool buildBlocked;

        public Vector2Int Cell { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public StrategyCoalDepositKind Kind { get; private set; }
        public int CoalAmount { get; private set; }
        public bool IsReserved => reservedBy != null;
        public bool IsDepleted => CoalAmount <= 0;

        public void Configure(
            StrategyCoalResourceController coalController,
            CityMapController mapController,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyCoalDepositKind kind,
            int coalAmount)
        {
            controller = coalController;
            map = mapController;
            Cell = cell;
            Footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            Kind = kind;
            CoalAmount = Mathf.Max(0, coalAmount);
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (map != null && CoalAmount > 0)
            {
                map.SetCellsBuildable(Cell, Footprint, false);
                buildBlocked = true;
            }

            controller?.RegisterDeposit(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = StrategyWorldInspectInfoFactory.CreateCoalDeposit(
                this,
                spriteRenderer != null ? spriteRenderer.sprite : null);
            return true;
        }

        private void OnDestroy()
        {
            if (buildBlocked)
            {
                map?.SetCellsBuildable(Cell, Footprint, true);
                buildBlocked = false;
            }

            controller?.UnregisterDeposit(this);
        }

        public bool TryReserve(object owner)
        {
            if (owner == null || IsDepleted || reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            reservedBy = owner;
            return true;
        }

        public void Release(object owner)
        {
            if (owner == null || reservedBy != owner)
            {
                return;
            }

            reservedBy = null;
        }

        public bool TryMine(object owner, int amount, out int minedAmount)
        {
            minedAmount = 0;
            if (owner == null || reservedBy != owner || amount <= 0 || IsDepleted)
            {
                return false;
            }

            minedAmount = Mathf.Min(amount, CoalAmount);
            CoalAmount -= minedAmount;
            if (CoalAmount <= 0)
            {
                StrategyDebugLogger.Info(
                    "Coal",
                    "CoalDepositDepleted",
                    StrategyDebugLogger.F("cell", Cell),
                    StrategyDebugLogger.F("kind", Kind));
                Destroy(gameObject);
            }

            return minedAmount > 0;
        }

        private static string GetCoalTitle(StrategyCoalDepositKind kind)
        {
            return kind switch
            {
                StrategyCoalDepositKind.CoalSeam => "Coal Seam",
                _ => "Coal Dust Ground"
            };
        }
    }
}
