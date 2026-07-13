using System;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyStartSiteCell
    {
        public StrategyStartSiteCell(
            CityMapCellKind kind,
            CityMapWaterKind waterKind,
            bool isWalkable,
            bool isBuildable)
        {
            Kind = kind;
            WaterKind = kind == CityMapCellKind.Water || kind == CityMapCellKind.Shore
                ? waterKind
                : CityMapWaterKind.None;
            IsWalkable = isWalkable;
            IsBuildable = isBuildable;
        }

        public CityMapCellKind Kind { get; }
        public CityMapWaterKind WaterKind { get; }
        public bool IsWalkable { get; }
        public bool IsBuildable { get; }
        public bool IsWater => Kind == CityMapCellKind.Water;
        public bool IsShore => Kind == CityMapCellKind.Shore;
        public bool IsDryLand => !IsWater && !IsShore;
        public bool IsOpenLand => Kind == CityMapCellKind.Grass
            || Kind == CityMapCellKind.Meadow
            || Kind == CityMapCellKind.Dirt;
    }

    public sealed class StrategyStartSiteMapSnapshot
    {
        private readonly StrategyStartSiteCell[] cells;

        public StrategyStartSiteMapSnapshot(
            int width,
            int height,
            int seed,
            StrategyStartSiteCell[] sourceCells)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (sourceCells == null)
            {
                throw new ArgumentNullException(nameof(sourceCells));
            }

            if (sourceCells.Length != width * height)
            {
                throw new ArgumentException(
                    "Start-site cell count must match map dimensions.",
                    nameof(sourceCells));
            }

            Width = width;
            Height = height;
            Seed = Math.Max(1, seed);
            cells = (StrategyStartSiteCell[])sourceCells.Clone();
        }

        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        public int CellCount => cells.Length;

        public static StrategyStartSiteMapSnapshot Capture(CityMapController map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            StrategyStartSiteCell[] snapshotCells = new StrategyStartSiteCell[map.Width * map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!map.TryGetCell(x, y, out CityMapCell cell))
                    {
                        throw new InvalidOperationException(
                            $"Cannot capture ungenerated map cell ({x}, {y}).");
                    }

                    snapshotCells[x + y * map.Width] = new StrategyStartSiteCell(
                        cell.Kind,
                        cell.WaterKind,
                        map.IsCellWalkable(x, y),
                        map.IsCellBuildable(x, y));
                }
            }

            return new StrategyStartSiteMapSnapshot(
                map.Width,
                map.Height,
                map.ActiveSeed,
                snapshotCells);
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool TryGetCell(int x, int y, out StrategyStartSiteCell cell)
        {
            if (IsInside(x, y))
            {
                cell = cells[x + y * Width];
                return true;
            }

            cell = default;
            return false;
        }

        public StrategyStartSiteCell GetCell(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Cell ({x}, {y}) is outside {Width}x{Height} map bounds.");
            }

            return cells[x + y * Width];
        }
    }
}
