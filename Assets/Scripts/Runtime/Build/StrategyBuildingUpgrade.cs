using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBuildingUpgradeType
    {
        GardenBeds,
        ChickenCoop
    }

    [DisallowMultipleComponent]
    public sealed class StrategyBuildingUpgrade : MonoBehaviour
    {
        private const float GardenHarvestSeconds = 8f;
        private const float MinEggProductionDelay = 8f;
        private const float MaxEggProductionDelay = 14f;
        private const float GardenWorkGrowthBoostSeconds = 2.5f;

        private float productionTimer;

        public StrategyBuildingUpgradeType Type { get; private set; }
        public StrategyPlacedBuilding Owner { get; private set; }
        public Vector2Int Origin { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public Bounds FootprintBounds { get; private set; }
        public StrategyResourceType ProducedResource { get; private set; }
        public float GardenGrowthProgress => Type == StrategyBuildingUpgradeType.GardenBeds
            ? 1f - Mathf.Clamp01(productionTimer / GardenHarvestSeconds)
            : 0f;
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

            if (Type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                productionTimer = Random.Range(MinEggProductionDelay * 0.5f, MaxEggProductionDelay);
            }
            else if (Type == StrategyBuildingUpgradeType.GardenBeds)
            {
                productionTimer = Random.Range(GardenHarvestSeconds * 0.35f, GardenHarvestSeconds);
            }
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

            if (Type != StrategyBuildingUpgradeType.ChickenCoop)
            {
                return;
            }

            if (productionTimer > 0f)
            {
                return;
            }

            Owner.Resources.AddResource(ProducedResource, 1);
            productionTimer = Random.Range(MinEggProductionDelay, MaxEggProductionDelay);
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
    }
}
