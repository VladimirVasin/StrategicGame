using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        public static void DebugCompleteAllActiveSites()
        {
            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            for (int i = 0; i < sites.Length; i++)
            {
                if (sites[i] != null && !sites[i].completed)
                {
                    sites[i].DebugCompleteInstantly();
                }
            }
        }

        public void DebugCompleteInstantly()
        {
            if (completed)
            {
                return;
            }

            deliveredLogs = cost.Logs;
            deliveredStone = cost.Stone;
            deliveredPlanks = cost.Planks;
            buildHits = buildHitsRequired;
            UpdateVisuals();
            StrategyDebugLogger.Info(
                "Construction",
                "InstantCompleted",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin));
            CompleteConstruction();
        }
    }
}
