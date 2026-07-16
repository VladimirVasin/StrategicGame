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
            titleText.text = introductionMode ? "THE FIRST EXPEDITION" : "APPOINT A SCOUT";
            subtitleText.text = introductionMode ? "Beyond the Firelight" : "Choose Who Takes the Trail";
            storyText.text = introductionMode
                ? "The roofs are standing and the camps are working, but beyond the last familiar path the valley is still only rumor. Choose one adult to carry our first map into the unknown."
                : "Every map begins with someone willing to leave the familiar road. Choose an adult to carry the Lodge's compass and chart what waits beyond the settlement.";
        }

        private void RefreshCandidates()
        {
            candidates.Clear();
            if (population != null)
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
                selectedResident = null;
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
            if (resident == null || lodge == null || !lodge.CanAppointWorker(resident))
            {
                SetActionStatus("That resident is no longer available for the trail.", true);
                RefreshCandidates();
                return;
            }

            selectedResident = resident;
            SetActionStatus(
                introductionMode
                    ? "Ready to carry the settlement's first map."
                    : "Ready to take the Lodge's compass beyond the familiar roads.",
                false);
            RefreshCandidates();
            PlaySfx(StrategyHudSfxKind.Step);
        }

        private void ConfirmSelection()
        {
            StrategyResidentAgent resident = selectedResident;
            if (resident == null || lodge == null || !lodge.CanAppointWorker(resident))
            {
                SetActionStatus("The chosen resident is no longer available. Choose another scout.", true);
                RefreshCandidates();
                return;
            }

            if (tryAssign == null || !tryAssign(resident))
            {
                SetActionStatus("The appointment could not be completed. Review the roster and try again.", true);
                RefreshCandidates();
                return;
            }

            callbackResolved = true;
            deferredCallback = null;
            panelTransition.SetVisible(false);
            RefreshInputContext(ShouldHoldInputContext);
            StrategyDebugLogger.Info(
                "ScoutAssignment",
                "ScoutAppointed",
                StrategyDebugLogger.F("resident", resident.FullName));
            ClearRequestState();
            PlaySfx(StrategyHudSfxKind.Confirm);
        }

        private void DeferSelection()
        {
            if (introductionMode && CountEligibleCandidates() > 0)
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
            candidateHeadingText.text = eligibleCount == 1
                ? "CHOOSE A RESIDENT  /  1 READY"
                : "CHOOSE A RESIDENT  /  " + eligibleCount + " READY";
            emptyText.gameObject.SetActive(candidates.Count == 0);
            if (candidates.Count == 0)
            {
                emptyText.text = "No adult residents are in the settlement yet.";
            }
        }

        private void UpdateActions()
        {
            bool hasSelection = selectedResident != null;
            confirmButton.interactable = hasSelection;
            confirmLabel.text = hasSelection
                ? "Appoint " + selectedResident.FullName + " as Scout"
                : "Choose a resident to continue";

            int eligibleCount = CountEligibleCandidates();
            bool showDefer = !introductionMode || eligibleCount == 0;
            deferButton.gameObject.SetActive(showDefer);
            deferLabel.text = introductionMode ? "Decide Later" : "Cancel";
            if (introductionMode && eligibleCount == 0 && string.IsNullOrEmpty(actionStatusText.text))
            {
                SetActionStatus("No one can leave their duties today. Return when an adult is free.", true);
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
            if (current != null && board != null && current.transform.IsChildOf(board))
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

        private void SetActionStatus(string message, bool warning)
        {
            if (actionStatusText == null)
            {
                return;
            }

            actionStatusText.text = message ?? string.Empty;
            actionStatusText.color = warning
                ? new Color(1f, 0.62f, 0.42f, 1f)
                : new Color(0.84f, 0.75f, 0.52f, 1f);
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
