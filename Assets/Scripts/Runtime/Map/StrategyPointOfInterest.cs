using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyPointOfInterestResourceKind
    {
        None = 0,
        Coal = 1,
        Iron = 2
    }

    [DisallowMultipleComponent]
    public sealed class StrategyPointOfInterest : MonoBehaviour
    {
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private StrategyResidentAgent reservedBy;
        private bool buildabilityBlocked;

        public string StableId { get; private set; } = string.Empty;
        public Vector2Int Cell { get; private set; }
        public StrategyPointOfInterestResourceKind ResourceKind { get; private set; }
        public bool HasMineralSite { get; private set; }
        public Vector2Int MineralOrigin { get; private set; }
        public bool IsInvestigated { get; private set; }
        public Bounds FootprintBounds => spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, Vector3.one);

        internal bool IsReserved => reservedBy != null;

        internal void Configure(
            CityMapController mapController,
            string stableId,
            Vector2Int cell,
            StrategyPointOfInterestResourceKind resourceKind,
            bool hasMineralSite,
            Vector2Int mineralOrigin,
            bool investigated,
            SpriteRenderer renderer)
        {
            ReleaseMapBuildability();
            map = mapController;
            StableId = string.IsNullOrWhiteSpace(stableId)
                ? BuildStableId(cell)
                : stableId;
            Cell = cell;
            ResourceKind = resourceKind;
            HasMineralSite = hasMineralSite;
            MineralOrigin = hasMineralSite ? mineralOrigin : default;
            IsInvestigated = investigated;
            reservedBy = null;
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            BlockMapBuildability();
            RefreshVisual();
            AttachShadow();
        }

        public bool IsReservedBy(StrategyResidentAgent resident)
        {
            return resident != null && reservedBy == resident;
        }

        internal bool TryReserve(StrategyResidentAgent resident)
        {
            if (resident == null || IsInvestigated)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != resident)
            {
                return false;
            }

            reservedBy = resident;
            RefreshVisual();
            return true;
        }

        internal void ReleaseReservation(StrategyResidentAgent resident)
        {
            if (resident == null || reservedBy != resident)
            {
                return;
            }

            reservedBy = null;
            RefreshVisual();
        }

        internal bool MarkInvestigated(StrategyResidentAgent resident)
        {
            if (IsInvestigated || resident == null || reservedBy != resident)
            {
                return false;
            }

            IsInvestigated = true;
            reservedBy = null;
            RefreshVisual();
            return true;
        }

        internal void ReleaseMapBuildability()
        {
            if (!buildabilityBlocked)
            {
                return;
            }

            if (map != null)
            {
                map.SetCellsBuildable(Cell, Vector2Int.one, true);
            }

            buildabilityBlocked = false;
        }

        internal static string BuildStableId(Vector2Int cell)
        {
            return "poi-" + cell.x + "-" + cell.y;
        }

        private void BlockMapBuildability()
        {
            if (map == null || buildabilityBlocked)
            {
                return;
            }

            map.SetCellsBuildable(Cell, Vector2Int.one, false);
            buildabilityBlocked = true;
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyPointOfInterestSpriteFactory.GetSprite(IsInvestigated);
            spriteRenderer.color = IsInvestigated
                ? GetInvestigatedColor()
                : reservedBy != null
                    ? new Color(1f, 0.94f, 0.67f, 1f)
                    : Color.white;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 1);
        }

        private Color GetInvestigatedColor()
        {
            return ResourceKind switch
            {
                StrategyPointOfInterestResourceKind.Coal => new Color(0.68f, 0.72f, 0.74f, 0.94f),
                StrategyPointOfInterestResourceKind.Iron => new Color(0.92f, 0.66f, 0.48f, 0.94f),
                _ => new Color(0.82f, 0.92f, 0.84f, 0.92f)
            };
        }

        private void AttachShadow()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0f, -0.02f),
                new Vector2(0.48f, 0.14f),
                0.12f,
                -3,
                0f,
                false);
        }

        private void OnDestroy()
        {
            ReleaseMapBuildability();
        }
    }
}
