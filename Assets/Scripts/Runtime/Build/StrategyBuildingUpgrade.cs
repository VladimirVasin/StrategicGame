using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBuildingUpgradeType
    {
        GardenBeds,
        ChickenCoop
    }

    [DisallowMultipleComponent]
    public sealed class StrategyBuildingUpgrade : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float GardenHarvestSeconds = 8f;
        private const float ChickenCoopEggCycleSeconds = 22f;
        private const float ChickenCoopEggCycleJitterSeconds = 2.5f;
        private const float ChickenCoopEggReadyProgress = 5f / 6f;
        private const float GardenWorkGrowthBoostSeconds = 2.5f;

        private float productionTimer;
        private float productionCycleSeconds;
        private SpriteRenderer spriteRenderer;
        private bool chickenEggStoredThisCycle;

        public StrategyBuildingUpgradeType Type { get; private set; }
        public StrategyPlacedBuilding Owner { get; private set; }
        public Vector2Int Origin { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public Bounds FootprintBounds { get; private set; }
        public StrategyResourceType ProducedResource { get; private set; }
        public float GardenGrowthProgress => Type == StrategyBuildingUpgradeType.GardenBeds
            ? 1f - Mathf.Clamp01(productionTimer / GardenHarvestSeconds)
            : 0f;
        public float ChickenCoopProductionProgress => Type == StrategyBuildingUpgradeType.ChickenCoop && productionCycleSeconds > 0f
            ? 1f - Mathf.Clamp01(productionTimer / productionCycleSeconds)
            : 0f;
        public bool ChickenCoopEggVisible => Type == StrategyBuildingUpgradeType.ChickenCoop
            && ChickenCoopProductionProgress >= ChickenCoopEggReadyProgress;
        public float NextProductionSeconds => Mathf.Max(0f, productionTimer);

        public void Configure(
            StrategyBuildingUpgradeType type,
            StrategyPlacedBuilding owner,
            Vector2Int origin,
            Vector2Int footprint,
            Bounds footprintBounds,
            StrategyResourceType producedResource)
        {
            Type = type;
            Owner = owner;
            Origin = origin;
            Footprint = footprint;
            FootprintBounds = footprintBounds;
            ProducedResource = producedResource;
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (Type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                StartChickenCoopCycle(Random.Range(0.05f, 0.65f), false);
            }
            else if (Type == StrategyBuildingUpgradeType.GardenBeds)
            {
                productionCycleSeconds = GardenHarvestSeconds;
                productionTimer = Random.Range(GardenHarvestSeconds * 0.35f, GardenHarvestSeconds);
            }
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            string body = "Produces: "
                + GetResourceTitle(ProducedResource)
                + "\nProgress: "
                + Mathf.RoundToInt(GetProductionProgress() * 100f)
                + "%\nOwner: "
                + (Owner != null ? "House " + Owner.Origin.x + ", " + Owner.Origin.y : "none");
            info = new StrategyWorldInspectInfo(
                GetUpgradeTitle(Type),
                "Household upgrade",
                body,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                Origin,
                true);
            return true;
        }

        private void Update()
        {
            if (ProducedResource == StrategyResourceType.None
                || Owner == null
                || Owner.Resources == null)
            {
                return;
            }

            productionTimer -= Time.deltaTime;
            if (Type == StrategyBuildingUpgradeType.GardenBeds)
            {
                UpdateGardenProduction();
                return;
            }

            if (Type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                UpdateChickenCoopProduction();
                return;
            }
        }

        public void BoostGardenGrowthFromWork()
        {
            if (Type != StrategyBuildingUpgradeType.GardenBeds
                || ProducedResource == StrategyResourceType.None
                || Owner == null
                || Owner.Resources == null)
            {
                return;
            }

            productionTimer -= GardenWorkGrowthBoostSeconds;
            UpdateGardenProduction();
        }

        private void UpdateGardenProduction()
        {
            while (productionTimer <= 0f)
            {
                Owner.Resources.AddResource(ProducedResource, 1);
                productionTimer += GardenHarvestSeconds;
            }
        }

        private void UpdateChickenCoopProduction()
        {
            if (!chickenEggStoredThisCycle && ChickenCoopProductionProgress >= ChickenCoopEggReadyProgress)
            {
                Owner.Resources.AddResource(ProducedResource, 1);
                chickenEggStoredThisCycle = true;
                StrategyDebugLogger.Info(
                    "BuildingUpgrade",
                    "ChickenCoopEggStored",
                    StrategyDebugLogger.F("houseOrigin", Owner.Origin),
                    StrategyDebugLogger.F("coopOrigin", Origin),
                    StrategyDebugLogger.F("progress", ChickenCoopProductionProgress),
                    StrategyDebugLogger.F("eggCount", Owner.Resources.GetAmount(ProducedResource)));
            }

            if (productionTimer > 0f)
            {
                return;
            }

            float overflowSeconds = -productionTimer;
            StartChickenCoopCycle(0f, true);
            productionTimer = Mathf.Max(0.25f, productionTimer - overflowSeconds);
        }

        private void StartChickenCoopCycle(float startProgress, bool logStart)
        {
            productionCycleSeconds = Random.Range(
                ChickenCoopEggCycleSeconds - ChickenCoopEggCycleJitterSeconds,
                ChickenCoopEggCycleSeconds + ChickenCoopEggCycleJitterSeconds);
            productionTimer = productionCycleSeconds * (1f - Mathf.Clamp01(startProgress));
            chickenEggStoredThisCycle = false;

            if (logStart && Owner != null)
            {
                StrategyDebugLogger.Info(
                    "BuildingUpgrade",
                    "ChickenCoopCycleStarted",
                    StrategyDebugLogger.F("houseOrigin", Owner.Origin),
                    StrategyDebugLogger.F("coopOrigin", Origin),
                    StrategyDebugLogger.F("cycleSeconds", productionCycleSeconds));
            }
        }

        private float GetProductionProgress()
        {
            return Type == StrategyBuildingUpgradeType.GardenBeds
                ? GardenGrowthProgress
                : Type == StrategyBuildingUpgradeType.ChickenCoop
                    ? ChickenCoopProductionProgress
                    : 0f;
        }

        private static string GetUpgradeTitle(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds ? "Garden Beds" : "Chicken Coop";
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Turnip => "Turnip",
                StrategyResourceType.Cabbage => "Cabbage",
                StrategyResourceType.Onion => "Onion",
                StrategyResourceType.Carrot => "Carrot",
                StrategyResourceType.Potato => "Potato",
                _ => type.ToString()
            };
        }
    }
}
