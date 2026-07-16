namespace ProjectUnknown.Strategy
{
    public enum StrategyGoalKind
    {
        None = 0,
        BuildAnyBuilding = 1,
        BuildThreeHouses = 2,
        BuildLumberjackCamp = 3,
        BuildStonecutterCamp = 4,
        BuildForagerCamp = 5,
        PrepareWinterFood = 6,
        PrepareWinterFuel = 7,
        SurviveFirstWinter = 8,
        BuildScoutLodge = 9,
        BuildStorageYard = 10,
        BuildGranary = 11
    }

    public readonly struct StrategyGoalDefinition
    {
        public StrategyGoalDefinition(StrategyGoalKind kind, string title, string description = "")
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public StrategyGoalKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
    }

    public readonly struct StrategyGoalViewState
    {
        public StrategyGoalViewState(
            StrategyGoalDefinition definition,
            bool completed,
            float progressCurrent = 0f,
            float progressTarget = 0f,
            string progressText = "")
        {
            Kind = definition.Kind;
            Title = definition.Title;
            Description = definition.Description;
            Completed = completed;
            ProgressCurrent = progressCurrent;
            ProgressTarget = progressTarget;
            ProgressText = progressText ?? string.Empty;
        }

        public StrategyGoalKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public bool Completed { get; }
        public float ProgressCurrent { get; }
        public float ProgressTarget { get; }
        public string ProgressText { get; }
        public bool HasProgress => ProgressTarget > 0.001f;
        public float ProgressNormalized => HasProgress
            ? UnityEngine.Mathf.Clamp01(ProgressCurrent / ProgressTarget)
            : 0f;
    }
}
