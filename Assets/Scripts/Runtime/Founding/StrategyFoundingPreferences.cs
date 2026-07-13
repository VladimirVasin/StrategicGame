using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public static class StrategyFoundingChoiceIds
    {
        public const string WaterQuestion = "water";
        public const string LandscapeQuestion = "landscape";
        public const string LivelihoodQuestion = "livelihood";
        public const string PriorityQuestion = "priority";

        public const string WaterRiver = "water.river";
        public const string WaterLake = "water.lake";
        public const string WaterInland = "water.inland";
        public const string WaterNoPreference = "water.no_preference";

        public const string LandscapeForestEdge = "landscape.forest_edge";
        public const string LandscapeOpenMeadow = "landscape.open_meadow";
        public const string LandscapeMixed = "landscape.mixed";

        public const string LivelihoodHunting = "livelihood.hunting";
        public const string LivelihoodFishing = "livelihood.fishing";
        public const string LivelihoodForaging = "livelihood.foraging";
        public const string LivelihoodBalanced = "livelihood.balanced";

        public const string PriorityConstruction = "priority.construction";
        public const string PriorityResources = "priority.resources";
        public const string PriorityBalanced = "priority.balanced";

        public const string BalancedProfile = "founding.balanced";
        public const string CustomProfile = "founding.custom";

        public static bool IsKnownQuestion(string questionId)
        {
            return questionId == WaterQuestion
                || questionId == LandscapeQuestion
                || questionId == LivelihoodQuestion
                || questionId == PriorityQuestion;
        }

        public static bool IsKnownAnswer(string questionId, string answerId)
        {
            return questionId switch
            {
                WaterQuestion => IsWaterAnswer(answerId),
                LandscapeQuestion => IsLandscapeAnswer(answerId),
                LivelihoodQuestion => IsLivelihoodAnswer(answerId),
                PriorityQuestion => IsPriorityAnswer(answerId),
                _ => false
            };
        }

        public static bool IsWaterAnswer(string answerId)
        {
            return answerId == WaterRiver
                || answerId == WaterLake
                || answerId == WaterInland
                || answerId == WaterNoPreference;
        }

        public static bool IsLandscapeAnswer(string answerId)
        {
            return answerId == LandscapeForestEdge
                || answerId == LandscapeOpenMeadow
                || answerId == LandscapeMixed;
        }

        public static bool IsLivelihoodAnswer(string answerId)
        {
            return answerId == LivelihoodHunting
                || answerId == LivelihoodFishing
                || answerId == LivelihoodForaging
                || answerId == LivelihoodBalanced;
        }

        public static bool IsPriorityAnswer(string answerId)
        {
            return answerId == PriorityConstruction
                || answerId == PriorityResources
                || answerId == PriorityBalanced;
        }
    }

    public sealed class StrategyFoundingPreferences
    {
        public const int CurrentVersion = 1;

        private static readonly StrategyFoundingPreferences balanced = new StrategyFoundingPreferences(
            CurrentVersion,
            StrategyFoundingChoiceIds.BalancedProfile,
            StrategyFoundingChoiceIds.WaterNoPreference,
            StrategyFoundingChoiceIds.LandscapeMixed,
            StrategyFoundingChoiceIds.LivelihoodBalanced,
            StrategyFoundingChoiceIds.PriorityBalanced);

        private readonly string[] selectedOptionIds;

        public StrategyFoundingPreferences(
            string waterChoiceId,
            string landscapeChoiceId,
            string livelihoodChoiceId,
            string priorityChoiceId,
            string profileId = StrategyFoundingChoiceIds.CustomProfile,
            int version = CurrentVersion)
        {
            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            ValidateChoice(
                StrategyFoundingChoiceIds.WaterQuestion,
                waterChoiceId,
                StrategyFoundingChoiceIds.IsWaterAnswer);
            ValidateChoice(
                StrategyFoundingChoiceIds.LandscapeQuestion,
                landscapeChoiceId,
                StrategyFoundingChoiceIds.IsLandscapeAnswer);
            ValidateChoice(
                StrategyFoundingChoiceIds.LivelihoodQuestion,
                livelihoodChoiceId,
                StrategyFoundingChoiceIds.IsLivelihoodAnswer);
            ValidateChoice(
                StrategyFoundingChoiceIds.PriorityQuestion,
                priorityChoiceId,
                StrategyFoundingChoiceIds.IsPriorityAnswer);

            Version = version;
            ProfileId = string.IsNullOrWhiteSpace(profileId)
                ? StrategyFoundingChoiceIds.CustomProfile
                : profileId.Trim();
            WaterChoiceId = waterChoiceId;
            LandscapeChoiceId = landscapeChoiceId;
            LivelihoodChoiceId = livelihoodChoiceId;
            PriorityChoiceId = priorityChoiceId;
            selectedOptionIds = new[]
            {
                WaterChoiceId,
                LandscapeChoiceId,
                LivelihoodChoiceId,
                PriorityChoiceId
            };
            StableHash = CalculateStableHash();
        }

        private StrategyFoundingPreferences(
            int version,
            string profileId,
            string waterChoiceId,
            string landscapeChoiceId,
            string livelihoodChoiceId,
            string priorityChoiceId)
            : this(
                waterChoiceId,
                landscapeChoiceId,
                livelihoodChoiceId,
                priorityChoiceId,
                profileId,
                version)
        {
        }

        public static StrategyFoundingPreferences Balanced => balanced;
        public int Version { get; }
        public string ProfileId { get; }
        public string WaterChoiceId { get; }
        public string LandscapeChoiceId { get; }
        public string LivelihoodChoiceId { get; }
        public string PriorityChoiceId { get; }
        public int StableHash { get; }
        public IReadOnlyList<string> SelectedOptionIds => Array.AsReadOnly(selectedOptionIds);

        public static bool TryCreate(
            string waterChoiceId,
            string landscapeChoiceId,
            string livelihoodChoiceId,
            string priorityChoiceId,
            out StrategyFoundingPreferences preferences,
            string profileId = StrategyFoundingChoiceIds.CustomProfile,
            int version = CurrentVersion)
        {
            if (!StrategyFoundingChoiceIds.IsWaterAnswer(waterChoiceId)
                || !StrategyFoundingChoiceIds.IsLandscapeAnswer(landscapeChoiceId)
                || !StrategyFoundingChoiceIds.IsLivelihoodAnswer(livelihoodChoiceId)
                || !StrategyFoundingChoiceIds.IsPriorityAnswer(priorityChoiceId)
                || version <= 0)
            {
                preferences = null;
                return false;
            }

            preferences = new StrategyFoundingPreferences(
                waterChoiceId,
                landscapeChoiceId,
                livelihoodChoiceId,
                priorityChoiceId,
                profileId,
                version);
            return true;
        }

        public bool TryGetAnswerId(string questionId, out string answerId)
        {
            answerId = questionId switch
            {
                StrategyFoundingChoiceIds.WaterQuestion => WaterChoiceId,
                StrategyFoundingChoiceIds.LandscapeQuestion => LandscapeChoiceId,
                StrategyFoundingChoiceIds.LivelihoodQuestion => LivelihoodChoiceId,
                StrategyFoundingChoiceIds.PriorityQuestion => PriorityChoiceId,
                _ => null
            };
            return answerId != null;
        }

        private static void ValidateChoice(
            string questionId,
            string answerId,
            Func<string, bool> validator)
        {
            if (!validator(answerId))
            {
                throw new ArgumentException(
                    $"Unknown founding answer '{answerId ?? "<null>"}' for question '{questionId}'.",
                    nameof(answerId));
            }
        }

        private int CalculateStableHash()
        {
            unchecked
            {
                uint hash = 2166136261u;
                AddHash(ref hash, Version.ToString());
                AddHash(ref hash, ProfileId);
                AddHash(ref hash, WaterChoiceId);
                AddHash(ref hash, LandscapeChoiceId);
                AddHash(ref hash, LivelihoodChoiceId);
                AddHash(ref hash, PriorityChoiceId);
                return (int)hash;
            }
        }

        private static void AddHash(ref uint hash, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }

            hash ^= 0xffu;
            hash *= 16777619u;
        }
    }
}
