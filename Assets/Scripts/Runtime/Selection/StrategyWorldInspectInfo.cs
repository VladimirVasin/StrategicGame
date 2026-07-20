using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyWorldInspectKind
    {
        Generic,
        Wildlife,
        Resource,
        Deposit,
        Tree,
        LoosePile,
        Grave,
        Nature
    }

    public readonly struct StrategyWorldInspectChip
    {
        public StrategyWorldInspectChip(string label, Sprite icon, Color color)
        {
            Label = label ?? string.Empty;
            Icon = icon;
            Color = color;
        }

        public string Label { get; }
        public Sprite Icon { get; }
        public Color Color { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Label);
    }

    public readonly struct StrategyWorldInspectRow
    {
        public StrategyWorldInspectRow(string label, string value, Sprite icon, Color color)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Icon = icon;
            Color = color;
        }

        public string Label { get; }
        public string Value { get; }
        public Sprite Icon { get; }
        public Color Color { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Label) || !string.IsNullOrWhiteSpace(Value);
    }

    public readonly struct StrategyWorldInspectInfo
    {
        public StrategyWorldInspectInfo(
            string title,
            string subtitle,
            string body,
            Sprite icon,
            Vector2Int cell,
            bool hasCell)
            : this(
                title,
                subtitle,
                body,
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Generic,
                Color.clear,
                null,
                null)
        {
        }

        public StrategyWorldInspectInfo(
            string title,
            string subtitle,
            string body,
            Sprite icon,
            Vector2Int cell,
            bool hasCell,
            StrategyWorldInspectKind kind,
            Color accentColor,
            StrategyWorldInspectChip[] chips,
            StrategyWorldInspectRow[] rows)
        {
            Title = string.IsNullOrWhiteSpace(title)
                ? StrategySelectionLocalization.Value("Unknown")
                : title;
            Subtitle = subtitle ?? string.Empty;
            Body = body ?? string.Empty;
            Icon = icon;
            Cell = cell;
            HasCell = hasCell;
            Kind = kind;
            AccentColor = accentColor == Color.clear ? new Color(0.86f, 0.62f, 0.26f, 1f) : accentColor;
            Chips = chips ?? System.Array.Empty<StrategyWorldInspectChip>();
            Rows = rows ?? System.Array.Empty<StrategyWorldInspectRow>();
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string Body { get; }
        public Sprite Icon { get; }
        public Vector2Int Cell { get; }
        public bool HasCell { get; }
        public StrategyWorldInspectKind Kind { get; }
        public Color AccentColor { get; }
        public StrategyWorldInspectChip[] Chips { get; }
        public StrategyWorldInspectRow[] Rows { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Title);
        public bool HasStructuredContent =>
            (Chips != null && Chips.Length > 0)
            || (Rows != null && Rows.Length > 0);
    }
}
