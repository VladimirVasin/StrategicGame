using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float SawmillWorkSecondsMin = 7.8f;
        private const float SawmillWorkSecondsMax = 11.5f;

        private StrategySawmill sawmillWorkplace;
        private StrategySawmill activeSawmill;
        private StrategyStorageYard activeSawmillLogYardSource;
        private StrategyLumberjackCamp activeSawmillLogCampSource;
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

            if (!sawmillWorkplace.TryReserveInputLogs(
                    this,
                    out activeSawmillLogYardSource,
                    out activeSawmillLogCampSource,
                    out Vector2Int pickupCell)
                || !TryBuildPathTo(pickupCell))
            {
                ReleaseSawmillLogReservation();
                sawmillWorkCooldown = Random.Range(2.0f, 4.8f);
                return false;
            }

            activeSawmill = sawmillWorkplace;
            activity = ResidentActivity.MovingToSawmillLogPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
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

        private void StartPickingUpSawmillLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeSawmill == null || (activeSawmillLogYardSource == null && activeSawmillLogCampSource == null))
            {
                ResetSawmillWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.PickingUpSawmillLogs;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeSawmillLogYardSource != null
                ? activeSawmillLogYardSource.FootprintBounds.center
                : activeSawmillLogCampSource.FootprintBounds.center);
        }

        private void UpdatePickingUpSawmillLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.9f, 3.1f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            bool taken = activeSawmillLogYardSource != null
                ? activeSawmillLogYardSource.TryTakeReservedLogs(this, out carriedLogAmount)
                : activeSawmillLogCampSource != null && activeSawmillLogCampSource.TryTakeReservedLogs(this, out carriedLogAmount);
            if (!taken
                || activeSawmill == null
                || !activeSawmill.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                ReleaseSawmillLogReservation();
                ResetSawmillWorkToIdle(true);
                return;
            }

            activeSawmillLogYardSource = null;
            activeSawmillLogCampSource = null;
            activity = ResidentActivity.CarryingLogsToSawmill;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedLogsVisible(true);
        }

        private void StartDepositingSawmillLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingSawmillLogs;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (activeSawmill != null)
            {
                FaceWorldPoint(activeSawmill.FootprintBounds.center);
            }
        }

        private void UpdateDepositingSawmillLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.0f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeSawmill == null || !activeSawmill.CanAcceptInputLogs(carriedLogAmount))
            {
                ResetSawmillWorkToIdle(true);
                return;
            }

            activeSawmill.AddLogs(carriedLogAmount);
            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            if (activeSawmill != null
                && activeSawmill.TryFindEntranceCell(out Vector2Int entranceCell)
                && TryBuildPathTo(entranceCell))
            {
                activity = ResidentActivity.MovingToSawmill;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.10f);
                return;
            }

            ResetSawmillWorkToIdle(false);
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
            ReleaseSawmillLogReservation();
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

            bool active = activity == ResidentActivity.MovingToSawmillLogPickup
                || activity == ResidentActivity.PickingUpSawmillLogs
                || activity == ResidentActivity.CarryingLogsToSawmill
                || activity == ResidentActivity.DepositingSawmillLogs
                || activity == ResidentActivity.MovingToSawmill
                || activity == ResidentActivity.SawingLogs;
            if (active)
            {
                ResetSawmillWorkToIdle(storeCarriedLogs);
                return;
            }

            ReleaseSawmillLogReservation();
            activeSawmill = null;
        }

        private void ReleaseSawmillLogReservation()
        {
            activeSawmillLogYardSource?.ReleaseStoredLogsReservation(this);
            activeSawmillLogCampSource?.ReleaseStoredLogsReservation(this);
            activeSawmillLogYardSource = null;
            activeSawmillLogCampSource = null;
        }
    }
}
