namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyLumberjackCamp
    {
        internal int HudMatureTreeCount =>
            forestry != null ? forestry.CountMatureTrees(Origin, WorkRadius) : 0;

        internal int HudGrowingTreeCount =>
            forestry != null ? forestry.CountGrowingTrees(Origin, WorkRadius) : 0;

        internal int HudProcessableWoodCount =>
            forestry != null ? forestry.CountProcessableWood(Origin, WorkRadius) : 0;
    }

    public sealed partial class StrategyStonecutterCamp
    {
        internal int HudAvailableDepositCount =>
            stone != null ? stone.CountAvailableDeposits(Origin, WorkRadius) : 0;
    }

    public sealed partial class StrategyMine
    {
        internal int HudAvailableDepositCount =>
            iron != null && building != null
                ? iron.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                : 0;
    }

    public sealed partial class StrategyCoalPit
    {
        internal int HudAvailableDepositCount
        {
            get
            {
                int count = coal != null && building != null
                    ? coal.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                    : 0;
                if (activeDeposit != null && !activeDeposit.IsDepleted)
                {
                    count++;
                }

                return count;
            }
        }
    }

    public sealed partial class StrategyClayPit
    {
        internal int HudAvailableDepositCount
        {
            get
            {
                int count = clay != null && building != null
                    ? clay.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                    : 0;
                if (activeDeposit != null && !activeDeposit.IsDepleted)
                {
                    count++;
                }

                return count;
            }
        }
    }

    public sealed partial class StrategyHunterCamp
    {
        internal int HudHuntableRabbitCount =>
            wildlife != null ? wildlife.CountHuntableRabbits(Origin, WorkRadius) : 0;

        internal int HudHuntableDeerCount =>
            CanHuntDeer && wildlife != null
                ? wildlife.CountHuntableDeer(Origin, WorkRadius)
                : 0;
    }

    public sealed partial class StrategyFisherHut
    {
        internal int HudCatchableFishCount =>
            wildlife != null ? wildlife.CountCatchableFish(Origin, WorkRadius) : 0;
    }

    public sealed partial class StrategySawmill
    {
        internal int HudIncomingLogs => reservedInputLogs;
    }

    public sealed partial class StrategyKiln
    {
        internal int HudIncomingClay => reservedInputClay;
        internal int HudIncomingCoal => reservedInputCoal;
    }

    public sealed partial class StrategyForge
    {
        internal int HudIncomingIron => reservedInputIron;
        internal int HudIncomingCoal => reservedInputCoal;
        internal int HudIncomingLogs => reservedInputLogs;
    }

    public sealed partial class StrategyForagerCamp
    {
        internal float HudStoredRations => GetStoredRations();
    }

    public sealed partial class StrategyGranary
    {
        internal void GetHudSourceCounts(
            out int gameSources,
            out int fishSources,
            out int eggSources,
            out int forageSources)
        {
            CountAvailableSources(out gameSources, out fishSources, out eggSources);
            forageSources = CountAvailableForagerSources();
        }
    }

    public sealed partial class StrategyScoutLodge
    {
        internal string HudMissionStatus => missionStatus;
    }
}
