using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        private static readonly List<StrategyConstructionSite> activeSites = new();

        private bool registeredActiveSite;

        public static IReadOnlyList<StrategyConstructionSite> ActiveSites => activeSites;

        private void RegisterActiveSite()
        {
            if (registeredActiveSite)
            {
                return;
            }

            activeSites.Add(this);
            registeredActiveSite = true;
        }

        private void UnregisterActiveSite()
        {
            if (!registeredActiveSite)
            {
                return;
            }

            activeSites.Remove(this);
            registeredActiveSite = false;
        }
    }
}
