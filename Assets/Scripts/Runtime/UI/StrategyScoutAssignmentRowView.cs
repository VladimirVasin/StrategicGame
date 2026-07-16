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
                name.text = "Unknown resident";
                detail.text = string.Empty;
                status.text = "Unavailable";
                return;
            }

            portrait.sprite = StrategyResidentSpriteFactory.GetPortraitSprite(
                resident.Gender,
                resident.VisualVariant,
                resident.LifeStage);
            portrait.color = eligible ? Color.white : new Color(0.52f, 0.55f, 0.52f, 0.88f);
            name.text = resident.FullName;
            name.color = eligible ? Color.white : new Color(0.62f, 0.66f, 0.63f, 1f);
            detail.text = resident.DisplayAgeYears + " years  /  " + StrategyResidentHudText.GetRoleTitle(resident);
            detail.color = eligible
                ? new Color(0.70f, 0.78f, 0.74f, 1f)
                : new Color(0.50f, 0.55f, 0.52f, 1f);
            status.text = eligible
                ? selected ? "SELECTED" : "READY FOR THE TRAIL"
                : string.IsNullOrWhiteSpace(blockReason) ? "Unavailable for scouting duty" : blockReason;
            status.color = eligible
                ? new Color(0.92f, 0.70f, 0.34f, 1f)
                : new Color(0.72f, 0.50f, 0.42f, 1f);
        }
    }
}
