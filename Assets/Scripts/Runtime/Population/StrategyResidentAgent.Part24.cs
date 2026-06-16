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
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || sawmillWorkCooldown > 0f)
            {
                return false;
            }

            if (sawmillWorkplace.HasInputLogs
                && sawmillWorkplace.CanStartWorkCycle()
                && TryMoveToSawmillWork())
            {
                return true;
            }

            sawmillWorkCooldown = Random.Range(2.0f, 4.8f);
            return false;
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
            if (activeSawmill == null || !activeSawmill.TryConsumeLogForWork(out sawmillPlanksPending))
            {
                ResetSawmillWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.SawingLogs;
            sawmillWorkTimer = Random.Range(SawmillWorkSecondsMin, SawmillWorkSecondsMax);
            activeSawmill.BeginSawing(this);
            transform.position = activeSawmill.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
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

            activeSawmill.AddPlanks(sawmillPlanksPending);
            activeSawmill.EndSawing(this);
            sawmillPlanksPending = 0;
            activeSawmill = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            sawmillWorkCooldown = Random.Range(0.8f, 2.2f);
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
