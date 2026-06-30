using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyNightLightSourceKind
    {
        Building,
        Roadside
    }

    [DisallowMultipleComponent]
    internal sealed class StrategyNightLightSource : MonoBehaviour
    {
        private const float WarmupSeconds = 1.25f;
        private static readonly List<StrategyNightLightSource> activeSources = new();

        private StrategyPlacedBuilding building;
        private StrategyRoadsideLightSource roadsideLight;
        private StrategyResidentAgent reservedBy;
        private Vector3 worldPosition;
        private Vector2Int workCell;
        private bool hasWorkCell;
        private float litAtUnscaledTime;
        private bool isLit;

        public StrategyNightLightSourceKind SourceKind { get; private set; }
        public StrategyPlacedBuilding Building => building;
        public StrategyRoadsideLightSource RoadsideLight => roadsideLight;
        public Vector3 WorldPosition => worldPosition;
        public Vector2Int WorkCell => workCell;
        public bool HasWorkCell => hasWorkCell;
        public bool IsLit => isLit;
        public bool IsReserved => reservedBy != null;
        public float LitVisibilityFactor
        {
            get
            {
                if (!isLit)
                {
                    return 0f;
                }

                float age = Mathf.Max(0f, Time.unscaledTime - litAtUnscaledTime);
                return StrategyCinematicVisualMath.Smooth01(age / WarmupSeconds);
            }
        }

        public static void CopyActiveSources(List<StrategyNightLightSource> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                StrategyNightLightSource source = activeSources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }

                target.Add(source);
            }
        }

        public void ConfigureForBuilding(StrategyPlacedBuilding owner, Vector3 anchorWorld)
        {
            building = owner;
            roadsideLight = null;
            SourceKind = StrategyNightLightSourceKind.Building;
            worldPosition = anchorWorld;
            RegisterActive();
        }

        public void ConfigureForRoadside(StrategyRoadsideLightSource owner)
        {
            building = null;
            roadsideLight = owner;
            SourceKind = StrategyNightLightSourceKind.Roadside;
            worldPosition = owner != null ? owner.transform.position : transform.position;
            RegisterActive();
        }

        public void ResetForNight()
        {
            isLit = false;
            reservedBy = null;
            litAtUnscaledTime = 0f;
        }

        public bool TryRefreshWorkCell(CityMapController map)
        {
            hasWorkCell = false;
            if (map == null)
            {
                return false;
            }

            if (roadsideLight != null && map.IsCellWalkable(roadsideLight.RoadCell))
            {
                workCell = roadsideLight.RoadCell;
                hasWorkCell = true;
                return true;
            }

            if (!map.TryWorldToCell(worldPosition, out Vector2Int anchorCell))
            {
                return false;
            }

            Vector2Int best = anchorCell;
            float bestDistance = float.MaxValue;
            for (int radius = 0; radius <= 5; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = anchorCell + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                        float distance = (candidateWorld - worldPosition).sqrMagnitude;
                        if (distance >= bestDistance)
                        {
                            continue;
                        }

                        bestDistance = distance;
                        best = candidate;
                    }
                }

                if (bestDistance < float.MaxValue)
                {
                    workCell = best;
                    hasWorkCell = true;
                    return true;
                }
            }

            return false;
        }

        public bool TryReserve(StrategyResidentAgent resident)
        {
            if (resident == null || isLit || reservedBy != null)
            {
                return false;
            }

            reservedBy = resident;
            return true;
        }

        public bool IsReservedBy(StrategyResidentAgent resident)
        {
            return resident != null && reservedBy == resident;
        }

        public void ReleaseReservation(StrategyResidentAgent resident)
        {
            if (IsReservedBy(resident))
            {
                reservedBy = null;
            }
        }

        public bool CompleteLighting(StrategyResidentAgent resident)
        {
            if (!IsReservedBy(resident))
            {
                return false;
            }

            reservedBy = null;
            isLit = true;
            litAtUnscaledTime = Time.unscaledTime;
            return true;
        }

        private void OnEnable()
        {
            RegisterActive();
        }

        private void OnDisable()
        {
            activeSources.Remove(this);
            reservedBy = null;
        }

        private void OnDestroy()
        {
            activeSources.Remove(this);
            reservedBy = null;
        }

        private void RegisterActive()
        {
            if (isActiveAndEnabled && !activeSources.Contains(this))
            {
                activeSources.Add(this);
            }
        }
    }
}
