using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private void AnimateWalkOrSwim()
        {
            if (IsSwimming())
            {
                AnimateSwim();
                return;
            }

            AnimateWalk();
        }

        private void AnimateRunOrSwim()
        {
            if (IsSwimming())
            {
                AnimateSwim();
                return;
            }

            AnimateRun();
        }

        private void AnimateStalkOrSwim()
        {
            if (IsSwimming())
            {
                AnimateSwim();
                return;
            }

            AnimateStalk();
        }

        private void AnimateSwim()
        {
            AdvanceLoopingFrame(5.3f, StrategyWolfSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyWolfSpritePose.Walk, frame);
        }

        private bool IsSwimming()
        {
            return StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position);
        }

        private void UpdateSwimmingVisual()
        {
            StrategyWildlifeRiverCrossing.UpdateSwimRipple(
                transform,
                spriteRenderer,
                ref swimRippleRenderer,
                IsSwimming(),
                new Vector3(0f, 0.025f, 0.018f),
                new Vector3(0.92f, 0.56f, 1f),
                variant * 1.9f);
        }
    }
}
