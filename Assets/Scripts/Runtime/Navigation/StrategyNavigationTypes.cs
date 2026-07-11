using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyNavigationMode
    {
        ResidentTrail,
        GroundCardinal,
        WildlifeLand
    }

    public enum StrategyNavigationStatus
    {
        Success,
        Unreachable,
        Deferred,
        Invalid
    }

    public enum StrategyNavigationPriority
    {
        Background,
        Normal,
        Critical
    }

    public readonly struct StrategyNavigationQuery
    {
        public StrategyNavigationQuery(
            Vector2Int start,
            Vector2Int target,
            StrategyNavigationMode mode,
            int maxVisited = 0,
            StrategyWildlifeController wildlife = null,
            bool allowWildlifeStructureBuffer = false,
            StrategyNavigationPriority priority = StrategyNavigationPriority.Normal)
        {
            Start = start;
            Target = target;
            Mode = mode;
            MaxVisited = maxVisited;
            Wildlife = wildlife;
            AllowWildlifeStructureBuffer = allowWildlifeStructureBuffer;
            Priority = mode == StrategyNavigationMode.WildlifeLand
                && priority == StrategyNavigationPriority.Normal
                    ? StrategyNavigationPriority.Background
                    : priority;
        }

        public Vector2Int Start { get; }
        public Vector2Int Target { get; }
        public StrategyNavigationMode Mode { get; }
        public int MaxVisited { get; }
        public StrategyWildlifeController Wildlife { get; }
        public bool AllowWildlifeStructureBuffer { get; }
        public StrategyNavigationPriority Priority { get; }
    }

    internal readonly struct StrategyNavigationQueryKey : System.IEquatable<StrategyNavigationQueryKey>
    {
        public StrategyNavigationQueryKey(StrategyNavigationQuery query, int revision)
        {
            Start = query.Start;
            Target = query.Target;
            Mode = query.Mode;
            MaxVisited = query.MaxVisited;
            Revision = revision;
            AllowWildlifeStructureBuffer = query.AllowWildlifeStructureBuffer;
        }

        public Vector2Int Start { get; }
        public Vector2Int Target { get; }
        public StrategyNavigationMode Mode { get; }
        public int MaxVisited { get; }
        public int Revision { get; }
        public bool AllowWildlifeStructureBuffer { get; }

        public bool Equals(StrategyNavigationQueryKey other)
        {
            return Start == other.Start
                && Target == other.Target
                && Mode == other.Mode
                && MaxVisited == other.MaxVisited
                && Revision == other.Revision
                && AllowWildlifeStructureBuffer == other.AllowWildlifeStructureBuffer;
        }

        public override bool Equals(object obj)
        {
            return obj is StrategyNavigationQueryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Start.GetHashCode();
                hash = hash * 397 ^ Target.GetHashCode();
                hash = hash * 397 ^ (int)Mode;
                hash = hash * 397 ^ MaxVisited;
                hash = hash * 397 ^ Revision;
                hash = hash * 397 ^ (AllowWildlifeStructureBuffer ? 1 : 0);
                return hash;
            }
        }
    }
}
