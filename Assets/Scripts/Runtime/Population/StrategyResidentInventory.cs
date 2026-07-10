namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyResidentInventory
    {
        public StrategyConstructionSite ConstructionReturnSite;
        public StrategyConstructionResourceKind ConstructionReturnResource;
        public StrategyResourceType HouseholdFoodResource;
        public StrategyResourceType ForageResource;
        public int Logs;
        public int Stone;
        public int Iron;
        public int Coal;
        public int Clay;
        public int Planks;
        public int Pottery;
        public int Tools;
        public int Game;
        public int Fish;
        public int Forage;

        public bool HasAnyResource => Logs > 0
            || Stone > 0
            || Iron > 0
            || Coal > 0
            || Clay > 0
            || Planks > 0
            || Pottery > 0
            || Tools > 0
            || Game > 0
            || Fish > 0
            || Forage > 0;

        public void ClearAmounts()
        {
            Logs = 0;
            Stone = 0;
            Iron = 0;
            Coal = 0;
            Clay = 0;
            Planks = 0;
            Pottery = 0;
            Tools = 0;
            Game = 0;
            Fish = 0;
            Forage = 0;
            HouseholdFoodResource = StrategyResourceType.None;
            ForageResource = StrategyResourceType.None;
        }

        public void ClearConstructionReturn()
        {
            ConstructionReturnSite = null;
            ConstructionReturnResource = StrategyConstructionResourceKind.None;
        }
    }
}
