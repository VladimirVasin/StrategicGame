using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {

        private static void ConfigureButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
        }

        internal static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        internal static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        internal sealed class BuildCategoryData
        {
            public BuildCategoryData(string label, Color accentColor, BuildItemData[] items)
            {
                Label = label;
                AccentColor = accentColor;
                Items = items ?? Array.Empty<BuildItemData>();
                Subcategories = Array.Empty<BuildSubcategoryData>();
            }

            public BuildCategoryData(string label, Color accentColor, BuildSubcategoryData[] subcategories)
            {
                Label = label;
                AccentColor = accentColor;
                Subcategories = subcategories ?? Array.Empty<BuildSubcategoryData>();
                Items = FlattenItems(Subcategories);
            }

            public string Label { get; }
            public Color AccentColor { get; }
            public BuildItemData[] Items { get; }
            public BuildSubcategoryData[] Subcategories { get; }
            public bool HasSubcategories => Subcategories.Length > 0;
        }

        internal sealed class BuildSubcategoryData
        {
            public BuildSubcategoryData(string label, Color accentColor, BuildItemData[] items)
            {
                Label = label;
                AccentColor = accentColor;
                Items = items ?? Array.Empty<BuildItemData>();
            }

            public string Label { get; }
            public Color AccentColor { get; }
            public BuildItemData[] Items { get; }
        }

        internal sealed class BuildItemData
        {
            public BuildItemData(StrategyBuildTool tool, string abbrev, string title, StrategyConstructionResourceCost cost, Color color)
            {
                Tool = tool;
                Abbrev = abbrev;
                Title = title;
                Cost = cost;
                Color = color;
            }

            public StrategyBuildTool Tool { get; }
            public string Abbrev { get; }
            public string Title { get; }
            public StrategyConstructionResourceCost Cost { get; }
            public Color Color { get; }
        }

        private sealed class CategoryUi
        {
            public BuildCategoryData Data;
            public int Index;
            public RectTransform Root;
            public Image Background;
            public Text Label;
            public Button Button;
            public BuildSubcategoryUi[] Subcategories;
            public BuildItemUi[] Items;
        }

        private sealed class BuildSubcategoryUi
        {
            public BuildSubcategoryData Data;
            public int Index;
            public RectTransform Root;
            public Image Background;
            public Text Label;
            public Button Button;
        }

        private sealed class BuildItemUi
        {
            public BuildItemData Data;
            public int SubcategoryIndex;
            public RectTransform Root;
            public Image Background;
            public Image IconBackground;
            public Text Title;
            public Image BadgeBackground;
            public Text BadgeText;
            public Button Button;
        }

        private static BuildItemData[] FlattenItems(BuildSubcategoryData[] subcategories)
        {
            if (subcategories == null || subcategories.Length <= 0)
            {
                return Array.Empty<BuildItemData>();
            }

            List<BuildItemData> items = new();
            for (int i = 0; i < subcategories.Length; i++)
            {
                BuildSubcategoryData subcategory = subcategories[i];
                if (subcategory == null || subcategory.Items == null)
                {
                    continue;
                }

                items.AddRange(subcategory.Items);
            }

            return items.ToArray();
        }
    }
}
