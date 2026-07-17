using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyStoryPointOfInterestTests
    {
        private readonly List<GameObject> roots = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                }
            }

            roots.Clear();
        }

        [Test]
        public void ProductionStoryCatalogStartsWithTrashHeapEncounter()
        {
            StrategyStoryPointOfInterestCatalog catalog =
                StrategyStoryPointOfInterestCatalog.Production;

            Assert.That(catalog.Count, Is.EqualTo(1));
            StrategyStoryPointOfInterestDefinition definition = catalog.Definitions[0];
            Assert.That(definition.Id, Is.EqualTo(StrategyStoryPointOfInterestCatalog.TrashHeapId));
            Assert.That(definition.SequenceOrder, Is.Zero);
            Assert.That(
                definition.DistanceTier,
                Is.EqualTo(StrategyStoryPointOfInterestDistanceTier.Tier1Near));
            Assert.That(
                definition.EncounterId,
                Is.EqualTo(StrategyStoryPointOfInterestCatalog.TrashHeapEncounterId));
            Assert.That(definition.UnresolvedSpriteResourcePath, Does.EndWith("TrashHeap"));
            Assert.That(definition.ResolvedSpriteResourcePath, Does.EndWith("TrashHeapSearched"));
        }

        [Test]
        public void CatalogUsesExplicitSequenceAndRejectsAmbiguity()
        {
            StrategyStoryPointOfInterestDefinition second =
                new(
                    "story-second",
                    1,
                    StrategyStoryPointOfInterestDistanceTier.Tier2Middle,
                    "Second",
                    "Second body");
            StrategyStoryPointOfInterestDefinition first =
                new(
                    "story-first",
                    0,
                    StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                    "First",
                    "First body");
            StrategyStoryPointOfInterestCatalog catalog = new(new[] { second, first });

            Assert.That(catalog.Definitions[0], Is.SameAs(first));
            Assert.That(catalog.Definitions[1], Is.SameAs(second));
            Assert.Throws<ArgumentException>(() => new StrategyStoryPointOfInterestCatalog(
                new[]
                {
                    first,
                    new StrategyStoryPointOfInterestDefinition(
                        "story-other",
                        0,
                        StrategyStoryPointOfInterestDistanceTier.Tier3Far,
                        "Other",
                        "")
                }));
        }

        [Test]
        public void ActivationBandStaysOutsideMaximumDaylightVisibility()
        {
            const float visibleOuterRadius = 7f;

            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(49, visibleOuterRadius),
                Is.False);
            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(64, visibleOuterRadius),
                Is.True);
            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(100, visibleOuterRadius),
                Is.True);
            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(121, visibleOuterRadius),
                Is.False);
        }

        [TestCase(StrategyStoryPointOfInterestDistanceTier.Tier1Near, 18, 30)]
        [TestCase(StrategyStoryPointOfInterestDistanceTier.Tier2Middle, 45, 70)]
        [TestCase(StrategyStoryPointOfInterestDistanceTier.Tier3Far, 85, 120)]
        public void DistanceTierRangesUseExplicitNonOverlappingRouteBands(
            StrategyStoryPointOfInterestDistanceTier tier,
            int minimum,
            int maximum)
        {
            Assert.That(StrategyStoryPointOfInterestPlacement.IsInsideTier(tier, minimum), Is.True);
            Assert.That(StrategyStoryPointOfInterestPlacement.IsInsideTier(tier, maximum), Is.True);
            Assert.That(StrategyStoryPointOfInterestPlacement.IsInsideTier(tier, minimum - 1), Is.False);
            Assert.That(StrategyStoryPointOfInterestPlacement.IsInsideTier(tier, maximum + 1), Is.False);
        }

        [Test]
        public void TieredPlacementCreatesNearCandidatesAcrossAllDirections()
        {
            Vector2Int camp = new(64, 64);
            List<StrategyStoryPointOfInterestCandidatePlan> candidates =
                StrategyStoryPointOfInterestPlacement.SelectCandidates(
                    128,
                    128,
                    9173,
                    camp,
                    StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                    StrategyStoryPointOfInterestPlacement.Tier1MinimumCandidateCount,
                    _ => true,
                    _ => true);

            Assert.That(
                candidates,
                Has.Count.EqualTo(StrategyStoryPointOfInterestPlacement.Tier1MinimumCandidateCount));
            Assert.That(
                candidates,
                Has.All.Matches<StrategyStoryPointOfInterestCandidatePlan>(candidate =>
                    candidate.DistanceTier == StrategyStoryPointOfInterestDistanceTier.Tier1Near
                    && StrategyStoryPointOfInterestPlacement.IsInsideTier(
                        candidate.DistanceTier,
                        candidate.RouteSteps)));

            const int directionalSectorCount = 16;
            HashSet<int> sectors = new();
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int delta = candidates[i].Cell - camp;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                float sectorWidth = 360f / directionalSectorCount;
                float shifted = Mathf.Repeat(angle + 180f + sectorWidth * 0.5f, 360f);
                sectors.Add(Mathf.Clamp(
                    Mathf.FloorToInt(shifted / sectorWidth),
                    0,
                    directionalSectorCount - 1));
            }

            Assert.That(sectors, Has.Count.EqualTo(directionalSectorCount));
        }

        [Test]
        public void TieredCandidateOrderIsDeterministicForTheSameMapSeed()
        {
            Vector2Int camp = new(96, 96);
            List<StrategyStoryPointOfInterestCandidatePlan> first =
                StrategyStoryPointOfInterestPlacement.SelectCandidates(
                    192,
                    192,
                    7319,
                    camp,
                    StrategyStoryPointOfInterestDistanceTier.Tier2Middle,
                    StrategyStoryPointOfInterestPlacement.Tier2MinimumCandidateCount,
                    _ => true,
                    _ => true);
            List<StrategyStoryPointOfInterestCandidatePlan> second =
                StrategyStoryPointOfInterestPlacement.SelectCandidates(
                    192,
                    192,
                    7319,
                    camp,
                    StrategyStoryPointOfInterestDistanceTier.Tier2Middle,
                    StrategyStoryPointOfInterestPlacement.Tier2MinimumCandidateCount,
                    _ => true,
                    _ => true);

            Assert.That(second, Has.Count.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Cell, Is.EqualTo(first[i].Cell));
                Assert.That(second[i].RouteSteps, Is.EqualTo(first[i].RouteSteps));
                Assert.That(second[i].DistanceTier, Is.EqualTo(first[i].DistanceTier));
            }
        }

        [Test]
        public void RouteFieldUsesCardinalAndDiagonalTravelCostsWithoutCornerCutting()
        {
            Vector2Int camp = new(4, 4);
            int[,] open = StrategyStoryPointOfInterestRouteField.Build(
                16,
                16,
                camp,
                _ => true);

            Assert.That(open[5, 4], Is.EqualTo(10));
            Assert.That(open[5, 5], Is.EqualTo(14));
            Assert.That(open[6, 6], Is.EqualTo(28));

            Vector2Int blocker = new(5, 4);
            int[,] blocked = StrategyStoryPointOfInterestRouteField.Build(
                16,
                16,
                camp,
                cell => cell != blocker);

            Assert.That(blocked[5, 5], Is.EqualTo(20));
        }

        [Test]
        public void NearCandidatesCoverSixteenFirstOutwardScoutDirections()
        {
            Vector2Int camp = new(64, 64);
            List<StrategyStoryPointOfInterestCandidatePlan> candidates =
                StrategyStoryPointOfInterestPlacement.SelectCandidates(
                    128,
                    128,
                    14017,
                    camp,
                    StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                    StrategyStoryPointOfInterestPlacement.Tier1MinimumCandidateCount,
                    _ => true,
                    _ => true);

            const int directionCount = 16;
            string candidateCells = string.Join(
                ", ",
                candidates.ConvertAll(candidate => $"{candidate.Cell}:{candidate.RouteSteps}"));
            for (int direction = 0; direction < directionCount; direction++)
            {
                float angle = direction * Mathf.PI * 2f / directionCount;
                Vector2Int scoutCell = camp + new Vector2Int(
                    Mathf.RoundToInt(Mathf.Cos(angle) * 9f),
                    Mathf.RoundToInt(Mathf.Sin(angle) * 9f));
                bool covered = false;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector2Int delta = candidates[i].Cell - scoutCell;
                    long distanceSquared = delta.x * delta.x + delta.y * delta.y;
                    if (StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(
                        distanceSquared,
                        7f))
                    {
                        covered = true;
                        break;
                    }
                }

                Assert.That(
                    covered,
                    Is.True,
                    $"No Tier I candidate covered direction {direction}. Candidates: {candidateCells}");
            }
        }

        [Test]
        public void ActivationTieBreakIsStableAcrossScouts()
        {
            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsBetterCandidate(
                    true,
                    64,
                    "story-anchor-a",
                    9,
                    64,
                    "story-anchor-b",
                    2),
                Is.True);
            Assert.That(
                StrategyStoryPointOfInterestActivationPolicy.IsBetterCandidate(
                    true,
                    64,
                    "story-anchor-a",
                    3,
                    64,
                    "story-anchor-a",
                    9),
                Is.True);
        }

        [Test]
        public void LatentAnchorMaterializesCommitsAndResolvesForOneResident()
        {
            GameObject anchorObject = CreateRoot("Story Anchor");
            StrategyStoryPointOfInterestAnchor anchor =
                anchorObject.AddComponent<StrategyStoryPointOfInterestAnchor>();
            StrategyStoryPointOfInterestDefinition definition =
                new(
                    "story-first",
                    0,
                    StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                    "First",
                    "Body");
            anchor.Configure(
                null,
                new StrategyStoryPointOfInterestCatalog(new[] { definition }),
                "story-anchor-test",
                new Vector2Int(8, 8),
                StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                StrategyStoryPointOfInterestState.Latent,
                string.Empty,
                -1,
                0,
                null);
            StrategyResidentAgent resident = CreateRoot("Scout").AddComponent<StrategyResidentAgent>();

            Assert.That(anchor.TryCommit(definition, 0, resident), Is.True);
            Assert.That(anchor.State, Is.EqualTo(StrategyStoryPointOfInterestState.Committed));
            Assert.That(anchor.IsCommittedTo(resident), Is.True);
            Assert.That(anchor.MarkResolved(resident), Is.True);
            Assert.That(anchor.State, Is.EqualTo(StrategyStoryPointOfInterestState.Resolved));
        }

        [Test]
        public void LatentAnchorDoesNotBlockBuildingBeforeCommitment()
        {
            GameObject mapObject = CreateRoot("Story Placement Map");
            CityMapController map = mapObject.AddComponent<CityMapController>();
            ConfigureAllLandMap(map, 48, 48);
            Vector2Int cell = new(20, 20);
            StrategyStoryPointOfInterestDefinition definition = new(
                "story-first",
                0,
                StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                "First",
                "Body");
            StrategyStoryPointOfInterestAnchor anchor =
                CreateRoot("Latent Story Anchor").AddComponent<StrategyStoryPointOfInterestAnchor>();
            anchor.Configure(
                map,
                new StrategyStoryPointOfInterestCatalog(new[] { definition }),
                "story-anchor-buildability",
                cell,
                StrategyStoryPointOfInterestDistanceTier.Tier1Near,
                StrategyStoryPointOfInterestState.Latent,
                string.Empty,
                -1,
                0,
                null);

            Assert.That(map.IsCellBuildable(cell), Is.True);
            StrategyResidentAgent resident =
                CreateRoot("Scout").AddComponent<StrategyResidentAgent>();
            Assert.That(anchor.TryCommit(definition, 0, resident), Is.True);
            Assert.That(map.IsCellBuildable(cell), Is.False);
        }

        [Test]
        public void LegacyLatentSaveIsDiscardedAndRegeneratedAsTransientCandidates()
        {
            GameObject mapObject = CreateRoot("Story Restore Map");
            CityMapController map = mapObject.AddComponent<CityMapController>();
            ConfigureAllLandMap(map, 128, 128);
            StrategyFogOfWarController fog =
                CreateRoot("Story Restore Fog").AddComponent<StrategyFogOfWarController>();
            fog.Configure(map, null, null, null);
            StrategyStoryPointOfInterestController controller =
                CreateRoot("Story Restore Controller")
                    .AddComponent<StrategyStoryPointOfInterestController>();
            controller.Configure(
                map,
                fog,
                null,
                null,
                null,
                null,
                new Vector2Int(64, 64),
                StrategyStoryPointOfInterestCatalog.Production,
                false);

            controller.RestorePersistentState(
                new[]
                {
                    new StrategyStoryPointOfInterestSaveData
                    {
                        stableId = "legacy-latent",
                        cellX = 28,
                        cellY = 28,
                        state = (int)StrategyStoryPointOfInterestState.Latent,
                        definitionId = string.Empty,
                        sequenceIndex = -1
                    }
                },
                0);

            Assert.That(controller.Anchors, Is.Empty);
            Assert.That(
                controller.LatentCandidateCount,
                Is.GreaterThanOrEqualTo(
                    StrategyStoryPointOfInterestPlacement.Tier1MinimumCandidateCount));
            List<StrategyStoryPointOfInterestSaveData> captured = new();
            controller.CapturePersistentState(captured, out int nextSequenceIndex);
            Assert.That(captured, Is.Empty);
            Assert.That(nextSequenceIndex, Is.Zero);
            Assert.That(map.IsCellBuildable(new Vector2Int(28, 28)), Is.True);
        }

        [Test]
        public void ResourcePlanContainsOnlyFixedResourceDiscoveries()
        {
            List<StrategyPointOfInterestPlan> plans =
                StrategyPointOfInterestPlacement.SelectResourcePlans(
                    128,
                    128,
                    14017,
                    new Vector2Int(64, 64),
                    StrategyPointOfInterestPlacement.DefaultResourcePointCount,
                    _ => true,
                    _ => true);

            Assert.That(plans, Has.Count.EqualTo(StrategyPointOfInterestPlacement.DefaultResourcePointCount));
            Assert.That(plans, Has.All.Matches<StrategyPointOfInterestPlan>(plan => plan.HasMineralSite));
            Assert.That(
                plans,
                Has.None.Matches<StrategyPointOfInterestPlan>(
                    plan => plan.ResourceKind == StrategyPointOfInterestResourceKind.None));
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new(name);
            roots.Add(root);
            return root;
        }

        private static void ConfigureAllLandMap(CityMapController map, int width, int height)
        {
            CityMapCell[,] cells = new CityMapCell[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[x, y] = new CityMapCell(x, y, CityMapCellKind.Grass);
                }
            }

            SetPrivateField(map, "width", width);
            SetPrivateField(map, "height", height);
            SetPrivateField(map, "cellSize", 1f);
            SetPrivateField(map, "activeSeed", 101);
            SetPrivateField(map, "cells", cells);
            SetPrivateField(map, "blockedWalkCounts", new int[width, height]);
            SetPrivateField(map, "blockedBuildCounts", new int[width, height]);
            SetPrivateField(map, "bridgeWalkableCells", new bool[width, height]);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
