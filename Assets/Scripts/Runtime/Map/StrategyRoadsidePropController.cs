using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyRoadsidePropController
    {
        private const float SideDistance = 0.46f;

        private sealed class RoadsidePropEntry
        {
            public GameObject Root;
        }

        private static readonly Vector2Int[] RefreshOffsets =
        {
            Vector2Int.zero,
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        private readonly Dictionary<int, RoadsidePropEntry> props = new();
        private readonly HashSet<int> pendingRefreshKeys = new();
        private readonly List<int> scratchRemoveKeys = new();
        private CityMapController map;
        private Transform parent;
        private Transform root;
        private bool needsLightingRefresh;

        public void Configure(CityMapController mapController, Transform parentTransform)
        {
            map = mapController;
            parent = parentTransform;
            EnsureRoot();
        }

        public void QueueRefreshAround(Vector2Int cell)
        {
            if (map == null)
            {
                return;
            }

            for (int i = 0; i < RefreshOffsets.Length; i++)
            {
                AddPending(cell + RefreshOffsets[i]);
            }
        }

        public void QueueRefreshArea(Vector2Int origin, Vector2Int size)
        {
            if (map == null || size.x <= 0 || size.y <= 0)
            {
                return;
            }

            int minX = Mathf.Max(0, origin.x - 1);
            int minY = Mathf.Max(0, origin.y - 1);
            int maxX = Mathf.Min(map.Width - 1, origin.x + size.x);
            int maxY = Mathf.Min(map.Height - 1, origin.y + size.y);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    AddPending(new Vector2Int(x, y));
                }
            }
        }

        public void FlushPending(Func<Vector2Int, bool> isRoadCell)
        {
            if (map == null || isRoadCell == null || pendingRefreshKeys.Count == 0)
            {
                return;
            }

            scratchRemoveKeys.Clear();
            foreach (int key in pendingRefreshKeys)
            {
                scratchRemoveKeys.Add(key);
            }

            pendingRefreshKeys.Clear();
            for (int i = 0; i < scratchRemoveKeys.Count; i++)
            {
                RefreshCell(GetCell(scratchRemoveKeys[i]), isRoadCell);
            }

            if (needsLightingRefresh)
            {
                needsLightingRefresh = false;
                RefreshCinematicLighting();
            }
        }

        public void Clear()
        {
            bool removedAny = props.Count > 0;
            foreach (RoadsidePropEntry entry in props.Values)
            {
                if (entry?.Root != null)
                {
                    UnityEngine.Object.Destroy(entry.Root);
                }
            }

            props.Clear();
            pendingRefreshKeys.Clear();
            scratchRemoveKeys.Clear();
            needsLightingRefresh = true;
            if (removedAny)
            {
                RefreshCinematicLighting();
            }
        }

        private void RefreshCell(Vector2Int cell, Func<Vector2Int, bool> isRoadCell)
        {
            if (!IsInside(cell))
            {
                return;
            }

            int key = GetKey(cell);
            RemoveProp(key);
            if (!isRoadCell(cell))
            {
                return;
            }

            bool CanPlaceSide(Vector2Int sideCell) => CanPlaceSideCell(sideCell, isRoadCell);
            if (!StrategyRoadsidePropPlanner.TryGetTorchPlacement(
                    cell,
                    map.ActiveSeed,
                    isRoadCell,
                    CanPlaceSide,
                    GetCellCenter,
                    SideDistance,
                    out StrategyRoadsidePropPlacement placement))
            {
                return;
            }

            CreateProp(key, placement);
        }

        private void CreateProp(int key, StrategyRoadsidePropPlacement placement)
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            EnsureRoot();
            if (root == null)
            {
                return;
            }

            GameObject propRoot = new GameObject($"Roadside Torch {placement.RoadCell.x},{placement.RoadCell.y}");
            propRoot.transform.SetParent(root, false);
            propRoot.transform.position = placement.WorldPosition;

            SpriteRenderer baseRenderer = propRoot.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = StrategyBuildingLightSpriteFactory.GetSprite(
                StrategyBuildingLightSpriteKind.Lantern,
                placement.Variant);
            baseRenderer.color = new Color(0.46f, 0.38f, 0.28f, 0.58f);
            baseRenderer.sortingOrder = StrategyWorldSorting.ForPosition(placement.WorldPosition, 21);

            StrategyRoadsideLightSource lightSource = propRoot.AddComponent<StrategyRoadsideLightSource>();
            lightSource.Configure(placement.RoadCell, placement.SideOffset);
            StrategyCinematicLightEmitter emitter = propRoot.AddComponent<StrategyCinematicLightEmitter>();
            emitter.ConfigureForRoadsideLight(lightSource);
            props[key] = new RoadsidePropEntry { Root = propRoot };
            needsLightingRefresh = true;
        }

        private void RemoveProp(int key)
        {
            if (!props.TryGetValue(key, out RoadsidePropEntry entry))
            {
                return;
            }

            props.Remove(key);
            if (entry?.Root != null)
            {
                UnityEngine.Object.Destroy(entry.Root);
                needsLightingRefresh = true;
            }
        }

        private bool CanPlaceSideCell(Vector2Int cell, Func<Vector2Int, bool> isRoadCell)
        {
            return IsInside(cell)
                && !isRoadCell(cell)
                && map.IsCellBuildable(cell);
        }

        private Vector3 GetCellCenter(Vector2Int cell)
        {
            return map.GetCellCenterWorld(cell.x, cell.y);
        }

        private void AddPending(Vector2Int cell)
        {
            if (IsInside(cell))
            {
                pendingRefreshKeys.Add(GetKey(cell));
            }
        }

        private bool IsInside(Vector2Int cell)
        {
            return map != null && cell.x >= 0 && cell.x < map.Width && cell.y >= 0 && cell.y < map.Height;
        }

        private int GetKey(Vector2Int cell)
        {
            return cell.y * map.Width + cell.x;
        }

        private Vector2Int GetCell(int key)
        {
            return new Vector2Int(key % map.Width, key / map.Width);
        }

        private void EnsureRoot()
        {
            if (root != null || parent == null || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            GameObject rootObject = new GameObject("Roadside Props");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;
        }

        private static void RefreshCinematicLighting()
        {
            StrategyCinematicVisualController visuals = UnityEngine.Object.FindAnyObjectByType<StrategyCinematicVisualController>();
            visuals?.RefreshSceneLightingNow();
        }
    }
}
