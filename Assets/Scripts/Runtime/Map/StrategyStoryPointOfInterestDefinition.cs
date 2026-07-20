using System;

namespace ProjectUnknown.Strategy
{
    public enum StrategyStoryPointOfInterestDistanceTier
    {
        Tier1Near = 1,
        Tier2Middle = 2,
        Tier3Far = 3
    }

    public sealed class StrategyStoryPointOfInterestDefinition
    {
        public const int MaximumIdLength = 96;

        private readonly string title;
        private readonly string body;
        private readonly string localizationTable;
        private readonly string titleKey;
        private readonly string bodyKey;

        public StrategyStoryPointOfInterestDefinition(
            string id,
            int sequenceOrder,
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            string title,
            string body,
            string encounterId = "",
            string unresolvedSpriteResourcePath = "",
            string resolvedSpriteResourcePath = "",
            string localizationTable = "",
            string titleKey = "",
            string bodyKey = "")
        {
            if (!IsValidId(id))
            {
                throw new ArgumentException(
                    "Story point IDs must contain 1-96 lowercase ASCII letters, digits, dots, hyphens, or underscores, and must start with a letter or digit.",
                    nameof(id));
            }

            if (sequenceOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceOrder));
            }

            if (!Enum.IsDefined(typeof(StrategyStoryPointOfInterestDistanceTier), distanceTier))
            {
                throw new ArgumentOutOfRangeException(nameof(distanceTier));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A story point title is required.", nameof(title));
            }

            Id = id;
            SequenceOrder = sequenceOrder;
            DistanceTier = distanceTier;
            this.title = title;
            this.body = body ?? string.Empty;
            EncounterId = encounterId ?? string.Empty;
            UnresolvedSpriteResourcePath = unresolvedSpriteResourcePath ?? string.Empty;
            ResolvedSpriteResourcePath = resolvedSpriteResourcePath ?? string.Empty;
            this.localizationTable = localizationTable ?? string.Empty;
            this.titleKey = titleKey ?? string.Empty;
            this.bodyKey = bodyKey ?? string.Empty;
        }

        public string Id { get; }
        public int SequenceOrder { get; }
        public StrategyStoryPointOfInterestDistanceTier DistanceTier { get; }
        public string Title => Resolve(title, titleKey);
        public string Body => Resolve(body, bodyKey);
        public string EncounterId { get; }
        public string UnresolvedSpriteResourcePath { get; }
        public string ResolvedSpriteResourcePath { get; }

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaximumIdLength)
            {
                return false;
            }

            for (int i = 0; i < id.Length; i++)
            {
                char character = id[i];
                bool valid = character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || i > 0 && (character == '.' || character == '-' || character == '_');
                if (!valid)
                {
                    return false;
                }
            }

            return true;
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
