namespace ProjectUnknown.Strategy
{
    public enum StrategyConstructionResourceKind
    {
        None,
        Logs,
        Stone,
        Planks
    }

    public readonly struct StrategyConstructionResourceCost
    {
        public StrategyConstructionResourceCost(int logs, int stone)
            : this(logs, stone, 0)
        {
        }

        public StrategyConstructionResourceCost(int logs, int stone, int planks)
        {
            Logs = logs < 0 ? 0 : logs;
            Stone = stone < 0 ? 0 : stone;
            Planks = planks < 0 ? 0 : planks;
        }

        public int Logs { get; }
        public int Stone { get; }
        public int Planks { get; }
        public int Total => Logs + Stone + Planks;
        public bool IsFree => Logs <= 0 && Stone <= 0 && Planks <= 0;

        public bool CanAfford(StrategyConstructionResourceCost available)
        {
            return available.Logs >= Logs
                && available.Stone >= Stone
                && available.Planks >= Planks;
        }

        public string ToBadgeText()
        {
            if (IsFree)
            {
                return "Free";
            }

            string text = string.Empty;
            AppendPart(ref text, "L", Logs);
            AppendPart(ref text, "S", Stone);
            AppendPart(ref text, "P", Planks);
            return text;
        }

        public string ToDisplayText()
        {
            if (IsFree)
            {
                return "Free";
            }

            string text = string.Empty;
            AppendPart(ref text, "Logs", Logs);
            AppendPart(ref text, "Stone", Stone);
            AppendPart(ref text, "Planks", Planks);
            return text;
        }

        public override string ToString()
        {
            return "Logs: " + Logs + ", Stone: " + Stone + ", Planks: " + Planks;
        }

        private static void AppendPart(ref string text, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                text += " / ";
            }

            text += label + " " + amount;
        }
    }
}
