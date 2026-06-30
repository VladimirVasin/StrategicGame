using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private StrategyRoadsidePropController roadsideProps;

        private void EnsureRoadsideProps()
        {
            roadsideProps ??= new StrategyRoadsidePropController();
            roadsideProps.Configure(map, transform);
        }

        private void QueueRoadsideRefreshAround(Vector2Int cell)
        {
            if (map == null)
            {
                return;
            }

            EnsureRoadsideProps();
            roadsideProps.QueueRefreshAround(cell);
        }

        private void QueueRoadsideRefreshArea(Vector2Int origin, Vector2Int size)
        {
            if (map == null)
            {
                return;
            }

            EnsureRoadsideProps();
            roadsideProps.QueueRefreshArea(origin, size);
        }

        private void FlushRoadsideRefreshes()
        {
            roadsideProps?.FlushPending(IsTrailCell);
        }

        private void ClearRoadsideProps()
        {
            roadsideProps?.Clear();
        }
    }
}
