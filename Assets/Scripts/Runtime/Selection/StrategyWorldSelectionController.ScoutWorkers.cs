using System.Globalization;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const float ScoutHudRefreshInterval = 0.2f;

        private float nextScoutHudRefreshAt;

        private void RefreshWorkers(StrategyScoutLodge lodge)
        {
            int workerCount = lodge != null ? lodge.WorkerCount : 0;
            bool canAssign = HasAppointableScoutCandidate(lodge);

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
                        ? GetScoutWorkerStatus(lodge, worker)
                        : canAssign
                            ? "free adult available"
                            : "no free adult available";
                }

                bool buttonEnabled = hasWorker
                    ? lodge.ExpeditionState == StrategyScoutExpeditionState.Ready
                        ? lodge.CanDispatchScout(worker)
                        : lodge.ExpeditionState == StrategyScoutExpeditionState.Exploring
                    : i == workerCount && canAssign;
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? lodge.ExpeditionState switch
                        {
                            StrategyScoutExpeditionState.Ready => "Send",
                            StrategyScoutExpeditionState.Exploring => "Recall",
                            StrategyScoutExpeditionState.Returning => "Returning",
                            _ => "Unavailable"
                        }
                        : "Assign";
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
                switch (lodge.ExpeditionState)
                {
                    case StrategyScoutExpeditionState.Ready:
                        assigned = scoutLodgeOnboarding != null
                            && scoutLodgeOnboarding.RequestExpedition(lodge);
                        action = "plan expedition";
                        break;
                    case StrategyScoutExpeditionState.Exploring:
                        assigned = lodge.RequestRecall();
                        action = "recall";
                        break;
                    default:
                        assigned = false;
                        action = "returning";
                        break;
                }
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

        private void UpdateSelectedScoutLodgeHud()
        {
            if (selectedTransform == null
                || selectedTransform.GetComponent<StrategyScoutLodge>() == null
                || Time.unscaledTime < nextScoutHudRefreshAt)
            {
                return;
            }

            nextScoutHudRefreshAt = Time.unscaledTime + ScoutHudRefreshInterval;
            RefreshHud();
        }

        private static string GetScoutWorkerStatus(
            StrategyScoutLodge lodge,
            StrategyResidentAgent worker)
        {
            if (lodge == null || worker == null)
            {
                return "unavailable";
            }

            return lodge.ExpeditionState switch
            {
                StrategyScoutExpeditionState.Ready => lodge.CanDispatchScout(worker)
                    ? "ready for an expedition"
                    : "finishing another duty",
                StrategyScoutExpeditionState.Returning => "returning to the Lodge",
                StrategyScoutExpeditionState.Exploring => FormatScoutExpeditionProgress(lodge),
                _ => GetResidentStatus(worker)
            };
        }

        private static string FormatScoutExpeditionProgress(StrategyScoutLodge lodge)
        {
            float seconds = Mathf.Max(0f, lodge.RemainingExpeditionSeconds);
            float totalHours = seconds / StrategyDayNightCycleController.DayLengthSeconds * 24f;
            int days = Mathf.FloorToInt(totalHours / 24f);
            int hours = Mathf.CeilToInt(totalHours - days * 24f);
            if (hours >= 24)
            {
                days++;
                hours = 0;
            }

            string time = days > 0
                ? days + "d " + hours + "h"
                : Mathf.Max(1, hours) + "h";
            return "exploring  /  " + time + " left  /  "
                + lodge.RemainingFieldRations.ToString("0.#", CultureInfo.InvariantCulture)
                + " rations";
        }

        private bool HasAppointableScoutCandidate(StrategyScoutLodge lodge)
        {
            if (lodge == null || lodge.WorkerCount >= StrategyScoutLodge.MaxWorkers)
            {
                return false;
            }

            population ??= Object.FindAnyObjectByType<StrategyPopulationController>();
            if (population == null)
            {
                return false;
            }

            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (lodge.CanAppointWorker(resident))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
