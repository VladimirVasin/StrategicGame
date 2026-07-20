using System;
using System.Linq;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyLocalizationFoundationTests
    {
        [TestCase("ru", StrategyGameLanguage.Russian)]
        [TestCase("ru-RU", StrategyGameLanguage.Russian)]
        [TestCase("en", StrategyGameLanguage.English)]
        [TestCase("en-US", StrategyGameLanguage.English)]
        [TestCase("de", StrategyGameLanguage.Russian)]
        [TestCase(null, StrategyGameLanguage.Russian)]
        public void LocaleCodeMapping_UsesRussianAsTheSafeDefault(
            string code,
            StrategyGameLanguage expected)
        {
            Assert.That(StrategyLocalization.FromLocaleCode(code), Is.EqualTo(expected));
        }

        [Test]
        public void LegacyKey_IsStableForExactEnglishSource()
        {
            Assert.That(
                StrategyLocalization.GetLegacyKey("Continue"),
                Is.EqualTo("literal.cbc4841825bc1ab4"));
            Assert.That(
                StrategyLocalization.GetLegacyKey("continue"),
                Is.Not.EqualTo(StrategyLocalization.GetLegacyKey("Continue")));
        }

        [Test]
        public void TableCatalog_HasNineUniqueOfficialCollections()
        {
            Assert.That(StrategyLocalizationTables.All, Has.Length.EqualTo(9));
            Assert.That(
                StrategyLocalizationTables.All.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(StrategyLocalizationTables.All.Length));
        }
    }
}
