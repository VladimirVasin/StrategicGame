using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyResourceOverviewHudController : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;
        private const float PanelOpenDuration = 0.16f;
        private const float PanelCloseDuration = 0.12f;

        private readonly StrategyResourceSnapshot snapshot = new();
        private readonly List<ResourceRowView> resourceRows = new();
        private readonly Dictionary<StrategyResourceType, ResourceRowView> rowsByResource = new();

        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private Canvas hudCanvas;
        private RectTransform launcherRoot;
        private Text launcherSummary;
        private RectTransform panelRoot;
        private GameObject overlayRoot;
        private CanvasGroup overlayGroup;
        private StrategyUiPanelTransition panelTransition;
        private Button closeButton;
        private GameObject previousSelectedObject;
        private bool hasStoredSelection;
        private bool initialized;
        private bool isOpen;
        private bool isClosing;
        private float closeReleaseTime;
        private float nextRefreshTime;

        public bool IsOpen => isOpen;
        public bool IsClosing => isClosing;
        public int VisibleResourceCount => resourceRows.Count;
        public bool HoldsInputContext => inputContext != null && !inputContext.IsDisposed;
        public RectTransform LauncherRoot => launcherRoot;
        public RectTransform PanelRoot => panelRoot;
        public Canvas HudCanvas => hudCanvas;
        public string LauncherSummary => launcherSummary != null
            ? launcherSummary.text
            : string.Empty;

        public void Configure(StrategyInputRouter router)
        {
            EnsureUi();
            SetInputRouter(router);
            RefreshNow();
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

        public void RefreshNow()
        {
            EnsureUi();
            RefreshLauncherSummary();
            StrategyResourceQueryService.PopulateSnapshot(snapshot);
            RefreshResourceRows();
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        public bool TryGetDisplayedCounts(
            StrategyResourceType resource,
            out int stored,
            out int available)
        {
            if (rowsByResource.TryGetValue(resource, out ResourceRowView row))
            {
                stored = row.StoredValue;
                available = row.AvailableValue;
                return true;
            }

            stored = 0;
            available = 0;
            return false;
        }

        public int GetDisplayedStored(StrategyResourceType resource)
        {
            return rowsByResource.TryGetValue(resource, out ResourceRowView row)
                ? row.StoredValue
                : 0;
        }

        public int GetDisplayedAvailable(StrategyResourceType resource)
        {
            return rowsByResource.TryGetValue(resource, out ResourceRowView row)
                ? row.AvailableValue
                : 0;
        }

        private void Awake()
        {
            EnsureUi();
            ApplyOpenState(false, true, false);
            RefreshNow();
        }

        private void OnEnable()
        {
            if (initialized && hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(true);
                RefreshNow();
            }
        }

        private void Update()
        {
            UpdateInputAndClosingState();
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            RefreshLauncherSummary();
            if (isOpen)
            {
                StrategyResourceQueryService.PopulateSnapshot(snapshot);
                RefreshResourceRows();
            }

            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void OnDisable()
        {
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
            ReleaseInputContext();
        }

        private sealed class ResourceRowView
        {
            public StrategyResourceType Resource;
            public RectTransform Root;
            public Image Background;
            public Image Icon;
            public Text Name;
            public Text Stored;
            public StrategyHudTooltip Tooltip;
            public int StoredValue;
            public int AvailableValue;
        }
    }
}
