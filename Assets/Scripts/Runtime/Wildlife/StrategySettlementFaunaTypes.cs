namespace ProjectUnknown.Strategy
{
    public enum StrategyFirstNightFaunaStage
    {
        Dormant = 0,
        MiceVisible = 1,
        StoryCompleted = 2
    }

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
        Rest,
        Pounce,
        Joy
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

    internal static class StrategySettlementFaunaPolicy
    {
        internal const int FirstNightMouseMinimum = 3;
        internal const int FirstNightCatMinimum = 1;

        internal static int GetMouseSpawnBudget(
            StrategyFirstNightFaunaStage stage,
            int currentCount,
            int targetCount)
        {
            return stage == StrategyFirstNightFaunaStage.MiceVisible
                ? UnityEngine.Mathf.Max(0, targetCount - currentCount)
                : 1;
        }

        internal static StrategySettlementFaunaTargets ApplyFirstNightStage(
            StrategySettlementFaunaTargets organicTargets,
            StrategyFirstNightFaunaStage stage,
            bool ownsCats)
        {
            int targetCats;
            int targetMice;
            switch (stage)
            {
                case StrategyFirstNightFaunaStage.MiceVisible:
                    targetCats = 0;
                    targetMice = UnityEngine.Mathf.Max(
                        FirstNightMouseMinimum,
                        organicTargets.TargetMice);
                    break;
                case StrategyFirstNightFaunaStage.StoryCompleted:
                    targetCats = ownsCats
                        ? UnityEngine.Mathf.Max(
                            FirstNightCatMinimum,
                            organicTargets.TargetCats)
                        : 0;
                    targetMice = UnityEngine.Mathf.Max(
                        FirstNightMouseMinimum,
                        organicTargets.TargetMice);
                    break;
                default:
                    targetCats = 0;
                    targetMice = 0;
                    break;
            }

            return new StrategySettlementFaunaTargets(
                organicTargets.CompletedBuildings,
                organicTargets.OccupiedHouses,
                organicTargets.FoodBuildings,
                targetCats,
                targetMice);
        }
    }
}
