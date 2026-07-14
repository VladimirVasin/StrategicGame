using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private RectTransform productionUpgradeRoot;
        private Image productionUpgradeBackground;
        private Image productionUpgradeIcon;
        private Text productionUpgradeTitleText;
        private Text productionUpgradeEffectText;
        private Text productionUpgradeCostText;
        private Text productionUpgradeActionText;
        private Text productionUpgradeStatusText;
        private Button productionUpgradeButton;

        private void CreateProductionUpgradeHud()
        {
            productionUpgradeRoot = CreateUiObject("ProductionUpgrade", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(productionUpgradeRoot, 24f, 356f, 24f, 132f);
            productionUpgradeRoot.gameObject.SetActive(false);

            productionUpgradeBackground = productionUpgradeRoot.gameObject.AddComponent<Image>();
            productionUpgradeBackground.color = new Color(0.07f, 0.11f, 0.10f, 0.92f);
            productionUpgradeBackground.raycastTarget = false;

            Text title = CreateText("SectionTitle", productionUpgradeRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            title.fontStyle = FontStyle.Bold;
            title.text = "Production Upgrade";
            SetTopStretch(title.rectTransform, 0f, 0f, 0f, 20f);

            RectTransform row = CreateUiObject("UpgradeRow", productionUpgradeRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 0f, 30f, 0f, 62f);
            Image rowBackground = row.gameObject.AddComponent<Image>();
            rowBackground.color = new Color(0.08f, 0.13f, 0.12f, 0.98f);

            RectTransform iconFrame = CreateUiObject("IconFrame", row).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 12f, 13f, 36f, 36f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.07f);
            frame.raycastTarget = false;

            productionUpgradeIcon = CreateUiObject("Icon", iconFrame).AddComponent<Image>();
            productionUpgradeIcon.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Tools);
            productionUpgradeIcon.preserveAspect = true;
            productionUpgradeIcon.raycastTarget = false;
            SetOffsets(productionUpgradeIcon.rectTransform, 5f, 5f, 5f, 5f);

            productionUpgradeTitleText = CreateText("Title", row, 13, TextAnchor.UpperLeft, Color.white);
            productionUpgradeTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(productionUpgradeTitleText.rectTransform, 58f, 8f, 86f, 18f);

            productionUpgradeEffectText = CreateText("Effect", row, 11, TextAnchor.UpperLeft, new Color(0.76f, 0.84f, 0.80f));
            productionUpgradeEffectText.resizeTextForBestFit = true;
            productionUpgradeEffectText.resizeTextMinSize = 8;
            productionUpgradeEffectText.resizeTextMaxSize = 11;
            SetTopStretch(productionUpgradeEffectText.rectTransform, 58f, 29f, 86f, 24f);

            RectTransform action = CreateUiObject("Action", row).GetComponent<RectTransform>();
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(76f, 30f);
            action.anchoredPosition = new Vector2(-8f, 0f);
            Image actionImage = action.gameObject.AddComponent<Image>();
            actionImage.color = new Color(0.19f, 0.31f, 0.29f, 0.96f);

            productionUpgradeButton = action.gameObject.AddComponent<Button>();
            productionUpgradeButton.targetGraphic = actionImage;
            ColorBlock colors = productionUpgradeButton.colors;
            colors.normalColor = new Color(0.19f, 0.31f, 0.29f, 0.96f);
            colors.highlightedColor = new Color(0.25f, 0.40f, 0.35f, 1f);
            colors.pressedColor = new Color(0.12f, 0.20f, 0.18f, 1f);
            colors.disabledColor = new Color(0.10f, 0.13f, 0.12f, 0.88f);
            productionUpgradeButton.colors = colors;
            productionUpgradeButton.onClick.AddListener(TryInstallSelectedProductionUpgrade);
            StrategyUiButtonFeedback.Attach(
                productionUpgradeButton,
                StrategyUiButtonFeedbackProfile.Compact,
                StrategyHudSfxKind.Click);

            productionUpgradeActionText = CreateText("ActionText", action, 11, TextAnchor.MiddleCenter, Color.white);
            productionUpgradeActionText.fontStyle = FontStyle.Bold;
            productionUpgradeActionText.resizeTextForBestFit = true;
            productionUpgradeActionText.resizeTextMinSize = 8;
            productionUpgradeActionText.resizeTextMaxSize = 11;
            SetOffsets(productionUpgradeActionText.rectTransform, 4f, 0f, 4f, 0f);

            productionUpgradeCostText = CreateText("Cost", productionUpgradeRoot, 11, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 0.84f));
            productionUpgradeCostText.fontStyle = FontStyle.Bold;
            SetTopStretch(productionUpgradeCostText.rectTransform, 0f, 98f, 0f, 16f);

            productionUpgradeStatusText = CreateText("Status", productionUpgradeRoot, 11, TextAnchor.UpperLeft, new Color(0.74f, 0.82f, 0.78f));
            SetTopStretch(productionUpgradeStatusText.rectTransform, 0f, 116f, 0f, 14f);
        }

        private void RefreshProductionUpgradeHud(StrategyPlacedBuilding building)
        {
            if (building == null
                || !StrategyProductionBuildingUpgradeCatalog.TryGetForTool(building.Tool, out StrategyProductionBuildingUpgradeDefinition definition))
            {
                SetProductionUpgradeHudVisible(false);
                return;
            }

            SetProductionUpgradeHudVisible(true);
            bool installed = building.HasProductionUpgrade(definition.Type);
            bool canAfford = StrategyStorageYard.CanAffordProductionUpgrade(definition.Cost);
            productionUpgradeBackground.color = installed
                ? new Color(0.10f, 0.18f, 0.14f, 0.94f)
                : canAfford
                    ? new Color(0.07f, 0.11f, 0.10f, 0.92f)
                    : new Color(0.20f, 0.13f, 0.09f, 0.92f);
            productionUpgradeTitleText.text = definition.Title;
            productionUpgradeEffectText.text = definition.EffectText;
            productionUpgradeCostText.text = installed ? "Installed" : "Cost: " + definition.Cost.ToDisplayText();
            productionUpgradeStatusText.text = string.IsNullOrEmpty(upgradeStatusMessage)
                ? GetProductionUpgradeStatus(installed, canAfford)
                : upgradeStatusMessage;
            productionUpgradeActionText.text = installed ? "Done" : canAfford ? "Install" : "No";
            productionUpgradeActionText.color = installed
                ? new Color(0.70f, 0.88f, 0.74f)
                : canAfford ? Color.white : new Color(0.65f, 0.69f, 0.67f);
            productionUpgradeButton.interactable = !installed && canAfford;
        }

        private void TryInstallSelectedProductionUpgrade()
        {
            StrategyPlacedBuilding building = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyPlacedBuilding>()
                : null;
            if (building == null)
            {
                return;
            }

            if (StrategyProductionBuildingUpgradeCatalog.TryInstall(building, out StrategyProductionUpgradeInstallFailureReason failureReason)
                && StrategyProductionBuildingUpgradeCatalog.TryGetForTool(building.Tool, out StrategyProductionBuildingUpgradeDefinition definition))
            {
                upgradeStatusMessage = definition.Title + " installed.";
            }
            else
            {
                upgradeStatusMessage = GetProductionUpgradeFailureText(failureReason);
            }

            RefreshHud();
        }

        private void SetProductionUpgradeHudVisible(bool visible)
        {
            if (productionUpgradeRoot != null)
            {
                productionUpgradeRoot.gameObject.SetActive(visible);
            }
        }

        private static string GetProductionUpgradeStatus(bool installed, bool canAfford)
        {
            if (installed)
            {
                return "Workers already use this upgrade.";
            }

            return canAfford
                ? "Storage stock is ready."
                : "Missing storage resources.";
        }

        private static string GetProductionUpgradeFailureText(StrategyProductionUpgradeInstallFailureReason reason)
        {
            return reason switch
            {
                StrategyProductionUpgradeInstallFailureReason.AlreadyInstalled => "Already installed.",
                StrategyProductionUpgradeInstallFailureReason.NotEnoughResources => "Not enough storage resources.",
                StrategyProductionUpgradeInstallFailureReason.InvalidTarget => "This building cannot use that upgrade.",
                _ => "Upgrade failed."
            };
        }
    }
}
