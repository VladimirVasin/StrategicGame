using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private void ShowHouseStoreRow(
            int rowIndex,
            StrategyResourceType iconType,
            string displayName,
            int amount,
            string valueText)
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
                resourceQuantityTexts[rowIndex].text = amount.ToString();
                resourceQuantityTexts[rowIndex].color = new Color(0.88f, 0.93f, 0.90f);
            }

            if (resourceNutritionTexts[rowIndex] != null)
            {
                resourceNutritionTexts[rowIndex].text = valueText;
                resourceNutritionTexts[rowIndex].color = new Color(0.88f, 0.93f, 0.90f);
            }
        }
    }
}
