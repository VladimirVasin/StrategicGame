using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyLooseCarriedResourcePile : MonoBehaviour,
        IStrategyWorldInspectable,
        IStrategyResourceStoreOwner,
        IStrategyResourceReservationProvider
    {
        private static Transform root;

        private CityMapController map;
        private readonly StrategyResourceStore resourceStore = new();
        private SpriteRenderer spriteRenderer;
        private Vector2Int origin;
        private Bounds footprintBounds;
        private StrategyResourceType resource;
        private object reservedBy;
        private int reservedAmount;
        private ref int amount => ref resourceStore.GetAmountRef(resource);

        public StrategyResourceType Resource => resource;
        public int Amount => amount;
        public Vector2Int Origin => origin;
        public Bounds FootprintBounds => footprintBounds;
        public bool IsReserved => reservedBy != null;
        public StrategyResourceStore ResourceStore => resourceStore;

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = StrategyWorldInspectInfoFactory.CreateLooseResource(
                resource,
                amount,
                IsReserved,
                spriteRenderer != null ? spriteRenderer.sprite : StrategyResourceIconFactory.GetSprite(resource),
                origin);
            return true;
        }

        public static StrategyLooseCarriedResourcePile Create(
            CityMapController map,
            Vector2Int origin,
            Vector3 world,
            StrategyResourceType resource,
            int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return null;
            }

            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return null;
            }

            EnsureRoot();
            GameObject obj = new GameObject("Loose " + resource);
            obj.transform.SetParent(root, false);
            StrategyLooseCarriedResourcePile pile = obj.AddComponent<StrategyLooseCarriedResourcePile>();
            pile.Configure(map, origin, world, resource, amount);
            return pile;
        }

        public static bool TryReserveNearestForGranary(
            StrategyGranary granary,
            StrategyResidentAgent worker,
            out StrategyLooseCarriedResourcePile pile,
            out StrategyResourceType resource,
            out Vector2Int pickupCell)
        {
            pile = null;
            resource = StrategyResourceType.None;
            pickupCell = default;
            if (granary == null || worker == null)
            {
                return false;
            }

            StrategyLooseCarriedResourcePile[] piles = GetPilesSortedByDistance(granary.FootprintBounds.center);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseCarriedResourcePile candidate = piles[i];
                if (candidate == null
                    || !IsGranaryFood(candidate.resource)
                    || candidate.amount <= 0
                    || candidate.reservedBy != null
                    || !candidate.TryFindPickupCell(out pickupCell))
                {
                    continue;
                }

                if (!candidate.TryReserve(worker, StrategyProductionStorage.HaulerCarryLimit))
                {
                    continue;
                }

                pile = candidate;
                resource = candidate.resource;
                StrategyDebugLogger.Info(
                    "Logistics",
                    "LooseFoodReservedForGranary",
                    StrategyDebugLogger.F("origin", candidate.origin),
                    StrategyDebugLogger.F("resource", candidate.resource),
                    StrategyDebugLogger.F("amount", candidate.amount),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin));
                return true;
            }

            return false;
        }

        public static bool TryReserveNearestForHouse(
            StrategyPlacedBuilding house,
            StrategyResidentAgent resident,
            int radius,
            Func<StrategyResourceType, bool> acceptsResource,
            out StrategyLooseCarriedResourcePile pile,
            out Vector2Int pickupCell)
        {
            pile = null;
            pickupCell = default;
            if (house == null || resident == null || acceptsResource == null)
            {
                return false;
            }

            int maxDistanceSqr = Mathf.Max(1, radius) * Mathf.Max(1, radius);
            StrategyLooseCarriedResourcePile[] piles = GetPilesSortedByDistance(house.FootprintBounds.center);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseCarriedResourcePile candidate = piles[i];
                if (candidate == null
                    || !IsHouseholdForage(candidate.resource)
                    || !acceptsResource(candidate.resource)
                    || candidate.amount <= 0
                    || candidate.reservedBy != null
                    || (candidate.origin - house.Origin).sqrMagnitude > maxDistanceSqr
                    || !candidate.TryFindPickupCell(out pickupCell))
                {
                    continue;
                }

                if (!candidate.TryReserve(resident, StrategyProductionStorage.HaulerCarryLimit))
                {
                    continue;
                }

                pile = candidate;
                StrategyDebugLogger.Info(
                    "Forage",
                    "LooseForageReservedForHouse",
                    StrategyDebugLogger.F("origin", candidate.origin),
                    StrategyDebugLogger.F("resource", candidate.resource),
                    StrategyDebugLogger.F("amount", candidate.amount),
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("homeOrigin", house.Origin));
                return true;
            }

            return false;
        }

        public bool TryReserve(object owner)
        {
            return TryReserve(owner, amount);
        }

        public bool TryReserve(object owner, int maxAmount)
        {
            if (owner == null || amount <= 0 || maxAmount <= 0)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            reservedBy = owner;
            reservedAmount = Mathf.Min(amount, maxAmount);
            UpdateVisual();
            return reservedAmount > 0;
        }

        public bool IsReservedBy(object owner)
        {
            return owner != null && reservedBy == owner;
        }

        public void ReleaseReservation(object owner)
        {
            if (owner != null && reservedBy == owner)
            {
                reservedBy = null;
                reservedAmount = 0;
                UpdateVisual();
            }
        }

        public bool TryTakeReserved(object owner, out StrategyResourceType takenResource, out int takenAmount)
        {
            bool taken = TryTakeReserved(owner, out StrategyLooseResourcePickup pickup);
            takenResource = pickup.Resource;
            takenAmount = pickup.Amount;
            return taken;
        }

        public int GetReservedResourceAmount(StrategyResourceType requestedResource)
        {
            return requestedResource == resource ? Mathf.Max(0, reservedAmount) : 0;
        }

        public bool TryFindPickupCell(out Vector2Int cell)
        {
            if (map != null && map.IsCellWalkable(origin))
            {
                cell = origin;
                return true;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map != null && map.IsCellWalkable(candidate))
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

        private void Configure(
            CityMapController mapController,
            Vector2Int pileOrigin,
            Vector3 world,
            StrategyResourceType pileResource,
            int pileAmount)
        {
            map = mapController;
            origin = pileOrigin;
            resource = pileResource;
            resourceStore.Bind(this, StrategyResourceStoreScope.Loose);
            amount = Mathf.Max(1, pileAmount);
            footprintBounds = map != null && map.TryGetCell(origin.x, origin.y, out _)
                ? map.GetCellRectWorld(origin, Vector2Int.one)
                : new Bounds(world, Vector3.one);
            transform.position = new Vector3(world.x, world.y, -0.115f);
            EnsureRenderer();
            UpdateVisual();
            StrategyDebugLogger.Warn(
                "Logistics",
                "LooseCarriedResourceCreated",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount));
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        private void UpdateVisual()
        {
            EnsureRenderer();
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResourceIconFactory.GetSprite(resource);
            spriteRenderer.color = reservedBy != null
                ? new Color(0.94f, 0.96f, 0.84f, 1f)
                : Color.white;
            float amountScale = Mathf.Lerp(0.82f, 1.12f, Mathf.Clamp01((amount - 1) / 4f));
            transform.localScale = Vector3.one * amountScale;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 2);
            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0.02f, -0.03f),
                new Vector2(0.28f * amountScale, 0.10f * amountScale),
                0.16f,
                -4,
                0f,
                false);
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Turnip => "Turnip",
                StrategyResourceType.Cabbage => "Cabbage",
                StrategyResourceType.Onion => "Onion",
                StrategyResourceType.Carrot => "Carrot",
                StrategyResourceType.Potato => "Potato",
                StrategyResourceType.Berries => "Berries",
                StrategyResourceType.Roots => "Roots",
                StrategyResourceType.Mushrooms => "Mushrooms",
                StrategyResourceType.Game => "Game",
                StrategyResourceType.Fish => "Fish",
                StrategyResourceType.Logs => "Logs",
                StrategyResourceType.Stone => "Stone",
                StrategyResourceType.Iron => "Iron",
                StrategyResourceType.Coal => "Coal",
                StrategyResourceType.Clay => "Clay",
                StrategyResourceType.Pottery => "Pottery",
                StrategyResourceType.Planks => "Planks",
                StrategyResourceType.Tools => "Tools",
                _ => type.ToString()
            };
        }

        private static bool IsGranaryFood(StrategyResourceType type)
        {
            return type == StrategyResourceType.Game
                || type == StrategyResourceType.Fish
                || type == StrategyResourceType.Eggs
                || type == StrategyResourceType.Berries
                || type == StrategyResourceType.Roots
                || type == StrategyResourceType.Mushrooms;
        }

        private static bool IsHouseholdForage(StrategyResourceType type)
        {
            return type == StrategyResourceType.Berries
                || type == StrategyResourceType.Roots
                || type == StrategyResourceType.Mushrooms;
        }

        private static void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Loose Carried Resources");
            root = rootObject.transform;
        }

        private static StrategyLooseCarriedResourcePile[] GetPilesSortedByDistance(Vector3 nearWorld)
        {
            StrategyLooseCarriedResourcePile[] piles = UnityEngine.Object.FindObjectsByType<StrategyLooseCarriedResourcePile>();
            Array.Sort(
                piles,
                (left, right) =>
                {
                    if (left == null && right == null)
                    {
                        return 0;
                    }

                    if (left == null)
                    {
                        return 1;
                    }

                    if (right == null)
                    {
                        return -1;
                    }

                    float leftDistance = (left.FootprintBounds.center - nearWorld).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - nearWorld).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });
            return piles;
        }
    }
}
