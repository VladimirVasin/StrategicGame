using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float KilnWorkSecondsMin = 9.8f;
        private const float KilnWorkSecondsMax = 14.2f;

        private StrategyKiln kilnWorkplace;
        private StrategyKiln activeKiln;
        private int kilnPotteryPending;
        private float kilnWorkCooldown;
        private float kilnWorkTimer;
        private float kilnWorkEffectTimer;

        public StrategyKiln KilnWorkplace => kilnWorkplace;

        public void AssignKilnWorkplace(StrategyKiln kiln)
        {
            if (kiln == null
                || kilnWorkplace == kiln
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || sawmillWorkplace != null
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
            CancelSawmillWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            kilnWorkplace = kiln;
            kilnWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Kiln",
                "ResidentKilnWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("kilnOrigin", kiln.Origin));
        }

        public void ClearKilnWorkplace(StrategyKiln kiln)
        {
            if (this == null)
            {
                return;
            }

            if (kiln != null && kilnWorkplace != kiln)
            {
                return;
            }

            StrategyKiln previous = kilnWorkplace;
            CancelKilnWork(true);
            kilnWorkplace = null;
            StrategyDebugLogger.Info(
                "Kiln",
                "ResidentKilnWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("kilnOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartKilnTask()
        {
            if (activity != ResidentActivity.Idle
                || kilnWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || sawmillWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || kilnWorkCooldown > 0f)
            {
                return false;
            }

            if (kilnWorkplace.HasInputMaterials
                && kilnWorkplace.CanStartWorkCycle()
                && TryMoveToKilnWork())
            {
                return true;
            }

            kilnWorkCooldown = Random.Range(2.0f, 4.8f);
            return false;
        }

        private bool TryMoveToKilnWork()
        {
            if (!kilnWorkplace.TryFindEntranceCell(out Vector2Int entranceCell)
                || !TryBuildPathTo(entranceCell))
            {
                kilnWorkCooldown = Random.Range(1.4f, 3.4f);
                return false;
            }

            activeKiln = kilnWorkplace;
            activity = ResidentActivity.MovingToKiln;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartFiringPottery()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeKiln == null || !activeKiln.TryConsumeInputsForWork(out kilnPotteryPending))
            {
                ResetKilnWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.FiringPottery;
            kilnWorkTimer = Random.Range(KilnWorkSecondsMin, KilnWorkSecondsMax);
            activeKiln.BeginFiring(this);
            transform.position = activeKiln.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            FaceWorldPoint(activeKiln.GetKilnFocusWorld());
            ResetKilnWorkEffectTimer(true);
        }

        private void UpdateFiringPottery()
        {
            if (activeKiln == null || kilnWorkplace == null || activeKiln != kilnWorkplace)
            {
                ResetKilnWorkToIdle(false);
                return;
            }

            transform.position = activeKiln.GetInteriorWorkWorld(this);
            FaceWorldPoint(activeKiln.GetKilnFocusWorld());
            AnimateLumberWork(8.2f, 2.8f);
            UpdateKilnWorkEffects();
            kilnWorkTimer -= Time.deltaTime;
            if (kilnWorkTimer > 0f)
            {
                return;
            }

            activeKiln.AddPottery(kilnPotteryPending);
            activeKiln.EndFiring(this);
            kilnPotteryPending = 0;
            activeKiln = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            kilnWorkCooldown = Random.Range(0.9f, 2.4f);
            waitTimer = Random.Range(0.20f, 0.55f);
        }

        private void ResetKilnWorkToIdle(bool storeCarried)
        {
            if (kilnPotteryPending > 0)
            {
                activeKiln?.ReleasePendingPottery(kilnPotteryPending);
            }

            activeKiln?.EndFiring(this);
            activeKiln = null;
            kilnPotteryPending = 0;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (storeCarried && TryStartStorageCarriedReturn("kiln_work_cancelled"))
            {
                return;
            }

            kilnWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelKilnWork(bool storeCarried)
        {
            if (this == null)
            {
                return;
            }

            bool active = activity == ResidentActivity.MovingToKiln
                || activity == ResidentActivity.FiringPottery;
            if (active)
            {
                ResetKilnWorkToIdle(storeCarried);
                return;
            }

            activeKiln = null;
        }

        private void ResetKilnWorkEffectTimer(bool immediate)
        {
            kilnWorkEffectTimer = immediate ? 0f : Random.Range(0.22f, 0.52f);
        }

        private void UpdateKilnWorkEffects()
        {
            if (activeKiln == null)
            {
                return;
            }

            kilnWorkEffectTimer -= Time.deltaTime;
            if (kilnWorkEffectTimer > 0f)
            {
                return;
            }

            activeKiln.PlayFiringWorkEffect(ResidentId + Mathf.RoundToInt(Time.time * 10f));
            ResetKilnWorkEffectTimer(false);
        }
    }
}
