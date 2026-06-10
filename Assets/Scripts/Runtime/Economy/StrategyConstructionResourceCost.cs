namespace ProjectUnknown.Strategy
{
    public enum StrategyConstructionResourceKind
    {
        None,
        Logs,
        Stone
    }

    public readonly struct StrategyConstructionResourceCost
    {
        public StrategyConstructionResourceCost(int logs, int stone)
        {
            Logs = logs < 0 ? 0 : logs;
            Stone = stone < 0 ? 0 : stone;
        }

        public int Logs { get; }
        public int Stone { get; }
        public int Total => Logs + Stone;
        public bool IsFree => Logs <= 0 && Stone <= 0;

        public bool CanAfford(StrategyConstructionResourceCost available)
        {
            return available.Logs >= Logs && available.Stone >= Stone;
        }

        public string ToBadgeText()
        {
            if (IsFree)
            {
                return "Free";
            }

            return "L" + Logs + " S" + Stone;
        }

        public override string ToString()
        {
            return "Logs: " + Logs + ", Stone: " + Stone;
        }
    }
}
