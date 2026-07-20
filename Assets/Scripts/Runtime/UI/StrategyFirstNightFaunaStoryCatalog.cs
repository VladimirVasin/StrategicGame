namespace ProjectUnknown.Strategy
{
    internal static class StrategyFirstNightFaunaStoryCatalog
    {
        public static StrategyFoundingStoryPanel[] Frames =>
            new StrategyFoundingStoryPanel[]
        {
            new(
                "Visual/FirstNightFauna/01_Rustle",
                S("first_night.frame.rustle.chapter"),
                S("first_night.frame.rustle.title"),
                S("first_night.frame.rustle.body"),
                StrategyFoundingAtmosphere.Mist,
                StrategyFoundingShot.LongRoad),
            new(
                "Visual/FirstNightFauna/02_MouseFeast",
                S("first_night.frame.mouse_feast.chapter"),
                S("first_night.frame.mouse_feast.title"),
                S("first_night.frame.mouse_feast.body"),
                StrategyFoundingAtmosphere.Embers,
                StrategyFoundingShot.Departure),
            new(
                "Visual/FirstNightFauna/03_QuietHunters",
                S("first_night.frame.quiet_hunters.chapter"),
                S("first_night.frame.quiet_hunters.title"),
                S("first_night.frame.quiet_hunters.body"),
                StrategyFoundingAtmosphere.Fireflies,
                StrategyFoundingShot.Council)
        };

        private static string S(string key)
        {
            return StrategyLocalization.Get(StrategyLocalizationTables.Stories, key);
        }
    }
}
