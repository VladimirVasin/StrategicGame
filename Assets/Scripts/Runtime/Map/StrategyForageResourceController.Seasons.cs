using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForageResourceController
    {
        private int GetSeasonalInitialForageNodeTarget()
        {
            StrategySeasonGameplayProfile profile = GetCurrentSeasonGameplayProfile();
            return Mathf.Clamp(
                Mathf.RoundToInt(InitialForageNodeTarget * profile.InitialForageMultiplier),
                40,
                MaxForageNodes);
        }

        private float GetSeasonalForageChance(CityMapCellKind kind)
        {
            return GetForageChance(kind) * GetCurrentSeasonGameplayProfile().ForageChanceMultiplier;
        }

        private float GetSeasonalForageRespawnDelay(StrategyResourceType resource)
        {
            float multiplier = GetCurrentSeasonGameplayProfile().ForageRespawnDelayMultiplier;
            if (resource == StrategyResourceType.Roots && StrategyDayNightCycleController.CurrentCalendarSnapshot.Season == StrategySeason.Winter)
            {
                multiplier *= 0.72f;
            }

            return Random.Range(RespawnSecondsMin, RespawnSecondsMax) * multiplier;
        }

        private float GetSeasonalCampSupportSpawnDelay()
        {
            return Random.Range(CampSupportSpawnSecondsMin, CampSupportSpawnSecondsMax)
                * GetCurrentSeasonGameplayProfile().CampSupportDelayMultiplier;
        }

        private int GetSeasonalCampSupportTarget()
        {
            return Mathf.Max(
                2,
                Mathf.RoundToInt(CampForageSupportTarget * GetCurrentSeasonGameplayProfile().CampSupportTargetMultiplier));
        }

        private int GetSeasonalCampForageSoftCap()
        {
            return Mathf.Max(
                GetSeasonalCampSupportTarget() + 2,
                Mathf.RoundToInt(CampForageSoftCap * GetCurrentSeasonGameplayProfile().CampSupportTargetMultiplier));
        }

        private bool ShouldKeepPreferredRespawnResource(StrategyResourceType resource, Vector2Int cell, int salt)
        {
            StrategySeason season = StrategyDayNightCycleController.CurrentCalendarSnapshot.Season;
            if (season != StrategySeason.Winter)
            {
                return true;
            }

            float chance = resource == StrategyResourceType.Roots ? 0.70f : 0.16f;
            return Hash01(map.ActiveSeed, cell.x, cell.y, salt + 811) <= chance;
        }

        private StrategyResourceType ChooseSeasonalForageResource(CityMapCellKind kind, float pick)
        {
            StrategySeasonGameplayProfile profile = GetCurrentSeasonGameplayProfile();
            GetTerrainForageWeights(kind, out float berries, out float roots, out float mushrooms);
            berries *= profile.BerriesWeight;
            roots *= profile.RootsWeight;
            mushrooms *= profile.MushroomsWeight;

            float total = berries + roots + mushrooms;
            if (total <= 0f)
            {
                return StrategyResourceType.None;
            }

            float roll = Mathf.Clamp01(pick) * total;
            if (ConsumeWeight(ref roll, berries))
            {
                return StrategyResourceType.Berries;
            }

            if (ConsumeWeight(ref roll, roots))
            {
                return StrategyResourceType.Roots;
            }

            return mushrooms > 0f ? StrategyResourceType.Mushrooms : StrategyResourceType.None;
        }

        private static void GetTerrainForageWeights(
            CityMapCellKind kind,
            out float berries,
            out float roots,
            out float mushrooms)
        {
            berries = 0f;
            roots = 0f;
            mushrooms = 0f;
            switch (kind)
            {
                case CityMapCellKind.Forest:
                    mushrooms = 0.56f;
                    berries = 0.26f;
                    roots = 0.18f;
                    break;
                case CityMapCellKind.Meadow:
                    berries = 0.56f;
                    roots = 0.26f;
                    mushrooms = 0.18f;
                    break;
                case CityMapCellKind.Grass:
                    roots = 0.52f;
                    berries = 0.30f;
                    mushrooms = 0.18f;
                    break;
                case CityMapCellKind.Dirt:
                    roots = 0.75f;
                    mushrooms = 0.25f;
                    break;
            }
        }

        private static bool ConsumeWeight(ref float roll, float weight)
        {
            if (weight <= 0f)
            {
                return false;
            }

            if (roll <= weight)
            {
                return true;
            }

            roll -= weight;
            return false;
        }

        private static StrategySeasonGameplayProfile GetCurrentSeasonGameplayProfile()
        {
            return StrategySeasonCalendar.GetGameplayProfile(StrategyDayNightCycleController.CurrentCalendarSnapshot.Season);
        }
    }
}
