using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFogOfWarController
    {
        public void CaptureExploredCells(List<int> target)
        {
            target.Clear();
            EnsureState();
            if (map == null || explored == null)
            {
                return;
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (explored[x, y])
                    {
                        target.Add(y * map.Width + x);
                    }
                }
            }
        }

        public void RestoreExploredCells(IReadOnlyList<int> source)
        {
            EnsureState();
            if (map == null || explored == null)
            {
                return;
            }

            System.Array.Clear(explored, 0, explored.Length);
            if (source != null)
            {
                int cellCount = map.Width * map.Height;
                for (int i = 0; i < source.Count; i++)
                {
                    int key = source[i];
                    if (key >= 0 && key < cellCount)
                    {
                        explored[key % map.Width, key / map.Width] = true;
                    }
                }
            }

            RequestRefresh();
        }
    }
}
