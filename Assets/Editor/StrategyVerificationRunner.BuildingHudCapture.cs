using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static readonly BuildingHudCaptureCase[] BuildingHudCaptureCases =
        {
            BuildingHudCaptureCase.Extraction,
            BuildingHudCaptureCase.Production,
            BuildingHudCaptureCase.Storage,
            BuildingHudCaptureCase.Trade,
            BuildingHudCaptureCase.Scout,
            BuildingHudCaptureCase.House,
            BuildingHudCaptureCase.Construction
        };

        private static void CaptureBuildingHudMatrix()
        {
            GameObject liveSelectionCanvas = GameObject.Find("SelectionHudCanvas");
            bool liveSelectionWasActive = liveSelectionCanvas != null
                && liveSelectionCanvas.activeSelf;
            if (liveSelectionWasActive)
            {
                liveSelectionCanvas.SetActive(false);
            }

            GameObject captureRoot = new(
                "Building MicroHUD Capture",
                typeof(Canvas),
                typeof(CanvasScaler));
            Canvas canvas = captureRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 490;
            StrategyHudStyle.ConfigureScaler(captureRoot.GetComponent<CanvasScaler>());

            BuildingHudCaptureView view = new(captureRoot.transform);
            StrategyBuildingHudSnapshot snapshot = new();
            try
            {
                for (int i = 0; i < BuildingHudCaptureCases.Length; i++)
                {
                    BuildingHudCaptureCase captureCase = BuildingHudCaptureCases[i];
                    FillBuildingHudCapture(captureCase, snapshot, out string title, out string subtitle);
                    view.Show(title, subtitle, snapshot);
                    Canvas.ForceUpdateCanvases();
                    string id = captureCase.ToString();
                    CaptureGameplayRender("VisualBuildingMicroHud_" + id + "_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualBuildingMicroHud_" + id + "_1484x839.png", 1484, 839);
                }

                File.WriteAllText(
                    GetResultPath("BuildingMicroHudCaptureManifest.txt"),
                    "PASS: synthetic building microHUD renderer matrix captured.\n"
                    + "Cases: Extraction, Production, Storage, Trade, Scout, House, Construction.\n"
                    + "Scope: shared snapshot renderer, shell geometry, typography, tones, progress, and overflow.\n"
                    + "Excluded: live Trade offers, Scout controls, House residents/dinner modules, and gameplay actions.\n");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(captureRoot);
                if (liveSelectionCanvas != null)
                {
                    liveSelectionCanvas.SetActive(liveSelectionWasActive);
                }
            }
        }

        private static void FillBuildingHudCapture(
            BuildingHudCaptureCase captureCase,
            StrategyBuildingHudSnapshot snapshot,
            out string title,
            out string subtitle)
        {
            switch (captureCase)
            {
                case BuildingHudCaptureCase.Extraction:
                    FillExtractionCapture(snapshot);
                    title = "Lumberjack Camp";
                    subtitle = "Extraction · Wood";
                    break;
                case BuildingHudCaptureCase.Production:
                    FillProductionCapture(snapshot);
                    title = "Sawmill";
                    subtitle = "Production · Planks";
                    break;
                case BuildingHudCaptureCase.Storage:
                    FillStorageCapture(snapshot);
                    title = "Storage Yard";
                    subtitle = "Storage · Materials";
                    break;
                case BuildingHudCaptureCase.Trade:
                    FillTradeCapture(snapshot);
                    title = "Trading Post";
                    subtitle = "Trade · Caravan market";
                    break;
                case BuildingHudCaptureCase.Scout:
                    FillScoutCapture(snapshot);
                    title = "Scout Lodge";
                    subtitle = "Exploration · Expeditions";
                    break;
                case BuildingHudCaptureCase.House:
                    FillHouseCapture(snapshot);
                    title = "House";
                    subtitle = "Housing · Alder family";
                    break;
                default:
                    FillConstructionCapture(snapshot);
                    title = "Construction";
                    subtitle = "Sawmill · 62% complete";
                    break;
            }
        }

        private static void FillExtractionCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.LumberjackCamp);
            snapshot.AddChip("workers", "Workers", "2 / 3", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("stock", "Logs", "7 / 12", ResourceIcon(StrategyResourceType.Logs));
            snapshot.AddChip("sources", "Trees", "14 ready", null, StrategyBuildingHudTone.Info);
            StrategyBuildingHudSection section = snapshot.AddSection("operation", "Operation");
            AddResourceCaptureRow(section, "output", "Output stock", "7 Logs", StrategyResourceType.Logs, "5 free slots", 7f / 12f);
            section.AddRow("sources", "Mature trees", "14", null, StrategyBuildingHudTone.Info, "6 growing nearby");
            snapshot.SetStatus("Working", "2 woodcutters are gathering nearby mature trees.", StrategyBuildingHudTone.Positive);
        }

        private static void FillProductionCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.Sawmill);
            snapshot.AddChip("workers", "Workers", "1 / 2", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("input", "Logs", "3", ResourceIcon(StrategyResourceType.Logs));
            snapshot.AddChip("output", "Planks", "5 / 12", ResourceIcon(StrategyResourceType.Planks));
            StrategyBuildingHudSection section = snapshot.AddSection("production", "Production");
            AddResourceCaptureRow(section, "recipe", "Logs → Planks", "3 ready", StrategyResourceType.Planks, "Input supplied");
            section.AddRow("cycle", "Current cycle", "68%", null, StrategyBuildingHudTone.Positive, "Sawyer is processing timber", 0.68f);
            snapshot.SetStatus("Processing", "Production is active and output storage has room.", StrategyBuildingHudTone.Positive);
        }

        private static void FillStorageCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.StorageYard);
            snapshot.AddChip("capacity", "Capacity", "21 / 48", null, StrategyBuildingHudTone.Info);
            snapshot.AddChip("reserved", "Reserved", "6", null, StrategyBuildingHudTone.Warning);
            snapshot.AddChip("logistics", "Logistics", "Ready", null, StrategyBuildingHudTone.Positive);
            StrategyBuildingHudSection section = snapshot.AddSection("stock", "Stored materials");
            AddResourceCaptureRow(section, "logs", "Logs", "9 / 16", StrategyResourceType.Logs, "3 reserved", 9f / 16f);
            AddResourceCaptureRow(section, "stone", "Stone", "7 / 16", StrategyResourceType.Stone, "2 reserved", 7f / 16f);
            AddResourceCaptureRow(section, "planks", "Planks", "5 / 16", StrategyResourceType.Planks, "1 reserved", 5f / 16f);
            snapshot.SetStatus("Ready for deliveries", "Haulers can collect and deposit construction materials.", StrategyBuildingHudTone.Positive);
        }

        private static void FillTradeCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.TradingPost);
            snapshot.AddChip("coins", "Coins", "34", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("caravan", "Caravan", "02:18", null, StrategyBuildingHudTone.Info);
            snapshot.AddChip("offers", "Offers", "3", null);
            StrategyBuildingHudSection section = snapshot.AddSection("market", "Market summary");
            AddResourceCaptureRow(section, "import", "Import Stone", "8 for 6c", StrategyResourceType.Stone, "Affordable now");
            AddResourceCaptureRow(section, "export", "Export Planks", "+9c", StrategyResourceType.Planks, "Requires 6 Planks");
            snapshot.SetStatus("Caravan en route", "Current offers remain available until the caravan departs.", StrategyBuildingHudTone.Info);
        }

        private static void FillScoutCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.ScoutLodge);
            snapshot.AddChip("scout", "Scout", "Edda", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("mission", "Mission", "Ready", null, StrategyBuildingHudTone.Info);
            snapshot.AddChip("provisions", "Provisions", "2 / 2", ResourceIcon(StrategyResourceType.Dish));
            StrategyBuildingHudSection section = snapshot.AddSection("expedition", "Expedition");
            section.AddRow("destination", "Destination", "Old road", null, StrategyBuildingHudTone.Info, "Unexplored point of interest");
            section.AddRow("risk", "Estimated risk", "Low", null, StrategyBuildingHudTone.Positive, "Scout is rested and supplied");
            snapshot.SetStatus("Ready to depart", "Choose a destination from the Scout Lodge action panel.", StrategyBuildingHudTone.Positive);
        }

        private static void FillHouseCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.House);
            snapshot.AddChip("household", "Household", "4 / 6", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("dinner", "Dinner", "4 / 4", ResourceIcon(StrategyResourceType.Dish), StrategyBuildingHudTone.Positive);
            snapshot.AddChip("warmth", "Warmth", "Warm", ResourceIcon(StrategyResourceType.Logs), StrategyBuildingHudTone.Positive);
            StrategyBuildingHudSection section = snapshot.AddSection("household", "Household overview");
            section.AddRow("residents", "Residents", "4", null, StrategyBuildingHudTone.Neutral, "2 adults · 2 children");
            AddResourceCaptureRow(section, "food", "Stored food", "6 rations", StrategyResourceType.Dish, "Dinner is covered");
            snapshot.SetStatus("Household stable", "Family needs are covered for the coming night.", StrategyBuildingHudTone.Positive);
        }

        private static void FillConstructionCapture(StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.Reset(StrategyBuildTool.Sawmill, true);
            snapshot.AddChip("progress", "Progress", "62%", null, StrategyBuildingHudTone.Info);
            snapshot.AddChip("builders", "Builders", "2 / 3", null, StrategyBuildingHudTone.Positive);
            snapshot.AddChip("materials", "Materials", "8 / 11", null, StrategyBuildingHudTone.Warning);
            StrategyBuildingHudSection section = snapshot.AddSection("materials", "Required materials");
            AddResourceCaptureRow(section, "logs", "Logs", "4 / 4", StrategyResourceType.Logs, "Delivered", 1f);
            AddResourceCaptureRow(section, "stone", "Stone", "2 / 3", StrategyResourceType.Stone, "1 still needed", 2f / 3f);
            AddResourceCaptureRow(section, "planks", "Planks", "2 / 4", StrategyResourceType.Planks, "2 still needed", 0.5f);
            snapshot.SetStatus("Waiting for materials", "Builders will continue when the remaining Stone and Planks arrive.", StrategyBuildingHudTone.Warning);
        }

        private static void AddResourceCaptureRow(
            StrategyBuildingHudSection section,
            string key,
            string label,
            string value,
            StrategyResourceType resource,
            string detail,
            float progress = -1f)
        {
            section?.AddRow(
                key,
                label,
                value,
                ResourceIcon(resource),
                StrategyBuildingHudTone.Neutral,
                detail,
                progress);
        }

        private static Sprite ResourceIcon(StrategyResourceType type) =>
            StrategyResourceIconFactory.GetSprite(type);

        private enum BuildingHudCaptureCase
        {
            Extraction,
            Production,
            Storage,
            Trade,
            Scout,
            House,
            Construction
        }

        private sealed class BuildingHudCaptureView
        {
            private readonly Text title;
            private readonly Text subtitle;
            private readonly Image preview;
            private readonly StrategyBuildingSelectionHudRenderer renderer;

            public BuildingHudCaptureView(Transform parent)
            {
                RectTransform panel = CreateBuildingCaptureObject("SelectionSideHud", parent);
                panel.anchorMin = new Vector2(1f, 0f);
                panel.anchorMax = new Vector2(1f, 1f);
                panel.pivot = new Vector2(1f, 0.5f);
                panel.sizeDelta = new Vector2(410f, -StrategyHudStyle.TopRailHeight);
                panel.anchoredPosition = new Vector2(0f, -StrategyHudStyle.TopRailHeight * 0.5f);
                Image background = panel.gameObject.AddComponent<Image>();
                StrategyHudStyle.StylePanel(background, BuildingCaptureAlpha(StrategyHudStyle.Background, 0.98f));

                RectTransform accent = CreateBuildingCaptureObject("Accent", panel);
                accent.anchorMin = new Vector2(0f, 0f);
                accent.anchorMax = new Vector2(0f, 1f);
                accent.pivot = new Vector2(0f, 0.5f);
                accent.sizeDelta = new Vector2(5f, 0f);
                accent.anchoredPosition = Vector2.zero;
                accent.gameObject.AddComponent<Image>().color = StrategyHudStyle.Primary;

                RectTransform previewFrame = CreateBuildingCaptureObject("PreviewFrame", panel);
                SetBuildingCaptureTopLeft(previewFrame, 24f, 24f, 70f, 70f);
                Image previewBackground = previewFrame.gameObject.AddComponent<Image>();
                StrategyHudStyle.StyleInset(previewBackground, new Color(1f, 1f, 1f, 0.12f));
                RectTransform previewInset = CreateBuildingCaptureObject("PreviewInset", previewFrame);
                SetBuildingCaptureOffsets(previewInset, 4f, 4f, 4f, 4f);
                Image insetBackground = previewInset.gameObject.AddComponent<Image>();
                StrategyHudStyle.StyleInset(insetBackground, BuildingCaptureAlpha(StrategyHudStyle.Background, 0.90f));
                RectTransform previewRect = CreateBuildingCaptureObject("PreviewImage", previewInset);
                SetBuildingCaptureOffsets(previewRect, 3f, 3f, 3f, 3f);
                preview = previewRect.gameObject.AddComponent<Image>();
                preview.preserveAspect = true;
                preview.raycastTarget = false;

                title = CreateBuildingCaptureText("Title", panel, 22, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
                title.fontStyle = FontStyle.Bold;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 17;
                title.resizeTextMaxSize = 22;
                SetBuildingCaptureTopStretch(title.rectTransform, 104f, 27f, 24f, 34f);
                subtitle = CreateBuildingCaptureText("Subtitle", panel, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
                subtitle.fontStyle = FontStyle.Bold;
                subtitle.resizeTextForBestFit = true;
                subtitle.resizeTextMinSize = 10;
                subtitle.resizeTextMaxSize = 13;
                SetBuildingCaptureTopStretch(subtitle.rectTransform, 104f, 64f, 24f, 22f);

                RectTransform divider = CreateBuildingCaptureObject("Divider", panel);
                SetBuildingCaptureTopStretch(divider, 24f, 112f, 24f, 2f);
                divider.gameObject.AddComponent<Image>().color = StrategyHudStyle.Divider;

                RectTransform content = CreateBuildingCaptureObject("ContextSections", panel);
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.one;
                content.offsetMin = new Vector2(0f, 8f);
                content.offsetMax = new Vector2(0f, -122f);
                content.gameObject.AddComponent<RectMask2D>();
                renderer = new StrategyBuildingSelectionHudRenderer(content);
            }

            public void Show(string header, string descriptor, StrategyBuildingHudSnapshot snapshot)
            {
                title.text = header;
                subtitle.text = descriptor;
                StrategyBuildingSpriteFactory.TryGetBuildSprite(snapshot.Tool, out Sprite sprite);
                preview.sprite = sprite;
                preview.color = sprite != null ? Color.white : Color.clear;
                renderer.Show(snapshot, 0f);
            }
        }

        private static RectTransform CreateBuildingCaptureObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Text CreateBuildingCaptureText(
            string name,
            Transform parent,
            int size,
            TextAnchor anchor,
            Color color)
        {
            Text text = CreateBuildingCaptureObject(name, parent).gameObject.AddComponent<Text>();
            StrategyHudStyle.StyleText(text, StrategyHudTextRole.Body, color);
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetBuildingCaptureTopLeft(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }

        private static void SetBuildingCaptureTopStretch(
            RectTransform rect,
            float left,
            float top,
            float right,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBuildingCaptureOffsets(
            RectTransform rect,
            float left,
            float top,
            float right,
            float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Color BuildingCaptureAlpha(Color color, float alpha) =>
            new(color.r, color.g, color.b, alpha);
    }
}
