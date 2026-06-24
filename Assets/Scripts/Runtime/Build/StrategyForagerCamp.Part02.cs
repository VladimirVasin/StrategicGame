using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForagerCamp
    {
        private StrategyResourceType ChooseAvailableForageResource()
        {
            if (berriesStored - GetReservedAmount(StrategyResourceType.Berries) > 0)
            {
                return StrategyResourceType.Berries;
            }

            if (rootsStored - GetReservedAmount(StrategyResourceType.Roots) > 0)
            {
                return StrategyResourceType.Roots;
            }

            if (mushroomsStored - GetReservedAmount(StrategyResourceType.Mushrooms) > 0)
            {
                return StrategyResourceType.Mushrooms;
            }

            return StrategyResourceType.None;
        }

        private int GetStoredAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Berries => berriesStored,
                StrategyResourceType.Roots => rootsStored,
                StrategyResourceType.Mushrooms => mushroomsStored,
                _ => 0
            };
        }

        private int GetReservedAmount(StrategyResourceType resource)
        {
            return forageReservedResource == resource ? forageReservedAmount : 0;
        }

        private void RemoveStoredForage(StrategyResourceType resource, int amount)
        {
            int taken = Mathf.Max(0, amount);
            switch (resource)
            {
                case StrategyResourceType.Berries:
                    berriesStored = Mathf.Max(0, berriesStored - taken);
                    break;
                case StrategyResourceType.Roots:
                    rootsStored = Mathf.Max(0, rootsStored - taken);
                    break;
                case StrategyResourceType.Mushrooms:
                    mushroomsStored = Mathf.Max(0, mushroomsStored - taken);
                    break;
            }
        }

        private float GetStoredRations()
        {
            return berriesStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Berries)
                + rootsStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Roots)
                + mushroomsStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Mushrooms);
        }

        private string FormatStock()
        {
            return "Berries "
                + berriesStored
                + " / Roots "
                + rootsStored
                + " / Mushrooms "
                + mushroomsStored
                + " ("
                + StrategyProductionStorage.Format(TotalForageStored)
                + ")";
        }

        private void ClearForageReservation()
        {
            forageReservationOwner = null;
            forageReservedResource = StrategyResourceType.None;
            forageReservedAmount = 0;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Forage Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            StrategyResourceType resource = ChooseStockSpriteResource();
            stockRenderer.sprite = resource == StrategyResourceType.None
                ? null
                : StrategyForageSpriteFactory.GetCarriedSprite(resource);
            stockRenderer.gameObject.SetActive(stockRenderer.sprite != null && TotalForageStored > 0);
            UpdateStockPosition();
        }

        private StrategyResourceType ChooseStockSpriteResource()
        {
            if (berriesStored >= rootsStored && berriesStored >= mushroomsStored && berriesStored > 0)
            {
                return StrategyResourceType.Berries;
            }

            if (rootsStored >= mushroomsStored && rootsStored > 0)
            {
                return StrategyResourceType.Roots;
            }

            return mushroomsStored > 0 ? StrategyResourceType.Mushrooms : StrategyResourceType.None;
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.34f, bounds.min.y + 0.36f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void PlayForageStoredEffect(StrategyResourceType resource, int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = FootprintBounds.center + new Vector3(0.15f, -0.18f, -0.02f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                resource,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                TotalForageStored + amount * 17);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearForagerWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
