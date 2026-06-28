using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyChickenCoop
    {
        private bool chickensInNightMode;

        private void SyncChickenNightState(bool force)
        {
            bool isNight = StrategyDayNightCycleController.CurrentCalendarSnapshot.IsNight;
            if (!force && chickensInNightMode == isNight)
            {
                return;
            }

            if (force && !isNight && !chickensInNightMode)
            {
                return;
            }

            chickensInNightMode = isNight;
            if (isNight)
            {
                SendChickensInsideForNight();
            }
            else
            {
                ReleaseChickensForDay();
            }
        }

        private void SendChickensInsideForNight()
        {
            HashSet<Vector2Int> usedCells = new();
            for (int i = 0; i < chickens.Count; i++)
            {
                StrategyChickenAgent chicken = chickens[i];
                if (chicken == null)
                {
                    continue;
                }

                Vector3 hiddenWorld = GetChickenShelterWorld(i);
                if (TryFindChickenSpawnCell(usedCells, i, out Vector2Int shelterCell))
                {
                    usedCells.Add(shelterCell);
                    chicken.BeginNightShelter(shelterCell, hiddenWorld);
                }
                else
                {
                    chicken.BeginNightShelter(Origin, hiddenWorld);
                }
            }

            StrategyDebugLogger.Info(
                "ChickenCoop",
                "ChickensShelteringForNight",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("chickens", chickens.Count));
        }

        private void ReleaseChickensForDay()
        {
            HashSet<Vector2Int> usedCells = new();
            for (int i = 0; i < chickens.Count; i++)
            {
                StrategyChickenAgent chicken = chickens[i];
                if (chicken == null)
                {
                    continue;
                }

                Vector3 exitWorld;
                if (TryFindChickenSpawnCell(usedCells, i, out Vector2Int exitCell))
                {
                    usedCells.Add(exitCell);
                    exitWorld = GetChickenExitWorld(exitCell, i);
                }
                else
                {
                    exitWorld = GetFallbackChickenSpawnWorld(i);
                }

                chicken.ReleaseFromNightShelter(exitWorld);
            }

            StrategyDebugLogger.Info(
                "ChickenCoop",
                "ChickensReleasedForDay",
                StrategyDebugLogger.F("coopOrigin", Origin),
                StrategyDebugLogger.F("chickens", chickens.Count));
        }

        private Vector3 GetChickenExitWorld(Vector2Int cell, int variant)
        {
            Vector3 center = map != null
                ? map.GetCellCenterWorld(cell.x, cell.y)
                : GetFallbackChickenSpawnWorld(variant);
            Vector2 jitter = Random.insideUnitCircle * (map != null ? map.CellSize * 0.18f : 0.12f);
            return new Vector3(center.x + jitter.x, center.y + jitter.y, -0.09f);
        }

        private Vector3 GetChickenShelterWorld(int variant)
        {
            Bounds bounds = FootprintBounds;
            float offset = (variant - (ChickensPerCoop - 1) * 0.5f) * 0.08f;
            return new Vector3(bounds.center.x + offset, bounds.min.y + 0.18f, -0.09f);
        }
    }
}
