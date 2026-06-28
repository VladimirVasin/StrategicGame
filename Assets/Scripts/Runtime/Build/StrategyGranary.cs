using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyGranary : MonoBehaviour
    {
        public const int MaxWorkers = 2;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer gameStockRenderer;
        private SpriteRenderer fishStockRenderer;
        private SpriteRenderer eggStockRenderer;
        private object householdFoodReservationOwner;
        private StrategyResourceType householdFoodReservedResource = StrategyResourceType.None;
        private int householdFoodReservedAmount;
        private int gameStored;
        private int fishStored;
        private int eggsStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int GameStored => gameStored;
        public int FishStored => fishStored;
        public int EggsStored => eggsStored;
        public int TotalFoodStored => gameStored + fishStored + eggsStored + ForageStored;
        public float TotalRationValue => gameStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Game)
            + fishStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Fish)
            + eggsStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Eggs)
            + GetStoredForageRations();
        public float AvailableHouseholdRationValue => GetAvailableFishForHouseholds() * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Fish)
            + GetAvailableGameForHouseholds() * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Game)
            + GetAvailableEggsForHouseholds() * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Eggs)
            + GetAvailableForageHouseholdRations();
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public static int GetTotalSettlementFood()
        {
            int total = 0;
            StrategyGranary[] granaries = Object.FindObjectsByType<StrategyGranary>();
            for (int i = 0; i < granaries.Length; i++)
            {
                StrategyGranary granary = granaries[i];
                if (granary != null)
                {
                    total += granary.TotalFoodStored;
                }
            }

            return total;
        }

        public static float GetTotalSettlementFoodRations()
        {
            float total = 0f;
            StrategyGranary[] granaries = Object.FindObjectsByType<StrategyGranary>();
            for (int i = 0; i < granaries.Length; i++)
            {
                StrategyGranary granary = granaries[i];
                if (granary != null)
                {
                    total += granary.AvailableHouseholdRationValue;
                }
            }

            return total;
        }

        public static int ConsumeSettlementFood(
            int requested,
            Vector3 requesterWorld,
            out int gameTaken,
            out int fishTaken)
        {
            gameTaken = 0;
            fishTaken = 0;
            int remaining = Mathf.Max(0, requested);
            if (remaining <= 0)
            {
                return 0;
            }

            List<StrategyGranary> granaries = new();
            StrategyGranary[] foundGranaries = Object.FindObjectsByType<StrategyGranary>();
            for (int i = 0; i < foundGranaries.Length; i++)
            {
                StrategyGranary granary = foundGranaries[i];
                if (granary != null && granary.TotalFoodStored > 0)
                {
                    granaries.Add(granary);
                }
            }

            granaries.Sort((left, right) =>
            {
                float leftDistance = (left.FootprintBounds.center - requesterWorld).sqrMagnitude;
                float rightDistance = (right.FootprintBounds.center - requesterWorld).sqrMagnitude;
                return leftDistance.CompareTo(rightDistance);
            });

            for (int i = 0; i < granaries.Count && remaining > 0; i++)
            {
                int consumed = granaries[i].ConsumeFood(remaining, out int granaryGame, out int granaryFish);
                remaining -= consumed;
                gameTaken += granaryGame;
                fishTaken += granaryFish;
            }

            return requested - remaining;
        }

        public static bool TryReserveNearestHouseholdFood(
            Vector3 requesterWorld,
            object owner,
            out StrategyGranary granary,
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int pickupCell)
        {
            granary = null;
            resource = StrategyResourceType.None;
            amount = 0;
            pickupCell = default;
            if (owner == null)
            {
                return false;
            }

            StrategyGranary[] granaries = GetGranariesSortedByDistance(requesterWorld);
            for (int i = 0; i < granaries.Length; i++)
            {
                StrategyGranary candidate = granaries[i];
                if (candidate == null
                    || candidate.AvailableHouseholdRationValue <= 0f
                    || !candidate.TryFindDropoffCell(out Vector2Int candidatePickupCell)
                    || !candidate.TryReserveHouseholdFood(owner, out StrategyResourceType reservedResource, out int reservedAmount))
                {
                    continue;
                }

                granary = candidate;
                resource = reservedResource;
                amount = reservedAmount;
                pickupCell = candidatePickupCell;
                return true;
            }

            return false;
        }

        public static float ConsumeSettlementFoodRations(
            float requestedRations,
            Vector3 requesterWorld,
            out int gameTaken,
            out int fishTaken)
        {
            gameTaken = 0;
            fishTaken = 0;
            float remaining = Mathf.Max(0f, requestedRations);
            if (remaining <= 0.01f)
            {
                return 0f;
            }

            float suppliedRations = 0f;
            List<StrategyGranary> granaries = new();
            StrategyGranary[] foundGranaries = Object.FindObjectsByType<StrategyGranary>();
            for (int i = 0; i < foundGranaries.Length; i++)
            {
                StrategyGranary granary = foundGranaries[i];
                if (granary != null && granary.AvailableHouseholdRationValue > 0f)
                {
                    granaries.Add(granary);
                }
            }

            granaries.Sort((left, right) =>
            {
                float leftDistance = (left.FootprintBounds.center - requesterWorld).sqrMagnitude;
                float rightDistance = (right.FootprintBounds.center - requesterWorld).sqrMagnitude;
                return leftDistance.CompareTo(rightDistance);
            });

            for (int i = 0; i < granaries.Count && remaining > 0.01f; i++)
            {
                float supplied = granaries[i].ConsumeFoodRations(remaining, out int granaryGame, out int granaryFish);
                remaining = Mathf.Max(0f, remaining - supplied);
                suppliedRations += supplied;
                gameTaken += granaryGame;
                fishTaken += granaryFish;
            }

            return suppliedRations;
        }

        public static bool TryFindNearestGranary(Vector3 nearWorld, out StrategyGranary granary)
        {
            StrategyGranary[] granaries = GetGranariesSortedByDistance(nearWorld);
            for (int i = 0; i < granaries.Length; i++)
            {
                if (granaries[i] != null)
                {
                    granary = granaries[i];
                    return true;
                }
            }

            granary = null;
            return false;
        }

        public static bool TryFindNearestDropoff(
            Vector3 nearWorld,
            out StrategyGranary granary,
            out Vector2Int dropoffCell)
        {
            StrategyGranary[] granaries = GetGranariesSortedByDistance(nearWorld);
            for (int i = 0; i < granaries.Length; i++)
            {
                granary = granaries[i];
                if (granary != null && granary.TryFindDropoffCell(out dropoffCell))
                {
                    return true;
                }
            }

            granary = null;
            dropoffCell = default;
            return false;
        }

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            EnsureStockRenderers();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("maxWorkers", MaxWorkers));
        }

        public bool CanAssignNextAvailableWorker()
        {
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableWorker(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            return AssignWorker(assigned);
        }

        public bool AssignWorker(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Count >= MaxWorkers
                || workers.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignGranaryWorkplace(this);
            StrategyDebugLogger.Info(
                "Granary",
                "WorkerAssigned",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("worker", resident.FullName),
                StrategyDebugLogger.F("workerCount", workers.Count));
            return true;
        }

        public void UnassignWorkerAt(int index)
        {
            if (index < 0 || index >= workers.Count)
            {
                return;
            }

            StrategyResidentAgent worker = workers[index];
            workers.RemoveAt(index);
            if (worker != null)
            {
                StrategyDebugLogger.Info(
                    "Granary",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("granaryOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearGranaryWorkplace(this);
            }
        }

        public void UnassignWorker(StrategyResidentAgent worker)
        {
            int index = workers.IndexOf(worker);
            if (index >= 0)
            {
                UnassignWorkerAt(index);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryReserveFoodSource(
            object owner,
            out StrategyResourceType resource,
            out StrategyHunterCamp gameSource,
            out StrategyFisherHut fishSource,
            out StrategyForagerCamp forageSource,
            out StrategyChickenCoop eggSource)
        {
            resource = StrategyResourceType.None;
            gameSource = null;
            fishSource = null;
            forageSource = null;
            eggSource = null;
            if (owner == null)
            {
                return false;
            }

            return TryReserveNearestFoodSource(owner, out resource, out gameSource, out fishSource, out forageSource, out eggSource);
        }

        public bool TryReserveHouseholdFood(
            object owner,
            out StrategyResourceType resource,
            out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null)
            {
                return false;
            }

            if (householdFoodReservationOwner != null && householdFoodReservationOwner != owner)
            {
                return false;
            }

            if (householdFoodReservationOwner == owner && householdFoodReservedAmount > 0)
            {
                resource = householdFoodReservedResource;
                amount = householdFoodReservedAmount;
                return true;
            }

            if (!TryChooseHouseholdFoodResource(out resource))
            {
                return false;
            }

            householdFoodReservationOwner = owner;
            householdFoodReservedResource = resource;
            householdFoodReservedAmount = 1;
            amount = householdFoodReservedAmount;
            StrategyDebugLogger.Info(
                "Granary",
                "HouseholdFoodReserved",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("gameStock", gameStored),
                StrategyDebugLogger.F("fishStock", fishStored),
                StrategyDebugLogger.F("eggStock", eggsStored),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }
    }
}
