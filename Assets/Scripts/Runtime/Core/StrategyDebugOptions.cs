namespace ProjectUnknown.Strategy
{
    public static class StrategyDebugOptions
    {
        public static bool InstantConstructionEnabled { get; private set; }

        public static void SetInstantConstructionEnabled(bool enabled)
        {
            if (InstantConstructionEnabled == enabled)
            {
                return;
            }

            InstantConstructionEnabled = enabled;
            StrategyDebugLogger.Info(
                "DebugOptions",
                "InstantConstructionToggled",
                StrategyDebugLogger.F("enabled", enabled));
        }
    }
}
