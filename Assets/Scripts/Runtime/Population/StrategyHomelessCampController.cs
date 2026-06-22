using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHomelessCampController : MonoBehaviour
    {
        private const int MaxSleepRadius = 5;

        private readonly Dictionary<StrategyResidentAgent, Vector2Int> sleepSpots = new();
        private readonly List<StrategyResidentAgent> staleResidents = new();
        private CityMapController map;
        private StrategyCampfireAnimator campfire;
        private Vector2Int campCell;
        private StrategyResidentAgent relightResident;

        public StrategyCampfireAnimator Campfire => campfire;
        public Vector2Int CampCell => campCell;
        public bool NeedsRelight => campfire != null && campfire.NeedsRelight;

        public void Configure(CityMapController mapController, StrategyCampfireAnimator campfireAnimator, Vector2Int cell)
        {
            map = mapController;
            campfire = campfireAnimator;
            campCell = cell;
            sleepSpots.Clear();
            relightResident = null;
        }

        private void Update()
        {
            if (Time.frameCount % 31 == 0)
            {
                PruneReservations();
            }
        }

        public bool TryReserveReachableSleepSpot(
            StrategyResidentAgent resident,
            Predicate<Vector2Int> canReach,
            out Vector2Int cell)
        {
            cell = default;
            if (resident == null || map == null)
            {
                return false;
            }

            PruneReservations();
            if (sleepSpots.TryGetValue(resident, out cell))
            {
                if (canReach == null || canReach(cell))
                {
                    return true;
                }

                sleepSpots.Remove(resident);
            }

            int offset = Mathf.Abs(resident.ResidentId * 17) % 97;
            for (int radius = 1; radius <= MaxSleepRadius; radius++)
            {
                int side = radius * 2 + 1;
                int perimeter = Mathf.Max(1, side * 4 - 4);
                for (int index = 0; index < perimeter; index++)
                {
                    Vector2Int candidate = GetRingCell(radius, (index + offset) % perimeter);
                    if (!map.IsCellWalkable(candidate)
                        || IsSpotReservedByOther(candidate, resident)
                        || (canReach != null && !canReach(candidate)))
                    {
                        continue;
                    }

                    sleepSpots[resident] = candidate;
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetSleepWorld(StrategyResidentAgent resident, Vector2Int cell)
        {
            if (map == null)
            {
                return transform.position;
            }

            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            Vector2 away = cell - campCell;
            if (away.sqrMagnitude <= 0.001f)
            {
                away = Vector2.up;
            }

            away.Normalize();
            Vector2 tangent = new Vector2(-away.y, away.x);
            float side = resident != null ? ((Mathf.Abs(resident.ResidentId) % 3) - 1) * 0.055f : 0f;
            world.x += away.x * 0.14f + tangent.x * side;
            world.y += away.y * 0.10f + tangent.y * side;
            world.z = -0.08f;
            return world;
        }

        public bool TryReserveRelight(StrategyResidentAgent resident)
        {
            if (resident == null || campfire == null || !campfire.NeedsRelight)
            {
                return false;
            }

            PruneReservations();
            if (relightResident != null && relightResident != resident)
            {
                return false;
            }

            relightResident = resident;
            return true;
        }

        public bool IsRelightReservedBy(StrategyResidentAgent resident)
        {
            return resident != null && relightResident == resident;
        }

        public bool BeginRelight(StrategyResidentAgent resident, float seconds)
        {
            return IsRelightReservedBy(resident)
                && campfire != null
                && campfire.BeginRelight(seconds);
        }

        public void CompleteRelight(StrategyResidentAgent resident)
        {
            if (!IsRelightReservedBy(resident))
            {
                return;
            }

            campfire?.CompleteRelight();
            relightResident = null;
        }

        public void Release(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            sleepSpots.Remove(resident);
            if (relightResident == resident)
            {
                campfire?.CancelRelight();
                relightResident = null;
            }
        }

        private Vector2Int GetRingCell(int radius, int index)
        {
            int side = radius * 2 + 1;
            int edge = side - 1;
            if (index < side)
            {
                return campCell + new Vector2Int(-radius + index, -radius);
            }

            index -= side;
            if (index < edge)
            {
                return campCell + new Vector2Int(radius, -radius + index + 1);
            }

            index -= edge;
            if (index < edge)
            {
                return campCell + new Vector2Int(radius - index - 1, radius);
            }

            index -= edge;
            return campCell + new Vector2Int(-radius, radius - index - 1);
        }

        private bool IsSpotReservedByOther(Vector2Int cell, StrategyResidentAgent resident)
        {
            foreach (KeyValuePair<StrategyResidentAgent, Vector2Int> entry in sleepSpots)
            {
                if (entry.Key != null && entry.Key != resident && entry.Value == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private void PruneReservations()
        {
            staleResidents.Clear();
            foreach (KeyValuePair<StrategyResidentAgent, Vector2Int> entry in sleepSpots)
            {
                if (entry.Key == null)
                {
                    staleResidents.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleResidents.Count; i++)
            {
                sleepSpots.Remove(staleResidents[i]);
            }

            if (relightResident == null)
            {
                relightResident = null;
            }
        }
    }
}
