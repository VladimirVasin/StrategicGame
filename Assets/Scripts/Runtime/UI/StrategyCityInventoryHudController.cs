using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyCityInventoryHudController : MonoBehaviour
    {
        private const float PanelOpenDuration = 0.18f;
        private const float PanelCloseDuration = 0.13f;
        private const float RewardFeedbackDuration = 0.58f;

        private readonly List<StrategyCityInventoryEntry> ownedItems = new();
        private readonly List<ItemRowView> itemRows = new();

        private StrategyCityInventory inventory;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private Canvas hudCanvas;
        private RectTransform launcherRoot;
        private RectTransform launcherChestIcon;
        private Vector3 launcherChestIconRestScale = Vector3.one;
        private GameObject overlayRoot;
        private RectTransform panelRoot;
        private CanvasGroup overlayGroup;
        private StrategyUiPanelTransition panelTransition;
        private GameObject badgeRoot;
        private Text badgeText;
        private GameObject itemViewportRoot;
        private RectTransform itemContent;
        private GameObject detailPanelRoot;
        private GameObject emptyStateRoot;
        private Text emptyStateTitle;
        private Text emptyStateBody;
        private Image detailIcon;
        private Text detailName;
        private Text detailQuantity;
        private Text detailDescription;
        private Text detailEffect;
        private StrategyHudTooltip launcherTooltip;
        private Button closeButton;
        private GameObject previousSelectedObject;
        private bool hasStoredSelection;
        private bool initialized;
        private bool subscribed;
        private bool isOpen;
        private bool isClosing;
        private bool rewardFeedbackActive;
        private float closeReleaseTime;
        private float rewardFeedbackStartedAt;
        private int selectedItemIndex = -1;

        public bool IsOpen => isOpen;
        public bool IsClosing => isClosing;
        public bool IsBadgeVisible => badgeRoot != null && badgeRoot.activeSelf;
        public int VisibleItemCount => ownedItems.Count;
        public int DisplayedBadgeCount => badgeText != null
            && int.TryParse(badgeText.text, out int count)
                ? count
                : 0;
        public string EmptyStateCopy => emptyStateTitle != null && emptyStateBody != null
            ? emptyStateTitle.text + "\n" + emptyStateBody.text
            : string.Empty;
        public RectTransform LauncherRoot => launcherRoot;
        public Canvas HudCanvas => hudCanvas;
        public bool HoldsInputContext => inputContext != null && !inputContext.IsDisposed;
        public string SelectedItemTitle => detailName != null ? detailName.text : string.Empty;
        public string SelectedItemEffect => detailEffect != null ? detailEffect.text : string.Empty;

        public bool TryGetRewardDestination(out Vector2 screenPoint)
        {
            EnsureUi();
            screenPoint = Vector2.zero;
            if (launcherChestIcon == null
                || hudCanvas == null
                || !hudCanvas.gameObject.activeInHierarchy)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            Camera eventCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : hudCanvas.worldCamera;
            Vector3 worldCenter = launcherChestIcon.TransformPoint(launcherChestIcon.rect.center);
            screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
            return !float.IsNaN(screenPoint.x)
                && !float.IsInfinity(screenPoint.x)
                && !float.IsNaN(screenPoint.y)
                && !float.IsInfinity(screenPoint.y);
        }

        public void PlayRewardReceivedFeedback()
        {
            EnsureUi();
            ResetRewardReceivedFeedback();
            if (!isActiveAndEnabled
                || hudCanvas == null
                || !hudCanvas.gameObject.activeInHierarchy
                || launcherChestIcon == null)
            {
                return;
            }

            rewardFeedbackStartedAt = Time.unscaledTime;
            rewardFeedbackActive = true;
        }

        public void Configure(
            StrategyCityInventory cityInventory,
            StrategyInputRouter router)
        {
            EnsureUi();
            BindInventory(cityInventory);
            SetInputRouter(router);
            RefreshInventoryView();
        }

        public void SetInputRouter(StrategyInputRouter router)
        {
            if (inputRouter == router)
            {
                RefreshInputContext();
                return;
            }

            ReleaseInputContext();
            inputRouter = router;
            RefreshInputContext();
        }

        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        public void SetOpen(bool open, bool immediate = false, bool playSfx = true)
        {
            if (open && !isActiveAndEnabled)
            {
                return;
            }

            EnsureUi();
            ApplyOpenState(open, immediate, playSfx);
        }

        private void Awake()
        {
            EnsureUi();
            ApplyOpenState(false, true, false);
        }

        private void OnEnable()
        {
            StrategyLocalization.LanguageChanged += HandleLanguageChanged;
            if (initialized && hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(true);
                RefreshInventoryView();
            }

            SubscribeInventory();
        }

        private void Update()
        {
            UpdateInputAndClosingState();
            UpdateRewardReceivedFeedback();
        }

        private void BindInventory(StrategyCityInventory cityInventory)
        {
            if (inventory == cityInventory)
            {
                SubscribeInventory();
                return;
            }

            UnsubscribeInventory();
            inventory = cityInventory;
            SubscribeInventory();
        }

        private void SubscribeInventory()
        {
            if (subscribed || inventory == null || !isActiveAndEnabled)
            {
                return;
            }

            inventory.Changed += HandleInventoryChanged;
            subscribed = true;
        }

        private void UnsubscribeInventory()
        {
            if (!subscribed)
            {
                return;
            }

            if (inventory != null)
            {
                inventory.Changed -= HandleInventoryChanged;
            }

            subscribed = false;
        }

        private void HandleInventoryChanged()
        {
            RefreshInventoryView();
        }

        private void HandleLanguageChanged()
        {
            if (!initialized)
            {
                return;
            }

            launcherTooltip?.SetText(StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.inventory.tooltip"));
            RefreshInventoryView();
        }

        private void OnDisable()
        {
            StrategyLocalization.LanguageChanged -= HandleLanguageChanged;
            UnsubscribeInventory();
            ResetRewardReceivedFeedback();
            isOpen = false;
            isClosing = false;
            panelTransition?.SetVisible(false, true);
            ReleaseInputContext();
            RestorePreviousSelection();
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeInventory();
            ResetRewardReceivedFeedback();
            ReleaseInputContext();
        }

        private sealed class ItemRowView
        {
            public RectTransform Root;
            public Image Background;
            public Image SelectionAccent;
            public Image Icon;
            public Text Name;
            public Text Summary;
            public Text Quantity;
            public Button Button;
            public int Index;
        }
    }
}
