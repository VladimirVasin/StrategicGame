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

        private readonly List<StrategyCityInventoryEntry> ownedItems = new();
        private readonly List<ItemCardView> itemCards = new();

        private StrategyCityInventory inventory;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private Canvas hudCanvas;
        private RectTransform launcherRoot;
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
        private Button closeButton;
        private GameObject previousSelectedObject;
        private bool hasStoredSelection;
        private bool initialized;
        private bool subscribed;
        private bool isOpen;
        private bool isClosing;
        private float closeReleaseTime;
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

        private void OnDisable()
        {
            UnsubscribeInventory();
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
            ReleaseInputContext();
        }

        private sealed class ItemCardView
        {
            public RectTransform Root;
            public Image Background;
            public Image Icon;
            public Text Name;
            public Text Quantity;
            public Button Button;
            public int Index;
        }
    }
}
