using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        internal bool SetFuneralNightTorchActive(bool active)
        {
            if (funeralNightTorchActive == active)
            {
                if (active && !nightTorchLightActive && ShouldUseFuneralNightTorch())
                {
                    EnableNightTorchLight();
                }

                return !active || nightTorchLightActive;
            }

            funeralNightTorchActive = active;
            if (!funeralNightTorchActive)
            {
                returningHomeWithFuneralTorch = false;
                if (!IsNightLightActivity(activity) && !ShouldUsePersonalNightTorch())
                {
                    DisableNightTorchLight();
                }

                return true;
            }

            EnableNightTorchLight();
            bool lit = nightTorchLightActive;
            if (lit)
            {
                StrategyDebugLogger.Info(
                    "Funeral",
                    "ResidentFuneralTorchLit",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("activity", activity));
            }
            else
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "ResidentFuneralTorchLightFailed",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("activity", activity),
                    StrategyDebugLogger.F("carryingResource", HasAnyCarriedResource()),
                    StrategyDebugLogger.F("hiddenInsideHome", hiddenInsideHome),
                    StrategyDebugLogger.F("hiddenUnderground", hiddenUnderground));
            }

            return lit;
        }

        private bool ShouldUseFuneralNightTorch()
        {
            return funeralNightTorchActive && CanCarryFuneralNightTorch();
        }

        private bool CanCarryFuneralNightTorch()
        {
            return !hiddenInsideHome
                && !hiddenUnderground
                && !deathRequested
                && !IsPendingRefugee
                && !IsHomeboundYoungChild
                && (!HasAnyCarriedResource() || IsFuneralActivity(activity))
                && (IsFuneralActivity(activity)
                    || returningHomeWithFuneralTorch
                    || activity == ResidentActivity.Idle
                    || activity == ResidentActivity.TendingHousehold
                    || activity == ResidentActivity.MovingHome
                    || activity == ResidentActivity.MovingToCampfireSleep);
        }

        private bool TryStartFuneralTorchReturnHome()
        {
            if (!funeralNightTorchActive || map == null)
            {
                return false;
            }

            if (home == null)
            {
                return TryStartFuneralTorchReturnToCamp();
            }

            returningHomeWithFuneralTorch = true;
            StartMovingHome(GetHomeExitWorld());
            UseNightTorchCarrySprite();
            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralTorchReturningHome",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
            return hasTarget;
        }

        private bool TryStartFuneralTorchReturnToCamp()
        {
            StrategyHomelessCampController camp = population != null ? population.HomelessCamp : null;
            if (camp == null || !TryFindFuneralTorchCampReturnCell(camp.CampCell, out Vector2Int targetCell))
            {
                return false;
            }

            returningHomeWithFuneralTorch = true;
            activity = ResidentActivity.MovingHome;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            UseNightTorchCarrySprite();
            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralTorchReturningToCamp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("campCell", camp.CampCell),
                StrategyDebugLogger.F("targetCell", targetCell));
            return hasTarget;
        }

        private bool TryFindFuneralTorchCampReturnCell(Vector2Int campCell, out Vector2Int targetCell)
        {
            if (map.IsCellWalkable(campCell) && TryBuildPathTo(campCell))
            {
                targetCell = campCell;
                return true;
            }

            int offset = Mathf.Abs(residentId * 17) % 97;
            for (int radius = 1; radius <= 5; radius++)
            {
                int side = radius * 2 + 1;
                int perimeter = Mathf.Max(1, side * 4 - 4);
                for (int index = 0; index < perimeter; index++)
                {
                    Vector2Int candidate = GetFuneralTorchCampRingCell(
                        campCell,
                        radius,
                        (index + offset) % perimeter);
                    if (!map.IsCellWalkable(candidate) || !TryBuildPathTo(candidate))
                    {
                        continue;
                    }

                    targetCell = candidate;
                    return true;
                }
            }

            path.Clear();
            pathIndex = 0;
            hasTarget = false;
            targetCell = default;
            return false;
        }

        private static Vector2Int GetFuneralTorchCampRingCell(Vector2Int center, int radius, int index)
        {
            int side = radius * 2 + 1;
            int edge = side - 1;
            if (index < side)
            {
                return center + new Vector2Int(-radius + index, -radius);
            }

            index -= side;
            if (index < edge)
            {
                return center + new Vector2Int(radius, -radius + index + 1);
            }

            index -= edge;
            if (index < edge)
            {
                return center + new Vector2Int(radius - index - 1, radius);
            }

            index -= edge;
            return center + new Vector2Int(-radius, radius - index - 1);
        }

        private void CompleteFuneralTorchReturnHome()
        {
            bool hadTorch = funeralNightTorchActive || returningHomeWithFuneralTorch;
            returningHomeWithFuneralTorch = false;
            SetFuneralNightTorchActive(false);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.75f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (hadTorch)
            {
                StrategyDebugLogger.Info(
                    "Funeral",
                    "ResidentFuneralTorchReturnedHome",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("returnTarget", home != null ? "home" : "camp"),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            }

            TryStartNightSleep();
        }
    }
}
