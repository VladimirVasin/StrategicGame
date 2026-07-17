using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTrashHeapStoryEncounterController : MonoBehaviour,
        IStrategyStoryPointOfInterestEncounter
    {
        private const string PauseReason = "TrashHeapStoryChoice";
        private const string RewardHeadline = "Вы нашли дырявую ложку";

        private StrategyPointOfInterestDialogController dialog;
        private StrategyTimeScaleController timeScale;
        private StrategyInGameCinematicPlayer cinematicPlayer;
        private StrategyResidentItemRewardRevealController rewardReveal;
        private StrategyStoryPointOfInterestDefinition activeDefinition;
        private StrategyStoryPointOfInterestAnchor activeAnchor;
        private StrategyResidentAgent activeResident;
        private Action<StrategyStoryPointOfInterestOutcome> completedCallback;
        private StrategyTrashHeapSearchCinematic cinematic;
        private EncounterState state;
        private bool configured;
        private bool pauseHeld;

        public string EncounterId => StrategyStoryPointOfInterestCatalog.TrashHeapEncounterId;
        public bool IsActive => state != EncounterState.Idle;

        public void Configure(
            StrategyPointOfInterestDialogController dialogController,
            StrategyTimeScaleController timeScaleController,
            StrategyInGameCinematicPlayer inGameCinematicPlayer,
            StrategyResidentItemRewardRevealController residentRewardReveal)
        {
            CancelActive();
            dialog = dialogController;
            timeScale = timeScaleController;
            cinematicPlayer = inGameCinematicPlayer;
            rewardReveal = residentRewardReveal;
            configured = dialog != null
                && timeScale != null
                && cinematicPlayer != null
                && rewardReveal != null;
        }

        public bool TryBegin(
            StrategyStoryPointOfInterestDefinition definition,
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident,
            Action<StrategyStoryPointOfInterestOutcome> onCompleted)
        {
            StrategyResidentPersonalInventoryFailure failure =
                StrategyResidentPersonalInventoryFailure.None;
            if (!configured
                || IsActive
                || definition == null
                || definition.EncounterId != EncounterId
                || anchor == null
                || resident == null
                || !anchor.IsCommittedTo(resident)
                || !resident.CanAddPersonalItem(
                    StrategyResidentItemCatalog.HoleySpoonId,
                    1,
                    out failure))
            {
                if (resident != null && failure != StrategyResidentPersonalInventoryFailure.None)
                {
                    StrategyDebugLogger.Warn(
                        "StoryPointOfInterest",
                        "TrashHeapRewardBlocked",
                        StrategyDebugLogger.F("residentId", resident.ResidentId),
                        StrategyDebugLogger.F("failure", failure));
                }

                return false;
            }

            activeDefinition = definition;
            activeAnchor = anchor;
            activeResident = resident;
            completedCallback = onCompleted;
            state = EncounterState.ChoicePending;
            if (!TryOpenChoice())
            {
                ResetActive();
                return false;
            }

            return true;
        }

        private void Update()
        {
            if (state == EncounterState.ChoicePending)
            {
                TryOpenChoice();
            }
            else if (state == EncounterState.RewardPending)
            {
                TryOpenReward();
            }
        }

        private bool TryOpenChoice()
        {
            if (!CanOpenModal())
            {
                return false;
            }

            timeScale.SetRequestedScale(1f);
            HoldPause();
            try
            {
                dialog.ShowChoice(
                    activeDefinition.Title,
                    activeDefinition.Body,
                    "Да, обыскать",
                    "Нет",
                    HandleAccepted,
                    HandleDeclined);
                state = EncounterState.ChoiceOpen;
                return true;
            }
            catch (Exception exception)
            {
                ReleasePause();
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "TrashHeapChoiceOpenFailed",
                    StrategyDebugLogger.F("error", exception.Message));
                return false;
            }
        }

        private void HandleDeclined()
        {
            if (state != EncounterState.ChoiceOpen)
            {
                return;
            }

            ReleasePause();
            Complete(StrategyStoryPointOfInterestOutcome.Declined);
        }

        private void HandleAccepted()
        {
            if (state != EncounterState.ChoiceOpen)
            {
                return;
            }

            ReleasePause();
            Sprite spoon = LoadRewardSprite();
            cinematic = new StrategyTrashHeapSearchCinematic(
                activeResident,
                activeAnchor,
                transform,
                spoon);
            if (spoon != null
                && cinematicPlayer.TryPlay(
                    cinematic,
                    new StrategyInGameCinematicOptions(
                        0.72f,
                        0.12f,
                        0.24f,
                        2.39f,
                        0.055f,
                        0.12f),
                    HandleCinematicCompleted))
            {
                state = EncounterState.Cinematic;
                return;
            }

            cinematic = null;
            state = EncounterState.ChoicePending;
            StrategyDebugLogger.Warn(
                "StoryPointOfInterest",
                "TrashHeapCinematicStartDeferred",
                StrategyDebugLogger.F("residentId", activeResident.ResidentId));
        }

        private void HandleCinematicCompleted(StrategyInGameCinematicResult result)
        {
            cinematic = null;
            if (state != EncounterState.Cinematic)
            {
                return;
            }

            if (result != StrategyInGameCinematicResult.Completed)
            {
                state = EncounterState.ChoicePending;
                return;
            }

            if (!activeResident.TryAddPersonalItem(
                    StrategyResidentItemCatalog.HoleySpoonId,
                    1,
                    out StrategyResidentPersonalInventoryFailure failure))
            {
                StrategyDebugLogger.Error(
                    "StoryPointOfInterest",
                    "TrashHeapRewardGrantFailed",
                    StrategyDebugLogger.F("residentId", activeResident.ResidentId),
                    StrategyDebugLogger.F("failure", failure));
                state = EncounterState.ChoicePending;
                return;
            }

            state = EncounterState.RewardPending;
            TryOpenReward();
        }

        private bool TryOpenReward()
        {
            if (activeResident == null
                || !activeResident.PersonalItemCatalog.TryGet(
                    StrategyResidentItemCatalog.HoleySpoonId,
                    out StrategyResidentItemDefinition definition))
            {
                Complete(StrategyStoryPointOfInterestOutcome.Accepted);
                return true;
            }

            Sprite artwork = LoadRewardSprite();
            if (artwork == null)
            {
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "TrashHeapRewardArtworkUnavailable");
                Complete(StrategyStoryPointOfInterestOutcome.Accepted);
                return true;
            }

            if (!rewardReveal.TryShow(
                    definition,
                    activeResident,
                    artwork,
                    RewardHeadline,
                    () => Complete(StrategyStoryPointOfInterestOutcome.Accepted)))
            {
                return false;
            }

            state = EncounterState.RewardOpen;
            return true;
        }

        private bool CanOpenModal()
        {
            return activeDefinition != null
                && activeAnchor != null
                && activeResident != null
                && activeAnchor.IsCommittedTo(activeResident)
                && dialog != null
                && !dialog.IsInputShieldActive
                && dialog.CanOpenWithoutStacking
                && (timeScale == null || !timeScale.IsPausedByLock);
        }

        private Sprite LoadRewardSprite()
        {
            if (!activeResident.PersonalItemCatalog.TryGet(
                    StrategyResidentItemCatalog.HoleySpoonId,
                    out StrategyResidentItemDefinition definition)
                || string.IsNullOrWhiteSpace(definition.IconResourcePath))
            {
                return null;
            }

            return Resources.Load<Sprite>(definition.IconResourcePath);
        }

        private void Complete(StrategyStoryPointOfInterestOutcome outcome)
        {
            Action<StrategyStoryPointOfInterestOutcome> callback = completedCallback;
            int residentId = activeResident != null ? activeResident.ResidentId : 0;
            ResetActive();
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "TrashHeapEncounterCompleted",
                StrategyDebugLogger.F("outcome", outcome),
                StrategyDebugLogger.F("residentId", residentId));
            callback?.Invoke(outcome);
        }

        private void HoldPause()
        {
            if (!pauseHeld)
            {
                timeScale.PushPauseLock(PauseReason);
                pauseHeld = true;
            }
        }

        private void ReleasePause()
        {
            if (pauseHeld)
            {
                timeScale?.PopPauseLock(PauseReason);
                pauseHeld = false;
            }
        }

        private void ResetActive()
        {
            ReleasePause();
            activeDefinition = null;
            activeAnchor = null;
            activeResident = null;
            completedCallback = null;
            cinematic = null;
            state = EncounterState.Idle;
        }

        private void CancelActive()
        {
            if (!IsActive)
            {
                return;
            }

            dialog?.Dismiss();
            cinematicPlayer?.Cancel(cinematic, false);
            ResetActive();
        }

        private void OnDisable()
        {
            CancelActive();
        }

        private enum EncounterState
        {
            Idle,
            ChoicePending,
            ChoiceOpen,
            Cinematic,
            RewardPending,
            RewardOpen
        }
    }
}
