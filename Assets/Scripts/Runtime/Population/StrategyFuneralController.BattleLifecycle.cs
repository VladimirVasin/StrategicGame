using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFuneralController
    {
        private StrategyBattleLifecycleController battleLifecycle;

        private bool AreFuneralsBlocked => battleLifecycle != null
            && battleLifecycle.IsBattleInProgress;

        internal int ActiveFuneralCount => activeFunerals.Count;

        internal int AwaitingBattleEndCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < activeFunerals.Count; i++)
                {
                    FuneralProcess funeral = activeFunerals[i];
                    if (funeral != null
                        && !funeral.Completed
                        && funeral.Stage == FuneralStage.AwaitingBattleEnd)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void ConfigureBattleLifecycle(StrategyBattleLifecycleController controller)
        {
            if (battleLifecycle == controller)
            {
                return;
            }

            if (battleLifecycle != null)
            {
                battleLifecycle.PhaseChanged -= HandleBattlePhaseChanged;
            }

            battleLifecycle = controller;
            if (battleLifecycle == null)
            {
                return;
            }

            battleLifecycle.PhaseChanged += HandleBattlePhaseChanged;
            if (battleLifecycle.IsBattleInProgress)
            {
                SuspendAllFuneralsForBattle();
            }
        }

        private void EnsureLegacyBattleLifecycleBinding()
        {
            if (battleLifecycle != null)
            {
                return;
            }

            ConfigureBattleLifecycle(Object.FindAnyObjectByType<StrategyBattleLifecycleController>());
        }

        private void HandleBattlePhaseChanged(
            StrategyBattlePhase previousPhase,
            StrategyBattlePhase currentPhase)
        {
            if (currentPhase != StrategyBattlePhase.Peaceful)
            {
                SuspendAllFuneralsForBattle();
                return;
            }

            ResumeAwaitingFunerals();
        }

        private void SuspendAllFuneralsForBattle()
        {
            for (int i = 0; i < activeFunerals.Count; i++)
            {
                SuspendFuneralForBattle(activeFunerals[i]);
            }

            ReleaseAllSettlementFuneralDuties();
        }

        private void SuspendFuneralForBattle(FuneralProcess funeral)
        {
            if (funeral == null
                || funeral.Completed
                || funeral.Stage == FuneralStage.AwaitingBattleEnd)
            {
                return;
            }

            FuneralStage interruptedStage = funeral.Stage;
            SetFuneralTorchInactive(funeral);
            EndFuneralDuties(funeral.Participants);
            EndFuneralDuties(funeral.Carriers);
            EndFuneralDuties(funeral.ExpectedBurialAttendees);
            ReleaseReservedGrave(funeral);
            funeral.Corpse?.ResetToGroundCorpseVisual();
            funeral.Participants.Clear();
            funeral.Carriers.Clear();
            funeral.ExpectedBurialAttendees.Clear();
            funeral.PrimaryCarrier = null;
            funeral.TorchBearer = null;
            funeral.GraveCell = default;
            funeral.GraveWorld = default;
            funeral.Timer = 0f;
            funeral.Dispatched = false;
            funeral.StartedAtNight = false;
            funeral.NextTorchAssignmentTime = 0f;
            funeral.NextGraveClearanceTime = 0f;
            funeral.Stage = FuneralStage.AwaitingBattleEnd;

            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralSuspendedForBattle",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("interruptedStage", interruptedStage),
                StrategyDebugLogger.F("battlePhase", battleLifecycle != null
                    ? battleLifecycle.Phase
                    : StrategyBattlePhase.Peaceful),
                StrategyDebugLogger.F("corpseWorld", funeral.Corpse != null
                    ? funeral.Corpse.transform.position
                    : funeral.Snapshot.DeathWorld));
        }

        private void ResumeAwaitingFunerals()
        {
            for (int i = 0; i < activeFunerals.Count; i++)
            {
                FuneralProcess funeral = activeFunerals[i];
                if (funeral != null
                    && !funeral.Completed
                    && funeral.Stage == FuneralStage.AwaitingBattleEnd)
                {
                    StartFuneralFromBeginning(funeral, "battle_ended");
                }
            }
        }

        private void StartFuneralFromBeginning(FuneralProcess funeral, string reason)
        {
            if (funeral == null
                || funeral.Completed
                || funeral.Corpse == null
                || AreFuneralsBlocked
                || funeral.Stage != FuneralStage.AwaitingBattleEnd)
            {
                return;
            }

            funeral.Stage = FuneralStage.WaitingForCorpse;
            funeral.Timer = 0f;
            funeral.Dispatched = false;
            funeral.StartedAtNight = IsNightFuneralTorchTime();
            funeral.NextTorchAssignmentTime = 0f;
            funeral.NextGraveClearanceTime = 0f;

            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("startedAtNight", funeral.StartedAtNight),
                StrategyDebugLogger.F("activeFunerals", activeFunerals.Count));
            RecallFamilyForFuneral(funeral, reason);
        }

        private void SetFuneralTorchInactive(FuneralProcess funeral)
        {
            funeral?.TorchBearer?.SetFuneralNightTorchActive(false);
        }

        private void ReleaseAllSettlementFuneralDuties()
        {
            if (population == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || !resident.IsFuneralDutyActive)
                {
                    continue;
                }

                resident.SetFuneralNightTorchActive(false);
                resident.EndFuneralDuty();
            }
        }

        private void OnDestroy()
        {
            if (battleLifecycle != null)
            {
                battleLifecycle.PhaseChanged -= HandleBattlePhaseChanged;
            }
        }
    }
}
