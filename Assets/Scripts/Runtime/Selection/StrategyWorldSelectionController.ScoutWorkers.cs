using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private void RefreshWorkers(StrategyScoutLodge lodge)
        {
            int workerCount = lodge != null ? lodge.WorkerCount : 0;
            bool canAssign = lodge != null && lodge.CanAssignNextAvailableWorker();

            if (workersRoot != null)
            {
                SetTopStretch(workersRoot, 18f, 128f, 18f, 96f);
            }

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(false);
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyScoutLodge.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = lodge != null && lodge.TryGetWorker(i, out worker);
                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(
                            worker.Gender,
                            worker.VisualVariant,
                            worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker ? worker.FullName : "Scout: open";
                    workerNameTexts[i].color = hasWorker
                        ? Color.white
                        : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : canAssign
                            ? "free adult available"
                            : "no free adult available";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker ? "Remove" : "Assign";
                    workerActionTexts[i].color = buttonEnabled
                        ? Color.white
                        : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void ToggleScoutWorkerSlot(StrategyScoutLodge lodge, int index)
        {
            if (lodge == null || index < 0 || index >= StrategyScoutLodge.MaxWorkers)
            {
                return;
            }

            bool assigned;
            StrategyResidentAgent worker;
            string action;
            if (index < lodge.WorkerCount)
            {
                lodge.TryGetWorker(index, out worker);
                lodge.UnassignWorkerAt(index);
                assigned = true;
                action = "unassign";
            }
            else
            {
                if (scoutLodgeOnboarding != null)
                {
                    worker = null;
                    assigned = scoutLodgeOnboarding.RequestAssignment(lodge);
                    action = "choose";
                }
                else
                {
                    assigned = lodge.TryAssignNextAvailableWorker(out worker);
                    action = "assign";
                }
            }

            StrategyDebugLogger.Info(
                "Selection",
                "WorkerSlotClicked",
                StrategyDebugLogger.F("action", action),
                StrategyDebugLogger.F("slot", index),
                StrategyDebugLogger.F("success", assigned),
                StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                StrategyDebugLogger.F("lodgeOrigin", lodge.Origin),
                StrategyDebugLogger.F("profession", "scout"));
            StrategyHudSfxAudio.Play(assigned ? StrategyHudSfxKind.Step : StrategyHudSfxKind.Deny);
            RefreshHud();
        }
    }
}
