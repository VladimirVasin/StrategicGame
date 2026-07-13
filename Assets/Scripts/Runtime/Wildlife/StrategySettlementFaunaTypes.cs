namespace ProjectUnknown.Strategy
{
    public enum StrategyCatTemperament
    {
        Bold,
        Shy,
        Hunter,
        Lazy,
        Social,
        Territorial
    }

    public enum StrategyCatCoat
    {
        Ginger,
        GrayTabby,
        Black,
        BlackAndWhite,
        Calico,
        BrownTabby,
        Cream
    }

    public enum StrategyCatBehaviorState
    {
        EnteringSettlement,
        Idle,
        Walking,
        Sitting,
        Grooming,
        Stretching,
        Sleeping,
        Watching,
        Alert,
        Fleeing,
        ReturningToTerritory,
        SearchingForMouse,
        StalkingMouse,
        WaitingToPounce,
        Pouncing,
        ChasingMouse,
        CatchingMouse,
        CarryingMouse,
        EatingMouse,
        HuntFailed
    }

    public enum StrategyMouseBehaviorState
    {
        Hidden,
        Emerging,
        Idle,
        Sniffing,
        Scurrying,
        Nibbling,
        Alert,
        Fleeing,
        ReturningToHideout,
        Cornered,
        Caught,
        Dead
    }

    internal enum StrategyCatSpritePose
    {
        Idle,
        Walk,
        Stalk,
        Rest
    }

    public readonly struct StrategySettlementFaunaTargets
    {
        public StrategySettlementFaunaTargets(
            int completedBuildings,
            int occupiedHouses,
            int foodBuildings,
            int cats,
            int mice)
        {
            CompletedBuildings = completedBuildings;
            OccupiedHouses = occupiedHouses;
            FoodBuildings = foodBuildings;
            TargetCats = cats;
            TargetMice = mice;
        }

        public int CompletedBuildings { get; }
        public int OccupiedHouses { get; }
        public int FoodBuildings { get; }
        public int TargetCats { get; }
        public int TargetMice { get; }
    }
}
