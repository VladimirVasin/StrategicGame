namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        private void CompleteConstruction()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            StrategyStorageYard.ReleaseConstructionReservations(this);
            StrategyDebugLogger.Info(
                "Construction",
                "Completed",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builders", builders.Count));

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.NotifyConstructionCompleted(this);
                }
            }

            placement?.CompleteConstructionSite(this);
        }
    }
}
