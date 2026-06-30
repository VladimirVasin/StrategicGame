using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    internal sealed class StrategyRoadsideLightSource : MonoBehaviour
    {
        public Vector2Int RoadCell { get; private set; }
        public Vector2Int SideOffset { get; private set; }

        public void Configure(Vector2Int roadCell, Vector2Int sideOffset)
        {
            RoadCell = roadCell;
            SideOffset = sideOffset;
        }
    }
}
