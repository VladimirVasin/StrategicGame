using System;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyStartSiteSelectorTests
    {
        [Test]
        public void PreferencesExposeStableQuestionAnswersAndHash()
        {
            StrategyFoundingPreferences first = CreatePreferences(
                StrategyFoundingChoiceIds.WaterRiver,
                StrategyFoundingChoiceIds.LandscapeForestEdge);
            StrategyFoundingPreferences second = CreatePreferences(
                StrategyFoundingChoiceIds.WaterRiver,
                StrategyFoundingChoiceIds.LandscapeForestEdge);

            Assert.That(first.StableHash, Is.EqualTo(second.StableHash));
            Assert.That(
                first.TryGetAnswerId(StrategyFoundingChoiceIds.WaterQuestion, out string water),
                Is.True);
            Assert.That(water, Is.EqualTo(StrategyFoundingChoiceIds.WaterRiver));
            Assert.That(first.SelectedOptionIds, Has.Count.EqualTo(4));
            Assert.That(
                StrategyFoundingPreferences.TryCreate(
                    "water.unknown",
                    StrategyFoundingChoiceIds.LandscapeMixed,
                    StrategyFoundingChoiceIds.LivelihoodBalanced,
                    StrategyFoundingChoiceIds.PriorityBalanced,
                    out _),
                Is.False);
            Assert.Throws<ArgumentException>(() => new StrategyFoundingPreferences(
                "water.unknown",
                StrategyFoundingChoiceIds.LandscapeMixed,
                StrategyFoundingChoiceIds.LivelihoodBalanced,
                StrategyFoundingChoiceIds.PriorityBalanced));
        }

        [Test]
        public void SameSeedAndAnswersProduceSameLayout()
        {
            TestMapBuilder builder = new TestMapBuilder(48, 36, CityMapCellKind.Meadow);
            builder.SetWaterColumn(5, CityMapWaterKind.River, CityMapCellKind.Water);
            builder.SetWaterColumn(6, CityMapWaterKind.River, CityMapCellKind.Shore);
            StrategyStartSiteMapSnapshot map = builder.Build(72501);
            StrategyFoundingPreferences preferences = CreatePreferences(
                StrategyFoundingChoiceIds.WaterRiver,
                StrategyFoundingChoiceIds.LandscapeMixed);

            StrategyStarterLayout first = StrategyStartSiteSelector.Select(map, preferences);
            StrategyStarterLayout second = StrategyStartSiteSelector.Select(map, preferences);

            Assert.That(first.IsValid, Is.True);
            Assert.That(second.IsValid, Is.True);
            Assert.That(second.CampCell, Is.EqualTo(first.CampCell));
            Assert.That(second.CaravanOrigin, Is.EqualTo(first.CaravanOrigin));
            Assert.That(second.Diagnostics.Score, Is.EqualTo(first.Diagnostics.Score));
            Assert.That(second.FallbackLevel, Is.EqualTo(first.FallbackLevel));
        }

        [Test]
        public void RiverPreferenceSelectsCloserSafeSiteThanInlandPreference()
        {
            TestMapBuilder builder = new TestMapBuilder(56, 36, CityMapCellKind.Meadow);
            builder.SetWaterColumn(5, CityMapWaterKind.River, CityMapCellKind.Water);
            builder.SetWaterColumn(6, CityMapWaterKind.River, CityMapCellKind.Shore);
            StrategyStartSiteMapSnapshot map = builder.Build(41177);

            StrategyStarterLayout river = StrategyStartSiteSelector.Select(
                map,
                CreatePreferences(
                    StrategyFoundingChoiceIds.WaterRiver,
                    StrategyFoundingChoiceIds.LandscapeOpenMeadow));
            StrategyStarterLayout inland = StrategyStartSiteSelector.Select(
                map,
                CreatePreferences(
                    StrategyFoundingChoiceIds.WaterInland,
                    StrategyFoundingChoiceIds.LandscapeOpenMeadow));

            Assert.That(river.FallbackLevel, Is.EqualTo(StrategyStartSiteFallbackLevel.None));
            Assert.That(inland.FallbackLevel, Is.EqualTo(StrategyStartSiteFallbackLevel.None));
            Assert.That(
                river.Diagnostics.NearestRiverDistance,
                Is.GreaterThanOrEqualTo(StrategyStartSiteSelector.SafeWaterClearance));
            Assert.That(
                river.Diagnostics.NearestRiverDistance,
                Is.LessThan(inland.Diagnostics.NearestRiverDistance));
        }

        [Test]
        public void ForestEdgePreferenceSelectsMoreForestThanOpenMeadowPreference()
        {
            TestMapBuilder builder = new TestMapBuilder(60, 36, CityMapCellKind.Grass);
            builder.FillRect(0, 0, 20, 35, CityMapCellKind.Forest);
            StrategyStartSiteMapSnapshot map = builder.Build(91831);

            StrategyStarterLayout forestEdge = StrategyStartSiteSelector.Select(
                map,
                CreatePreferences(
                    StrategyFoundingChoiceIds.WaterNoPreference,
                    StrategyFoundingChoiceIds.LandscapeForestEdge));
            StrategyStarterLayout openMeadow = StrategyStartSiteSelector.Select(
                map,
                CreatePreferences(
                    StrategyFoundingChoiceIds.WaterNoPreference,
                    StrategyFoundingChoiceIds.LandscapeOpenMeadow));

            Assert.That(forestEdge.IsValid, Is.True);
            Assert.That(openMeadow.IsValid, Is.True);
            Assert.That(
                forestEdge.Diagnostics.ForestRatio,
                Is.GreaterThan(openMeadow.Diagnostics.ForestRatio));
            Assert.That(
                openMeadow.Diagnostics.OpenLandRatio,
                Is.GreaterThan(forestEdge.Diagnostics.OpenLandRatio));
        }

        [Test]
        public void PreferredLayoutReservesValidCaravanBlockAwayFromCamp()
        {
            TestMapBuilder builder = new TestMapBuilder(40, 32, CityMapCellKind.Grass);
            StrategyStartSiteMapSnapshot map = builder.Build(33809);

            StrategyStarterLayout layout = StrategyStartSiteSelector.Select(map);

            Assert.That(layout.IsValid, Is.True);
            Assert.That(layout.FallbackLevel, Is.EqualTo(StrategyStartSiteFallbackLevel.None));
            Assert.That(layout.HasCaravanReservation, Is.True);
            Assert.That(
                Contains(layout.CaravanOrigin, layout.CaravanReservedFootprint, layout.CampCell),
                Is.False);
            for (int y = 0; y < layout.CaravanReservedFootprint.y; y++)
            {
                for (int x = 0; x < layout.CaravanReservedFootprint.x; x++)
                {
                    StrategyStartSiteCell cell = map.GetCell(
                        layout.CaravanOrigin.x + x,
                        layout.CaravanOrigin.y + y);
                    Assert.That(cell.IsDryLand && cell.IsWalkable && cell.IsBuildable, Is.True);
                }
            }
        }

        [Test]
        public void DenseWaterMapUsesDiagnosedWaterClearanceFallback()
        {
            TestMapBuilder builder = new TestMapBuilder(28, 28, CityMapCellKind.Grass);
            for (int x = 4; x < 28; x += 5)
            {
                builder.SetWaterColumn(x, CityMapWaterKind.River, CityMapCellKind.Water);
            }

            StrategyStarterLayout layout = StrategyStartSiteSelector.Select(builder.Build(11003));

            Assert.That(layout.IsValid, Is.True);
            Assert.That(
                layout.FallbackLevel,
                Is.EqualTo(StrategyStartSiteFallbackLevel.RelaxedWaterClearance));
            Assert.That(layout.Diagnostics.DiagnosticCode, Is.EqualTo(StrategyStarterLayout.RelaxedWaterDiagnostic));
            Assert.That(layout.HasCaravanReservation, Is.True);
        }

        [Test]
        public void IsolatedLandUsesLegacyFallbackAndReportsMissingCaravanReservation()
        {
            TestMapBuilder builder = new TestMapBuilder(15, 15, CityMapCellKind.Water);
            builder.SetCell(7, 7, CityMapCellKind.Grass);

            StrategyStarterLayout layout = StrategyStartSiteSelector.Select(builder.Build(9001));

            Assert.That(layout.IsValid, Is.True);
            Assert.That(layout.CampCell, Is.EqualTo(new Vector2Int(7, 7)));
            Assert.That(layout.HasCaravanReservation, Is.False);
            Assert.That(layout.FallbackLevel, Is.EqualTo(StrategyStartSiteFallbackLevel.LegacyCenterOut));
            Assert.That(layout.Diagnostics.DiagnosticCode, Is.EqualTo(StrategyStarterLayout.LegacyDiagnostic));
        }

        [Test]
        public void AllWaterMapReturnsExplicitFailure()
        {
            StrategyStartSiteMapSnapshot map = new TestMapBuilder(
                16,
                16,
                CityMapCellKind.Water).Build(55);

            StrategyStarterLayout layout = StrategyStartSiteSelector.Select(map);

            Assert.That(layout.IsValid, Is.False);
            Assert.That(layout.FallbackLevel, Is.EqualTo(StrategyStartSiteFallbackLevel.NoValidSite));
            Assert.That(layout.Diagnostics.DiagnosticCode, Is.EqualTo(StrategyStarterLayout.NoValidSiteDiagnostic));
        }

        [Test]
        public void SnapshotDefensivelyCopiesSourceCells()
        {
            StrategyStartSiteCell grass = CreateCell(CityMapCellKind.Grass);
            StrategyStartSiteCell[] source = { grass, grass, grass, grass };
            StrategyStartSiteMapSnapshot snapshot = new StrategyStartSiteMapSnapshot(2, 2, 12, source);

            source[0] = CreateCell(CityMapCellKind.Water);

            Assert.That(snapshot.GetCell(0, 0).Kind, Is.EqualTo(CityMapCellKind.Grass));
        }

        [Test]
        public void FoundingStartStateRoundTripsChosenProfileAndLayout()
        {
            TestMapBuilder builder = new TestMapBuilder(48, 36, CityMapCellKind.Meadow);
            builder.SetWaterColumn(5, CityMapWaterKind.River, CityMapCellKind.Water);
            builder.SetWaterColumn(6, CityMapWaterKind.River, CityMapCellKind.Shore);
            StrategyFoundingPreferences preferences = new StrategyFoundingPreferences(
                StrategyFoundingChoiceIds.WaterRiver,
                StrategyFoundingChoiceIds.LandscapeForestEdge,
                StrategyFoundingChoiceIds.LivelihoodFishing,
                StrategyFoundingChoiceIds.PriorityResources);
            StrategyStarterLayout layout = StrategyStartSiteSelector.Select(
                builder.Build(61397),
                preferences);
            Assert.That(layout.IsValid, Is.True);

            GameObject source = new GameObject("FoundingStartStateSource");
            GameObject restoredObject = new GameObject("FoundingStartStateRestored");
            try
            {
                StrategyFoundingStartState state = source.AddComponent<StrategyFoundingStartState>();
                state.Configure(preferences, layout);
                Assert.That(state.IsRestoredFromSave, Is.False);
                StrategyFoundingStartSaveData data = state.CreateSaveData();

                StrategyFoundingStartState restored =
                    restoredObject.AddComponent<StrategyFoundingStartState>();
                restored.Configure(data);

                Assert.That(restored.IsRestoredFromSave, Is.True);
                Assert.That(restored.HasCampCell, Is.True);
                Assert.That(restored.CampCell, Is.EqualTo(layout.CampCell));
                Assert.That(restored.HasCaravanOrigin, Is.EqualTo(layout.HasCaravanReservation));
                Assert.That(restored.CaravanOrigin, Is.EqualTo(layout.CaravanOrigin));
                Assert.That(restored.Preferences.ProfileId, Is.EqualTo(preferences.ProfileId));
                Assert.That(restored.Preferences.WaterChoiceId, Is.EqualTo(preferences.WaterChoiceId));
                Assert.That(restored.Preferences.LandscapeChoiceId, Is.EqualTo(preferences.LandscapeChoiceId));
                Assert.That(restored.Preferences.LivelihoodChoiceId, Is.EqualTo(preferences.LivelihoodChoiceId));
                Assert.That(restored.Preferences.PriorityChoiceId, Is.EqualTo(preferences.PriorityChoiceId));
                Assert.That(data.answers, Has.Count.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(restoredObject);
            }
        }

        [Test]
        public void FoundingJourneyCatalogUsesKnownAnswersAndImportableStoryArt()
        {
            Assert.That(StrategyFoundingJourneyCatalog.StoryPanels, Has.Length.EqualTo(4));
            for (int i = 0; i < StrategyFoundingJourneyCatalog.StoryPanels.Length; i++)
            {
                StrategyFoundingStoryPanel panel = StrategyFoundingJourneyCatalog.StoryPanels[i];
                Assert.That(Resources.Load<Sprite>(panel.ResourcePath), Is.Not.Null, panel.ResourcePath);
            }

            Assert.That(StrategyFoundingJourneyCatalog.Questions, Has.Length.EqualTo(4));
            for (int i = 0; i < StrategyFoundingJourneyCatalog.Questions.Length; i++)
            {
                StrategyFoundingQuestion question = StrategyFoundingJourneyCatalog.Questions[i];
                Assert.That(StrategyFoundingChoiceIds.IsKnownQuestion(question.Id), Is.True, question.Id);
                Assert.That(question.Options, Has.Length.EqualTo(3), question.Id);
                for (int option = 0; option < question.Options.Length; option++)
                {
                    Assert.That(
                        StrategyFoundingChoiceIds.IsKnownAnswer(question.Id, question.Options[option].Id),
                        Is.True,
                        question.Options[option].Id);
                }
            }
        }

        private static StrategyFoundingPreferences CreatePreferences(
            string water,
            string landscape)
        {
            return new StrategyFoundingPreferences(
                water,
                landscape,
                StrategyFoundingChoiceIds.LivelihoodBalanced,
                StrategyFoundingChoiceIds.PriorityBalanced);
        }

        private static StrategyStartSiteCell CreateCell(
            CityMapCellKind kind,
            CityMapWaterKind waterKind = CityMapWaterKind.None)
        {
            bool dry = kind != CityMapCellKind.Water && kind != CityMapCellKind.Shore;
            return new StrategyStartSiteCell(kind, waterKind, dry, dry);
        }

        private static bool Contains(Vector2Int origin, Vector2Int footprint, Vector2Int cell)
        {
            return cell.x >= origin.x
                && cell.x < origin.x + footprint.x
                && cell.y >= origin.y
                && cell.y < origin.y + footprint.y;
        }

        private sealed class TestMapBuilder
        {
            private readonly int width;
            private readonly int height;
            private readonly StrategyStartSiteCell[] cells;

            public TestMapBuilder(int width, int height, CityMapCellKind fillKind)
            {
                this.width = width;
                this.height = height;
                cells = new StrategyStartSiteCell[width * height];
                StrategyStartSiteCell fill = CreateCell(
                    fillKind,
                    fillKind == CityMapCellKind.Water ? CityMapWaterKind.River : CityMapWaterKind.None);
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = fill;
                }
            }

            public void SetWaterColumn(int x, CityMapWaterKind waterKind, CityMapCellKind kind)
            {
                for (int y = 0; y < height; y++)
                {
                    SetCell(x, y, kind, waterKind);
                }
            }

            public void FillRect(
                int minX,
                int minY,
                int maxX,
                int maxY,
                CityMapCellKind kind)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        SetCell(x, y, kind);
                    }
                }
            }

            public void SetCell(
                int x,
                int y,
                CityMapCellKind kind,
                CityMapWaterKind waterKind = CityMapWaterKind.None)
            {
                cells[x + y * width] = CreateCell(kind, waterKind);
            }

            public StrategyStartSiteMapSnapshot Build(int seed)
            {
                return new StrategyStartSiteMapSnapshot(width, height, seed, cells);
            }
        }
    }
}
