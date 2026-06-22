using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildMenuCatalog
    {
        private const float StoneCostMultiplier = 1.2f;

        private static Color HtmlColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
        }

        private static StrategyConstructionResourceCost Cost(int logs, int stone, int planks = 0)
        {
            int adjustedStone = stone <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(stone * StoneCostMultiplier));
            return new StrategyConstructionResourceCost(logs, adjustedStone, planks);
        }

        internal static StrategyBuildMenuControllerDriver.BuildCategoryData[] CreateCatalog()
        {
            return new[]
            {
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "Housing",
                    HtmlColor("#A97845"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.House, "HM", "House", new StrategyConstructionResourceCost(2, 3), HtmlColor("#C39B69"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "Extraction",
                    HtmlColor("#5F7545"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.LumberjackCamp, "LC", "Lumberjack Camp", Cost(4, 2), HtmlColor("#7D8E4A")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.StonecutterCamp, "SC", "Stonecutter Camp", Cost(3, 4), HtmlColor("#7B8582")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Mine, "MN", "Mine", Cost(5, 5, 3), HtmlColor("#765F4C")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.CoalPit, "CP", "Coal Pit", Cost(4, 4, 2), HtmlColor("#464B4D")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.ClayPit, "CL", "Clay Pit", Cost(3, 3, 1), HtmlColor("#A96945")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.HunterCamp, "HC", "Hunter Camp", Cost(4, 2), HtmlColor("#7B6A45")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.FisherHut, "FH", "Fisher Hut", Cost(4, 2), HtmlColor("#4F7E8A"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "Production",
                    HtmlColor("#8A6138"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Sawmill, "SW", "Sawmill", Cost(8, 4), HtmlColor("#9A6B3A")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Kiln, "KI", "Kiln", Cost(5, 5, 2), HtmlColor("#B46A3F"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "Storage",
                    HtmlColor("#78624B"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.StorageYard, "ST", "Storage Yard", Cost(6, 4), HtmlColor("#9B8061")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Granary, "GR", "Granary", Cost(5, 3), HtmlColor("#A8874D"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "Infrastructure",
                    HtmlColor("#4E6E70"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Bridge, "BR", "Bridge", Cost(6, 2), HtmlColor("#8B7150"))
                    })
            };
        }

        internal static void DrawBuildIcon(RectTransform parent, Color beam, Color metal)
        {
            DrawRect(parent, "Beam", new Vector2(0.50f, 0.50f), new Vector2(8f, 30f), beam, -42f);
            DrawRect(parent, "Head", new Vector2(0.34f, 0.68f), new Vector2(24f, 8f), metal, -42f);
            DrawRect(parent, "Tip", new Vector2(0.64f, 0.30f), new Vector2(18f, 8f), metal, -42f);
        }

        internal static void DrawItemIcon(RectTransform parent, StrategyBuildMenuControllerDriver.BuildItemData data)
        {
            if (StrategyBuildingSpriteFactory.TryGetBuildSprite(data.Tool, out Sprite sprite))
            {
                RectTransform spriteRoot = StrategyBuildMenuControllerDriver.CreateUiObject("Sprite", parent).GetComponent<RectTransform>();
                StrategyBuildMenuControllerDriver.Stretch(spriteRoot, -4f, -2f, -4f, -2f);

                Image image = spriteRoot.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text label = StrategyBuildMenuControllerDriver.CreateText("Abbrev", parent, data.Abbrev, 18, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            StrategyBuildMenuControllerDriver.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
        }

        private static void DrawRect(RectTransform parent, string name, Vector2 anchor, Vector2 size, Color color, float rotation)
        {
            RectTransform rect = StrategyBuildMenuControllerDriver.CreateUiObject(name, parent).GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
