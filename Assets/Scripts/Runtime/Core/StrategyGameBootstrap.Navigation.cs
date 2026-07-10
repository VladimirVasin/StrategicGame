using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureNavigation(CityMapController map)
        {
            StrategyNavigationService navigation = Object.FindAnyObjectByType<StrategyNavigationService>();
            if (navigation == null)
            {
                GameObject navigationObject = new GameObject("Strategy Navigation");
                navigation = navigationObject.AddComponent<StrategyNavigationService>();
            }

            navigation.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "NavigationReady");
        }
    }
}
