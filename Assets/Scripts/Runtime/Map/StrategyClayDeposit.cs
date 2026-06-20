using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyClayDepositKind
    {
        ClayPatch,
        ClayBank
    }

    [DisallowMultipleComponent]
    public sealed class StrategyClayDeposit : MonoBehaviour, IStrategyWorldInspectable
    {
        private StrategyClayResourceController controller;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private object reservedBy;
        private bool buildBlocked;

        public Vector2Int Cell { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public StrategyClayDepositKind Kind { get; private set; }
        public int ClayAmount { get; private set; }
        public bool IsReserved => reservedBy != null;
        public bool IsDepleted => ClayAmount <= 0;

        public void Configure(
            StrategyClayResourceController clayController,
            CityMapController mapController,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyClayDepositKind kind,
            int clayAmount)
        {
            controller = clayController;
            map = mapController;
            Cell = cell;
            Footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            Kind = kind;
            ClayAmount = Mathf.Max(0, clayAmount);
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (map != null && ClayAmount > 0)
            {
                map.SetCellsBuildable(Cell, Footprint, false);
                buildBlocked = true;
            }

            controller?.RegisterDeposit(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = StrategyWorldInspectInfoFactory.CreateClayDeposit(
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

            minedAmount = Mathf.Min(amount, ClayAmount);
            ClayAmount -= minedAmount;
            if (ClayAmount <= 0)
            {
                StrategyDebugLogger.Info(
                    "Clay",
                    "ClayDepositDepleted",
                    StrategyDebugLogger.F("cell", Cell),
                    StrategyDebugLogger.F("kind", Kind));
                Destroy(gameObject);
            }

            return minedAmount > 0;
        }
    }
}
