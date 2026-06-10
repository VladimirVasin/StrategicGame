using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildingUpgradeAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.22f;

        private SpriteRenderer spriteRenderer;
        private StrategyBuildingUpgradeType type;
        private int frameIndex;
        private float frameTimer;

        public void Configure(SpriteRenderer renderer, StrategyBuildingUpgradeType upgradeType)
        {
            spriteRenderer = renderer;
            type = upgradeType;
            frameIndex = Random.Range(0, StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount);
            frameTimer = Random.Range(0f, FrameDuration);
            ApplyFrame();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
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
    }
}
