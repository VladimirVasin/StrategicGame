using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float PersonalTorchCoverageIntervalMin = 0.18f;
        private const float PersonalTorchCoverageIntervalMax = 0.30f;
        private const float PersonalTorchExtinguishRadiusScale = 0.86f;
        private const float PersonalTorchIgniteRadiusScale = 1f;

        private float personalTorchCoverageTimer;
        private bool personalNightTorchRequired;

        private void UpdatePersonalNightTorchState()
        {
            personalTorchCoverageTimer -= Mathf.Max(0f, Time.unscaledDeltaTime);
            if (personalTorchCoverageTimer > 0f)
            {
                if (nightTorchLightActive)
                {
                    UpdateNightTorchLight();
                }

                return;
            }

            personalTorchCoverageTimer = Random.Range(
                PersonalTorchCoverageIntervalMin,
                PersonalTorchCoverageIntervalMax);

            bool eligible = IsAdult
                && !deathRequested
                && !IsPendingRefugee
                && !hiddenInsideHome
                && !hiddenUnderground
                && !sleepingInsideHome
                && !IsSleepingAtCampfire
                && IsEveningNightTorchTime();
            if (!eligible)
            {
                personalNightTorchRequired = false;
                if (!IsNightLightActivity(activity) && !ShouldUseFuneralNightTorch())
                {
                    DisableNightTorchLight();
                }

                return;
            }

            float stationaryRadiusScale = personalNightTorchRequired
                ? PersonalTorchExtinguishRadiusScale
                : PersonalTorchIgniteRadiusScale;
            bool covered = StrategyCinematicLightEmitter.IsWorldPositionCovered(
                transform.position,
                stationaryRadiusScale);
            if (!covered)
            {
                covered = IsCoveredByHigherPriorityPersonalTorch(transform.position);
            }

            personalNightTorchRequired = !covered;
            if (personalNightTorchRequired || IsNightLightActivity(activity) || ShouldUseFuneralNightTorch())
            {
                EnableNightTorchLight();
            }
            else
            {
                DisableNightTorchLight();
            }
        }

        private bool IsCoveredByHigherPriorityPersonalTorch(Vector3 world)
        {
            int ownPriority = GetPersonalTorchPriority();
            for (int i = 0; i < ActiveNightTorchLights.Count; i++)
            {
                StrategyResidentAgent other = ActiveNightTorchLights[i];
                if (other == null
                    || other == this
                    || other.GetPersonalTorchPriority() >= ownPriority
                    || !other.TryGetNightTorchMaskLight(out Vector3 center, out float radius, out _, out _))
                {
                    continue;
                }

                float effectiveRadius = radius * PersonalTorchExtinguishRadiusScale;
                if ((world - center).sqrMagnitude <= effectiveRadius * effectiveRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetPersonalTorchPriority()
        {
            return residentId > 0 ? residentId : GetHashCode();
        }
    }
}
