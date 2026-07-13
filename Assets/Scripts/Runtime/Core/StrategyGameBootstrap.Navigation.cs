using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureNavigation(StrategyGameContext context, CityMapController map)
        {
            StrategyNavigationService navigation = context.GetOrCreate<StrategyNavigationService>("Strategy Navigation");
            navigation.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "NavigationReady");
        }
    }
}
