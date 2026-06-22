using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {

        private void ToggleHunterWorkerSlot(StrategyHunterCamp camp, int index)
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
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "hunter"));
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
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "hunter"));
            }

            RefreshHud();
        }

        private void ToggleFisherWorkerSlot(StrategyFisherHut hut, int index)
        {
            if (hut == null)
            {
                return;
            }

            if (index < hut.WorkerCount)
            {
                hut.TryGetWorker(index, out StrategyResidentAgent worker);
                hut.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("hutOrigin", hut.Origin),
                    StrategyDebugLogger.F("profession", "fisher"));
            }
            else
            {
                bool assigned = hut.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("hutOrigin", hut.Origin),
                    StrategyDebugLogger.F("profession", "fisher"));
            }

            RefreshHud();
        }

        private void ToggleStonecutterWorkerSlot(StrategyStonecutterCamp camp, int index)
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
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "stonecutter"));
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
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "stonecutter"));
            }

            RefreshHud();
        }

        private void ToggleStorageWorkerSlot(StrategyStorageYard yard, int index)
        {
            if (yard == null)
            {
                return;
            }

            if (index >= StorageWorkerHudSlots)
            {
                int builderIndex = index - StorageWorkerHudSlots;
                if (builderIndex < yard.BuilderCount)
                {
                    yard.TryGetBuilder(builderIndex, out StrategyResidentAgent builder);
                    yard.UnassignBuilderAt(builderIndex);
                    StrategyDebugLogger.Info(
                        "Selection",
                        "WorkerSlotClicked",
                        StrategyDebugLogger.F("action", "unassign"),
                        StrategyDebugLogger.F("slot", index),
                        StrategyDebugLogger.F("worker", builder != null ? builder.FullName : string.Empty),
                        StrategyDebugLogger.F("yardOrigin", yard.Origin),
                        StrategyDebugLogger.F("profession", "builder"));
                }
                else
                {
                    bool assigned = yard.TryAssignNextAvailableBuilder(out StrategyResidentAgent builder);
                    StrategyDebugLogger.Info(
                        "Selection",
                        "WorkerSlotClicked",
                        StrategyDebugLogger.F("action", "assign"),
                        StrategyDebugLogger.F("slot", index),
                        StrategyDebugLogger.F("success", assigned),
                        StrategyDebugLogger.F("worker", builder != null ? builder.FullName : string.Empty),
                        StrategyDebugLogger.F("yardOrigin", yard.Origin),
                        StrategyDebugLogger.F("profession", "builder"));
                }

                RefreshHud();
                return;
            }

            if (index < yard.WorkerCount)
            {
                yard.TryGetWorker(index, out StrategyResidentAgent worker);
                yard.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
            }
            else
            {
                bool assigned = yard.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
            }

            RefreshHud();
        }

        private void TryInstallSelectedUpgrade(StrategyBuildingUpgradeType type)
        {
            StrategyPlacedBuilding building = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyPlacedBuilding>()
                : null;
            if (building == null || building.Tool != StrategyBuildTool.House)
            {
                return;
            }

            if (building.HasUpgrade(type))
            {
                upgradeStatusMessage = "Already installed.";
                RefreshHud();
                return;
            }

            if (upgradeController == null)
            {
                upgradeStatusMessage = "Upgrade system is not ready.";
                RefreshHud();
                return;
            }

            if (upgradeController.TryInstallUpgrade(building, type, out _, out StrategyBuildingUpgradeInstallFailureReason failureReason))
            {
                upgradeStatusMessage = GetUpgradeTitle(type)
                    + " "
                    + "installed near the house.";
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.NotEnoughResources)
            {
                upgradeStatusMessage = "Not enough resources: "
                    + FormatUpgradeCost(StrategyBuildingUpgradeController.GetUpgradeCost(type))
                    + ".";
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.AlreadyInstalled)
            {
                upgradeStatusMessage = "Already installed.";
            }
            else
            {
                upgradeStatusMessage = "No free space near the house.";
            }

            RefreshHud();
        }

        private void RefreshUpgradeActions(StrategyPlacedBuilding building)
        {
            if (building == null || gardenBedsButton == null || chickenCoopButton == null)
            {
                return;
            }

            RefreshUpgradeButton(
                gardenBedsButton,
                gardenBedsButtonText,
                gardenBedsStateText,
                gardenBedsActionText,
                building,
                StrategyBuildingUpgradeType.GardenBeds,
                "Garden Beds");
            RefreshUpgradeButton(
                chickenCoopButton,
                chickenCoopButtonText,
                chickenCoopStateText,
                chickenCoopActionText,
                building,
                StrategyBuildingUpgradeType.ChickenCoop,
                "Chicken Coop");

            if (upgradeStatusText != null)
            {
                upgradeStatusText.text = upgradeStatusMessage;
            }
        }

        private void RefreshUpgradeButton(
            Button button,
            Text titleText,
            Text stateText,
            Text actionText,
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            string title)
        {
            bool installed = building.HasUpgrade(type);
            StrategyConstructionResourceCost cost = StrategyBuildingUpgradeController.GetUpgradeCost(type);
            bool canAfford = cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
            button.interactable = !installed && upgradeController != null && canAfford;
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (stateText != null)
            {
                stateText.text = GetUpgradeStateText(building, type, installed, canAfford, cost);
                stateText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : canAfford
                        ? new Color(0.76f, 0.83f, 0.80f)
                        : new Color(0.95f, 0.58f, 0.45f);
            }

            if (actionText != null)
            {
                actionText.text = installed
                    ? "Done"
                    : canAfford
                        ? "Add"
                        : "No";
                actionText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : canAfford
                        ? Color.white
                        : new Color(0.65f, 0.69f, 0.67f);
            }
        }

        private string GetUpgradeStateText(
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            bool installed,
            bool canAfford,
            StrategyConstructionResourceCost cost)
        {
            if (installed)
            {
                return type == StrategyBuildingUpgradeType.GardenBeds
                    ? "Crop: " + GetGardenCropTitle(building)
                    : "installed";
            }

            return upgradeController != null
                ? canAfford
                    ? "Cost: " + FormatUpgradeCost(cost)
                    : "Missing: " + FormatUpgradeCost(cost)
                : "not ready";
        }

        private static string GetGardenCropTitle(StrategyPlacedBuilding building)
        {
            return building != null
                && building.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden)
                && garden.ProducedResource != StrategyResourceType.None
                    ? GetResourceTitle(garden.ProducedResource)
                    : "None";
        }

        private static string FormatUpgradeCost(StrategyConstructionResourceCost cost)
        {
            return cost.ToDisplayText();
        }

        private void SetUpgradeActionsVisible(bool visible)
        {
            if (upgradeActionsRoot != null)
            {
                upgradeActionsRoot.gameObject.SetActive(visible);
            }

            if (!visible && upgradeStatusText != null)
            {
                upgradeStatusText.text = string.Empty;
            }
        }

        private void RefreshResources(StrategyPlacedBuilding building)
        {
            if (building == null || resourcesRoot == null)
            {
                return;
            }

            RefreshHouseFoodRows(building);
            StrategyHouseResourceStore store = building.Resources;
            int visibleResourceIndex = 0;
            for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length; i++)
            {
                StrategyResourceType type = StrategyHouseResourceStore.DisplayOrder[i];
                int amount = store != null ? store.GetAmount(type) : 0;
                bool isVisible = amount > 0;

                if (resourceSlots[i] != null)
                {
                    resourceSlots[i].gameObject.SetActive(isVisible);
                    if (isVisible)
                    {
                        int column = visibleResourceIndex % 2;
                        int row = visibleResourceIndex / 2;
                        resourceSlots[i].anchoredPosition = new Vector2(column * ResourceCellWidth, -138f - row * 40f);
                        visibleResourceIndex++;
                    }
                }

                if (!isVisible)
                {
                    continue;
                }

                if (resourceIconImages[i] != null)
                {
                    resourceIconImages[i].sprite = StrategyResourceIconFactory.GetSprite(type);
                    resourceIconImages[i].color = Color.white;
                }

                if (resourceAmountTexts[i] != null)
                {
                    float rationValue = amount * StrategyFoodNutrition.GetRationValue(type);
                    if (type == StrategyResourceType.Dish)
                    {
                        resourceAmountTexts[i].text = GetResourceTitle(type)
                            + "\n"
                            + amount
                            + " / "
                            + FormatRations(store.GetPreparedDishRations())
                            + "r "
                            + store.GetPreparedDishSummary(2);
                    }
                    else if (type == StrategyResourceType.Pottery)
                    {
                        resourceAmountTexts[i].text = GetResourceTitle(type) + "\n" + amount;
                    }
                    else
                    {
                        resourceAmountTexts[i].text = GetResourceTitle(type)
                            + "\n"
                            + amount
                            + " / "
                            + FormatRations(rationValue)
                            + "r";
                    }

                    resourceAmountTexts[i].color = new Color(0.88f, 0.93f, 0.90f);
                }
            }

            if (resourcesEmptyText != null)
            {
                resourcesEmptyText.gameObject.SetActive(visibleResourceIndex <= 0);
            }
        }

        private void SetResourcesVisible(bool visible)
        {
            if (resourcesRoot != null)
            {
                resourcesRoot.gameObject.SetActive(visible);
            }
        }
    }
}
