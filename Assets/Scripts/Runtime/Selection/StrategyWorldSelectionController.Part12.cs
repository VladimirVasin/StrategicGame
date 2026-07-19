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
            productionUpgradeRoot = CreateUiObject("ProductionUpgrade", hudContent).GetComponent<RectTransform>();
            SetTopStretch(productionUpgradeRoot, 24f, 356f, 24f, 132f);
            productionUpgradeRoot.gameObject.SetActive(false);

            productionUpgradeBackground = productionUpgradeRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(
                productionUpgradeBackground,
                WithAlpha(StrategyHudStyle.Surface, 0.92f));

            Text title = CreateText("SectionTitle", productionUpgradeRoot, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            title.fontStyle = FontStyle.Bold;
            title.text = "Production Upgrade";
            SetTopStretch(title.rectTransform, 0f, 0f, 0f, 20f);

            RectTransform row = CreateUiObject("UpgradeRow", productionUpgradeRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 0f, 30f, 0f, 62f);
            Image rowBackground = row.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(
                rowBackground,
                WithAlpha(StrategyHudStyle.Elevated, 0.96f));

            RectTransform iconFrame = CreateUiObject("IconFrame", row).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 12f, 13f, 36f, 36f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(
                frame,
                WithAlpha(StrategyHudStyle.Surface, 0.86f));

            productionUpgradeIcon = CreateUiObject("Icon", iconFrame).AddComponent<Image>();
            productionUpgradeIcon.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Tools);
            productionUpgradeIcon.preserveAspect = true;
            productionUpgradeIcon.raycastTarget = false;
            SetOffsets(productionUpgradeIcon.rectTransform, 5f, 5f, 5f, 5f);

            productionUpgradeTitleText = CreateText("Title", row, 13, TextAnchor.UpperLeft, Color.white);
            productionUpgradeTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(productionUpgradeTitleText.rectTransform, 58f, 8f, 86f, 18f);

            productionUpgradeEffectText = CreateText("Effect", row, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
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

            productionUpgradeButton = action.gameObject.AddComponent<Button>();
            StrategyHudStyle.StyleButton(productionUpgradeButton, actionImage, true);
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

            productionUpgradeCostText = CreateText("Cost", productionUpgradeRoot, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            productionUpgradeCostText.fontStyle = FontStyle.Bold;
            SetTopStretch(productionUpgradeCostText.rectTransform, 0f, 98f, 0f, 16f);

            productionUpgradeStatusText = CreateText("Status", productionUpgradeRoot, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            SetTopStretch(productionUpgradeStatusText.rectTransform, 0f, 116f, 0f, 14f);
        }

        private void LayoutProductionUpgradeHud(float top)
        {
            if (productionUpgradeRoot != null)
            {
                SetTopStretch(
                    productionUpgradeRoot,
                    24f,
                    top,
                    24f,
                    ProductionUpgradeHeight);
            }
        }

        private void RefreshProductionUpgradeHud(StrategyPlacedBuilding building)
        {
            if (building == null
                || building.IsDemolishing
                || !StrategyProductionBuildingUpgradeCatalog.TryGetForTool(building.Tool, out StrategyProductionBuildingUpgradeDefinition definition))
            {
                SetProductionUpgradeHudVisible(false);
                return;
            }

            SetProductionUpgradeHudVisible(true);
            bool installed = building.HasProductionUpgrade(definition.Type);
            bool canAfford = StrategyStorageYard.CanAffordProductionUpgrade(definition.Cost);
            Color stateColor = installed
                ? StrategyHudStyle.Success
                : canAfford ? StrategyHudStyle.Secondary : StrategyHudStyle.Warning;
            productionUpgradeBackground.color = Color.Lerp(
                WithAlpha(StrategyHudStyle.Surface, 0.94f),
                WithAlpha(stateColor, 0.94f),
                0.18f);
            productionUpgradeTitleText.text = definition.Title;
            productionUpgradeEffectText.text = definition.EffectText;
            productionUpgradeCostText.text = installed ? "Installed" : "Cost: " + definition.Cost.ToDisplayText();
            productionUpgradeStatusText.text = string.IsNullOrEmpty(upgradeStatusMessage)
                ? GetProductionUpgradeStatus(installed, canAfford)
                : upgradeStatusMessage;
            productionUpgradeActionText.text = installed ? "Done" : canAfford ? "Install" : "No";
            productionUpgradeActionText.color = installed
                ? StrategyHudStyle.Success
                : canAfford ? StrategyHudStyle.TextPrimary : StrategyHudStyle.TextMuted;
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
