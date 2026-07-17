namespace ProjectUnknown.Strategy
{
    internal static class StrategyFirstNightFaunaStoryCatalog
    {
        public static readonly StrategyFoundingStoryPanel[] Frames =
        {
            new(
                "Visual/FirstNightFauna/01_Rustle",
                "THE FIRST DUSK",
                "A rustle beneath the grain.",
                "At first it was only a whisper under the sacks. Then a grey shape slipped between the crates, and another followed it into the dark.",
                StrategyFoundingAtmosphere.Mist,
                StrategyFoundingShot.LongRoad),
            new(
                "Visual/FirstNightFauna/02_MouseFeast",
                "THE MIDNIGHT FEAST",
                "Tiny teeth found our stores.",
                "By moonrise the mice were everywhere. They gnawed at grain, rooted through baskets, and celebrated our first harvest as if we had gathered it for them.",
                StrategyFoundingAtmosphere.Embers,
                StrategyFoundingShot.Departure),
            new(
                "Visual/FirstNightFauna/03_QuietHunters",
                "QUIET HUNTERS",
                "Then green eyes opened by the fire.",
                "A lean cat stepped from the firelight, silent and entirely at home. The mice had found our stores, but their hunters had found us too. We would be all right.",
                StrategyFoundingAtmosphere.Fireflies,
                StrategyFoundingShot.Council)
        };
    }
}
