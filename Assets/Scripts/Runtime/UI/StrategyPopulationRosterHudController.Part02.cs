using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationRosterHudController
    {
        private enum RosterSortColumn
        {
            Name,
            Age,
            Home,
            Role,
            Status,
            Food
        }

        private readonly Dictionary<RosterSortColumn, Text> sortHeaders = new();
        private RosterSortColumn sortColumn = RosterSortColumn.Name;
        private bool sortAscending = true;

        private Text CreateSortableHeaderText(Transform parent, string value, float x, float width)
        {
            RosterSortColumn column = ResolveSortColumn(value);
            Color normal = new Color(0.88f, 0.72f, 0.42f, 1f);
            Text text = CreateText("Header_" + value, parent, GetHeaderLabel(column), 11, TextAnchor.MiddleLeft, normal);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = true;
            SetTopLeft(text.rectTransform, x, 0f, width, 28f);

            Button button = text.gameObject.AddComponent<Button>();
            button.targetGraphic = text;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(1f, 0.86f, 0.55f, 1f);
            colors.pressedColor = new Color(0.72f, 0.55f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => SetSortColumn(column));

            sortHeaders[column] = text;
            UpdateSortHeaderLabels();
            return text;
        }

        private void SetSortColumn(RosterSortColumn column)
        {
            if (sortColumn == column)
            {
                sortAscending = !sortAscending;
            }
            else
            {
                sortColumn = column;
                sortAscending = true;
            }

            UpdateSortHeaderLabels();
            RefreshNow();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
        }

        private void UpdateSortHeaderLabels()
        {
            foreach (KeyValuePair<RosterSortColumn, Text> pair in sortHeaders)
            {
                if (pair.Value != null)
                {
                    pair.Value.text = GetHeaderLabel(pair.Key);
                }
            }
        }

        private string GetHeaderLabel(RosterSortColumn column)
        {
            string label = column.ToString();
            return sortColumn == column
                ? label + (sortAscending ? " ^" : " v")
                : label;
        }

        private static RosterSortColumn ResolveSortColumn(string value)
        {
            return value switch
            {
                "Age" => RosterSortColumn.Age,
                "Home" => RosterSortColumn.Home,
                "Role" => RosterSortColumn.Role,
                "Status" => RosterSortColumn.Status,
                "Food" => RosterSortColumn.Food,
                _ => RosterSortColumn.Name
            };
        }

        private int CompareResidents(StrategyResidentAgent left, StrategyResidentAgent right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int result = sortColumn switch
            {
                RosterSortColumn.Age => left.DisplayAgeYears.CompareTo(right.DisplayAgeYears),
                RosterSortColumn.Home => CompareText(StrategyResidentHudText.GetHomeTitle(left), StrategyResidentHudText.GetHomeTitle(right)),
                RosterSortColumn.Role => CompareText(StrategyResidentHudText.GetRoleTitle(left), StrategyResidentHudText.GetRoleTitle(right)),
                RosterSortColumn.Status => CompareText(StrategyResidentHudText.GetStatusText(left), StrategyResidentHudText.GetStatusText(right)),
                RosterSortColumn.Food => CompareText(StrategyResidentHudText.GetFoodTitle(left), StrategyResidentHudText.GetFoodTitle(right)),
                _ => CompareText(left.FullName, right.FullName)
            };

            if (result == 0 && sortColumn != RosterSortColumn.Name)
            {
                result = CompareText(left.FullName, right.FullName);
            }

            return sortAscending ? result : -result;
        }

        private static int CompareText(string left, string right)
        {
            return string.Compare(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
