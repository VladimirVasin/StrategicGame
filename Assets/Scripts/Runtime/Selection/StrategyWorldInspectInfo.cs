using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyWorldInspectInfo
    {
        public StrategyWorldInspectInfo(
            string title,
            string subtitle,
            string body,
            Sprite icon,
            Vector2Int cell,
            bool hasCell)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Unknown" : title;
            Subtitle = subtitle ?? string.Empty;
            Body = body ?? string.Empty;
            Icon = icon;
            Cell = cell;
            HasCell = hasCell;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string Body { get; }
        public Sprite Icon { get; }
        public Vector2Int Cell { get; }
        public bool HasCell { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Title);
    }
}
