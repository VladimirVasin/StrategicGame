using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        public void RestorePersistentProgress(
            int savedDeliveredLogs,
            int savedDeliveredStone,
            int savedDeliveredPlanks,
            float savedProgress)
        {
            deliveredLogs = Mathf.Clamp(savedDeliveredLogs, 0, cost.Logs);
            deliveredStone = Mathf.Clamp(savedDeliveredStone, 0, cost.Stone);
            deliveredPlanks = Mathf.Clamp(savedDeliveredPlanks, 0, cost.Planks);
            buildHits = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Clamp01(savedProgress) * buildHitsRequired),
                0,
                BuildableHitLimit);
            hasBegun = true;
            builderRequestTimer = 0f;
            UpdateVisuals();
        }
    }
}
