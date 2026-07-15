using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ForgeWorkSecondsMin = 12.8f;
        private const float ForgeWorkSecondsMax = 18.5f;

        private StrategyForge forgeWorkplace;
        private StrategyForge activeForge;
        private int forgeToolsPending;
        private float forgeWorkCooldown;
        private float forgeWorkTimer;
        private float forgeWorkEffectTimer;

        public StrategyForge ForgeWorkplace => forgeWorkplace;

        public void AssignForgeWorkplace(StrategyForge forge)
        {
            if (forge == null
                || forgeWorkplace == forge
                || forgeWorkplace != null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
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
            CancelKilnWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            forgeWorkplace = forge;
            forgeWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Forge",
                "ResidentForgeWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("forgeOrigin", forge.Origin));
        }

        public void ClearForgeWorkplace(StrategyForge forge)
        {
            if (this == null)
            {
                return;
            }

            if (forge != null && forgeWorkplace != forge)
            {
                return;
            }

            StrategyForge previous = forgeWorkplace;
            CancelForgeWork(true);
            forgeWorkplace = null;
            StrategyDebugLogger.Info(
                "Forge",
                "ResidentForgeWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("forgeOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartForgeTask()
        {
            if (activity != ResidentActivity.Idle
                || forgeWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || forgeWorkCooldown > 0f)
            {
                return false;
            }

            return TryMoveToForgeWork();
        }

        private bool TryMoveToForgeWork()
        {
            if (!forgeWorkplace.TryFindEntranceCell(out Vector2Int entranceCell)
                || !TryBuildPathTo(entranceCell))
            {
                forgeWorkCooldown = Random.Range(1.4f, 3.4f);
                return false;
            }

            activeForge = forgeWorkplace;
            activity = ResidentActivity.MovingToForge;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartForgingTools()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeForge == null
                || forgeWorkplace == null
                || activeForge != forgeWorkplace
                || !CanWork)
            {
                ResetForgeWorkToIdle(false);
                return;
            }

            if (!TryBeginForgingCycle())
            {
                EnterForgeStandby();
            }
        }

        private bool TryBeginForgingCycle()
        {
            if (activeForge == null || !activeForge.TryConsumeInputsForWork(out forgeToolsPending))
            {
                return false;
            }

            activity = ResidentActivity.ForgingTools;
            forgeWorkTimer = GetUpgradedWorkDuration(ForgeWorkSecondsMin, ForgeWorkSecondsMax, activeForge);
            activeForge.BeginForging(this);
            transform.position = activeForge.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            FaceWorldPoint(activeForge.GetForgeFocusWorld());
            ResetForgeWorkEffectTimer(true);
            return true;
        }

        private void EnterForgeStandby()
        {
            activity = ResidentActivity.StandingByAtForge;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.position = activeForge.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            FaceWorldPoint(activeForge.GetForgeFocusWorld());
        }

        private void UpdateForgeStandby()
        {
            if (activeForge == null
                || forgeWorkplace == null
                || activeForge != forgeWorkplace
                || !CanWork)
            {
                ResetForgeWorkToIdle(false);
                return;
            }

            transform.position = activeForge.GetInteriorWorkWorld(this);
            if (forgeWorkCooldown <= 0f && TryBeginForgingCycle())
            {
                return;
            }

            AnimateIdle();
            FaceWorldPoint(activeForge.GetForgeFocusWorld());
        }

        private void UpdateForgingTools()
        {
            if (activeForge == null || forgeWorkplace == null || activeForge != forgeWorkplace)
            {
                ResetForgeWorkToIdle(false);
                return;
            }

            transform.position = activeForge.GetInteriorWorkWorld(this);
            FaceWorldPoint(activeForge.GetForgeFocusWorld());
            AnimateLumberWork(9.4f, 3.4f);
            UpdateForgeWorkEffects();
            forgeWorkTimer -= Time.deltaTime;
            if (forgeWorkTimer > 0f)
            {
                return;
            }

            StrategyForge completedForge = activeForge;
            completedForge.AddTools(forgeToolsPending);
            completedForge.EndForging(this);
            forgeToolsPending = 0;
            forgeWorkCooldown = Random.Range(1.0f, 2.6f);
            if (forgeWorkplace == completedForge
                && CanWork
                && !StrategyDayNightCycleController.IsResidentEveningHomeTime)
            {
                activeForge = completedForge;
                EnterForgeStandby();
                return;
            }

            activeForge = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.20f, 0.55f);
        }

        private void ResetForgeWorkToIdle(bool storeCarried)
        {
            if (forgeToolsPending > 0)
            {
                activeForge?.ReleasePendingTools(forgeToolsPending);
            }

            activeForge?.EndForging(this);
            activeForge = null;
            forgeToolsPending = 0;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (storeCarried && TryStartStorageCarriedReturn("forge_work_cancelled"))
            {
                return;
            }

            forgeWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelForgeWork(bool storeCarried)
        {
            if (this == null)
            {
                return;
            }

            bool active = activity == ResidentActivity.MovingToForge
                || activity == ResidentActivity.StandingByAtForge
                || activity == ResidentActivity.ForgingTools;
            if (active)
            {
                ResetForgeWorkToIdle(storeCarried);
                return;
            }

            activeForge = null;
        }

        private void ResetForgeWorkEffectTimer(bool immediate)
        {
            forgeWorkEffectTimer = immediate ? 0f : Random.Range(0.18f, 0.45f);
        }

        private void UpdateForgeWorkEffects()
        {
            if (activeForge == null)
            {
                return;
            }

            forgeWorkEffectTimer -= Time.deltaTime;
            if (forgeWorkEffectTimer > 0f)
            {
                return;
            }

            activeForge.PlayForgingWorkEffect(ResidentId + Mathf.RoundToInt(Time.time * 12f));
            ResetForgeWorkEffectTimer(false);
        }
    }
}
