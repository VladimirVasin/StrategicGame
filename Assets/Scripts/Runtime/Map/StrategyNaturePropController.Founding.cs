using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private RectInt additionalExclusion;
        private bool hasAdditionalExclusion;

        public void SetAdditionalExclusion(Vector2Int origin, Vector2Int footprint)
        {
            hasAdditionalExclusion = footprint.x > 0 && footprint.y > 0;
            additionalExclusion = hasAdditionalExclusion
                ? new RectInt(origin, footprint)
                : default;
        }

        private bool IsInsideAdditionalExclusion(int x, int y)
        {
            return hasAdditionalExclusion && additionalExclusion.Contains(new Vector2Int(x, y));
        }
    }
}
