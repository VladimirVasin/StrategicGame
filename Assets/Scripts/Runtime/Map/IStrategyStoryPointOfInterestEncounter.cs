using System;

namespace ProjectUnknown.Strategy
{
    public enum StrategyStoryPointOfInterestOutcome
    {
        Declined,
        Accepted
    }

    public interface IStrategyStoryPointOfInterestEncounter
    {
        string EncounterId { get; }

        bool TryBegin(
            StrategyStoryPointOfInterestDefinition definition,
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident,
            Action<StrategyStoryPointOfInterestOutcome> onCompleted);
    }
}
