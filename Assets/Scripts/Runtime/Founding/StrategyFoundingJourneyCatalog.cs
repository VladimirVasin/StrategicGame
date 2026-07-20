using System;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyFoundingAtmosphere
    {
        Embers,
        Rain,
        Mist,
        Fireflies
    }

    internal enum StrategyFoundingShot
    {
        Departure,
        LongRoad,
        QuietValley,
        Council
    }

    internal readonly struct StrategyFoundingStoryPanel
    {
        public StrategyFoundingStoryPanel(
            string resourcePath,
            string chapter,
            string title,
            string body,
            StrategyFoundingAtmosphere atmosphere,
            StrategyFoundingShot shot)
        {
            ResourcePath = resourcePath;
            Chapter = chapter;
            Title = title;
            Body = body;
            Atmosphere = atmosphere;
            Shot = shot;
        }

        public string ResourcePath { get; }
        public string Chapter { get; }
        public string Title { get; }
        public string Body { get; }
        public StrategyFoundingAtmosphere Atmosphere { get; }
        public StrategyFoundingShot Shot { get; }
    }

    internal readonly struct StrategyFoundingAnswerOption
    {
        public StrategyFoundingAnswerOption(string id, string label, string description)
        {
            Id = id;
            Label = label;
            Description = description;
        }

        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
    }

    internal readonly struct StrategyFoundingQuestion
    {
        public StrategyFoundingQuestion(
            string id,
            string prompt,
            string context,
            StrategyFoundingAnswerOption[] options)
        {
            Id = id;
            Prompt = prompt;
            Context = context;
            Options = options ?? Array.Empty<StrategyFoundingAnswerOption>();
        }

        public string Id { get; }
        public string Prompt { get; }
        public string Context { get; }
        public StrategyFoundingAnswerOption[] Options { get; }
    }

    internal static class StrategyFoundingJourneyCatalog
    {
        public static StrategyFoundingStoryPanel[] StoryPanels =>
            new StrategyFoundingStoryPanel[]
        {
            new(
                "Visual/Founding/01_Departure",
                F("story.departure.chapter"),
                F("story.departure.title"),
                F("story.departure.body"),
                StrategyFoundingAtmosphere.Embers,
                StrategyFoundingShot.Departure),
            new(
                "Visual/Founding/02_LongRoad",
                F("story.long_road.chapter"),
                F("story.long_road.title"),
                F("story.long_road.body"),
                StrategyFoundingAtmosphere.Rain,
                StrategyFoundingShot.LongRoad),
            new(
                "Visual/Founding/03_QuietValley",
                F("story.quiet_valley.chapter"),
                F("story.quiet_valley.title"),
                F("story.quiet_valley.body"),
                StrategyFoundingAtmosphere.Mist,
                StrategyFoundingShot.QuietValley),
            new(
                "Visual/Founding/04_Council",
                F("story.council.chapter"),
                F("story.council.title"),
                F("story.council.body"),
                StrategyFoundingAtmosphere.Fireflies,
                StrategyFoundingShot.Council)
        };

        public static StrategyFoundingQuestion[] Questions =>
            new StrategyFoundingQuestion[]
        {
            new(
                StrategyFoundingChoiceIds.WaterQuestion,
                F("question.water.prompt"),
                F("question.water.context"),
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterRiver, F("answer.water.river.label"), F("answer.water.river.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterLake, F("answer.water.lake.label"), F("answer.water.lake.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterInland, F("answer.water.inland.label"), F("answer.water.inland.description"))
                }),
            new(
                StrategyFoundingChoiceIds.LandscapeQuestion,
                F("question.landscape.prompt"),
                F("question.landscape.context"),
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeForestEdge, F("answer.landscape.forest_edge.label"), F("answer.landscape.forest_edge.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeOpenMeadow, F("answer.landscape.open_meadow.label"), F("answer.landscape.open_meadow.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeMixed, F("answer.landscape.mixed.label"), F("answer.landscape.mixed.description"))
                }),
            new(
                StrategyFoundingChoiceIds.LivelihoodQuestion,
                F("question.livelihood.prompt"),
                F("question.livelihood.context"),
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodHunting, F("answer.livelihood.hunting.label"), F("answer.livelihood.hunting.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodFishing, F("answer.livelihood.fishing.label"), F("answer.livelihood.fishing.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodForaging, F("answer.livelihood.foraging.label"), F("answer.livelihood.foraging.description"))
                }),
            new(
                StrategyFoundingChoiceIds.PriorityQuestion,
                F("question.priority.prompt"),
                F("question.priority.context"),
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityConstruction, F("answer.priority.construction.label"), F("answer.priority.construction.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityResources, F("answer.priority.resources.label"), F("answer.priority.resources.description")),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityBalanced, F("answer.priority.balanced.label"), F("answer.priority.balanced.description"))
                })
        };

        private static string F(string key)
        {
            return StrategyLocalization.Get(StrategyLocalizationTables.Founding, key);
        }
    }
}
