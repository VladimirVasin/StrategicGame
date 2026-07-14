using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const int TradeOfferSlotCount = 8;

        private RectTransform tradingPostHudRoot;
        private Text tradeCoinsText;
        private Text tradeStatusText;
        private Text tradeEmptyText;
        private readonly RectTransform[] tradeOfferRows = new RectTransform[TradeOfferSlotCount];
        private readonly Image[] tradeOfferIcons = new Image[TradeOfferSlotCount];
        private readonly Text[] tradeOfferTitleTexts = new Text[TradeOfferSlotCount];
        private readonly Text[] tradeOfferDetailTexts = new Text[TradeOfferSlotCount];
        private readonly Button[] tradeOfferButtons = new Button[TradeOfferSlotCount];
        private readonly Text[] tradeOfferButtonTexts = new Text[TradeOfferSlotCount];

        private void CreateTradingPostHud()
        {
            tradingPostHudRoot = CreateUiObject("TradingPostHud", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(tradingPostHudRoot, 18f, 128f, 18f, 508f);
            tradingPostHudRoot.gameObject.SetActive(false);

            RectTransform status = CreateUiObject("Status", tradingPostHudRoot).GetComponent<RectTransform>();
            SetTopStretch(status, 0f, 0f, 0f, 78f);
            Image statusImage = status.gameObject.AddComponent<Image>();
            statusImage.color = new Color(0.10f, 0.15f, 0.14f, 0.94f);
            statusImage.raycastTarget = false;

            Text title = CreateText("Title", status, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            title.fontStyle = FontStyle.Bold;
            title.text = "Trade";
            SetTopStretch(title.rectTransform, 12f, 9f, 12f, 18f);

            tradeCoinsText = CreateText("Coins", status, 13, TextAnchor.UpperRight, new Color(0.95f, 0.78f, 0.40f));
            tradeCoinsText.fontStyle = FontStyle.Bold;
            SetTopStretch(tradeCoinsText.rectTransform, 130f, 9f, 12f, 18f);

            tradeStatusText = CreateText("StatusText", status, 11, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.82f));
            tradeStatusText.lineSpacing = 1.03f;
            SetTopStretch(tradeStatusText.rectTransform, 12f, 31f, 12f, 40f);

            Text offersTitle = CreateText("OffersTitle", tradingPostHudRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            offersTitle.fontStyle = FontStyle.Bold;
            offersTitle.text = "Offers";
            SetTopStretch(offersTitle.rectTransform, 0f, 92f, 0f, 20f);

            tradeEmptyText = CreateText("Empty", tradingPostHudRoot, 12, TextAnchor.UpperLeft, new Color(0.70f, 0.78f, 0.75f));
            tradeEmptyText.text = "No caravan is trading here.";
            SetTopStretch(tradeEmptyText.rectTransform, 0f, 120f, 0f, 24f);

            for (int i = 0; i < TradeOfferSlotCount; i++)
            {
                CreateTradeOfferRow(i, 120f + i * 46f);
            }
        }

        private void RefreshTradingPostHud(StrategyTradingPost post)
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

            StrategyTradeCaravanController controller = StrategyTradeCaravanController.Active;
            int coins = StrategySettlementTreasury.Active != null ? StrategySettlementTreasury.Active.Coins : 0;
            tradeCoinsText.text = "Coins " + coins;
            tradeStatusText.text = post != null ? post.GetHudStatusText() : "Trading post unavailable.";

            IReadOnlyList<StrategyTradeOffer> offers = controller != null ? controller.CurrentOffers : null;
            bool isTrading = controller != null && controller.IsTradingAt(post);
            int count = offers != null ? offers.Count : 0;
            tradeEmptyText.gameObject.SetActive(count <= 0);
            for (int i = 0; i < TradeOfferSlotCount; i++)
            {
                bool visible = offers != null && i < count && offers[i].IsValid;
                tradeOfferRows[i].gameObject.SetActive(visible);
                if (visible)
                {
                    RefreshTradeOfferRow(i, offers[i], post, isTrading);
                }
            }
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
            background.color = new Color(0.08f, 0.11f, 0.10f, 0.90f);
            background.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", row).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 8f, 7f, 26f, 26f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.06f);
            frame.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 4f, 4f, 4f, 4f);
            tradeOfferIcons[index] = iconRect.gameObject.AddComponent<Image>();
            tradeOfferIcons[index].preserveAspect = true;
            tradeOfferIcons[index].raycastTarget = false;

            tradeOfferTitleTexts[index] = CreateText("Title", row, 12, TextAnchor.UpperLeft, Color.white);
            tradeOfferTitleTexts[index].fontStyle = FontStyle.Bold;
            SetTopStretch(tradeOfferTitleTexts[index].rectTransform, 42f, 6f, 72f, 17f);

            tradeOfferDetailTexts[index] = CreateText("Detail", row, 10, TextAnchor.UpperLeft, new Color(0.70f, 0.78f, 0.75f));
            SetTopStretch(tradeOfferDetailTexts[index].rectTransform, 42f, 23f, 72f, 13f);

            RectTransform action = CreateUiObject("Action", row).GetComponent<RectTransform>();
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(62f, 26f);
            action.anchoredPosition = new Vector2(-7f, 0f);
            Image actionImage = action.gameObject.AddComponent<Image>();
            actionImage.color = new Color(0.19f, 0.31f, 0.29f, 0.96f);

            Button button = action.gameObject.AddComponent<Button>();
            button.targetGraphic = actionImage;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.19f, 0.31f, 0.29f, 0.96f);
            colors.highlightedColor = new Color(0.25f, 0.40f, 0.35f, 1f);
            colors.pressedColor = new Color(0.12f, 0.20f, 0.18f, 1f);
            colors.disabledColor = new Color(0.09f, 0.11f, 0.11f, 0.88f);
            button.colors = colors;
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
            string verb = offer.Direction == StrategyTradeDirection.PlayerSells ? "Sell" : "Buy";
            string sign = offer.Direction == StrategyTradeDirection.PlayerSells ? "+" : "-";
            int available = StrategyTradeTransactionService.GetAvailableStock(offer.Resource);
            Vector3 nearWorld = post != null ? post.FootprintBounds.center : Vector3.zero;
            bool canExecute = isTrading && StrategyTradeTransactionService.CanExecute(offer, nearWorld);

            tradeOfferIcons[index].sprite = StrategyResourceIconFactory.GetSprite(offer.Resource);
            tradeOfferIcons[index].color = canExecute ? Color.white : new Color(1f, 1f, 1f, 0.42f);
            tradeOfferTitleTexts[index].text = verb + " " + offer.Amount + " " + GetResourceTitle(offer.Resource);
            tradeOfferDetailTexts[index].text = sign + offer.TotalCoins + " Coins / stock " + available;
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

            controller.TryExecuteOffer(post, index, out _);
            RefreshHud();
        }
    }
}
