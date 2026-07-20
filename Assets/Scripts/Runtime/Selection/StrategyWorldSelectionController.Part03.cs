using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {

        private void RefreshWorkers(StrategyFisherHut hut)
        {
            int workerCount = hut != null ? hut.WorkerCount : 0;
            bool canAssign = hut != null && hut.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? L("workers.assign_fishers")
                    : L("workers.no_free_residents");
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyFisherHut.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = hut != null && hut.TryGetWorker(i, out worker);

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant, worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : L("workers.fisher_open");
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : L("workers.catches_fish");
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? L("action.remove")
                        : L("action.assign");
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void RefreshWorkers(StrategyStorageYard yard)
        {
            int workerCount = yard != null ? yard.WorkerCount : 0;
            int builderCount = yard != null ? yard.BuilderCount : 0;
            bool canAssignWorker = yard != null && yard.CanAssignNextAvailableWorker();
            bool canAssignBuilder = yard != null && yard.CanAssignNextAvailableBuilder();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount + builderCount <= 0);
                workersEmptyText.text = canAssignWorker || canAssignBuilder
                    ? L("workers.hire_haulers_builders")
                    : L("workers.no_free_residents");
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                StrategyResidentAgent worker = null;
                bool isBuilderSlot = i >= StorageWorkerHudSlots;
                int staffIndex = isBuilderSlot ? i - StorageWorkerHudSlots : i;
                bool hasWorker = yard != null
                    && (isBuilderSlot
                        ? yard.TryGetBuilder(staffIndex, out worker)
                        : yard.TryGetWorker(staffIndex, out worker));
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(true);
                }

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant, worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : isBuilderSlot
                            ? L("workers.builder_open")
                            : L("workers.hauler_open");
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : isBuilderSlot
                            ? L("workers.builds_structures")
                            : L("workers.hauls_resources_food");
                }

                bool buttonEnabled = hasWorker
                    || (isBuilderSlot
                        ? staffIndex == builderCount && canAssignBuilder
                        : staffIndex == workerCount && canAssignWorker);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? L("action.remove")
                        : L("action.assign");
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void RefreshWorkers(StrategyStonecutterCamp camp)
        {
            int workerCount = camp != null ? camp.WorkerCount : 0;
            bool canAssign = camp != null && camp.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? L("workers.assign_residents")
                    : L("workers.no_free_residents");
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyStonecutterCamp.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = camp != null && camp.TryGetWorker(i, out worker);

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant, worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : L("workers.open_slot");
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : L("workers.up_to_two");
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? L("action.remove")
                        : L("action.assign");
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void SetProfileSectionVisible(bool visible)
        {
            if (summaryBackground != null)
            {
                summaryBackground.gameObject.SetActive(visible);
            }

            if (hudSummaryTitleText != null)
            {
                hudSummaryTitleText.gameObject.SetActive(visible);
            }

            if (hudBodyText != null)
            {
                hudBodyText.gameObject.SetActive(visible);
            }
        }

        private void SetStatusSectionVisible(bool visible)
        {
            if (statusBackground != null)
            {
                statusBackground.gameObject.SetActive(visible);
            }

            if (hudStatusTitleText != null)
            {
                hudStatusTitleText.gameObject.SetActive(visible);
            }

            if (hudStatusBodyText != null)
            {
                hudStatusBodyText.gameObject.SetActive(visible);
            }
        }

        private void LayoutStatusSection(float top, float height)
        {
            if (statusBackground != null)
            {
                SetTopStretch(statusBackground, 18f, top, 18f, height);
            }

            if (hudStatusTitleText != null)
            {
                SetTopStretch(hudStatusTitleText.rectTransform, 24f, top + 12f, 24f, 20f);
            }

            if (hudStatusBodyText != null)
            {
                SetTopStretch(hudStatusBodyText.rectTransform, 24f, top + 38f, 24f, Mathf.Max(28f, height - 50f));
            }
        }
        private void SetContextSectionVisible(bool visible)
        {
            if (contextBackground != null)
            {
                contextBackground.gameObject.SetActive(visible);
            }

            if (hudContextTitleText != null)
            {
                hudContextTitleText.gameObject.SetActive(visible);
            }

            if (hudContextBodyText != null)
            {
                hudContextBodyText.gameObject.SetActive(visible);
            }
        }

        private void LayoutContextSection(float top, float height)
        {
            if (contextBackground != null)
            {
                SetTopStretch(contextBackground, 18f, top, 18f, height);
            }

            if (hudContextTitleText != null)
            {
                SetTopStretch(hudContextTitleText.rectTransform, 24f, top + 12f, 24f, 20f);
            }

            if (hudContextBodyText != null)
            {
                SetTopStretch(hudContextBodyText.rectTransform, 24f, top + 38f, 24f, Mathf.Max(28f, height - 50f));
            }
        }

        private void SetResidentsSectionVisible(bool visible)
        {
            if (residentsRoot != null)
            {
                residentsRoot.gameObject.SetActive(visible);
            }
        }

        private void SetWorkersSectionVisible(bool visible)
        {
            if (workersRoot != null)
            {
                workersRoot.gameObject.SetActive(visible);
            }
        }

        private void ToggleWorkerSlot(int index)
        {
            StrategyLumberjackCamp camp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyLumberjackCamp>()
                : null;
            if (camp != null)
            {
                ToggleLumberjackWorkerSlot(camp, index);
                return;
            }

            StrategyStonecutterCamp stoneCamp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyStonecutterCamp>()
                : null;
            if (stoneCamp != null)
            {
                ToggleStonecutterWorkerSlot(stoneCamp, index);
                return;
            }

            StrategyHunterCamp hunterCamp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyHunterCamp>()
                : null;
            if (hunterCamp != null)
            {
                ToggleHunterWorkerSlot(hunterCamp, index);
                return;
            }

            StrategyFisherHut fisherHut = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyFisherHut>()
                : null;
            if (fisherHut != null)
            {
                ToggleFisherWorkerSlot(fisherHut, index);
                return;
            }

            StrategyScoutLodge scoutLodge = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyScoutLodge>()
                : null;
            if (scoutLodge != null)
            {
                ToggleScoutWorkerSlot(scoutLodge, index);
                return;
            }

            StrategyStorageYard yard = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyStorageYard>()
                : null;
            if (yard != null)
            {
                ToggleStorageWorkerSlot(yard, index);
            }
        }

        private void ToggleLumberjackWorkerSlot(StrategyLumberjackCamp camp, int index)
        {
            if (camp == null)
            {
                return;
            }

            if (index < camp.WorkerCount)
            {
                camp.TryGetWorker(index, out StrategyResidentAgent worker);
                camp.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin));
            }
            else
            {
                bool assigned = camp.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin));
            }

            RefreshHud();
        }
    }
}
