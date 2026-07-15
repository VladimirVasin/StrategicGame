using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float SawmillWorkSecondsMin = 7.8f;
        private const float SawmillWorkSecondsMax = 11.5f;

        private StrategySawmill sawmillWorkplace;
        private StrategySawmill activeSawmill;
        private int sawmillPlanksPending;
        private float sawmillWorkCooldown;
        private float sawmillWorkTimer;

        public StrategySawmill SawmillWorkplace => sawmillWorkplace;

        public void AssignSawmillWorkplace(StrategySawmill sawmill)
        {
            if (sawmill == null
                || sawmillWorkplace == sawmill
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelMineWork();
            CancelCoalPitWork();
            CancelClayPitWork();
            CancelKilnWork(true);
            CancelForgeWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            sawmillWorkplace = sawmill;
            sawmillWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Sawmill",
                "ResidentSawmillWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sawmillOrigin", sawmill.Origin));
        }

        public void ClearSawmillWorkplace(StrategySawmill sawmill)
        {
            if (this == null)
            {
                return;
            }

            if (sawmill != null && sawmillWorkplace != sawmill)
            {
                return;
            }

            StrategySawmill previous = sawmillWorkplace;
            CancelSawmillWork(true);
            sawmillWorkplace = null;
            StrategyDebugLogger.Info(
                "Sawmill",
                "ResidentSawmillWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sawmillOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartSawmillTask()
        {
            if (activity != ResidentActivity.Idle
                || sawmillWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || sawmillWorkCooldown > 0f)
            {
                return false;
            }

            return TryMoveToSawmillWork();
        }

        private bool TryMoveToSawmillWork()
        {
            if (!sawmillWorkplace.TryFindEntranceCell(out Vector2Int entranceCell)
                || !TryBuildPathTo(entranceCell))
            {
                sawmillWorkCooldown = Random.Range(1.4f, 3.4f);
                return false;
            }

            activeSawmill = sawmillWorkplace;
            activity = ResidentActivity.MovingToSawmill;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartSawingLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeSawmill == null
                || sawmillWorkplace == null
                || activeSawmill != sawmillWorkplace
                || !CanWork)
            {
                ResetSawmillWorkToIdle(false);
                return;
            }

            if (!TryBeginSawingCycle())
            {
                EnterSawmillStandby();
            }
        }

        private bool TryBeginSawingCycle()
        {
            if (activeSawmill == null || !activeSawmill.TryConsumeLogForWork(out sawmillPlanksPending))
            {
                return false;
            }

            activity = ResidentActivity.SawingLogs;
            sawmillWorkTimer = GetUpgradedWorkDuration(SawmillWorkSecondsMin, SawmillWorkSecondsMax, activeSawmill);
            activeSawmill.BeginSawing(this);
            transform.position = activeSawmill.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            FaceWorldPoint(activeSawmill.GetSawFocusWorld());
            return true;
        }

        private void EnterSawmillStandby()
        {
            activity = ResidentActivity.StandingByAtSawmill;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.position = activeSawmill.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            FaceWorldPoint(activeSawmill.GetSawFocusWorld());
        }

        private void UpdateSawmillStandby()
        {
            if (activeSawmill == null
                || sawmillWorkplace == null
                || activeSawmill != sawmillWorkplace
                || !CanWork)
            {
                ResetSawmillWorkToIdle(false);
                return;
            }

            transform.position = activeSawmill.GetInteriorWorkWorld(this);
            if (sawmillWorkCooldown <= 0f && TryBeginSawingCycle())
            {
                return;
            }

            AnimateIdle();
            FaceWorldPoint(activeSawmill.GetSawFocusWorld());
        }

        private void UpdateSawingLogs()
        {
            if (activeSawmill == null || sawmillWorkplace == null || activeSawmill != sawmillWorkplace)
            {
                ResetSawmillWorkToIdle(false);
                return;
            }

            transform.position = activeSawmill.GetInteriorWorkWorld(this);
            FaceWorldPoint(activeSawmill.GetSawFocusWorld());
            AnimateLumberWork(11.2f, 4.4f);
            sawmillWorkTimer -= Time.deltaTime;
            if (sawmillWorkTimer > 0f)
            {
                return;
            }

            StrategySawmill completedSawmill = activeSawmill;
            completedSawmill.AddPlanks(sawmillPlanksPending);
            completedSawmill.EndSawing(this);
            sawmillPlanksPending = 0;
            sawmillWorkCooldown = Random.Range(0.8f, 2.2f);
            if (sawmillWorkplace == completedSawmill
                && CanWork
                && !StrategyDayNightCycleController.IsResidentEveningHomeTime)
            {
                activeSawmill = completedSawmill;
                EnterSawmillStandby();
                return;
            }

            activeSawmill = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.20f, 0.55f);
        }

        private void ResetSawmillWorkToIdle(bool storeCarriedLogs)
        {
            if (sawmillPlanksPending > 0)
            {
                activeSawmill?.ReleasePendingPlanks(sawmillPlanksPending);
            }

            activeSawmill?.EndSawing(this);
            activeSawmill = null;
            sawmillPlanksPending = 0;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (storeCarriedLogs && carriedLogAmount > 0 && TryStartStorageCarriedReturn("sawmill_work_cancelled"))
            {
                return;
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            sawmillWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelSawmillWork(bool storeCarriedLogs)
        {
            if (this == null)
            {
                return;
            }

            bool active = activity == ResidentActivity.MovingToSawmill
                || activity == ResidentActivity.StandingByAtSawmill
                || activity == ResidentActivity.SawingLogs;
            if (active)
            {
                ResetSawmillWorkToIdle(storeCarriedLogs);
                return;
            }

            activeSawmill = null;
        }
    }
}
