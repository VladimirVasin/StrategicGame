using System;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyCityItemDefinition
    {
        public const int MaximumIdLength = 64;
        public const int MaximumQuantity = 1_000_000;

        private readonly string title;
        private readonly string description;
        private readonly string effectText;
        private readonly string localizationTable;
        private readonly string titleKey;
        private readonly string descriptionKey;
        private readonly string effectTextKey;

        public StrategyCityItemDefinition(
            string id,
            string title,
            int maxStack,
            string description = "",
            string effectText = "",
            string iconResourcePath = "",
            int sortOrder = 0,
            string localizationTable = "",
            string titleKey = "",
            string descriptionKey = "",
            string effectTextKey = "")
        {
            if (!IsValidId(id))
            {
                throw new ArgumentException(
                    "Item IDs must contain 1-64 lowercase ASCII letters, digits, dots, hyphens, or underscores, and must start with a letter or digit.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("An item title is required.", nameof(title));
            }

            if (maxStack <= 0 || maxStack > MaximumQuantity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxStack),
                    $"Maximum stack must be between 1 and {MaximumQuantity}.");
            }

            Id = id;
            this.title = title;
            MaxStack = maxStack;
            this.description = description ?? string.Empty;
            this.effectText = effectText ?? string.Empty;
            IconResourcePath = iconResourcePath ?? string.Empty;
            SortOrder = sortOrder;
            this.localizationTable = localizationTable ?? string.Empty;
            this.titleKey = titleKey ?? string.Empty;
            this.descriptionKey = descriptionKey ?? string.Empty;
            this.effectTextKey = effectTextKey ?? string.Empty;
        }

        public string Id { get; }
        public string Title => Resolve(title, titleKey);
        public int MaxStack { get; }
        public string Description => Resolve(description, descriptionKey);
        public string EffectText => Resolve(effectText, effectTextKey);
        public string IconResourcePath { get; }
        public int SortOrder { get; }

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaximumIdLength)
            {
                return false;
            }

            if (!IsAsciiLetterOrDigit(id[0]))
            {
                return false;
            }

            for (int index = 1; index < id.Length; index++)
            {
                char character = id[index];
                if (!IsAsciiLetterOrDigit(character)
                    && character != '.'
                    && character != '-'
                    && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
        }

        private string Resolve(string fallback, string key)
        {
            if (string.IsNullOrEmpty(localizationTable) || string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            string localized = StrategyLocalization.Get(localizationTable, key);
            return localized == key ? fallback : localized;
        }
    }
}
