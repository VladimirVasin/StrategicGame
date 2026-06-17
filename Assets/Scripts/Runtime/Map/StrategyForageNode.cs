using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyForageNode : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float RegrowSecondsMin = 70f;
        private const float RegrowSecondsMax = 130f;

        private StrategyForageResourceController controller;
        private SpriteRenderer spriteRenderer;
        private StrategyResourceType resourceType;
        private Vector2Int cell;
        private int variant;
        private int yieldAmount;
        private float regrowTimer;
        private StrategyResidentAgent reservedBy;
        private StrategyPlacedBuilding reservedForHome;
        private bool depleted;

        public StrategyResourceType ResourceType => resourceType;
        public Vector2Int Cell => cell;
        public Bounds FootprintBounds => spriteRenderer != null ? spriteRenderer.bounds : new Bounds(transform.position, Vector3.one);
        public bool IsReady => !depleted && reservedBy == null && yieldAmount > 0;

        public void Configure(
            StrategyForageResourceController resourceController,
            StrategyResourceType type,
            Vector2Int nodeCell,
            int amount,
            int visualVariant,
            SpriteRenderer renderer)
        {
            controller = resourceController;
            resourceType = type;
            cell = nodeCell;
            yieldAmount = Mathf.Max(1, amount);
            variant = visualVariant;
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            depleted = false;
            regrowTimer = 0f;
            reservedBy = null;
            reservedForHome = null;
            UpdateVisual();
            AttachShadow();
            controller?.RegisterNode(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = StrategyWorldInspectInfoFactory.CreateForage(
                GetResourceTitle(resourceType),
                resourceType,
                yieldAmount,
                depleted,
                reservedBy != null,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                cell);
            return true;
        }

        private void OnDestroy()
        {
            controller?.UnregisterNode(this);
        }

        private void Update()
        {
            if (!depleted)
            {
                return;
            }

            regrowTimer -= Time.deltaTime;
            if (regrowTimer > 0f)
            {
                return;
            }

            depleted = false;
            reservedBy = null;
            reservedForHome = null;
            UpdateVisual();
            StrategyDebugLogger.Info(
                "Forage",
                "NodeRegrown",
                StrategyDebugLogger.F("resource", resourceType),
                StrategyDebugLogger.F("cell", cell));
        }

        public bool IsReservedBy(StrategyResidentAgent resident)
        {
            return reservedBy != null && reservedBy == resident;
        }

        public bool TryReserve(StrategyResidentAgent resident, StrategyPlacedBuilding home)
        {
            if (resident == null || home == null || depleted || yieldAmount <= 0)
            {
                return false;
            }

            if (reservedBy != null)
            {
                return reservedBy == resident;
            }

            reservedBy = resident;
            reservedForHome = home;
            UpdateVisual();
            StrategyDebugLogger.Info(
                "Forage",
                "NodeReserved",
                StrategyDebugLogger.F("resource", resourceType),
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
            return true;
        }

        public void Release(StrategyResidentAgent resident)
        {
            if (resident == null || reservedBy != resident)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Forage",
                "NodeReservationReleased",
                StrategyDebugLogger.F("resource", resourceType),
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("resident", resident.FullName));
            reservedBy = null;
            reservedForHome = null;
            UpdateVisual();
        }

        public bool TryGather(StrategyResidentAgent resident, out StrategyResourceType gatheredType, out int amount)
        {
            gatheredType = StrategyResourceType.None;
            amount = 0;
            if (resident == null || reservedBy != resident || depleted || yieldAmount <= 0)
            {
                return false;
            }

            gatheredType = resourceType;
            amount = yieldAmount;
            depleted = true;
            regrowTimer = Random.Range(RegrowSecondsMin, RegrowSecondsMax);
            reservedBy = null;
            reservedForHome = null;
            UpdateVisual();
            StrategyDebugLogger.Info(
                "Forage",
                "NodeGathered",
                StrategyDebugLogger.F("resource", resourceType),
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("regrowSeconds", regrowTimer));
            return true;
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyForageSpriteFactory.GetNodeSprite(resourceType, variant, depleted);
            spriteRenderer.color = depleted
                ? new Color(0.72f, 0.76f, 0.68f, 0.82f)
                : reservedBy != null
                    ? new Color(0.94f, 0.96f, 0.86f, 1f)
                    : Color.white;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, -1);
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
                new Vector2(0.02f, -0.02f),
                new Vector2(0.42f, 0.12f),
                0.10f,
                -3,
                0f,
                false);
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Berries => "Berries",
                StrategyResourceType.Roots => "Roots",
                StrategyResourceType.Mushrooms => "Mushrooms",
                _ => type.ToString()
            };
        }
    }
}
