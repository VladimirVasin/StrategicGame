using System;
using System.Collections.Generic;
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
                definition.EncounterId,
                Is.EqualTo(StrategyStoryPointOfInterestCatalog.TrashHeapEncounterId));
            Assert.That(definition.UnresolvedSpriteResourcePath, Does.EndWith("TrashHeap"));
            Assert.That(definition.ResolvedSpriteResourcePath, Does.EndWith("TrashHeapSearched"));
        }

        [Test]
        public void CatalogUsesExplicitSequenceAndRejectsAmbiguity()
        {
            StrategyStoryPointOfInterestDefinition second =
                new("story-second", 1, "Second", "Second body");
            StrategyStoryPointOfInterestDefinition first =
                new("story-first", 0, "First", "First body");
            StrategyStoryPointOfInterestCatalog catalog = new(new[] { second, first });

            Assert.That(catalog.Definitions[0], Is.SameAs(first));
            Assert.That(catalog.Definitions[1], Is.SameAs(second));
            Assert.Throws<ArgumentException>(() => new StrategyStoryPointOfInterestCatalog(
                new[] { first, new StrategyStoryPointOfInterestDefinition("story-other", 0, "Other", "") }));
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
                Is.False);
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
                new("story-first", 0, "First", "Body");
            anchor.Configure(
                null,
                new StrategyStoryPointOfInterestCatalog(new[] { definition }),
                "story-anchor-test",
                new Vector2Int(8, 8),
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
    }
}
