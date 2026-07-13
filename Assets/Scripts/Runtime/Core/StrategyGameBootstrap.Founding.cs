namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static StrategyFoundingStartState ResolveFoundingStartState(CityMapController map)
        {
            if (map == null)
            {
                return null;
            }

            StrategyFoundingStartState state = map.GetComponent<StrategyFoundingStartState>();
            if (state != null && state.HasCampCell)
            {
                return state;
            }

            if (!StrategySaveSystem.TryGetPendingFoundingStartData(
                    out StrategyFoundingStartSaveData savedStart))
            {
                return state;
            }

            state = StrategyFoundingStartState.GetOrCreate(map);
            state.Configure(savedStart);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "FoundingStartRestored",
                StrategyDebugLogger.F("campCell", state.CampCell),
                StrategyDebugLogger.F("hasCaravanOrigin", state.HasCaravanOrigin),
                StrategyDebugLogger.F("profile", state.Preferences?.ProfileId ?? "legacy"));
            return state;
        }
    }
}
