using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHouseholdForagingState : MonoBehaviour
    {
        private const float CheckIntervalMin = 6f;
        private const float CheckIntervalMax = 10f;
        private const int AdultSearchRadius = 10;
        private const int ChildSearchRadius = 6;
        private const int BerriesCap = 8;
        private const int RootsCap = 8;
        private const int MushroomsCap = 6;

        private StrategyPlacedBuilding house;
        private float checkTimer;

        private void Awake()
        {
            house = GetComponent<StrategyPlacedBuilding>();
            ResetTimer();
        }

        private void Update()
        {
            if (house == null)
            {
                house = GetComponent<StrategyPlacedBuilding>();
            }

            if (house == null || house.Tool != StrategyBuildTool.House || house.ResidentCount <= 0)
            {
                return;
            }

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
            {
                return;
            }

            ResetTimer();
            TryDispatchForagers();
        }

        private void TryDispatchForagers()
        {
            if (!StrategyDayNightCycleController.IsHouseholdOutdoorWorkTime || !HasForageStorageRoom())
            {
                return;
            }

            StrategyForageResourceController forage = StrategyForageResourceController.Active;
            if (forage == null)
            {
                return;
            }

            int activeForagers = CountActiveForagers();
            int maxForagers = house.ResidentCount >= 4 ? 2 : 1;
            if (activeForagers >= maxForagers)
            {
                return;
            }

            for (int i = 0; i < house.Residents.Count && activeForagers < maxForagers; i++)
            {
                StrategyResidentAgent resident = house.Residents[i];
                if (resident == null || !resident.CanStartHouseholdForagingForHome(house))
                {
                    continue;
                }

                int radius = resident.IsAdult ? AdultSearchRadius : ChildSearchRadius;
                if (StrategyLooseCarriedResourcePile.TryReserveNearestForHouse(
                        house,
                        resident,
                        radius,
                        IsForageResourceBelowCap,
                        out StrategyLooseCarriedResourcePile loosePile,
                        out Vector2Int loosePickupCell))
                {
                    if (resident.TryStartHouseholdLooseForagePickup(loosePile, loosePickupCell))
                    {
                        activeForagers++;
                        continue;
                    }

                    loosePile.ReleaseReservation(resident);
                }

                if (!forage.TryReserveForHouse(
                        house,
                        resident,
                        radius,
                        IsForageResourceBelowCap,
                        out StrategyForageNode node,
                        out Vector2Int workCell))
                {
                    continue;
                }

                if (resident.TryStartHouseholdForaging(node, workCell))
                {
                    activeForagers++;
                    continue;
                }

                node.Release(resident);
            }
        }

        private int CountActiveForagers()
        {
            int count = 0;
            for (int i = 0; i < house.Residents.Count; i++)
            {
                StrategyResidentAgent resident = house.Residents[i];
                if (resident != null && resident.IsHouseholdForaging)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasForageStorageRoom()
        {
            return IsForageResourceBelowCap(StrategyResourceType.Berries)
                || IsForageResourceBelowCap(StrategyResourceType.Roots)
                || IsForageResourceBelowCap(StrategyResourceType.Mushrooms);
        }

        private bool IsForageResourceBelowCap(StrategyResourceType resource)
        {
            StrategyHouseResourceStore store = house != null ? house.Resources : null;
            int amount = store != null ? store.GetAmount(resource) : 0;
            return resource switch
            {
                StrategyResourceType.Berries => amount < BerriesCap,
                StrategyResourceType.Roots => amount < RootsCap,
                StrategyResourceType.Mushrooms => amount < MushroomsCap,
                _ => false
            };
        }

        private void ResetTimer()
        {
            checkTimer = Random.Range(CheckIntervalMin, CheckIntervalMax);
        }
    }
}
