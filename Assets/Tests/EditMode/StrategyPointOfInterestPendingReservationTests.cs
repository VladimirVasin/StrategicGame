using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPointOfInterestPendingReservationTests
    {
        private const int MapWidth = 48;
        private const int MapHeight = 48;

        private readonly List<GameObject> ownedObjects = new();
        private CityMapController map;

        [SetUp]
        public void SetUp()
        {
            GameObject mapObject = Own(new GameObject("POI Pending Reservation Test Map"));
            map = mapObject.AddComponent<CityMapController>();
            ConfigureAllLandMap(map);
            StrategySaveSystem.ReleasePendingPointOfInterestCells(map);
            StrategySaveSystem.ClearPendingLoad();
        }

        [TearDown]
        public void TearDown()
        {
            StrategySaveSystem.ReleasePendingPointOfInterestCells(map);
            StrategySaveSystem.ClearPendingLoad();
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
            map = null;
        }

        [Test]
        public void PositivePendingMineralReservesMarkersAndEntireExtractionBlockUntilRelease()
        {
            Vector2Int neutralMarker = new(8, 8);
            Vector2Int typedMarker = new(20, 20);
            Vector2Int mineralOrigin = new(24, 20);
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateNeutralPoint(neutralMarker));
            save.pointsOfInterest.Add(CreateTypedPoint(typedMarker, mineralOrigin, 64));
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);

            StrategySaveSystem.PreparePendingLoad(save);
            StrategySaveSystem.ReservePendingPointOfInterestCells(map);

            AssertBuildOnlyReserved(neutralMarker);
            AssertBuildOnlyReserved(typedMarker);
            AssertExtractionBlock(mineralOrigin, false);

            StrategySaveSystem.ReleasePendingPointOfInterestCells(map);

            Assert.That(map.IsCellBuildable(neutralMarker), Is.True);
            Assert.That(map.IsCellBuildable(typedMarker), Is.True);
            AssertExtractionBlock(mineralOrigin, true);
        }

        [Test]
        public void DepletedPendingMineralReservesOnlyMarker()
        {
            Vector2Int marker = new(20, 20);
            Vector2Int mineralOrigin = new(24, 20);
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateTypedPoint(marker, mineralOrigin, 0));
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);

            StrategySaveSystem.PreparePendingLoad(save);
            StrategySaveSystem.ReservePendingPointOfInterestCells(map);

            AssertBuildOnlyReserved(marker);
            AssertExtractionBlock(mineralOrigin, true);

            StrategySaveSystem.ReleasePendingPointOfInterestCells(map);

            Assert.That(map.IsCellBuildable(marker), Is.True);
            AssertExtractionBlock(mineralOrigin, true);
        }

        [Test]
        public void PendingReservationsIgnoreLegacyLatentStoryCells()
        {
            Vector2Int latentCell = new(12, 12);
            Vector2Int resolvedCell = new(18, 18);
            StrategySaveData save = CreateValidSave();
            save.storyPointsOfInterest.Add(new StrategyStoryPointOfInterestSaveData
            {
                stableId = "legacy-latent-story",
                cellX = latentCell.x,
                cellY = latentCell.y,
                state = (int)StrategyStoryPointOfInterestState.Latent,
                definitionId = string.Empty,
                sequenceIndex = -1
            });
            save.storyPointsOfInterest.Add(new StrategyStoryPointOfInterestSaveData
            {
                stableId = "durable-resolved-story",
                cellX = resolvedCell.x,
                cellY = resolvedCell.y,
                state = (int)StrategyStoryPointOfInterestState.Resolved,
                definitionId = "story-first",
                sequenceIndex = 0
            });
            save.nextStoryPointOfInterestSequenceIndex = 1;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);

            StrategySaveSystem.PreparePendingLoad(save);
            StrategySaveSystem.ReservePendingPointOfInterestCells(map);

            Assert.That(map.IsCellBuildable(latentCell), Is.True);
            Assert.That(map.IsCellBuildable(resolvedCell), Is.False);
        }

        private static StrategySaveData CreateValidSave()
        {
            return new StrategySaveData
            {
                mapSeed = 101,
                mapWidth = MapWidth,
                mapHeight = MapHeight,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
        }

        private static StrategyPointOfInterestSaveData CreateNeutralPoint(Vector2Int marker)
        {
            return new StrategyPointOfInterestSaveData
            {
                stableId = $"poi-{marker.x}-{marker.y}",
                cellX = marker.x,
                cellY = marker.y,
                resourceKind = (int)StrategyPointOfInterestResourceKind.None
            };
        }

        private static StrategyPointOfInterestSaveData CreateTypedPoint(
            Vector2Int marker,
            Vector2Int mineralOrigin,
            int remainingAmount)
        {
            return new StrategyPointOfInterestSaveData
            {
                stableId = $"poi-{marker.x}-{marker.y}",
                cellX = marker.x,
                cellY = marker.y,
                resourceKind = (int)StrategyPointOfInterestResourceKind.Coal,
                hasMineralSite = true,
                mineralOriginX = mineralOrigin.x,
                mineralOriginY = mineralOrigin.y,
                remainingMineralAmount = remainingAmount
            };
        }

        private void AssertBuildOnlyReserved(Vector2Int cell)
        {
            Assert.That(map.IsCellWalkable(cell), Is.True, $"Reservation at {cell} must not block movement.");
            Assert.That(map.IsCellBuildable(cell), Is.False, $"Reservation at {cell} must block spawning/building.");
        }

        private void AssertExtractionBlock(Vector2Int origin, bool expectedBuildable)
        {
            Vector2Int footprint = StrategyPointOfInterestPlacement.ExtractionBlockFootprint;
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    Assert.That(map.IsCellWalkable(cell), Is.True, $"Reservation at {cell} must not block movement.");
                    Assert.That(
                        map.IsCellBuildable(cell),
                        Is.EqualTo(expectedBuildable),
                        $"Unexpected buildability at extraction cell {cell}.");
                }
            }
        }

        private GameObject Own(GameObject gameObject)
        {
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void ConfigureAllLandMap(CityMapController target)
        {
            CityMapCell[,] cells = new CityMapCell[MapWidth, MapHeight];
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    cells[x, y] = new CityMapCell(x, y, CityMapCellKind.Grass);
                }
            }

            SetPrivateField(target, "width", MapWidth);
            SetPrivateField(target, "height", MapHeight);
            SetPrivateField(target, "cellSize", 1f);
            SetPrivateField(target, "activeSeed", 101);
            SetPrivateField(target, "cells", cells);
            SetPrivateField(target, "blockedWalkCounts", new int[MapWidth, MapHeight]);
            SetPrivateField(target, "blockedBuildCounts", new int[MapWidth, MapHeight]);
            SetPrivateField(target, "bridgeWalkableCells", new bool[MapWidth, MapHeight]);
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
