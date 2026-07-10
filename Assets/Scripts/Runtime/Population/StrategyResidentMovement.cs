using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyResidentMovement
    {
        private readonly List<Vector3> path = new();
        private readonly List<Vector2Int> navigationRawCells = new();
        private readonly List<Vector2Int> navigationSmoothedCells = new();
        private CityMapController map;
        private Transform ownerTransform;

        public List<Vector3> Path => path;
        public IReadOnlyList<Vector2Int> NavigationRawCells => navigationRawCells;
        public IReadOnlyList<Vector2Int> NavigationSmoothedCells => navigationSmoothedCells;
        public int PathIndex { get; set; }
        public bool HasTarget { get; set; }

        public void Configure(CityMapController mapController, Transform transformOwner)
        {
            map = mapController;
            ownerTransform = transformOwner;
            ClearPath();
        }

        public StrategyNavigationStatus TryBuildPath(Vector2Int startCell, Vector2Int targetCell)
        {
            StrategyNavigationService navigation = StrategyNavigationService.Active;
            if (navigation == null)
            {
                navigationRawCells.Clear();
                navigationSmoothedCells.Clear();
                return StrategyNavigationStatus.Invalid;
            }

            return navigation.TryBuildPath(
                new StrategyNavigationQuery(
                    startCell,
                    targetCell,
                    StrategyNavigationMode.ResidentTrail),
                navigationRawCells,
                navigationSmoothedCells);
        }

        public Vector3 MoveTowards(Vector3 targetWorld, float speed, float deltaTime)
        {
            if (ownerTransform == null)
            {
                return Vector3.zero;
            }

            Vector3 previous = ownerTransform.position;
            ownerTransform.position = Vector3.MoveTowards(
                previous,
                targetWorld,
                Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
            return ownerTransform.position - previous;
        }

        public void BuildWorldPath(IReadOnlyList<Vector2Int> cells, float z, float finalJitterRadius)
        {
            path.Clear();
            if (map == null || cells == null)
            {
                PathIndex = 0;
                HasTarget = false;
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                Vector3 center = map.GetCellCenterWorld(cell.x, cell.y);
                if (i == cells.Count - 1 && finalJitterRadius > 0f)
                {
                    Vector2 jitter = Random.insideUnitCircle * finalJitterRadius;
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, z));
            }

            PathIndex = 0;
            HasTarget = path.Count > 0;
        }

        public void BuildDirectWorldPath(Vector3 targetWorld, float z)
        {
            path.Clear();
            path.Add(new Vector3(targetWorld.x, targetWorld.y, z));
            PathIndex = 0;
            HasTarget = true;
        }

        public void UseCurrentPositionAsPath(float z)
        {
            path.Clear();
            if (ownerTransform != null)
            {
                path.Add(new Vector3(ownerTransform.position.x, ownerTransform.position.y, z));
            }

            PathIndex = 0;
            HasTarget = path.Count > 0;
        }

        public void ClearPath()
        {
            path.Clear();
            navigationRawCells.Clear();
            navigationSmoothedCells.Clear();
            PathIndex = 0;
            HasTarget = false;
        }
    }
}
