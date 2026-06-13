using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildingUpgradeAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.22f;

        private SpriteRenderer spriteRenderer;
        private StrategyBuildingUpgrade upgrade;
        private StrategyBuildingUpgradeType type;
        private int frameIndex;
        private float frameTimer;

        public void Configure(
            SpriteRenderer renderer,
            StrategyBuildingUpgradeType upgradeType,
            StrategyBuildingUpgrade linkedUpgrade = null)
        {
            spriteRenderer = renderer;
            upgrade = linkedUpgrade;
            type = upgradeType;
            frameIndex = GetSyncedProductionFrame();
            frameTimer = Random.Range(0f, FrameDuration);
            ApplyFrame();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (upgrade == null)
            {
                upgrade = GetComponent<StrategyBuildingUpgrade>();
            }
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (type == StrategyBuildingUpgradeType.GardenBeds || type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                int productionFrame = GetSyncedProductionFrame();
                if (productionFrame != frameIndex)
                {
                    frameIndex = productionFrame;
                    ApplyFrame();
                }

                return;
            }

            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frameIndex = (frameIndex + 1) % StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(type, frameIndex);
            }
        }

        private int GetGardenGrowthFrame()
        {
            if (upgrade == null)
            {
                return 0;
            }

            int frameCount = StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount;
            return Mathf.Clamp(Mathf.FloorToInt(upgrade.GardenGrowthProgress * frameCount), 0, frameCount - 1);
        }

        private int GetChickenCoopProductionFrame()
        {
            if (upgrade == null)
            {
                return 0;
            }

            int frameCount = StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount;
            return Mathf.Clamp(Mathf.FloorToInt(upgrade.ChickenCoopProductionProgress * frameCount), 0, frameCount - 1);
        }

        private int GetSyncedProductionFrame()
        {
            return type == StrategyBuildingUpgradeType.ChickenCoop
                ? GetChickenCoopProductionFrame()
                : GetGardenGrowthFrame();
        }
    }
}
