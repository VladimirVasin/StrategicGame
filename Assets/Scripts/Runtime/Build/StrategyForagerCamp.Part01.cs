using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForagerCamp
    {
        public bool TryReserveForageNode(StrategyResidentAgent resident, out StrategyForageNode node, out Vector2Int workCell)
        {
            node = null;
            workCell = default;
            if (!HasStorageSpace || resident == null)
            {
                return false;
            }

            if (forage == null)
            {
                forage = StrategyForageResourceController.Active;
            }

            return forage != null
                && forage.TryReserveForWorksite(
                    building,
                    resident,
                    WorkRadius,
                    CanAcceptForageResource,
                    out node,
                    out workCell);
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void AddForage(StrategyResourceType resource, int amount)
        {
            if (!IsForageResource(resource) || amount <= 0)
            {
                return;
            }

            int accepted;
            switch (resource)
            {
                case StrategyResourceType.Berries:
                    berriesStored = StrategyProductionStorage.AddCapped(berriesStored, TotalForageStored, amount, out accepted);
                    break;
                case StrategyResourceType.Roots:
                    rootsStored = StrategyProductionStorage.AddCapped(rootsStored, TotalForageStored, amount, out accepted);
                    break;
                default:
                    mushroomsStored = StrategyProductionStorage.AddCapped(mushroomsStored, TotalForageStored, amount, out accepted);
                    break;
            }

            if (accepted > 0)
            {
                PlayForageStoredEffect(resource, accepted);
                UpdateStockVisual();
            }
        }

        public bool TryReserveStoredForage(object owner, out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null || AvailableForage <= 0)
            {
                return false;
            }

            if (forageReservationOwner != null && forageReservationOwner != owner)
            {
                return false;
            }

            if (forageReservationOwner == owner && forageReservedAmount > 0)
            {
                resource = forageReservedResource;
                amount = forageReservedAmount;
                return true;
            }

            resource = ChooseAvailableForageResource();
            if (resource == StrategyResourceType.None)
            {
                return false;
            }

            forageReservationOwner = owner;
            forageReservedResource = resource;
            forageReservedAmount = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, GetStoredAmount(resource));
            amount = forageReservedAmount;
            return amount > 0;
        }

        public bool TryTakeReservedForage(object owner, out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null
                || forageReservationOwner != owner
                || forageReservedResource == StrategyResourceType.None
                || forageReservedAmount <= 0)
            {
                return false;
            }

            resource = forageReservedResource;
            amount = Mathf.Min(forageReservedAmount, GetStoredAmount(resource));
            if (amount <= 0)
            {
                ClearForageReservation();
                return false;
            }

            RemoveStoredForage(resource, amount);
            ClearForageReservation();
            UpdateStockVisual();
            return true;
        }

        public void ReleaseStoredForageReservation(object owner)
        {
            if (owner != null && forageReservationOwner == owner)
            {
                ClearForageReservation();
            }
        }

        public string GetHudStatusText()
        {
            return "Foragers gather nearby food"
                + "\n"
                + "Stock: "
                + FormatStock()
                + "\n"
                + "Rations: "
                + GetStoredRations().ToString("0.#")
                + "\n"
                + "Haulers deliver stock to Granaries";
        }

        private bool CanAcceptForageResource(StrategyResourceType resource)
        {
            return IsForageResource(resource)
                && StrategyProductionStorage.GetRemaining(TotalForageStored + forageReservedAmount) > 0;
        }

        private static bool IsForageResource(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Berries
                || resource == StrategyResourceType.Roots
                || resource == StrategyResourceType.Mushrooms;
        }
    }
}
