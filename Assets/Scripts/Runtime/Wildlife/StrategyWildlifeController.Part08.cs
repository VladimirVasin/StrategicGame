using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private void ClearWildlife()
        {
            ReleaseAllCombatEncounterThreats();
            deer.Clear();
            breedCooldowns.Clear();
            deerMigrations.Clear();
            rabbits.Clear();
            rabbitBreedCooldowns.Clear();
            rabbitMigrations.Clear();
            fish.Clear();
            fishBreedCooldowns.Clear();
            fishMigrations.Clear();
            fishLakeBirthBlockedLogTimes.Clear();
            lakeFishRegions.Clear();
            lakeRegionByCell.Clear();
            riverRouteCells.Clear();
            birds.Clear();
            birdMigrations.Clear();
            wolfPacks.Clear();
            wolves.Clear();
            combatEncounterWolves.Clear();
            wolfMigrations.Clear();
            wolfResidentTargets.Clear();
            settlementBuildings = null;
            settlementConstructionSites = null;
            if (wildlifeRoot == null)
            {
                return;
            }

            for (int i = wildlifeRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = wildlifeRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void RemoveMissingWolves()
        {
            for (int i = wolves.Count - 1; i >= 0; i--)
            {
                StrategyWolfAgent wolf = wolves[i];
                if (wolf != null)
                {
                    continue;
                }

                wolves.RemoveAt(i);
            }

            for (int i = wolfPacks.Count - 1; i >= 0; i--)
            {
                StrategyWolfPack pack = wolfPacks[i];
                if (pack == null)
                {
                    wolfPacks.RemoveAt(i);
                    continue;
                }

                pack.RemoveMissingMembers();
                if (pack.MemberCount <= 0)
                {
                    wolfPacks.RemoveAt(i);
                }
            }

            RemoveMissingWolfResidentTargets();
        }

        private void RemoveMissingWolfResidentTargets()
        {
            if (wolfResidentTargets.Count <= 0)
            {
                return;
            }

            List<StrategyResidentAgent> missing = null;
            foreach (KeyValuePair<StrategyResidentAgent, StrategyWolfAgent> pair in wolfResidentTargets)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    continue;
                }

                missing ??= new List<StrategyResidentAgent>();
                missing.Add(pair.Key);
            }

            if (missing == null)
            {
                return;
            }

            for (int i = 0; i < missing.Count; i++)
            {
                wolfResidentTargets.Remove(missing[i]);
            }
        }

        private void RemoveMissingDeer()
        {
            for (int i = deer.Count - 1; i >= 0; i--)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null)
                {
                    continue;
                }

                deer.RemoveAt(i);
            }

            List<StrategyDeerAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyDeerAgent, float> pair in breedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyDeerAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                breedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void RemoveMissingRabbits()
        {
            for (int i = rabbits.Count - 1; i >= 0; i--)
            {
                StrategyRabbitAgent agent = rabbits[i];
                if (agent != null)
                {
                    continue;
                }

                rabbits.RemoveAt(i);
            }

            List<StrategyRabbitAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyRabbitAgent, float> pair in rabbitBreedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyRabbitAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                rabbitBreedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void RemoveMissingFish()
        {
            for (int i = fish.Count - 1; i >= 0; i--)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null)
                {
                    continue;
                }

                fish.RemoveAt(i);
            }

            List<StrategyFishAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyFishAgent, float> pair in fishBreedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyFishAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                fishBreedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void RemoveMissingBirds()
        {
            for (int i = birds.Count - 1; i >= 0; i--)
            {
                if (birds[i] == null)
                {
                    birds.RemoveAt(i);
                }
            }
        }

        private void RemoveMissingWildlifeForFogVisibility()
        {
            RemoveMissingDeer();
            RemoveMissingRabbits();
            RemoveMissingFish();
            RemoveMissingBirds();
            RemoveMissingWolves();
        }

        private void OnDestroy()
        {
            ReleaseAllCombatEncounterThreats();
            if (Active == this)
            {
                Active = null;
            }
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            return Hash(seed, x, y, salt, 0) / (float)int.MaxValue;
        }

        private static int Hash(int seed, int a, int b, int c, int d)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h = h * 1597334677 + d * 381201580;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }
    }
}
