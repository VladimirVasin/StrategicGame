namespace ProjectUnknown.Strategy
{
    internal readonly struct StrategyResidentPerformanceSnapshot
    {
        public StrategyResidentPerformanceSnapshot(
            long pathRequests,
            long pathSuccesses,
            long pathFailures,
            long pathBudgetDeferrals,
            long scheduledDecisionRuns,
            long scheduledDecisionDeferrals)
        {
            PathRequests = pathRequests;
            PathSuccesses = pathSuccesses;
            PathFailures = pathFailures;
            PathBudgetDeferrals = pathBudgetDeferrals;
            ScheduledDecisionRuns = scheduledDecisionRuns;
            ScheduledDecisionDeferrals = scheduledDecisionDeferrals;
        }

        public long PathRequests { get; }
        public long PathSuccesses { get; }
        public long PathFailures { get; }
        public long PathBudgetDeferrals { get; }
        public long ScheduledDecisionRuns { get; }
        public long ScheduledDecisionDeferrals { get; }
    }

    internal static class StrategyResidentPerformanceCounters
    {
        private static long pathRequests;
        private static long pathSuccesses;
        private static long pathFailures;
        private static long pathBudgetDeferrals;
        private static long scheduledDecisionRuns;
        private static long scheduledDecisionDeferrals;

        public static void RecordPathRequest()
        {
            pathRequests++;
        }

        public static void RecordPathResult(bool success)
        {
            if (success)
            {
                pathSuccesses++;
            }
            else
            {
                pathFailures++;
            }
        }

        public static void RecordPathBudgetDeferral()
        {
            pathBudgetDeferrals++;
        }

        public static void RecordScheduledDecisionRun()
        {
            scheduledDecisionRuns++;
        }

        public static void RecordScheduledDecisionDeferral()
        {
            scheduledDecisionDeferrals++;
        }

        public static StrategyResidentPerformanceSnapshot Capture()
        {
            return new StrategyResidentPerformanceSnapshot(
                pathRequests,
                pathSuccesses,
                pathFailures,
                pathBudgetDeferrals,
                scheduledDecisionRuns,
                scheduledDecisionDeferrals);
        }
    }
}
