using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySettlementFaunaController
    {
        private readonly List<StrategyCatAgent> cats = new();
        private readonly List<StrategyMouseAgent> mice = new();
        private int nextFaunaId;
        private float nextMouseSpawnTime;

        partial void UpdateFaunaPopulation()
        {
            cats.RemoveAll(agent => agent == null);
            mice.RemoveAll(agent => agent == null || agent.IsCaught);
            while (cats.Count > targets.TargetCats)
            {
                int index = cats.Count - 1;
                StrategyCatAgent excess = cats[index];
                cats.RemoveAt(index);
                if (excess != null)
                {
                    excess.gameObject.SetActive(false);
                    Destroy(excess.gameObject);
                }
            }

            while (mice.Count > targets.TargetMice)
            {
                int index = mice.Count - 1;
                StrategyMouseAgent excess = mice[index];
                mice.RemoveAt(index);
                if (excess != null)
                {
                    Destroy(excess.gameObject);
                }
            }

            int mouseSpawnBudget = StrategySettlementFaunaPolicy.GetMouseSpawnBudget(
                firstNightStage,
                mice.Count,
                targets.TargetMice);
            while (mouseSpawnBudget > 0
                && mice.Count < targets.TargetMice
                && Time.time >= nextMouseSpawnTime)
            {
                bool foundMouseCell = TryFindSpawnCell(true, out Vector2Int mouseCell);
                if (!foundMouseCell && firstNightStage != StrategyFirstNightFaunaStage.Dormant)
                {
                    foundMouseCell = TryFindSpawnCell(false, out mouseCell)
                        || TryFindFirstNightFallbackCell(out mouseCell);
                }

                if (foundMouseCell)
                {
                    SpawnMouse(mouseCell);
                }

                if (!foundMouseCell)
                {
                    break;
                }

                mouseSpawnBudget--;
            }

            bool firstStoryCat = cats.Count == 0;
            if (firstNightStage == StrategyFirstNightFaunaStage.StoryCompleted
                && OwnsCats
                && cats.Count < targets.TargetCats)
            {
                bool foundCatCell = TryFindSpawnCell(firstStoryCat, out Vector2Int catCell);
                if (!foundCatCell && firstStoryCat)
                {
                    foundCatCell = TryFindSpawnCell(false, out catCell)
                        || TryFindFirstNightFallbackCell(out catCell);
                }

                if (foundCatCell)
                {
                    SpawnCat(catCell);
                }
            }
        }

        partial void ClearFaunaPopulation()
        {
            for (int i = 0; i < cats.Count; i++)
            {
                if (cats[i] != null)
                {
                    cats[i].gameObject.SetActive(false);
                    Destroy(cats[i].gameObject);
                }
            }

            for (int i = 0; i < mice.Count; i++)
            {
                if (mice[i] != null)
                {
                    mice[i].gameObject.SetActive(false);
                    Destroy(mice[i].gameObject);
                }
            }

            cats.Clear();
            mice.Clear();
            nextFaunaId = 0;
            nextMouseSpawnTime = 0f;
        }

        internal StrategyMouseAgent FindMouseForCat(StrategyCatAgent cat, float radius)
        {
            StrategyMouseAgent best = null;
            float bestSqr = radius * radius;
            for (int i = 0; i < mice.Count; i++)
            {
                StrategyMouseAgent mouse = mice[i];
                if (mouse == null || mouse.IsCaught || mouse.IsReservedByOther(cat))
                {
                    continue;
                }

                float sqr = (mouse.transform.position - cat.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = mouse;
                    bestSqr = sqr;
                }
            }

            return best != null && best.TryReserve(cat) ? best : null;
        }

        internal void NotifyMouseCaught(StrategyMouseAgent mouse, StrategyCatAgent cat)
        {
            mice.Remove(mouse);
            nextMouseSpawnTime = Mathf.Max(nextMouseSpawnTime, Time.time + Random.Range(20f, 40f));
            StrategyDebugLogger.Info(
                "SettlementFauna",
                "MouseCaught",
                StrategyDebugLogger.F("cat", cat != null ? cat.FaunaId : -1),
                StrategyDebugLogger.F("mouse", mouse != null ? mouse.FaunaId : -1),
                StrategyDebugLogger.F("remainingMice", mice.Count),
                StrategyDebugLogger.F("respawnDelay", Mathf.Max(0f, nextMouseSpawnTime - Time.time)));
        }

        private bool TryFindSpawnCell(bool preferFoodBuilding, out Vector2Int cell)
        {
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            int start = buildings.Count > 0 ? Mathf.Abs(nextFaunaId * 17 + map.ActiveSeed) % buildings.Count : 0;
            for (int offset = 0; offset < buildings.Count; offset++)
            {
                StrategyPlacedBuilding building = buildings[(start + offset) % buildings.Count];
                if (building == null || building.Tool == StrategyBuildTool.Bridge
                    || preferFoodBuilding && !IsMouseFoodBuilding(building))
                {
                    continue;
                }

                for (int radius = 1; radius <= 5; radius++)
                {
                    for (int y = -radius; y <= building.Footprint.y + radius - 1; y++)
                    {
                        for (int x = -radius; x <= building.Footprint.x + radius - 1; x++)
                        {
                            bool edge = x == -radius || y == -radius
                                || x == building.Footprint.x + radius - 1
                                || y == building.Footprint.y + radius - 1;
                            Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                            if (edge && map.IsCellWalkable(candidate) && !IsFaunaCellOccupied(candidate))
                            {
                                cell = candidate;
                                return true;
                            }
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        private bool IsFaunaCellOccupied(Vector2Int cell)
        {
            for (int i = 0; i < mice.Count; i++)
            {
                if (mice[i] != null && mice[i].CurrentCell == cell) return true;
            }
            for (int i = 0; i < cats.Count; i++)
            {
                if (cats[i] != null && cats[i].CurrentCell == cell) return true;
            }
            return false;
        }

        private bool TryFindFirstNightFallbackCell(out Vector2Int cell)
        {
            if (map == null)
            {
                cell = default;
                return false;
            }

            Vector2Int center = population != null && population.TryGetCampCell(out Vector2Int campCell)
                ? campCell
                : new Vector2Int(map.Width / 2, map.Height / 2);
            for (int radius = 0; radius <= 8; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = center + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !IsFaunaCellOccupied(candidate))
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        private void SpawnMouse(Vector2Int cell)
        {
            int id = nextFaunaId++;
            GameObject obj = new GameObject($"Settlement Mouse {id}");
            obj.transform.SetParent(faunaRoot, false);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategySettlementFaunaSpriteFactory.GetMouseSprite(id);
            StrategyMouseAgent agent = obj.AddComponent<StrategyMouseAgent>();
            agent.Configure(this, map, id, cell, renderer);
            mice.Add(agent);
            StrategyDebugLogger.Info("SettlementFauna", "MouseSpawned",
                StrategyDebugLogger.F("id", id), StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("count", mice.Count), StrategyDebugLogger.F("target", targets.TargetMice));
        }

        private void SpawnCat(Vector2Int cell)
        {
            int id = nextFaunaId++;
            StrategyCatCoat coat = (StrategyCatCoat)(Mathf.Abs(map.ActiveSeed + id * 31) % 7);
            StrategyCatTemperament temperament = (StrategyCatTemperament)(Mathf.Abs(map.ActiveSeed + id * 47) % 6);
            GameObject obj = new GameObject($"Settlement Cat {id}");
            obj.transform.SetParent(faunaRoot, false);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategySettlementFaunaSpriteFactory.GetCatSprite(coat);
            StrategyCatAgent agent = obj.AddComponent<StrategyCatAgent>();
            agent.Configure(this, map, id, cell, coat, temperament, renderer);
            cats.Add(agent);
            StrategyDebugLogger.Info("SettlementFauna", "CatSpawned",
                StrategyDebugLogger.F("id", id), StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("coat", coat), StrategyDebugLogger.F("temperament", temperament),
                StrategyDebugLogger.F("count", cats.Count), StrategyDebugLogger.F("target", targets.TargetCats));
        }
    }
}
