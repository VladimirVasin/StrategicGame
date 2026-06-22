using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private static float GetUpgradedWorkDuration(float minSeconds, float maxSeconds, MonoBehaviour worksite)
        {
            float multiplier = StrategyProductionBuildingUpgradeCatalog.GetWorkSpeedMultiplier(worksite);
            return Random.Range(minSeconds, maxSeconds) / Mathf.Max(0.1f, multiplier);
        }

        private static float GetUpgradedWorkDuration(float seconds, MonoBehaviour worksite)
        {
            float multiplier = StrategyProductionBuildingUpgradeCatalog.GetWorkSpeedMultiplier(worksite);
            return seconds / Mathf.Max(0.1f, multiplier);
        }

        private float GetLumberWorkAnimationRate()
        {
            return WoodcutAnimationFrameRate
                * StrategyProductionBuildingUpgradeCatalog.GetWorkSpeedMultiplier(workplace);
        }

        private float GetStonecutWorkAnimationRate()
        {
            return StonecutAnimationFrameRate
                * StrategyProductionBuildingUpgradeCatalog.GetWorkSpeedMultiplier(stoneWorkplace);
        }

        private float GetFishingWorkAnimationRate()
        {
            return FishingAnimationFrameRate
                * StrategyProductionBuildingUpgradeCatalog.GetWorkSpeedMultiplier(fisherWorkplace);
        }

        private float GetUpgradedFisherWorkDuration(float seconds)
        {
            return GetUpgradedWorkDuration(seconds, fisherWorkplace);
        }

        private float GetUpgradedFisherWorkDuration(float minSeconds, float maxSeconds)
        {
            return GetUpgradedWorkDuration(minSeconds, maxSeconds, fisherWorkplace);
        }
    }
}
