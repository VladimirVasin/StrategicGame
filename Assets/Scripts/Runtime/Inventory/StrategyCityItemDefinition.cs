using System;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyCityItemDefinition
    {
        public const int MaximumIdLength = 64;
        public const int MaximumQuantity = 1_000_000;

        public StrategyCityItemDefinition(
            string id,
            string title,
            int maxStack,
            string description = "",
            string effectText = "",
            string iconResourcePath = "",
            int sortOrder = 0)
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
            Title = title;
            MaxStack = maxStack;
            Description = description ?? string.Empty;
            EffectText = effectText ?? string.Empty;
            IconResourcePath = iconResourcePath ?? string.Empty;
            SortOrder = sortOrder;
        }

        public string Id { get; }
        public string Title { get; }
        public int MaxStack { get; }
        public string Description { get; }
        public string EffectText { get; }
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
    }
}
