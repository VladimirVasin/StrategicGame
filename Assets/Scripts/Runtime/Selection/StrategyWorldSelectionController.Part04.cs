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
                upgradeStatusMessage = L("upgrade.already_installed");
                RefreshHud();
                return;
            }

            if (upgradeController == null)
            {
                upgradeStatusMessage = L("upgrade.system_not_ready");
                RefreshHud();
                return;
            }

            if (upgradeController.TryInstallUpgrade(building, type, out _, out StrategyBuildingUpgradeInstallFailureReason failureReason))
            {
                upgradeStatusMessage = L("upgrade.installed_near_house", GetUpgradeTitle(type));
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.NotEnoughResources)
            {
                upgradeStatusMessage = L("upgrade.not_enough_resources",
                    FormatUpgradeCost(StrategyBuildingUpgradeController.GetUpgradeCost(type)));
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.AlreadyInstalled)
            {
                upgradeStatusMessage = L("upgrade.already_installed");
            }
            else
            {
                upgradeStatusMessage = L("upgrade.no_free_space");
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
                GetUpgradeTitle(StrategyBuildingUpgradeType.GardenBeds));
            RefreshUpgradeButton(
                chickenCoopButton,
                chickenCoopButtonText,
                chickenCoopStateText,
                chickenCoopActionText,
                building,
                StrategyBuildingUpgradeType.ChickenCoop,
                GetUpgradeTitle(StrategyBuildingUpgradeType.ChickenCoop));

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
                    ? L("action.done")
                    : canAfford
                        ? L("action.add")
                        : L("action.no");
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
                    ? L("upgrade.crop", GetGardenCropTitle(building))
                    : LocalizedValue("installed");
            }

            return upgradeController != null
                ? canAfford
                    ? L("upgrade.cost", FormatUpgradeCost(cost))
                    : L("upgrade.missing", FormatUpgradeCost(cost))
                : LocalizedValue("not ready");
        }

        private static string GetGardenCropTitle(StrategyPlacedBuilding building)
        {
            return building != null
                && building.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden)
                && garden.ProducedResource != StrategyResourceType.None
                    ? GetResourceTitle(garden.ProducedResource)
                    : StrategySelectionLocalization.Resource(StrategyResourceType.None);
        }

        private static string FormatUpgradeCost(StrategyConstructionResourceCost cost)
        {
            return StrategySelectionLocalization.ConstructionCost(cost);
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
            if (store != null)
            {
                List<StrategyPreparedDishStack> preparedDishes = new();
                store.CopyPreparedDishStacks(preparedDishes);
                for (int i = 0; i < preparedDishes.Count && visibleResourceIndex < resourceSlots.Length; i++)
                {
                    StrategyPreparedDishStack stack = preparedDishes[i];
                    ShowDinnerFoodRow(
                        visibleResourceIndex++,
                        StrategyResourceType.Dish,
                        stack.Recipe != null ? stack.Recipe.DisplayName : GetResourceTitle(StrategyResourceType.Dish),
                        stack.Amount,
                        stack.Rations);
                }

                if (store.LeftoverRations > 0.01f && visibleResourceIndex < resourceSlots.Length)
                {
                    ShowDinnerFoodRow(
                        visibleResourceIndex++,
                        StrategyResourceType.Dish,
                        L("house.food.leftovers"),
                        0,
                        store.LeftoverRations);
                }

                for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length && visibleResourceIndex < resourceSlots.Length; i++)
                {
                    StrategyResourceType type = StrategyHouseResourceStore.DisplayOrder[i];
                    if (!StrategyFoodNutrition.IsIngredientFood(type))
                    {
                        continue;
                    }

                    int amount = store.GetAmount(type);
                    if (amount <= 0)
                    {
                        continue;
                    }

                    ShowDinnerFoodRow(
                        visibleResourceIndex++,
                        type,
                        GetResourceTitle(type),
                        amount,
                        amount * StrategyFoodNutrition.GetRationValue(type));
                }

                int logs = store.GetLogsAmount();
                if (logs > 0 && visibleResourceIndex < resourceSlots.Length)
                {
                    ShowHouseStoreRow(
                        visibleResourceIndex++,
                        StrategyResourceType.Logs,
                        GetResourceTitle(StrategyResourceType.Logs),
                        logs,
                        L("house.food.fuel"));
                }
            }

            for (int i = visibleResourceIndex; i < resourceSlots.Length; i++)
            {
                if (resourceSlots[i] != null)
                {
                    resourceSlots[i].gameObject.SetActive(false);
                }
            }

            if (resourcesEmptyText != null)
            {
                resourcesEmptyText.gameObject.SetActive(visibleResourceIndex <= 0);
            }
        }

        private void ShowDinnerFoodRow(
            int rowIndex,
            StrategyResourceType iconType,
            string displayName,
            int amount,
            float rationValue)
        {
            if (rowIndex < 0 || rowIndex >= resourceSlots.Length || resourceSlots[rowIndex] == null)
            {
                return;
            }

            resourceSlots[rowIndex].gameObject.SetActive(true);
            resourceSlots[rowIndex].anchoredPosition = new Vector2(0f, -142f - rowIndex * 30f);

            if (resourceIconImages[rowIndex] != null)
            {
                resourceIconImages[rowIndex].sprite = StrategyResourceIconFactory.GetSprite(iconType);
                resourceIconImages[rowIndex].color = Color.white;
            }

            if (resourceAmountTexts[rowIndex] != null)
            {
                resourceAmountTexts[rowIndex].text = displayName;
                resourceAmountTexts[rowIndex].color = new Color(0.88f, 0.93f, 0.90f);
            }

            if (resourceQuantityTexts[rowIndex] != null)
            {
                resourceQuantityTexts[rowIndex].text = amount > 0 ? amount.ToString() : "--";
                resourceQuantityTexts[rowIndex].color = new Color(0.88f, 0.93f, 0.90f);
            }

            if (resourceNutritionTexts[rowIndex] != null)
            {
                resourceNutritionTexts[rowIndex].text = StrategySelectionLocalization.Rations(rationValue);
                resourceNutritionTexts[rowIndex].color = new Color(0.88f, 0.93f, 0.90f);
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
