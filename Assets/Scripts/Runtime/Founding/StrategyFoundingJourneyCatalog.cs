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
        public static readonly StrategyFoundingStoryPanel[] StoryPanels =
        {
            new(
                "Visual/Founding/01_Departure",
                "THE LAST NIGHT",
                "We left before dawn.",
                "War had reached every road behind us. We took only what the cart could carry and promised the children we would find a place beyond its reach.",
                StrategyFoundingAtmosphere.Embers,
                StrategyFoundingShot.Departure),
            new(
                "Visual/Founding/02_LongRoad",
                "THE LONG ROAD",
                "The road asked everything of us.",
                "Weeks passed beneath rain and cold stone. We crossed the high paths together, following no banner and serving no lord.",
                StrategyFoundingAtmosphere.Rain,
                StrategyFoundingShot.LongRoad),
            new(
                "Visual/Founding/03_QuietValley",
                "A QUIET VALLEY",
                "At last, the horizon was silent.",
                "No smoke. No marching columns. Only water, forest and open land. For the first time, the road ahead belonged to us.",
                StrategyFoundingAtmosphere.Mist,
                StrategyFoundingShot.QuietValley),
            new(
                "Visual/Founding/04_Council",
                "THE FIRST DECISION",
                "Safety was only the beginning.",
                "Before unloading the cart, the families gathered around the map. Where we stop tonight will shape every winter that follows.",
                StrategyFoundingAtmosphere.Fireflies,
                StrategyFoundingShot.Council)
        };

        public static readonly StrategyFoundingQuestion[] Questions =
        {
            new(
                StrategyFoundingChoiceIds.WaterQuestion,
                "What landmark did the scouts follow?",
                "Water shapes travel, food and the first routes through the valley.",
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterRiver, "A running river", "Safe ground within reach of a river."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterLake, "A quiet lake", "Sheltered water and reliable fishing access."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.WaterInland, "High, dry ground", "More distance from shore and floodwater.")
                }),
            new(
                StrategyFoundingChoiceIds.LandscapeQuestion,
                "What kind of shelter did they seek?",
                "The land around camp determines visibility and room to grow.",
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeForestEdge, "The forest edge", "Timber and cover beside open ground."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeOpenMeadow, "An open meadow", "Clear sightlines and easier construction."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LandscapeMixed, "A little of both", "A varied landscape without one strong bias.")
                }),
            new(
                StrategyFoundingChoiceIds.LivelihoodQuestion,
                "What should feed the first winter?",
                "The scouts marked the places most suited to an early livelihood.",
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodHunting, "Game trails", "Meadow and forest-edge habitat for hunting."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodFishing, "Fishing water", "Reachable water with better fishing potential."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.LivelihoodForaging, "Wild harvest", "Mixed forest and meadow for gathering.")
                }),
            new(
                StrategyFoundingChoiceIds.PriorityQuestion,
                "What matters most when the cart stops?",
                "Every site remains playable; this choice breaks close decisions.",
                new[]
                {
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityConstruction, "Room to build", "Favor open, buildable ground."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityResources, "Nearby resources", "Favor varied terrain and resource opportunity."),
                    new StrategyFoundingAnswerOption(StrategyFoundingChoiceIds.PriorityBalanced, "A balanced beginning", "Keep construction and resources in balance.")
                })
        };
    }
}
