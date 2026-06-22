namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyProductionUpgradeCost
    {
        public StrategyProductionUpgradeCost(int tools, int planks, int stone = 0)
        {
            Tools = tools < 0 ? 0 : tools;
            Planks = planks < 0 ? 0 : planks;
            Stone = stone < 0 ? 0 : stone;
        }

        public int Tools { get; }
        public int Planks { get; }
        public int Stone { get; }
        public bool IsFree => Tools <= 0 && Planks <= 0 && Stone <= 0;

        public bool CanAfford(int tools, int planks, int stone)
        {
            return tools >= Tools && planks >= Planks && stone >= Stone;
        }

        public int GetAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Tools => Tools,
                StrategyResourceType.Planks => Planks,
                StrategyResourceType.Stone => Stone,
                _ => 0
            };
        }

        public string ToDisplayText()
        {
            if (IsFree)
            {
                return "Free";
            }

            string text = string.Empty;
            AppendPart(ref text, "Tools", Tools);
            AppendPart(ref text, "Planks", Planks);
            AppendPart(ref text, "Stone", Stone);
            return text;
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
