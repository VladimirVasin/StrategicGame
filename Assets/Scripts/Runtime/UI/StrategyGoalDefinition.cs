namespace ProjectUnknown.Strategy
{
    public enum StrategyGoalKind
    {
        None = 0,
        BuildAnyBuilding = 1,
        BuildThreeHouses = 2,
        BuildLumberjackCamp = 3,
        BuildStonecutterCamp = 4
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
        public StrategyGoalViewState(StrategyGoalDefinition definition, bool completed)
        {
            Kind = definition.Kind;
            Title = definition.Title;
            Description = definition.Description;
            Completed = completed;
        }

        public StrategyGoalKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public bool Completed { get; }
    }
}
