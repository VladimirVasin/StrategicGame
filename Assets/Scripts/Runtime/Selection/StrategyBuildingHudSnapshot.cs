using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyBuildingHudTone
    {
        Neutral,
        Info,
        Positive,
        Warning,
        Critical
    }

    internal readonly struct StrategyBuildingHudChip
    {
        public StrategyBuildingHudChip(
            string key,
            string label,
            string value,
            Sprite icon,
            StrategyBuildingHudTone tone)
        {
            Key = key ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Icon = icon;
            Tone = tone;
        }

        public string Key { get; }
        public string Label { get; }
        public string Value { get; }
        public Sprite Icon { get; }
        public StrategyBuildingHudTone Tone { get; }
    }

    internal readonly struct StrategyBuildingHudRow
    {
        public StrategyBuildingHudRow(
            string key,
            string label,
            string value,
            string detail,
            Sprite icon,
            StrategyBuildingHudTone tone,
            float progress)
        {
            Key = key ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Detail = detail ?? string.Empty;
            Icon = icon;
            Tone = tone;
            Progress = progress;
        }

        public string Key { get; }
        public string Label { get; }
        public string Value { get; }
        public string Detail { get; }
        public Sprite Icon { get; }
        public StrategyBuildingHudTone Tone { get; }
        public float Progress { get; }
        public bool HasProgress => Progress >= 0f;
    }

    internal sealed class StrategyBuildingHudSection
    {
        internal const int MaxRows = 8;

        private readonly StrategyBuildingHudRow[] rows =
            new StrategyBuildingHudRow[MaxRows];

        public string Key { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public int RowCount { get; private set; }

        public StrategyBuildingHudRow GetRow(int index) =>
            index >= 0 && index < RowCount ? rows[index] : default;

        internal void Reset(string key, string title)
        {
            Key = key ?? string.Empty;
            Title = title ?? string.Empty;
            RowCount = 0;
        }

        internal void AddRow(
            string key,
            string label,
            string value,
            Sprite icon,
            StrategyBuildingHudTone tone = StrategyBuildingHudTone.Neutral,
            string detail = "",
            float progress = -1f)
        {
            if (RowCount >= rows.Length)
            {
                return;
            }

            rows[RowCount++] = new StrategyBuildingHudRow(
                key,
                label,
                value,
                detail,
                icon,
                tone,
                progress);
        }
    }

    internal sealed class StrategyBuildingHudSnapshot
    {
        internal const int MaxChips = 3;
        internal const int MaxSections = 4;

        private readonly StrategyBuildingHudChip[] chips =
            new StrategyBuildingHudChip[MaxChips];
        private readonly StrategyBuildingHudSection[] sections =
            new StrategyBuildingHudSection[MaxSections];

        public StrategyBuildingHudSnapshot()
        {
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i] = new StrategyBuildingHudSection();
            }
        }

        public StrategyBuildTool Tool { get; private set; }
        public bool IsConstruction { get; private set; }
        public int ChipCount { get; private set; }
        public int SectionCount { get; private set; }
        public string StatusTitle { get; private set; } = string.Empty;
        public string StatusBody { get; private set; } = string.Empty;
        public StrategyBuildingHudTone StatusTone { get; private set; }
        public bool HasStatus => !string.IsNullOrWhiteSpace(StatusTitle)
            || !string.IsNullOrWhiteSpace(StatusBody);

        public StrategyBuildingHudChip GetChip(int index) =>
            index >= 0 && index < ChipCount ? chips[index] : default;

        public StrategyBuildingHudSection GetSection(int index) =>
            index >= 0 && index < SectionCount ? sections[index] : null;

        internal void Reset(StrategyBuildTool tool, bool construction = false)
        {
            Tool = tool;
            IsConstruction = construction;
            ChipCount = 0;
            SectionCount = 0;
            StatusTitle = string.Empty;
            StatusBody = string.Empty;
            StatusTone = StrategyBuildingHudTone.Neutral;
        }

        internal void AddChip(
            string key,
            string label,
            string value,
            Sprite icon,
            StrategyBuildingHudTone tone = StrategyBuildingHudTone.Neutral)
        {
            if (ChipCount >= chips.Length)
            {
                return;
            }

            chips[ChipCount++] = new StrategyBuildingHudChip(
                key,
                label,
                value,
                icon,
                tone);
        }

        internal StrategyBuildingHudSection AddSection(string key, string title)
        {
            if (SectionCount >= sections.Length)
            {
                return null;
            }

            StrategyBuildingHudSection section = sections[SectionCount++];
            section.Reset(key, title);
            return section;
        }

        internal void SetStatus(
            string title,
            string body,
            StrategyBuildingHudTone tone)
        {
            StatusTitle = title ?? string.Empty;
            StatusBody = body ?? string.Empty;
            StatusTone = tone;
        }
    }
}
