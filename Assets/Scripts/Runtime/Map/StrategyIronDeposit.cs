using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyIronDepositKind
    {
        IronStainedGround,
        IronVein
    }

    [DisallowMultipleComponent]
    public sealed class StrategyIronDeposit : MonoBehaviour, IStrategyWorldInspectable
    {
        private StrategyIronResourceController controller;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private object reservedBy;
        private bool buildBlocked;

        public Vector2Int Cell { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public StrategyIronDepositKind Kind { get; private set; }
        public int IronAmount { get; private set; }
        public bool IsReserved => reservedBy != null;
        public bool IsDepleted => IronAmount <= 0;

        public void Configure(
            StrategyIronResourceController ironController,
            CityMapController mapController,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyIronDepositKind kind,
            int ironAmount)
        {
            controller = ironController;
            map = mapController;
            Cell = cell;
            Footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            Kind = kind;
            IronAmount = Mathf.Max(0, ironAmount);
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (map != null && IronAmount > 0)
            {
                map.SetCellsBuildable(Cell, Footprint, false);
                buildBlocked = true;
            }

            controller?.RegisterDeposit(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            string body = "Iron ore: "
                + IronAmount
                + "\nFootprint: "
                + Footprint.x
                + "x"
                + Footprint.y
                + "\nState: "
                + (IsDepleted ? "depleted" : IsReserved ? "reserved for mining" : "underground, mineable")
                + "\nBlocks movement: no"
                + "\nBlocks building: yes, except Mine";
            info = new StrategyWorldInspectInfo(
                GetIronTitle(Kind),
                "Iron deposit",
                body,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                Cell,
                true);
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

            minedAmount = Mathf.Min(amount, IronAmount);
            IronAmount -= minedAmount;
            if (IronAmount <= 0)
            {
                StrategyDebugLogger.Info(
                    "Iron",
                    "IronDepositDepleted",
                    StrategyDebugLogger.F("cell", Cell),
                    StrategyDebugLogger.F("kind", Kind));
                Destroy(gameObject);
            }

            return minedAmount > 0;
        }

        private static string GetIronTitle(StrategyIronDepositKind kind)
        {
            return kind switch
            {
                StrategyIronDepositKind.IronVein => "Iron Vein",
                _ => "Iron-stained Ground"
            };
        }
    }
}
