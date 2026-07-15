using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyRoadOccupancyTests
    {
        private const int MapWidth = 12;
        private const int MapHeight = 12;

        private readonly List<GameObject> ownedObjects = new();
        private CityMapController map;
        private StrategyTrailController trails;

        [SetUp]
        public void SetUp()
        {
            GameObject mapObject = Own(new GameObject("Road Occupancy Test Map"));
            map = mapObject.AddComponent<CityMapController>();
            ConfigureAllLandMap(map);

            GameObject trailObject = Own(new GameObject("Road Occupancy Test Trails"));
            trails = trailObject.AddComponent<StrategyTrailController>();
            trails.Configure(map);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void RestoreAndCaptureRejectBuildOnlyBlockedRoadCells()
        {
            Vector2Int acceptedCell = new Vector2Int(3, 4);
            Vector2Int blockedCell = new Vector2Int(4, 4);
            map.SetCellsBuildable(blockedCell, Vector2Int.one, false);

            trails.RestorePersistentTrailCells(new[]
            {
                GetKey(acceptedCell),
                GetKey(blockedCell)
            });

            List<int> captured = new();
            trails.CapturePersistentTrailCells(captured);

            Assert.That(map.IsCellWalkable(blockedCell), Is.True, "The fixture must model an underground resource field, not a movement obstacle.");
            Assert.That(map.IsCellBuildable(blockedCell), Is.False);
            Assert.That(trails.HasRouteRoadAt(acceptedCell), Is.True);
            Assert.That(trails.HasRouteRoadAt(blockedCell), Is.False);
            CollectionAssert.AreEquivalent(new[] { GetKey(acceptedCell) }, captured);
        }

        [Test]
        public void RawRoadMembershipRemainsAvailableForSpawnExclusionUntilPruned()
        {
            Vector2Int roadCell = new Vector2Int(5, 5);
            trails.RestorePersistentTrailCells(new[] { GetKey(roadCell) });

            Assert.That(trails.HasRouteRoadAt(roadCell), Is.True);
            Assert.That(trails.IsTrailCell(roadCell), Is.True);

            map.SetCellsBuildable(roadCell, Vector2Int.one, false);
            List<int> captured = new();
            trails.CapturePersistentTrailCells(captured);

            Assert.That(trails.HasRouteRoadAt(roadCell), Is.True, "Static-object spawning needs raw road membership before the periodic prune runs.");
            Assert.That(trails.IsTrailCell(roadCell), Is.False, "An invalidated road must stop affecting movement immediately.");
            Assert.That(captured, Is.Empty, "Invalid raw membership must never be persisted.");
        }

        [Test]
        public void RouteConnectionRepairsAroundBuildOnlyBlocker()
        {
            Vector2Int blockedCell = new Vector2Int(4, 5);
            map.SetCellsBuildable(blockedCell, Vector2Int.one, false);
            List<Vector2Int> source = new()
            {
                new Vector2Int(2, 5),
                new Vector2Int(3, 5),
                blockedCell,
                new Vector2Int(5, 5),
                new Vector2Int(6, 5),
                new Vector2Int(7, 5)
            };
            List<Vector2Int> repaired = new();

            InvokePrivate(trails, "BuildSingleSidedRouteCells", source, repaired);

            Assert.That(repaired, Is.Not.Empty);
            Assert.That(repaired[0], Is.EqualTo(source[0]));
            Assert.That(repaired[repaired.Count - 1], Is.EqualTo(source[source.Count - 1]));
            CollectionAssert.DoesNotContain(repaired, blockedCell);
            Assert.That(repaired.Exists(cell => cell.y != blockedCell.y), Is.True, "The road must leave the straight line to go around the resource field.");
            for (int i = 0; i < repaired.Count; i++)
            {
                Assert.That(map.IsCellWalkable(repaired[i]), Is.True, $"Road cell {repaired[i]} must remain walkable.");
                Assert.That(map.IsCellBuildable(repaired[i]), Is.True, $"Road cell {repaired[i]} must remain buildable.");
                if (i > 0)
                {
                    Assert.That(ManhattanDistance(repaired[i - 1], repaired[i]), Is.EqualTo(1), "The repaired road must stay cardinally connected.");
                }
            }
        }

        [Test]
        public void PendingRoadReservationsPreventNatureRespawnBeforeRestore()
        {
            Vector2Int roadCell = new Vector2Int(4, 4);
            trails.ReservePendingPersistentTrailCells(new[] { GetKey(roadCell) });

            GameObject natureObject = Own(new GameObject("Road Occupancy Test Nature"));
            StrategyNaturePropController nature = natureObject.AddComponent<StrategyNaturePropController>();
            GameObject propRootObject = new GameObject("Nature Test Props");
            propRootObject.transform.SetParent(natureObject.transform, false);
            SetPrivateField(nature, "map", map);
            SetPrivateField(nature, "propRoot", propRootObject.transform);

            Assert.That(trails.HasRouteRoadAt(roadCell), Is.True);
            Assert.That(trails.IsTrailCell(roadCell), Is.False, "A bootstrap reservation must not grant movement bonuses before save application.");
            InvokePrivate(
                nature,
                "CreateProp",
                new CityMapCell(roadCell.x, roadCell.y, CityMapCellKind.Grass),
                StrategyNaturePropKind.Bush,
                43,
                0.8f,
                1f,
                2);

            Assert.That(propRootObject.transform.childCount, Is.Zero);
            trails.RestorePersistentTrailCells(System.Array.Empty<int>());
            Assert.That(trails.HasRouteRoadAt(roadCell), Is.False, "Final restore must clear temporary bootstrap reservations.");
        }

        [Test]
        public void PendingSaveTrailCellsAreDimensionBoundAndDefensive()
        {
            Vector2Int roadCell = new Vector2Int(6, 6);
            StrategySaveData save = new()
            {
                mapSeed = 101,
                mapWidth = MapWidth,
                mapHeight = MapHeight,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
            save.trailCells.Add(GetKey(roadCell));
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);

            StrategySaveSystem.PreparePendingLoad(save);
            try
            {
                Assert.That(
                    StrategySaveSystem.TryGetPendingTrailCells(MapWidth, MapHeight, out List<int> first),
                    Is.True);
                first.Clear();

                Assert.That(
                    StrategySaveSystem.TryGetPendingTrailCells(MapWidth, MapHeight, out List<int> second),
                    Is.True);
                CollectionAssert.AreEqual(new[] { GetKey(roadCell) }, second);
                Assert.That(
                    StrategySaveSystem.TryGetPendingTrailCells(MapWidth + 1, MapHeight, out _),
                    Is.False);
            }
            finally
            {
                StrategySaveSystem.ClearPendingLoad();
            }
        }

        [Test]
        public void RoadsAndForageNodesExcludeEachOther()
        {
            Vector2Int roadCell = new Vector2Int(3, 3);
            Vector2Int forageCell = new Vector2Int(7, 7);
            trails.RestorePersistentTrailCells(new[] { GetKey(roadCell) });

            GameObject forageObject = Own(new GameObject("Road Occupancy Test Forage"));
            StrategyForageResourceController forage = forageObject.AddComponent<StrategyForageResourceController>();
            forage.Configure(null);
            SetPrivateField(forage, "map", map);

            bool createdOnRoad = InvokePrivate<bool>(
                forage,
                "TryCreateNode",
                roadCell,
                StrategyResourceType.Berries,
                17);
            Assert.That(createdOnRoad, Is.False);
            Assert.That(forage.HasNodeAt(roadCell), Is.False);

            HashSet<Vector2Int> usedCells = GetPrivateField<HashSet<Vector2Int>>(forage, "usedCells");
            usedCells.Add(forageCell);
            trails.RestorePersistentTrailCells(new[]
            {
                GetKey(roadCell),
                GetKey(forageCell)
            });

            Assert.That(trails.HasRouteRoadAt(roadCell), Is.True);
            Assert.That(trails.HasRouteRoadAt(forageCell), Is.False);
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

        private static int GetKey(Vector2Int cell)
        {
            return cell.y * MapWidth + cell.x;
        }

        private static int ManhattanDistance(Vector2Int left, Vector2Int right)
        {
            Vector2Int delta = left - right;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}.");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            _ = InvokePrivate<object>(target, methodName, arguments);
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName} on {target.GetType().Name}.");
            return (T)method.Invoke(target, arguments);
        }
    }
}
