using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        public int CaptureCarriedResourcesForSave(List<StrategyLooseResourceSaveData> target)
        {
            if (target == null || map == null)
            {
                return 0;
            }

            if (!map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                Vector3 mapMin = map.WorldBounds.min;
                cell = new Vector2Int(
                    Mathf.Clamp(
                        Mathf.FloorToInt((transform.position.x - mapMin.x) / map.CellSize),
                        0,
                        map.Width - 1),
                    Mathf.Clamp(
                        Mathf.FloorToInt((transform.position.y - mapMin.y) / map.CellSize),
                        0,
                        map.Height - 1));
            }

            int capturedEntries = 0;
            int logs = Mathf.Max(0, carriedLogAmount);
            int stone = Mathf.Max(0, carriedStoneAmount);
            int planks = Mathf.Max(0, carriedPlanksAmount);
            if (logs > 0 || stone > 0 || planks > 0)
            {
                target.Add(new StrategyLooseResourceSaveData
                {
                    constructionPile = true,
                    originX = cell.x,
                    originY = cell.y,
                    logs = logs,
                    stone = stone,
                    planks = planks
                });
                capturedEntries++;
            }

            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Iron, carriedIronAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Coal, carriedCoalAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Clay, carriedClayAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Pottery, carriedPotteryAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Tools, carriedToolsAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Game, carriedGameAmount);
            capturedEntries += AddCarriedResourceForSave(
                target, cell, StrategyResourceType.Fish, carriedFishAmount);
            capturedEntries += AddCarriedHouseholdFoodForSave(target, cell);

            if (capturedEntries > 0)
            {
                StrategyDebugLogger.Info(
                    "Save",
                    "CarriedResourcesCapturedAsLoose",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("origin", cell),
                    StrategyDebugLogger.F("entries", capturedEntries));
            }

            return capturedEntries;
        }

        private int AddCarriedHouseholdFoodForSave(
            List<StrategyLooseResourceSaveData> target,
            Vector2Int cell)
        {
            int amount = Mathf.Max(0, carriedForageAmount);
            StrategyResourceType resource = carriedForageResource;
            if (amount <= 0 || resource == StrategyResourceType.None)
            {
                return 0;
            }

            bool exactPreparedDish = resource == StrategyResourceType.Dish
                && (carriedPreparedDishAmount > 0 || carriedPreparedDishLeftoverRations > 0f);
            if (!exactPreparedDish)
            {
                return AddCarriedResourceForSave(target, cell, resource, amount);
            }

            target.Add(new StrategyLooseResourceSaveData
            {
                originX = cell.x,
                originY = cell.y,
                resource = (int)StrategyResourceType.Dish,
                amount = Mathf.Max(1, carriedPreparedDishAmount),
                preparedDishPile = true,
                preparedDishRecipeId = carriedPreparedDishRecipeId,
                preparedDishAmount = carriedPreparedDishAmount,
                preparedDishLeftoverRations = carriedPreparedDishLeftoverRations
            });
            return 1;
        }

        private static int AddCarriedResourceForSave(
            List<StrategyLooseResourceSaveData> target,
            Vector2Int cell,
            StrategyResourceType resource,
            int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return 0;
            }

            target.Add(new StrategyLooseResourceSaveData
            {
                originX = cell.x,
                originY = cell.y,
                resource = (int)resource,
                amount = amount
            });
            return 1;
        }
    }
}
