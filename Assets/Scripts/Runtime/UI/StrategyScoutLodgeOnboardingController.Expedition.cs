using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutLodgeOnboardingController
    {
        private StrategyResidentAgent pendingExpeditionResident;
        private bool dispatchExistingScout;

        public bool RequestExpedition(StrategyScoutLodge lodge)
        {
            if (!isActiveAndEnabled
                || lodge == null
                || lodge.ExpeditionState != StrategyScoutExpeditionState.Ready
                || lodge.WorkerCount <= 0
                || IsActive
                || (dialog != null && dialog.IsInputShieldActive)
                || !lodge.TryGetWorker(0, out StrategyResidentAgent resident)
                || !lodge.CanDispatchScout(resident))
            {
                return false;
            }

            StrategyPlacedBuilding building = lodge.GetComponent<StrategyPlacedBuilding>();
            if (building == null)
            {
                return false;
            }

            QueueFlow(building, lodge, false);
            dispatchExistingScout = true;
            pendingExpeditionResident = resident;
            return true;
        }

        private bool CanProceedWithPendingRequest()
        {
            if (!dispatchExistingScout)
            {
                return pendingLodge != null
                    && pendingLodge.WorkerCount < StrategyScoutLodge.MaxWorkers;
            }

            return pendingLodge != null
                && pendingLodge.ExpeditionState == StrategyScoutExpeditionState.Ready
                && pendingLodge.TryGetWorker(0, out StrategyResidentAgent resident)
                && resident == pendingExpeditionResident
                && pendingLodge.CanDispatchScout(resident);
        }

        private bool TryStartSelectedExpedition(
            StrategyResidentAgent resident,
            int expeditionDays)
        {
            if (!HasUsableTarget()
                || !dispatchExistingScout
                || resident == null
                || resident != pendingExpeditionResident
                || !pendingLodge.TryStartExpedition(expeditionDays))
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
                return false;
            }

            StrategyPlacedBuilding completedBuilding = pendingBuilding;
            selection?.SelectBuilding(completedBuilding);
            StrategyEventLogHudController.Notify(
                resident.FullName + " departed on a " + expeditionDays + "-day expedition.",
                new Color(0.86f, 0.70f, 0.42f));
            StrategyDebugLogger.Info(
                "ScoutOnboarding",
                "ExpeditionStarted",
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("days", expeditionDays),
                StrategyDebugLogger.F("origin", completedBuilding.Origin));
            CompleteFlow();
            return true;
        }

        private void ClearExpeditionDispatchRequest()
        {
            dispatchExistingScout = false;
            pendingExpeditionResident = null;
        }
    }
}
