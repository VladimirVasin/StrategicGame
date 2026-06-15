using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {
        private void AnimateSwim()
        {
            float bob = Mathf.Sin((Time.time + bobPhase) * 6.1f);
            SetAnimatedScale(1.12f + Mathf.Abs(bob) * 0.035f, 0.58f + bob * 0.025f);
            AdvanceLoopingFrame(5.0f, StrategyRabbitSpriteFactory.HopFrameCount);
            ApplySprite(StrategyRabbitSpritePose.Hop, frame);
        }

        private void UpdateSwimmingVisual()
        {
            bool active = StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position);
            StrategyWildlifeRiverCrossing.UpdateSwimRipple(
                transform,
                spriteRenderer,
                ref swimRippleRenderer,
                active,
                new Vector3(0f, 0.018f, 0.018f),
                new Vector3(0.58f, 0.42f, 1f),
                bobPhase);
        }
    }
}
