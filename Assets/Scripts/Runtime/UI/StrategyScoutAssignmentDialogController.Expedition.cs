using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutAssignmentDialogController
    {
        private const float RationComparisonEpsilon = 0.0001f;

        private StrategyResidentAgent fixedExpeditionResident;
        private Button durationMinusButton;
        private Button durationPlusButton;
        private Text durationDaysText;
        private Text rationSummaryText;
        private Text expeditionEndText;
        private Text durationWarningText;
        private bool expeditionOnlyMode;
        private int selectedExpeditionDays = StrategyScoutExpeditionPolicy.DefaultDays;

        private void BuildExpeditionDurationControls()
        {
            RectTransform panel = CreateUiObject("ExpeditionDurationPanel", board)
                .GetComponent<RectTransform>();
            SetTopStretch(panel, 44f, 579f, 28f, 84f);
            Image background = panel.gameObject.AddComponent<Image>();
            background.sprite = StrategyUiThemeProvider.GetPanelSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.075f, 0.098f, 0.09f, 0.98f);

            Text label = CreateText(
                "DurationLabel",
                panel,
                string.Empty,
                10,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 0.70f, 0.34f, 1f));
            label.fontStyle = FontStyle.Bold;
            SetTopLeft(label.rectTransform, 15f, 7f, 205f, 17f);
            StrategyLocalizedTextBinding.Bind(
                label,
                StrategyLocalizationTables.Residents,
                "resident.dialog.scout.expedition.range");

            durationMinusButton = CreateActionButton(
                "DurationMinusButton",
                panel,
                new Color(0.15f, 0.19f, 0.17f, 1f),
                () => ChangeExpeditionDays(-1),
                out Text minusLabel);
            minusLabel.text = "-";
            SetTopLeft(durationMinusButton.GetComponent<RectTransform>(), 15f, 31f, 35f, 35f);
            StrategyUiButtonFeedback.Attach(
                durationMinusButton,
                StrategyUiButtonFeedbackProfile.Compact,
                null);

            durationDaysText = CreateText(
                "DurationDays",
                panel,
                string.Empty,
                14,
                TextAnchor.MiddleCenter,
                Color.white);
            durationDaysText.fontStyle = FontStyle.Bold;
            SetTopLeft(durationDaysText.rectTransform, 56f, 31f, 113f, 35f);

            durationPlusButton = CreateActionButton(
                "DurationPlusButton",
                panel,
                new Color(0.30f, 0.23f, 0.12f, 1f),
                () => ChangeExpeditionDays(1),
                out Text plusLabel);
            plusLabel.text = "+";
            SetTopLeft(durationPlusButton.GetComponent<RectTransform>(), 175f, 31f, 35f, 35f);
            StrategyUiButtonFeedback.Attach(
                durationPlusButton,
                StrategyUiButtonFeedbackProfile.Compact,
                null);

            rationSummaryText = CreateText(
                "RationSummary",
                panel,
                string.Empty,
                12,
                TextAnchor.MiddleLeft,
                new Color(0.88f, 0.82f, 0.63f, 1f));
            rationSummaryText.fontStyle = FontStyle.Bold;
            SetTopLeft(rationSummaryText.rectTransform, 232f, 7f, 414f, 20f);

            expeditionEndText = CreateText(
                "ExpeditionEnd",
                panel,
                string.Empty,
                11,
                TextAnchor.MiddleLeft,
                new Color(0.70f, 0.80f, 0.75f, 1f));
            SetTopLeft(expeditionEndText.rectTransform, 232f, 29f, 414f, 19f);

            durationWarningText = CreateText(
                "DurationWarning",
                panel,
                string.Empty,
                11,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.62f, 0.42f, 1f));
            durationWarningText.resizeTextForBestFit = true;
            durationWarningText.resizeTextMinSize = 9;
            durationWarningText.resizeTextMaxSize = 11;
            SetTopLeft(durationWarningText.rectTransform, 232f, 50f, 414f, 25f);
        }

        private void PrepareExpeditionRequest(StrategyResidentAgent assignedScout)
        {
            fixedExpeditionResident = assignedScout;
            expeditionOnlyMode = assignedScout != null;
            selectedResident = assignedScout;
            selectedExpeditionDays = StrategyScoutExpeditionPolicy.DefaultDays;
        }

        private void ClearExpeditionRequestState()
        {
            fixedExpeditionResident = null;
            expeditionOnlyMode = false;
            selectedExpeditionDays = StrategyScoutExpeditionPolicy.DefaultDays;
        }

        private void ChangeExpeditionDays(int direction)
        {
            int previous = selectedExpeditionDays;
            selectedExpeditionDays = StrategyScoutExpeditionPolicy.ClampDurationDays(
                selectedExpeditionDays + direction);
            if (selectedExpeditionDays != previous)
            {
                SetActionStatus(string.Empty, false);
                PlaySfx(StrategyHudSfxKind.Step);
            }

            UpdateActions();
        }

        private void RefreshExpeditionControls()
        {
            selectedExpeditionDays = StrategyScoutExpeditionPolicy.ClampDurationDays(
                selectedExpeditionDays);
            float required = StrategyScoutExpeditionPolicy.GetRequiredRations(
                selectedExpeditionDays);
            float available = GetAvailableExpeditionRations();
            bool affordable = available + RationComparisonEpsilon >= required;

            SetLocalizedText(
                durationDaysText,
                GetDayFormKey(
                    "resident.dialog.scout.expedition.duration",
                    selectedExpeditionDays),
                selectedExpeditionDays);
            SetLocalizedText(
                rationSummaryText,
                "resident.dialog.scout.expedition.ration_summary",
                FormatRations(required),
                FormatAvailableRations(available));
            rationSummaryText.color = affordable
                ? new Color(0.88f, 0.82f, 0.63f, 1f)
                : new Color(1f, 0.62f, 0.42f, 1f);

            StrategyCalendarSnapshot now = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            SetLocalizedText(
                expeditionEndText,
                "resident.dialog.scout.expedition.ends",
                now.DisplayDay + selectedExpeditionDays,
                now.ClockText);

            string staleReasonKey = GetSelectionStaleReasonKey();
            if (!string.IsNullOrEmpty(staleReasonKey))
            {
                SetLocalizedText(durationWarningText, staleReasonKey);
            }
            else if (!affordable)
            {
                SetLocalizedText(
                    durationWarningText,
                    "resident.dialog.scout.expedition.not_enough_provisions",
                    FormatRations(required - available));
            }
            else
            {
                SetLocalizedText(
                    durationWarningText,
                    "resident.dialog.scout.expedition.provisions_packed");
                durationWarningText.color = new Color(0.66f, 0.78f, 0.70f, 1f);
            }

            if (!affordable || !string.IsNullOrEmpty(staleReasonKey))
            {
                durationWarningText.color = new Color(1f, 0.62f, 0.42f, 1f);
            }

            durationMinusButton.interactable = selectedExpeditionDays
                > StrategyScoutExpeditionPolicy.MinimumDays;
            durationPlusButton.interactable = selectedExpeditionDays
                < StrategyScoutExpeditionPolicy.MaximumDays;
        }

        private bool IsResidentEligibleForCurrentRequest(StrategyResidentAgent resident)
        {
            if (resident == null || lodge == null)
            {
                return false;
            }

            if (!expeditionOnlyMode)
            {
                return lodge.CanAppointWorker(resident);
            }

            return resident == fixedExpeditionResident
                && lodge.CanDispatchScout(resident);
        }

        private bool HasSufficientExpeditionRations()
        {
            float required = StrategyScoutExpeditionPolicy.GetRequiredRations(
                selectedExpeditionDays);
            return GetAvailableExpeditionRations() + RationComparisonEpsilon >= required;
        }

        private float GetAvailableExpeditionRations()
        {
            return lodge != null ? Mathf.Max(0f, lodge.GetAvailableExpeditionRations()) : 0f;
        }

        private string GetSelectionStaleReasonKey()
        {
            if (selectedResident == null)
            {
                return string.Empty;
            }

            if (IsResidentEligibleForCurrentRequest(selectedResident))
            {
                return string.Empty;
            }

            return expeditionOnlyMode
                ? "resident.dialog.scout.status.scout_lodge_not_ready"
                : "resident.dialog.scout.status.selected_unavailable_for_trail";
        }

        private static string FormatRations(float value)
        {
            return Mathf.Max(0f, value).ToString("0.#", GetNumberCulture());
        }

        private static string FormatAvailableRations(float value)
        {
            return Mathf.Max(0f, value).ToString("0.0", GetNumberCulture());
        }

        private static CultureInfo GetNumberCulture()
        {
            return StrategyLocalization.CurrentLanguage == StrategyGameLanguage.Russian
                ? CultureInfo.GetCultureInfo("ru-RU")
                : CultureInfo.GetCultureInfo("en-US");
        }
    }
}
