using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyBuildingVisualAnchorProfileTests
    {
        private const float Epsilon = 0.001f;
        private const float PlacementAnchorY = 0.20f;

        private static IEnumerable<TestCaseData> StockAnchorCases()
        {
            Bounds twoByTwo = BoundsFor(2f, 2f);
            Bounds threeByTwo = BoundsFor(3f, 2f);
            Bounds fourByFour = BoundsFor(4f, 4f);

            yield return StockCase(StrategyBuildTool.LumberjackCamp, StrategyResourceType.Logs, twoByTwo, 0.72f, -0.66f, -0.13f);
            yield return StockCase(StrategyBuildTool.StonecutterCamp, StrategyResourceType.Stone, twoByTwo, 0.70f, -0.66f, -0.13f);
            yield return StockCase(StrategyBuildTool.Mine, StrategyResourceType.Iron, twoByTwo, 0.74f, -0.68f, -0.13f);
            yield return StockCase(StrategyBuildTool.CoalPit, StrategyResourceType.Coal, twoByTwo, 0.74f, -0.70f, -0.13f);
            yield return StockCase(StrategyBuildTool.ClayPit, StrategyResourceType.Clay, twoByTwo, 0.72f, -0.69f, -0.13f);
            yield return StockCase(StrategyBuildTool.HunterCamp, StrategyResourceType.Game, twoByTwo, 0.70f, -0.65f, -0.13f);
            yield return StockCase(StrategyBuildTool.FisherHut, StrategyResourceType.Fish, twoByTwo, 0.75f, -0.62f, -0.13f);
            yield return StockCase(StrategyBuildTool.ForagerCamp, StrategyResourceType.Berries, twoByTwo, 0.54f, -0.38f, -0.13f);
            yield return StockCase(StrategyBuildTool.ChickenCoop, StrategyResourceType.Eggs, fourByFour, 1.64f, -1.64f, -0.13f);
            yield return StockCase(StrategyBuildTool.Sawmill, StrategyResourceType.Logs, threeByTwo, -1.04f, -0.64f, -0.14f);
            yield return StockCase(StrategyBuildTool.Sawmill, StrategyResourceType.Planks, threeByTwo, 1.08f, -0.66f, -0.13f);
            yield return StockCase(StrategyBuildTool.Kiln, StrategyResourceType.Clay, twoByTwo, -0.70f, -0.67f, -0.14f);
            yield return StockCase(StrategyBuildTool.Kiln, StrategyResourceType.Coal, twoByTwo, 0.70f, -0.69f, -0.14f);
            yield return StockCase(StrategyBuildTool.Kiln, StrategyResourceType.Pottery, twoByTwo, 0.42f, -0.48f, -0.13f);
            yield return StockCase(StrategyBuildTool.Forge, StrategyResourceType.Iron, twoByTwo, -0.68f, -0.68f, -0.14f);
            yield return StockCase(StrategyBuildTool.Forge, StrategyResourceType.Coal, twoByTwo, 0.70f, -0.69f, -0.14f);
            yield return StockCase(StrategyBuildTool.Forge, StrategyResourceType.Logs, twoByTwo, -0.76f, -0.48f, -0.13f);
            yield return StockCase(StrategyBuildTool.Forge, StrategyResourceType.Tools, twoByTwo, 0.44f, -0.46f, -0.13f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Logs, threeByTwo, 0.28f, -0.55f, -0.16f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Stone, threeByTwo, -0.86f, -0.63f, -0.155f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Iron, threeByTwo, 0.82f, -0.66f, -0.15f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Coal, threeByTwo, 0.18f, -0.72f, -0.145f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Clay, threeByTwo, 1.34f, -0.70f, -0.145f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Pottery, threeByTwo, 1.16f, -0.42f, -0.146f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Planks, threeByTwo, 0.94f, -0.61f, -0.148f);
            yield return StockCase(StrategyBuildTool.StorageYard, StrategyResourceType.Tools, threeByTwo, 0.64f, -0.40f, -0.147f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Game, threeByTwo, -1.08f, -0.65f, -0.13f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Fish, threeByTwo, 1.08f, -0.63f, -0.13f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Eggs, threeByTwo, 0f, -0.68f, -0.13f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Berries, threeByTwo, 1.08f, -0.63f, -0.13f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Roots, threeByTwo, 1.08f, -0.63f, -0.13f);
            yield return StockCase(StrategyBuildTool.Granary, StrategyResourceType.Mushrooms, threeByTwo, 1.08f, -0.63f, -0.13f);
        }

        private static IEnumerable<TestCaseData> TorchAnchorCases()
        {
            yield return TorchCase(StrategyBuildTool.LumberjackCamp, 2f, 2f, 1.30f, -0.40f);
            yield return TorchCase(StrategyBuildTool.StonecutterCamp, 2f, 2f, 1.28f, -0.44f);
            yield return TorchCase(StrategyBuildTool.Sawmill, 3f, 2f, -1.98f, -0.32f);
            yield return TorchCase(StrategyBuildTool.Mine, 2f, 2f, -1.28f, -0.40f);
            yield return TorchCase(StrategyBuildTool.CoalPit, 2f, 2f, 1.28f, -0.36f);
            yield return TorchCase(StrategyBuildTool.ClayPit, 2f, 2f, -1.30f, -0.38f);
            yield return TorchCase(StrategyBuildTool.Kiln, 2f, 2f, 1.28f, -0.38f);
            yield return TorchCase(StrategyBuildTool.Forge, 2f, 2f, 1.32f, -0.32f);
            yield return TorchCase(StrategyBuildTool.HunterCamp, 2f, 2f, -1.32f, -0.38f);
            yield return TorchCase(StrategyBuildTool.FisherHut, 2f, 2f, 1.30f, -0.40f);
            yield return TorchCase(StrategyBuildTool.ForagerCamp, 2f, 2f, 1.20f, 0.42f, -0.22f);
            yield return TorchCase(StrategyBuildTool.ChickenCoop, 4f, 4f, 0.95f, -0.95f);
            yield return TorchCase(StrategyBuildTool.StorageYard, 3f, 2f, -1.95f, -0.34f);
            yield return TorchCase(StrategyBuildTool.Granary, 3f, 2f, 1.95f, -0.36f);
            yield return TorchCase(StrategyBuildTool.TradingPost, 3f, 2f, 1.08f, -0.38f);
            yield return TorchCase(StrategyBuildTool.StarterCaravanCart, 3f, 2f, -1.00f, -0.28f);
        }

        private static IEnumerable<SpriteDensityCase> SpriteDensityCases()
        {
            yield return new(StrategyBuildTool.LumberjackCamp, StrategyResourceType.Logs, 2f, 2f, 80, 76, 24f, 0.10f);
            yield return new(StrategyBuildTool.StonecutterCamp, StrategyResourceType.Stone, 2f, 2f, 80, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.Mine, StrategyResourceType.Iron, 2f, 2f, 84, 76, 24f, 0.10f);
            yield return new(StrategyBuildTool.CoalPit, StrategyResourceType.Coal, 2f, 2f, 84, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.ClayPit, StrategyResourceType.Clay, 2f, 2f, 92, 70, 24f, 0.10f);
            yield return new(StrategyBuildTool.HunterCamp, StrategyResourceType.Game, 2f, 2f, 80, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.FisherHut, StrategyResourceType.Fish, 2f, 2f, 84, 76, 24f, 0.10f);
            yield return new(StrategyBuildTool.ForagerCamp, StrategyResourceType.Berries, 2f, 2f, 88, 58, 24f, 0.20f);
            yield return new(StrategyBuildTool.ChickenCoop, StrategyResourceType.Eggs, 4f, 4f, 46, 46, 21f, 0.10f);
            yield return new(StrategyBuildTool.Sawmill, StrategyResourceType.Logs, 3f, 2f, 100, 78, 24f, 0.10f);
            yield return new(StrategyBuildTool.Kiln, StrategyResourceType.Clay, 2f, 2f, 92, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.Forge, StrategyResourceType.Iron, 2f, 2f, 92, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.StorageYard, StrategyResourceType.Logs, 3f, 2f, 96, 74, 24f, 0.10f);
            yield return new(StrategyBuildTool.Granary, StrategyResourceType.Game, 3f, 2f, 96, 84, 24f, 0.10f);
            yield return new(StrategyBuildTool.TradingPost, StrategyResourceType.None, 3f, 2f, 96, 60, 24f, 0.20f);
            yield return new(StrategyBuildTool.StarterCaravanCart, StrategyResourceType.None, 3f, 2f, 64, 36, 24f, 0.10f);
        }

        [TestCaseSource(nameof(StockAnchorCases))]
        public void StockAndDepositEffectsShareFootprintAnchors(
            StrategyBuildTool tool,
            StrategyResourceType resource,
            Bounds bounds,
            Vector3 expected)
        {
            Vector3 actual = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(tool, resource, bounds);
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Epsilon));
        }

        [TestCaseSource(nameof(TorchAnchorCases))]
        public void RuntimeLightsStayAttachedToAuthoredBuildingGeometry(
            StrategyBuildTool tool,
            Bounds bounds,
            Vector3 expected)
        {
            Vector3 actual = StrategyBuildingVisualAnchorProfile.GetTorchAnchorWorld(tool, bounds);
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Epsilon));
        }

        [Test]
        public void MineCoalClayAndOpenProductionWorkAnchorsStayInAuthoredWorkAreas()
        {
            Bounds twoByTwo = BoundsFor(2f, 2f);
            Bounds threeByTwo = BoundsFor(3f, 2f);

            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetMineEntranceEffectWorld(twoByTwo), -0.22f, -0.60f, -0.12f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.CoalPit, twoByTwo, 0, 2), -0.27f, -0.24f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.CoalPit, twoByTwo, 1, 2), 0.27f, -0.14f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.ClayPit, twoByTwo, 0, 2), -0.25f, -0.22f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.ClayPit, twoByTwo, 1, 2), 0.25f, -0.12f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.Sawmill, threeByTwo, 0, 2), -0.34f, -0.12f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.Sawmill, threeByTwo, 1, 2), 0.34f, -0.12f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(StrategyBuildTool.Sawmill, threeByTwo), 0f, -0.10f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.Kiln, twoByTwo, 0, 1), -0.24f, -0.16f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(StrategyBuildTool.Kiln, twoByTwo), 0.16f, -0.12f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(StrategyBuildTool.Forge, twoByTwo, 0, 1), -0.18f, -0.08f, -0.08f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(StrategyBuildTool.Forge, twoByTwo), 0.10f, -0.14f, -0.08f);

            int buildingOrder = StrategyWorldSorting.ForWorldY(twoByTwo.min.y + PlacementAnchorY);
            foreach (StrategyBuildTool tool in new[] { StrategyBuildTool.CoalPit, StrategyBuildTool.ClayPit })
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    Vector3 worker = StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(
                        tool,
                        twoByTwo,
                        slot,
                        2);
                    int workerOrder = StrategyWorldSorting.ForPosition(
                        worker,
                        StrategyBuildingVisualAnchorProfile.GetInteriorWorkerSortingOffset(slot));
                    Assert.That(workerOrder, Is.GreaterThan(buildingOrder), $"{tool} worker must remain visible inside the open pit");
                }
            }
        }

        [Test]
        public void IndustrialCinematicAnchorsStayInAuthoredEffectRegions()
        {
            Bounds twoByTwo = BoundsFor(2f, 2f);
            Bounds threeByTwo = BoundsFor(3f, 2f);

            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.Mine, twoByTwo), -0.16f, -0.56f, -0.22f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.CoalPit, twoByTwo), 0f, -0.30f, -0.22f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.Kiln, twoByTwo), 0f, -0.32f, -0.22f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.Forge, twoByTwo), 0.10f, -0.24f, -0.22f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.StorageYard, threeByTwo), -0.72f, -0.16f, -0.22f);
            AssertAnchor(StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(StrategyBuildTool.Granary, threeByTwo), 0f, -0.08f, -0.22f);
        }

        [TestCaseSource(nameof(SpriteDensityCases))]
        public void DoubledSpriteDensityPreservesWorldAttachmentPoints(SpriteDensityCase spec)
        {
            Bounds bounds = BoundsFor(spec.FootprintWidth, spec.FootprintHeight);
            List<Vector3> anchors = new()
            {
                StrategyBuildingVisualAnchorProfile.GetTorchAnchorWorld(spec.Tool, bounds),
                StrategyBuildingVisualAnchorProfile.GetCinematicAnchorWorld(spec.Tool, bounds)
            };
            if (spec.Resource != StrategyResourceType.None)
            {
                anchors.Add(StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(spec.Tool, spec.Resource, bounds));
            }

            foreach (Vector3 anchor in anchors)
            {
                Vector2 legacyPixels = ToSpritePixels(anchor, bounds, spec.Width, spec.Height, spec.PixelsPerUnit, spec.PivotY);
                Vector2 authoredPixels = ToSpritePixels(anchor, bounds, spec.Width * 2, spec.Height * 2, spec.PixelsPerUnit * 2f, spec.PivotY);
                Assert.That(
                    Vector2.Distance(authoredPixels, legacyPixels * 2f),
                    Is.LessThan(Epsilon),
                    $"{spec.Tool} attachment moved when sprite density doubled");
            }
        }

        [Test]
        public void CompactBuildingTorchAnchorsStayInsideTheirSpriteWorldWidths()
        {
            Bounds chickenBounds = BoundsFor(4f, 4f);
            Vector3 chicken = StrategyBuildingVisualAnchorProfile.GetTorchAnchorWorld(StrategyBuildTool.ChickenCoop, chickenBounds);
            Assert.That(Mathf.Abs(chicken.x - chickenBounds.center.x), Is.LessThan(46f / 21f * 0.5f));
            AssertPixels(ToSpritePixels(chicken, chickenBounds, 46, 46, 21f, 0.10f), 42.95f, 22.45f);

            Bounds tradingBounds = BoundsFor(3f, 2f);
            Vector3 trading = StrategyBuildingVisualAnchorProfile.GetTorchAnchorWorld(StrategyBuildTool.TradingPost, tradingBounds);
            Assert.That(Mathf.Abs(trading.x - tradingBounds.center.x), Is.LessThan(96f / 24f * 0.5f));
            AssertPixels(ToSpritePixels(trading, tradingBounds, 96, 60, 24f, 0.20f), 73.92f, 22.08f);

            Bounds cartBounds = BoundsFor(3f, 2f);
            Vector3 cart = StrategyBuildingVisualAnchorProfile.GetTorchAnchorWorld(StrategyBuildTool.StarterCaravanCart, cartBounds);
            Assert.That(Mathf.Abs(cart.x - cartBounds.center.x), Is.LessThan(64f / 24f * 0.5f));
            AssertPixels(ToSpritePixels(cart, cartBounds, 64, 36, 24f, 0.10f), 8f, 16.08f);
        }

        private static TestCaseData StockCase(
            StrategyBuildTool tool,
            StrategyResourceType resource,
            Bounds bounds,
            float x,
            float y,
            float z)
        {
            return new TestCaseData(tool, resource, bounds, new Vector3(x, y, z))
                .SetName($"Stock_{tool}_{resource}");
        }

        private static TestCaseData TorchCase(
            StrategyBuildTool tool,
            float width,
            float height,
            float x,
            float y,
            float z = -0.20f)
        {
            return new TestCaseData(tool, BoundsFor(width, height), new Vector3(x, y, z))
                .SetName($"Torch_{tool}");
        }

        private static Bounds BoundsFor(float width, float height)
        {
            return new Bounds(Vector3.zero, new Vector3(width, height, 0f));
        }

        private static Vector2 ToSpritePixels(
            Vector3 world,
            Bounds footprint,
            int width,
            int height,
            float pixelsPerUnit,
            float pivotY)
        {
            Vector3 spriteAnchor = new(footprint.center.x, footprint.min.y + PlacementAnchorY, 0f);
            return new Vector2(
                (world.x - spriteAnchor.x) * pixelsPerUnit + width * 0.5f,
                (world.y - spriteAnchor.y) * pixelsPerUnit + height * pivotY);
        }

        private static void AssertAnchor(Vector3 actual, float x, float y, float z)
        {
            Assert.That(Vector3.Distance(actual, new Vector3(x, y, z)), Is.LessThan(Epsilon));
        }

        private static void AssertPixels(Vector2 actual, float x, float y)
        {
            Assert.That(Vector2.Distance(actual, new Vector2(x, y)), Is.LessThan(Epsilon));
        }

        public sealed class SpriteDensityCase
        {
            public SpriteDensityCase(
                StrategyBuildTool tool,
                StrategyResourceType resource,
                float footprintWidth,
                float footprintHeight,
                int width,
                int height,
                float pixelsPerUnit,
                float pivotY)
            {
                Tool = tool;
                Resource = resource;
                FootprintWidth = footprintWidth;
                FootprintHeight = footprintHeight;
                Width = width;
                Height = height;
                PixelsPerUnit = pixelsPerUnit;
                PivotY = pivotY;
            }

            public StrategyBuildTool Tool { get; }
            public StrategyResourceType Resource { get; }
            public float FootprintWidth { get; }
            public float FootprintHeight { get; }
            public int Width { get; }
            public int Height { get; }
            public float PixelsPerUnit { get; }
            public float PivotY { get; }

            public override string ToString()
            {
                return Tool.ToString();
            }
        }
    }
}
