using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResourceOverviewHudController
    {
        private const float ResourceRowHeight = 31f;
        private const float ResourceRowStep = 34f;
        private const float SectionLeft = 18f;
        private const float SectionWidth = 614f;
        private const float ColumnGap = 7f;
        private const int SectionColumns = 3;

        private static readonly Color ResourceRowColor =
            new(0.085f, 0.115f, 0.11f, 0.96f);
        private static readonly Color EmptyResourceRowColor =
            new(0.060f, 0.080f, 0.078f, 0.72f);
        private static readonly StrategyResourceType[] ConstructionResources =
        {
            StrategyResourceType.Logs,
            StrategyResourceType.Stone,
            StrategyResourceType.Planks
        };
        private static readonly StrategyResourceType[] MaterialResources =
        {
            StrategyResourceType.Iron,
            StrategyResourceType.Coal,
            StrategyResourceType.Clay,
            StrategyResourceType.Pottery,
            StrategyResourceType.Tools
        };
        private static readonly StrategyResourceType[] FoodResources =
        {
            StrategyResourceType.Dish,
            StrategyResourceType.Eggs,
            StrategyResourceType.Game,
            StrategyResourceType.Fish,
            StrategyResourceType.Turnip,
            StrategyResourceType.Cabbage,
            StrategyResourceType.Onion,
            StrategyResourceType.Carrot,
            StrategyResourceType.Potato,
            StrategyResourceType.Berries,
            StrategyResourceType.Roots,
            StrategyResourceType.Mushrooms
        };

        private void CreateResourceSections()
        {
            constructionSectionTitleText = CreateResourceSection(
                "Construction",
                "hud.resources.section.construction",
                96f,
                ConstructionResources);
            materialsSectionTitleText = CreateResourceSection(
                "Materials",
                "hud.resources.section.materials",
                164f,
                MaterialResources);
            foodSectionTitleText = CreateResourceSection(
                "Food",
                "hud.resources.section.food",
                262f,
                FoodResources);
        }

        private Text CreateResourceSection(
            string objectName,
            string titleKey,
            float top,
            StrategyResourceType[] resources)
        {
            Text sectionTitle = CreateText(
                objectName + "Title",
                panelRoot,
                StrategyLocalization.Get(StrategyLocalizationTables.Hud, titleKey),
                11,
                TextAnchor.MiddleLeft,
                MutedGold);
            sectionTitle.fontStyle = FontStyle.Bold;
            SetTopLeft(
                sectionTitle.rectTransform,
                SectionLeft + 2f,
                top,
                SectionWidth - 4f,
                22f);

            float rowTop = top + 26f;
            float rowWidth = (SectionWidth - (ColumnGap * (SectionColumns - 1)))
                / SectionColumns;
            for (int index = 0; index < resources.Length; index++)
            {
                int column = index % SectionColumns;
                int row = index / SectionColumns;
                CreateResourceRow(
                    resources[index],
                    SectionLeft + (column * (rowWidth + ColumnGap)),
                    rowTop + (row * ResourceRowStep),
                    rowWidth);
            }

            return sectionTitle;
        }

        private void CreateResourceRow(
            StrategyResourceType resource,
            float left,
            float top,
            float width)
        {
            RectTransform root = CreateUiObject("ResourceRow_" + resource, panelRoot)
                .GetComponent<RectTransform>();
            SetTopLeft(root, left, top, width, ResourceRowHeight);
            Image background = root.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, ResourceRowColor, true);

            RectTransform iconFrame = CreateUiObject("IconFrame", root)
                .GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 5f, 3.5f, 24f, 24f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.055f);
            frame.raycastTarget = false;
            RectTransform iconRect = CreateUiObject("Icon", iconFrame)
                .GetComponent<RectTransform>();
            Stretch(iconRect, 2f, 2f, 2f, 2f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = StrategyResourceIconFactory.GetSprite(resource);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text name = CreateText(
                "Name",
                root,
                GetResourceDisplayName(resource),
                12,
                TextAnchor.MiddleLeft,
                StrategyHudStyle.TextPrimary);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 10;
            name.resizeTextMaxSize = 12;
            Stretch(name.rectTransform, 36f, 0f, 52f, 1f);

            Text stored = CreateText(
                "Stored",
                root,
                "0",
                13,
                TextAnchor.MiddleRight,
                MutedGold);
            stored.fontStyle = FontStyle.Bold;
            RectTransform storedRect = stored.rectTransform;
            storedRect.anchorMin = new Vector2(1f, 0f);
            storedRect.anchorMax = new Vector2(1f, 1f);
            storedRect.pivot = new Vector2(1f, 0.5f);
            storedRect.offsetMin = new Vector2(-48f, 1f);
            storedRect.offsetMax = new Vector2(-7f, 0f);

            StrategyHudTooltip tooltip = StrategyHudTooltip.Attach(
                root.gameObject,
                GetResourceTooltip(resource, 0, 0, 0));
            ResourceRowView row = new()
            {
                Resource = resource,
                Root = root,
                Background = background,
                Icon = icon,
                Name = name,
                Stored = stored,
                Tooltip = tooltip
            };
            resourceRows.Add(row);
            rowsByResource.Add(resource, row);
        }

        private void RefreshResourceRows()
        {
            for (int index = 0; index < resourceRows.Count; index++)
            {
                ResourceRowView row = resourceRows[index];
                int stored = snapshot.GetStored(row.Resource);
                int available = snapshot.GetAvailable(row.Resource);
                row.Name.text = GetResourceDisplayName(row.Resource);
                row.StoredValue = stored;
                row.AvailableValue = available;
                row.Stored.text = stored.ToString();

                bool hasStock = stored > 0;
                row.Background.color = hasStock ? ResourceRowColor : EmptyResourceRowColor;
                row.Icon.color = hasStock
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.30f);
                row.Name.color = hasStock
                    ? StrategyHudStyle.TextPrimary
                    : new Color(
                        StrategyHudStyle.TextMuted.r,
                        StrategyHudStyle.TextMuted.g,
                        StrategyHudStyle.TextMuted.b,
                        0.55f);
                row.Stored.color = hasStock
                    ? MutedGold
                    : new Color(MutedGold.r, MutedGold.g, MutedGold.b, 0.48f);

                int reserved = Mathf.Max(0, stored - available);
                row.Tooltip?.SetText(GetResourceTooltip(
                    row.Resource,
                    stored,
                    available,
                    reserved));
            }
        }

        private static string GetResourceDisplayName(StrategyResourceType resource)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Resources,
                "resource." + resource.ToString().ToLowerInvariant() + ".name");
        }

        private static string GetResourceTooltip(
            StrategyResourceType resource,
            int stored,
            int available,
            int reserved)
        {
            string key = reserved > 0
                ? "hud.resources.row_tooltip_reserved"
                : "hud.resources.row_tooltip";
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                key,
                GetResourceDisplayName(resource),
                stored,
                available,
                reserved);
        }
    }
}
