using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyScoutAssignmentRowView
    {
        internal const float RowHeight = 70f;

        private readonly Image background;
        private readonly Button button;
        private readonly Image portrait;
        private readonly Text name;
        private readonly Text detail;
        private readonly Text status;
        private readonly GameObject selectionMark;

        internal StrategyScoutAssignmentRowView(
            RectTransform root,
            Image rowBackground,
            Button rowButton,
            Image portraitImage,
            Text nameText,
            Text detailText,
            Text statusText,
            GameObject selectedMark)
        {
            Root = root;
            background = rowBackground;
            button = rowButton;
            portrait = portraitImage;
            name = nameText;
            detail = detailText;
            status = statusText;
            selectionMark = selectedMark;
        }

        internal RectTransform Root { get; }

        internal void Set(
            StrategyResidentAgent resident,
            bool eligible,
            string blockReason,
            bool selected,
            int index,
            Action<StrategyResidentAgent> onSelected)
        {
            button.onClick.RemoveAllListeners();
            if (resident != null)
            {
                button.onClick.AddListener(() => onSelected?.Invoke(resident));
            }

            button.interactable = eligible;
            selectionMark.SetActive(selected);
            background.color = selected
                ? new Color(0.24f, 0.21f, 0.12f, 0.98f)
                : eligible
                    ? index % 2 == 0
                        ? new Color(0.085f, 0.115f, 0.105f, 0.96f)
                        : new Color(0.105f, 0.135f, 0.12f, 0.96f)
                    : new Color(0.065f, 0.075f, 0.072f, 0.82f);

            if (resident == null)
            {
                portrait.sprite = null;
                name.text = ResidentText("resident.scout_assignment.unknown_resident");
                detail.text = string.Empty;
                status.text = ResidentText("resident.scout_assignment.unavailable");
                return;
            }

            portrait.sprite = StrategyResidentSpriteFactory.GetPortraitSprite(
                resident.Gender,
                resident.VisualVariant,
                resident.LifeStage);
            portrait.color = eligible ? Color.white : new Color(0.52f, 0.55f, 0.52f, 0.88f);
            name.text = resident.FullName;
            name.color = eligible ? Color.white : new Color(0.62f, 0.66f, 0.63f, 1f);
            string roleTitle = StrategyResidentHudText.GetRoleTitle(resident);
            detail.text = ResidentText(
                "resident.scout_assignment.age_role",
                resident.DisplayAgeYears,
                roleTitle);
            detail.color = eligible
                ? new Color(0.70f, 0.78f, 0.74f, 1f)
                : new Color(0.50f, 0.55f, 0.52f, 1f);
            status.text = eligible
                ? selected
                    ? ResidentText("resident.scout_assignment.selected")
                    : resident.HasExternalWorkplace
                        ? ResidentText(
                            "resident.scout_assignment.reassign_from",
                            roleTitle.ToUpperInvariant())
                        : ResidentText("resident.scout_assignment.ready_for_trail")
                : string.IsNullOrWhiteSpace(blockReason)
                    ? ResidentText("resident.scout_assignment.unavailable_for_duty")
                    : LocalizeBlockReason(blockReason);
            status.color = eligible
                ? new Color(0.92f, 0.70f, 0.34f, 1f)
                : new Color(0.72f, 0.50f, 0.42f, 1f);
        }

        private static string LocalizeBlockReason(string reason)
        {
            if (reason.StartsWith("Currently ", StringComparison.Ordinal))
            {
                return ResidentText(
                    "resident.scout_assignment.block.currently",
                    reason.Substring("Currently ".Length));
            }

            string key = reason switch
            {
                "Resident unavailable" => "resident.scout_assignment.block.resident_unavailable",
                "Already assigned to this Lodge" => "resident.scout_assignment.block.already_assigned",
                "Scout slot already filled" => "resident.scout_assignment.block.slot_filled",
                "Only adults can become Scouts" => "resident.scout_assignment.block.adults_only",
                "Has not joined the settlement" => "resident.scout_assignment.block.not_joined",
                "Assigned to construction" => "resident.scout_assignment.block.construction",
                "Responsible for a household" => "resident.scout_assignment.block.householder",
                _ => string.Empty
            };
            return string.IsNullOrEmpty(key)
                ? StrategyLocalization.TranslateLiteral(reason)
                : ResidentText(key);
        }

        private static string ResidentText(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Residents,
                key,
                arguments);
        }
    }
}
