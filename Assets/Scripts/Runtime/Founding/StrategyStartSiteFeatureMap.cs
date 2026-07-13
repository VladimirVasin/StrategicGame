using System;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyStartSiteFeatureMap
    {
        private const int AnyWaterSource = 0;
        private const int RiverSource = 1;
        private const int LakeSource = 2;

        private readonly StrategyStartSiteMapSnapshot map;
        private readonly int prefixStride;
        private readonly int[] nearestWater;
        private readonly int[] nearestRiver;
        private readonly int[] nearestLake;
        private readonly int[] connectedLandSize;
        private readonly int[] forestPrefix;
        private readonly int[] meadowPrefix;
        private readonly int[] grassPrefix;
        private readonly int[] dirtPrefix;
        private readonly int[] openPrefix;
        private readonly int[] buildablePrefix;

        public StrategyStartSiteFeatureMap(StrategyStartSiteMapSnapshot map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            prefixStride = map.Width + 1;
            nearestWater = BuildDistanceMap(AnyWaterSource);
            nearestRiver = BuildDistanceMap(RiverSource);
            nearestLake = BuildDistanceMap(LakeSource);
            connectedLandSize = BuildConnectedLandSizes();

            int prefixLength = (map.Width + 1) * (map.Height + 1);
            forestPrefix = new int[prefixLength];
            meadowPrefix = new int[prefixLength];
            grassPrefix = new int[prefixLength];
            dirtPrefix = new int[prefixLength];
            openPrefix = new int[prefixLength];
            buildablePrefix = new int[prefixLength];
            BuildIntegralMaps();
        }

        public StrategyStartSiteMapSnapshot Map => map;
        public int MissingSourceDistance => map.Width + map.Height + 1;

        public int GetNearestWaterDistance(int x, int y)
        {
            return nearestWater[GetIndex(x, y)];
        }

        public int GetNearestRiverDistance(int x, int y)
        {
            return nearestRiver[GetIndex(x, y)];
        }

        public int GetNearestLakeDistance(int x, int y)
        {
            return nearestLake[GetIndex(x, y)];
        }

        public int GetConnectedLandSize(int x, int y)
        {
            return connectedLandSize[GetIndex(x, y)];
        }

        public int CountForest(int x, int y, int radius)
        {
            return Query(forestPrefix, x, y, radius);
        }

        public int CountMeadow(int x, int y, int radius)
        {
            return Query(meadowPrefix, x, y, radius);
        }

        public int CountGrass(int x, int y, int radius)
        {
            return Query(grassPrefix, x, y, radius);
        }

        public int CountDirt(int x, int y, int radius)
        {
            return Query(dirtPrefix, x, y, radius);
        }

        public int CountOpen(int x, int y, int radius)
        {
            return Query(openPrefix, x, y, radius);
        }

        public int CountBuildable(int x, int y, int radius)
        {
            return Query(buildablePrefix, x, y, radius);
        }

        public int GetAreaCellCount(int x, int y, int radius)
        {
            int minX = Math.Max(0, x - radius);
            int maxX = Math.Min(map.Width - 1, x + radius);
            int minY = Math.Max(0, y - radius);
            int maxY = Math.Min(map.Height - 1, y + radius);
            return (maxX - minX + 1) * (maxY - minY + 1);
        }

        public static int GetDiagnosticDistance(int distance, int missingSourceDistance)
        {
            return distance >= missingSourceDistance ? -1 : distance;
        }

        private int[] BuildDistanceMap(int sourceKind)
        {
            int count = map.CellCount;
            int missingDistance = MissingSourceDistance;
            int[] distances = new int[count];
            int[] queue = new int[count];
            int head = 0;
            int tail = 0;

            for (int i = 0; i < count; i++)
            {
                distances[i] = missingDistance;
                StrategyStartSiteCell cell = map.GetCell(i % map.Width, i / map.Width);
                if (!IsDistanceSource(cell, sourceKind))
                {
                    continue;
                }

                distances[i] = 0;
                queue[tail++] = i;
            }

            while (head < tail)
            {
                int index = queue[head++];
                int x = index % map.Width;
                int y = index / map.Width;
                int nextDistance = distances[index] + 1;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if ((offsetX == 0 && offsetY == 0)
                            || !map.IsInside(x + offsetX, y + offsetY))
                        {
                            continue;
                        }

                        int neighbor = GetIndex(x + offsetX, y + offsetY);
                        if (distances[neighbor] <= nextDistance)
                        {
                            continue;
                        }

                        distances[neighbor] = nextDistance;
                        queue[tail++] = neighbor;
                    }
                }
            }

            return distances;
        }

        private int[] BuildConnectedLandSizes()
        {
            int[] sizes = new int[map.CellCount];
            bool[] visited = new bool[map.CellCount];
            int[] queue = new int[map.CellCount];
            int[] offsetX = { -1, 1, 0, 0 };
            int[] offsetY = { 0, 0, -1, 1 };

            for (int start = 0; start < map.CellCount; start++)
            {
                int startX = start % map.Width;
                int startY = start / map.Width;
                if (visited[start] || !IsConnectedLand(map.GetCell(startX, startY)))
                {
                    continue;
                }

                int head = 0;
                int tail = 0;
                visited[start] = true;
                queue[tail++] = start;
                while (head < tail)
                {
                    int index = queue[head++];
                    int x = index % map.Width;
                    int y = index / map.Width;
                    for (int direction = 0; direction < offsetX.Length; direction++)
                    {
                        int nextX = x + offsetX[direction];
                        int nextY = y + offsetY[direction];
                        if (!map.IsInside(nextX, nextY))
                        {
                            continue;
                        }

                        int neighbor = GetIndex(nextX, nextY);
                        if (visited[neighbor] || !IsConnectedLand(map.GetCell(nextX, nextY)))
                        {
                            continue;
                        }

                        visited[neighbor] = true;
                        queue[tail++] = neighbor;
                    }
                }

                for (int i = 0; i < tail; i++)
                {
                    sizes[queue[i]] = tail;
                }
            }

            return sizes;
        }

        private void BuildIntegralMaps()
        {
            for (int y = 0; y < map.Height; y++)
            {
                int forestRow = 0;
                int meadowRow = 0;
                int grassRow = 0;
                int dirtRow = 0;
                int openRow = 0;
                int buildableRow = 0;
                for (int x = 0; x < map.Width; x++)
                {
                    StrategyStartSiteCell cell = map.GetCell(x, y);
                    forestRow += cell.Kind == CityMapCellKind.Forest ? 1 : 0;
                    meadowRow += cell.Kind == CityMapCellKind.Meadow ? 1 : 0;
                    grassRow += cell.Kind == CityMapCellKind.Grass ? 1 : 0;
                    dirtRow += cell.Kind == CityMapCellKind.Dirt ? 1 : 0;
                    openRow += cell.IsOpenLand ? 1 : 0;
                    buildableRow += cell.IsBuildable && cell.IsWalkable && cell.IsDryLand ? 1 : 0;

                    int current = x + 1 + (y + 1) * prefixStride;
                    int above = x + 1 + y * prefixStride;
                    forestPrefix[current] = forestPrefix[above] + forestRow;
                    meadowPrefix[current] = meadowPrefix[above] + meadowRow;
                    grassPrefix[current] = grassPrefix[above] + grassRow;
                    dirtPrefix[current] = dirtPrefix[above] + dirtRow;
                    openPrefix[current] = openPrefix[above] + openRow;
                    buildablePrefix[current] = buildablePrefix[above] + buildableRow;
                }
            }
        }

        private int Query(int[] prefix, int x, int y, int radius)
        {
            int minX = Math.Max(0, x - radius);
            int maxX = Math.Min(map.Width - 1, x + radius) + 1;
            int minY = Math.Max(0, y - radius);
            int maxY = Math.Min(map.Height - 1, y + radius) + 1;
            return prefix[maxX + maxY * prefixStride]
                - prefix[minX + maxY * prefixStride]
                - prefix[maxX + minY * prefixStride]
                + prefix[minX + minY * prefixStride];
        }

        private int GetIndex(int x, int y)
        {
            return x + y * map.Width;
        }

        private static bool IsDistanceSource(StrategyStartSiteCell cell, int sourceKind)
        {
            if (!cell.IsWater && !cell.IsShore)
            {
                return false;
            }

            return sourceKind == AnyWaterSource
                || (sourceKind == RiverSource && cell.WaterKind == CityMapWaterKind.River)
                || (sourceKind == LakeSource && cell.WaterKind == CityMapWaterKind.Lake);
        }

        private static bool IsConnectedLand(StrategyStartSiteCell cell)
        {
            return cell.IsDryLand && cell.IsWalkable;
        }
    }
}
