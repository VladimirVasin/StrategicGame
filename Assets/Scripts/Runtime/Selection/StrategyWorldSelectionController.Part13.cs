using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const int TradeOfferSlotCount = 8;

        private RectTransform tradingPostHudRoot;
        private Text tradeOffersTitleText;
        private Text tradeEmptyText;
        private readonly RectTransform[] tradeOfferRows = new RectTransform[TradeOfferSlotCount];
        private readonly Image[] tradeOfferIcons = new Image[TradeOfferSlotCount];
        private readonly Text[] tradeOfferTitleTexts = new Text[TradeOfferSlotCount];
        private readonly Text[] tradeOfferDetailTexts = new Text[TradeOfferSlotCount];
        private readonly Button[] tradeOfferButtons = new Button[TradeOfferSlotCount];
        private readonly Text[] tradeOfferButtonTexts = new Text[TradeOfferSlotCount];
        private readonly int[] tradeOfferSourceIndices = new int[TradeOfferSlotCount];

        private void CreateTradingPostHud()
        {
            tradingPostHudRoot = CreateUiObject("TradingPostHud", hudContent).GetComponent<RectTransform>();
            SetTopStretch(tradingPostHudRoot, 18f, 128f, 18f, 508f);
            tradingPostHudRoot.gameObject.SetActive(false);

            tradeOffersTitleText = CreateText(
                "OffersTitle",
                tradingPostHudRoot,
                13,
                TextAnchor.UpperLeft,
                StrategyHudStyle.Primary);
            tradeOffersTitleText.fontStyle = FontStyle.Bold;
            tradeOffersTitleText.text = L("label.active_offers");
            SetTopStretch(tradeOffersTitleText.rectTransform, 0f, 0f, 0f, 20f);

            tradeEmptyText = CreateText("Empty", tradingPostHudRoot, 12, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            tradeEmptyText.text = L("trade.no_caravan_here");
            SetTopStretch(tradeEmptyText.rectTransform, 0f, 28f, 0f, 24f);

            for (int i = 0; i < TradeOfferSlotCount; i++)
            {
                CreateTradeOfferRow(i, 28f + i * 46f);
            }
        }

        private float RefreshTradingPostHud(StrategyTradingPost post, float top = 128f)
        {
            SetProfileSectionVisible(false);
            SetStatusSectionVisible(false);
            SetContextSectionVisible(false);
            SetResidentsSectionVisible(false);
            SetWorkersSectionVisible(false);
            SetResourcesVisible(false);
            SetUpgradeActionsVisible(false);
            SetProductionUpgradeHudVisible(false);
            SetTradingPostHudVisible(true);

            if (tradeOffersTitleText != null)
            {
                tradeOffersTitleText.text = L("label.active_offers");
            }

            if (tradeEmptyText != null)
            {
                tradeEmptyText.text = L("trade.no_caravan_here");
            }

            StrategyTradeCaravanController controller = StrategyTradeCaravanController.Active;
            IReadOnlyList<StrategyTradeOffer> offers = controller != null ? controller.CurrentOffers : null;
            bool isTrading = controller != null && controller.IsTradingAt(post);
            int count = offers != null ? offers.Count : 0;
            int visibleCount = 0;
            for (int sourceIndex = 0; sourceIndex < count && visibleCount < TradeOfferSlotCount; sourceIndex++)
            {
                if (!offers[sourceIndex].IsValid)
                {
                    continue;
                }

                tradeOfferSourceIndices[visibleCount] = sourceIndex;
                tradeOfferRows[visibleCount].gameObject.SetActive(true);
                RefreshTradeOfferRow(
                    visibleCount,
                    offers[sourceIndex],
                    post,
                    isTrading);
                visibleCount++;
            }

            tradeEmptyText.gameObject.SetActive(visibleCount <= 0);
            for (int i = 0; i < TradeOfferSlotCount; i++)
            {
                if (i >= visibleCount)
                {
                    tradeOfferRows[i].gameObject.SetActive(false);
                }
            }

            float height = Mathf.Max(56f, 28f + visibleCount * 46f);
            SetTopStretch(tradingPostHudRoot, 18f, top, 18f, height);
            return top + height;
        }

        private void SetTradingPostHudVisible(bool visible)
        {
            if (tradingPostHudRoot != null)
            {
                tradingPostHudRoot.gameObject.SetActive(visible);
            }
        }

        private void CreateTradeOfferRow(int index, float top)
        {
            RectTransform row = CreateUiObject("Offer_" + index, tradingPostHudRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 0f, top, 0f, 40f);
            tradeOfferRows[index] = row;

            Image background = row.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(
                background,
                WithAlpha(StrategyHudStyle.Surface, 0.90f));

            RectTransform iconFrame = CreateUiObject("IconFrame", row).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 8f, 7f, 26f, 26f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(
                frame,
                WithAlpha(StrategyHudStyle.Elevated, 0.90f));

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 4f, 4f, 4f, 4f);
            tradeOfferIcons[index] = iconRect.gameObject.AddComponent<Image>();
            tradeOfferIcons[index].preserveAspect = true;
            tradeOfferIcons[index].raycastTarget = false;

            tradeOfferTitleTexts[index] = CreateText("Title", row, 12, TextAnchor.UpperLeft, Color.white);
            tradeOfferTitleTexts[index].fontStyle = FontStyle.Bold;
            SetTopStretch(tradeOfferTitleTexts[index].rectTransform, 42f, 6f, 72f, 17f);

            tradeOfferDetailTexts[index] = CreateText("Detail", row, 10, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            SetTopStretch(tradeOfferDetailTexts[index].rectTransform, 42f, 23f, 72f, 13f);

            RectTransform action = CreateUiObject("Action", row).GetComponent<RectTransform>();
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(62f, 26f);
            action.anchoredPosition = new Vector2(-7f, 0f);
            Image actionImage = action.gameObject.AddComponent<Image>();

            Button button = action.gameObject.AddComponent<Button>();
            StrategyHudStyle.StyleButton(button, actionImage, true);
            int slot = index;
            button.onClick.AddListener(() => TryExecuteSelectedTradeOffer(slot));
            StrategyUiButtonFeedback.Attach(
                button,
                StrategyUiButtonFeedbackProfile.Compact,
                StrategyHudSfxKind.Click);
            tradeOfferButtons[index] = button;

            tradeOfferButtonTexts[index] = CreateText("Text", action, 10, TextAnchor.MiddleCenter, Color.white);
            tradeOfferButtonTexts[index].fontStyle = FontStyle.Bold;
            SetOffsets(tradeOfferButtonTexts[index].rectTransform, 4f, 0f, 4f, 0f);
            row.gameObject.SetActive(false);
        }

        private void RefreshTradeOfferRow(
            int index,
            StrategyTradeOffer offer,
            StrategyTradingPost post,
            bool isTrading)
        {
            string verb = offer.Direction == StrategyTradeDirection.PlayerSells
                ? L("action.sell")
                : L("action.buy");
            string sign = offer.Direction == StrategyTradeDirection.PlayerSells ? "+" : "-";
            int available = StrategyTradeTransactionService.GetAvailableStock(offer.Resource);
            Vector3 nearWorld = post != null ? post.FootprintBounds.center : Vector3.zero;
            bool canExecute = isTrading && StrategyTradeTransactionService.CanExecute(offer, nearWorld);

            tradeOfferIcons[index].sprite = StrategyResourceIconFactory.GetSprite(offer.Resource);
            tradeOfferIcons[index].color = canExecute ? Color.white : WithAlpha(Color.white, 0.42f);
            tradeOfferTitleTexts[index].text = L(
                "trade.offer_title",
                verb,
                offer.Amount,
                GetResourceTitle(offer.Resource));
            tradeOfferDetailTexts[index].text = L(
                "trade.offer_detail",
                sign + offer.TotalCoins,
                available);
            tradeOfferButtons[index].interactable = canExecute;
            tradeOfferButtonTexts[index].text = verb;
        }

        private void TryExecuteSelectedTradeOffer(int index)
        {
            if (selectedTransform == null)
            {
                return;
            }

            StrategyTradingPost post = selectedTransform.GetComponent<StrategyTradingPost>();
            StrategyTradeCaravanController controller = StrategyTradeCaravanController.Active;
            if (post == null || controller == null)
            {
                return;
            }

            int sourceIndex = index >= 0 && index < tradeOfferSourceIndices.Length
                ? tradeOfferSourceIndices[index]
                : index;
            controller.TryExecuteOffer(post, sourceIndex, out _);
            RefreshHud();
        }
    }
}
