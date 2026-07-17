using NUnit.Framework;

namespace ProjectUnknown.Strategy.Tests
{
    public sealed class StrategyScoutExpeditionPolicyTests
    {
        [Test]
        public void PolicyExposesOneToSevenDayRange()
        {
            Assert.That(StrategyScoutExpeditionPolicy.MinimumDays, Is.EqualTo(1));
            Assert.That(StrategyScoutExpeditionPolicy.MaximumDays, Is.EqualTo(7));
            Assert.That(StrategyScoutExpeditionPolicy.DefaultDays, Is.EqualTo(1));
            Assert.That(StrategyScoutExpeditionPolicy.RationsPerDay, Is.EqualTo(1f));
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(4, true)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        public void SupportedDurationMatchesPolicyRange(int days, bool expected)
        {
            Assert.That(
                StrategyScoutExpeditionPolicy.IsSupportedDuration(days),
                Is.EqualTo(expected));
        }

        [TestCase(-3, 1)]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(12, 7)]
        public void DurationValuesClampToPolicyRange(int requestedDays, int expectedDays)
        {
            Assert.That(
                StrategyScoutExpeditionPolicy.ClampDurationDays(requestedDays),
                Is.EqualTo(expectedDays));
            Assert.That(
                StrategyScoutExpeditionPolicy.GetRequiredRations(requestedDays),
                Is.EqualTo(expectedDays * StrategyScoutExpeditionPolicy.RationsPerDay));
            Assert.That(
                StrategyScoutExpeditionPolicy.GetDurationSeconds(requestedDays),
                Is.EqualTo(expectedDays * StrategyDayNightCycleController.DayLengthSeconds));
        }
    }
}
