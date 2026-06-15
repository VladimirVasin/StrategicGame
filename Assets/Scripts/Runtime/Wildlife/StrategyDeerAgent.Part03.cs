using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDeerAgent
    {
        private void AnimateSwim()
        {
            float bob = Mathf.Sin((Time.time + bobPhase) * 5.2f);
            SetAnimatedScale(1.06f + Mathf.Abs(bob) * 0.025f, 0.62f + bob * 0.025f);
            AdvanceLoopingFrame(4.6f, StrategyDeerSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyDeerSpritePose.Walk, frame);
        }

        private void UpdateSwimmingVisual()
        {
            bool active = StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position);
            StrategyWildlifeRiverCrossing.UpdateSwimRipple(
                transform,
                spriteRenderer,
                ref swimRippleRenderer,
                active,
                new Vector3(0f, 0.015f, 0.018f),
                sex == StrategyDeerSex.Male
                    ? new Vector3(1.16f, 0.70f, 1f)
                    : new Vector3(0.98f, 0.62f, 1f),
                bobPhase);
        }
    }
}
