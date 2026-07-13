using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForageResourceController
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

        private bool IsInsideAdditionalExclusion(Vector2Int cell)
        {
            return hasAdditionalExclusion && additionalExclusion.Contains(cell);
        }
    }
}
