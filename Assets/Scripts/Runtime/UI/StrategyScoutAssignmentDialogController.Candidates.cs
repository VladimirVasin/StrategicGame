using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutAssignmentDialogController
    {
        private const int IntroductionCandidateCount = 3;

        private void ApplyModeCopy()
        {
            if (expeditionOnlyMode)
            {
                SetLocalizedText(titleText, "resident.dialog.scout.mode.expedition.title");
                SetLocalizedText(subtitleText, "resident.dialog.scout.mode.expedition.subtitle");
                SetLocalizedText(storyText, "resident.dialog.scout.mode.expedition.story");
                return;
            }

            SetLocalizedText(
                titleText,
                introductionMode
                    ? "resident.dialog.scout.mode.introduction.title"
                    : "resident.dialog.scout.mode.appointment.title");
            SetLocalizedText(
                subtitleText,
                introductionMode
                    ? "resident.dialog.scout.mode.introduction.subtitle"
                    : "resident.dialog.scout.mode.appointment.subtitle");
            SetLocalizedText(
                storyText,
                introductionMode
                    ? "resident.dialog.scout.mode.introduction.story"
                    : "resident.dialog.scout.mode.appointment.story");
        }

        private void RefreshCandidates()
        {
            candidates.Clear();
            if (expeditionOnlyMode && fixedExpeditionResident != null)
            {
                bool eligible = IsResidentEligibleForCurrentRequest(fixedExpeditionResident);
                candidates.Add(new ScoutCandidate(
                    fixedExpeditionResident,
                    eligible,
                    eligible
                        ? string.Empty
                        : L("resident.dialog.scout.row.blocked.scout_lodge")));
            }
            else if (population != null)
            {
                IReadOnlyList<StrategyResidentAgent> residents = introductionMode
                    ? introductionCandidates
                    : population.Residents;
                for (int i = 0; i < residents.Count; i++)
                {
                    StrategyResidentAgent resident = residents[i];
                    if (resident == null || !resident.IsAdult)
                    {
                        continue;
                    }

                    bool eligible = lodge != null && lodge.CanAppointWorker(resident);
                    string reason = eligible || lodge == null
                        ? string.Empty
                        : lodge.GetAssignmentBlockReason(resident);
                    candidates.Add(new ScoutCandidate(resident, eligible, reason));
                }
            }

            candidates.Sort(CompareCandidates);
            EnsureRowCount(candidates.Count);
            bool selectedStillEligible = false;
            for (int i = 0; i < rowPool.Count; i++)
            {
                bool active = i < candidates.Count;
                StrategyScoutAssignmentRowView row = rowPool[i];
                row.Root.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                ScoutCandidate candidate = candidates[i];
                bool selected = candidate.Resident == selectedResident && candidate.Eligible;
                selectedStillEligible |= selected;
                row.Root.anchoredPosition = new Vector2(0f, -i * StrategyScoutAssignmentRowView.RowHeight);
                row.Set(candidate.Resident, candidate.Eligible, candidate.Reason, selected, i, SelectCandidate);
            }

            if (!selectedStillEligible)
            {
                if (selectedResident != null && !expeditionOnlyMode)
                {
                    SetActionStatus(
                        "resident.dialog.scout.status.selected_unavailable_choose_another",
                        true);
                }

                selectedResident = expeditionOnlyMode ? fixedExpeditionResident : null;
            }

            contentRoot.sizeDelta = new Vector2(
                0f,
                Mathf.Max(1f, candidates.Count * StrategyScoutAssignmentRowView.RowHeight));
            UpdateCandidateSummary();
            UpdateActions();
            EnsureDialogFocus();
        }

        private void EnsureRowCount(int count)
        {
            while (rowPool.Count < count)
            {
                rowPool.Add(CreateCandidateRow(rowPool.Count));
            }
        }

        private void SelectCandidate(StrategyResidentAgent resident)
        {
            if (!IsResidentEligibleForCurrentRequest(resident))
            {
                SetActionStatus(
                    "resident.dialog.scout.status.resident_unavailable_for_trail",
                    true);
                RefreshCandidates();
                return;
            }

            selectedResident = resident;
            SetActionStatus(
                expeditionOnlyMode
                    ? "resident.dialog.scout.status.ready_choose_range"
                    : introductionMode
                    ? "resident.dialog.scout.status.ready_first_map"
                    : "resident.dialog.scout.status.ready_lodge_compass",
                false);
            RefreshCandidates();
            PlaySfx(StrategyHudSfxKind.Step);
        }

        private void ConfirmSelection()
        {
            StrategyResidentAgent resident = selectedResident;
            if (!IsResidentEligibleForCurrentRequest(resident))
            {
                SetActionStatus(
                    expeditionOnlyMode
                        ? "resident.dialog.scout.status.scout_lodge_not_ready"
                        : "resident.dialog.scout.status.chosen_unavailable_choose_another",
                    true);
                RefreshCandidates();
                return;
            }

            if (!HasSufficientExpeditionRations())
            {
                SetActionStatus(
                    "resident.dialog.scout.status.required_rations_missing",
                    true);
                RefreshCandidates();
                return;
            }

            int expeditionDays = selectedExpeditionDays;
            if (tryAssign == null || !tryAssign(resident, expeditionDays))
            {
                SetActionStatus(
                    "resident.dialog.scout.status.departure_failed",
                    true);
                RefreshCandidates();
                return;
            }

            callbackResolved = true;
            deferredCallback = null;
            panelTransition.SetVisible(false);
            RefreshInputContext(ShouldHoldInputContext);
            StrategyDebugLogger.Info(
                "ScoutAssignment",
                expeditionOnlyMode ? "ExpeditionStarted" : "ScoutAppointedAndDispatched",
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("days", expeditionDays));
            ClearRequestState();
            PlaySfx(StrategyHudSfxKind.Confirm);
        }

        private void DeferSelection()
        {
            if (introductionMode
                && CountEligibleCandidates() > 0
                && HasSufficientExpeditionRations())
            {
                return;
            }

            StrategyDebugLogger.Info(
                "ScoutAssignment",
                introductionMode ? "IntroductionDeferred" : "DialogCancelled");
            Close(true, true);
        }

        private void UpdateCandidateSummary()
        {
            int eligibleCount = CountEligibleCandidates();
            if (expeditionOnlyMode)
            {
                SetLocalizedText(
                    candidateHeadingText,
                    eligibleCount > 0
                        ? "resident.dialog.scout.candidates.expedition_ready"
                        : "resident.dialog.scout.candidates.expedition_blocked");
                emptyText.gameObject.SetActive(false);
                return;
            }

            SetLocalizedText(
                candidateHeadingText,
                eligibleCount == 1
                    ? "resident.dialog.scout.candidates.ready.one"
                    : "resident.dialog.scout.candidates.ready.many",
                eligibleCount);
            emptyText.gameObject.SetActive(candidates.Count == 0);
            if (candidates.Count == 0)
            {
                SetLocalizedText(
                    emptyText,
                    "resident.dialog.scout.candidates.empty");
            }
        }

        private void UpdateActions()
        {
            RefreshExpeditionControls();
            bool hasSelection = IsResidentEligibleForCurrentRequest(selectedResident);
            bool affordable = HasSufficientExpeditionRations();
            confirmButton.interactable = hasSelection && affordable;
            if (!hasSelection)
            {
                SetLocalizedText(
                    confirmLabel,
                    "resident.dialog.scout.confirm.choose_resident");
            }
            else if (expeditionOnlyMode)
            {
                SetLocalizedText(
                    confirmLabel,
                    GetDayFormKey("resident.dialog.scout.confirm.send", selectedExpeditionDays),
                    selectedResident.FullName,
                    selectedExpeditionDays);
            }
            else
            {
                SetLocalizedText(
                    confirmLabel,
                    GetDayFormKey("resident.dialog.scout.confirm.appoint", selectedExpeditionDays),
                    selectedResident.FullName,
                    selectedExpeditionDays);
            }

            int eligibleCount = CountEligibleCandidates();
            bool showDefer = !introductionMode || eligibleCount == 0 || !affordable;
            deferButton.gameObject.SetActive(showDefer);
            SetLocalizedText(
                deferLabel,
                introductionMode
                    ? "resident.dialog.scout.action.decide_later"
                    : "resident.dialog.scout.action.cancel");
            if (introductionMode && eligibleCount == 0 && !HasActionStatus)
            {
                SetActionStatus(
                    "resident.dialog.scout.status.no_free_adult",
                    true);
            }
            else if (introductionMode && !affordable && !HasActionStatus)
            {
                SetActionStatus(
                    "resident.dialog.scout.status.first_expedition_needs_rations",
                    true);
            }
        }

        private void EnsureDialogFocus()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject current = eventSystem.currentSelectedGameObject;
            if (current != null
                && current.activeInHierarchy
                && board != null
                && current.transform.IsChildOf(board))
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Eligible && i < rowPool.Count)
                {
                    rowPool[i].Root.GetComponent<StrategyUiButtonFeedback>()
                        ?.SuppressNextFocusCue();
                    eventSystem.SetSelectedGameObject(rowPool[i].Root.gameObject);
                    return;
                }
            }

            if (deferButton != null && deferButton.gameObject.activeInHierarchy)
            {
                deferButton.GetComponent<StrategyUiButtonFeedback>()?.SuppressNextFocusCue();
                eventSystem.SetSelectedGameObject(deferButton.gameObject);
            }
        }

        private int CountEligibleCandidates()
        {
            int count = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                count += candidates[i].Eligible ? 1 : 0;
            }

            return count;
        }

        private bool HasActionStatus => !string.IsNullOrEmpty(actionStatusKey);

        private void SetActionStatus(
            string key,
            bool warning,
            params object[] arguments)
        {
            actionStatusKey = key ?? string.Empty;
            actionStatusArguments = arguments ?? Array.Empty<object>();
            actionStatusWarning = warning;
            RefreshActionStatus();
        }

        private void RefreshActionStatus()
        {
            if (actionStatusText == null)
            {
                return;
            }

            SetLocalizedText(
                actionStatusText,
                HasActionStatus ? actionStatusKey : string.Empty,
                actionStatusArguments);
            actionStatusText.color = actionStatusWarning
                ? new Color(1f, 0.62f, 0.42f, 1f)
                : new Color(0.84f, 0.75f, 0.52f, 1f);
        }

        private static string L(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Residents,
                key,
                arguments);
        }

        private static void SetLocalizedText(
            UnityEngine.UI.Text target,
            string key,
            params object[] arguments)
        {
            if (target == null)
            {
                return;
            }

            StrategyLocalizedTextBinding.Bind(
                target,
                StrategyLocalizationTables.Residents,
                key,
                arguments);
        }

        private static string GetDayFormKey(string prefix, int days)
        {
            if (StrategyLocalization.CurrentLanguage == StrategyGameLanguage.English)
            {
                return prefix + (days == 1 ? ".one" : ".many");
            }

            int value = Mathf.Abs(days);
            int mod100 = value % 100;
            if (mod100 < 11 || mod100 > 14)
            {
                int mod10 = value % 10;
                if (mod10 == 1)
                {
                    return prefix + ".one";
                }

                if (mod10 >= 2 && mod10 <= 4)
                {
                    return prefix + ".few";
                }
            }

            return prefix + ".many";
        }

        private static int CompareCandidates(ScoutCandidate left, ScoutCandidate right)
        {
            int eligibility = right.Eligible.CompareTo(left.Eligible);
            if (eligibility != 0)
            {
                return eligibility;
            }

            string leftName = left.Resident != null ? left.Resident.FullName : string.Empty;
            string rightName = right.Resident != null ? right.Resident.FullName : string.Empty;
            int name = string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
            if (name != 0)
            {
                return name;
            }

            int leftId = left.Resident != null ? left.Resident.ResidentId : int.MaxValue;
            int rightId = right.Resident != null ? right.Resident.ResidentId : int.MaxValue;
            return leftId.CompareTo(rightId);
        }

        private void PrepareIntroductionCandidates()
        {
            introductionCandidates.Clear();
            if (!introductionMode || population == null)
            {
                return;
            }

            List<StrategyResidentAgent> preferred = new();
            List<StrategyResidentAgent> otherReady = new();
            List<StrategyResidentAgent> blocked = new();
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || !resident.IsAdult)
                {
                    continue;
                }

                if (lodge != null && lodge.CanAppointWorker(resident))
                {
                    bool haulerOrBuilder = resident.IsSettlementHauler
                        || resident.IsSettlementBuilder
                        || resident.StorageWorkplace != null
                        || resident.BuilderWorkplace != null
                        || resident.GranaryWorkplace != null;
                    (haulerOrBuilder ? preferred : otherReady).Add(resident);
                }
                else
                {
                    blocked.Add(resident);
                }
            }

            ShuffleCandidates(preferred);
            ShuffleCandidates(otherReady);
            ShuffleCandidates(blocked);
            AppendCandidates(preferred);
            AppendCandidates(otherReady);
            AppendCandidates(blocked);
        }

        private void AppendCandidates(List<StrategyResidentAgent> source)
        {
            for (int i = 0;
                i < source.Count && introductionCandidates.Count < IntroductionCandidateCount;
                i++)
            {
                introductionCandidates.Add(source[i]);
            }
        }

        private static void ShuffleCandidates(List<StrategyResidentAgent> source)
        {
            for (int i = source.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (source[i], source[swapIndex]) = (source[swapIndex], source[i]);
            }
        }
    }
}
