using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void StartAimingBow()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeHuntTarget == null || !activeHuntTarget.IsAlive || activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.AimingBow;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            bowShotReleased = false;
            huntingWorkTimer = 0.35f;
            FaceWorldPoint(activeHuntTarget.transform.position);
            StrategyDebugLogger.Info(
                "Hunting",
                "BowAimingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitWorld", activeHuntTarget.transform.position),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateAimingBow()
        {
            if (activeHuntTarget == null)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (activeHuntTarget.IsCarcass)
            {
                TryMoveToHuntCarcass();
                return;
            }

            if (!activeHuntTarget.IsAlive)
            {
                activity = ResidentActivity.WaitingForHuntHit;
                huntingWorkTimer = 1.2f;
                return;
            }

            FaceWorldPoint(activeHuntTarget.transform.position);
            AnimateBowWork();
            if (!bowShotReleased)
            {
                return;
            }

            huntingWorkTimer -= Time.deltaTime;
            if (huntingWorkTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingForHuntHit;
            huntingWorkTimer = 1.8f;
        }

        private void UpdateWaitingForHuntHit()
        {
            if (activeHuntTarget == null)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (activeHuntTarget.IsCarcass)
            {
                TryMoveToHuntCarcass();
                return;
            }

            if (activeHuntTarget.IsAlive)
            {
                huntingWorkTimer -= Time.deltaTime;
                ApplyBowFrame(9);
                if (huntingWorkTimer <= 0f)
                {
                    ResetHunterWorkToIdle(true);
                }

                return;
            }

            ApplyBowFrame(10);
        }

        private void TryMoveToHuntCarcass()
        {
            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (!activeHuntTarget.TryGetCurrentCell(out Vector2Int carcassCell))
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (!TryBuildPathTo(carcassCell))
            {
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int candidate = carcassCell + CardinalDirections[i];
                    if (map.IsCellWalkable(candidate) && TryBuildPathTo(candidate))
                    {
                        activity = ResidentActivity.MovingToHuntCarcass;
                        hasTarget = true;
                        waitTimer = Random.Range(0.04f, 0.18f);
                        return;
                    }
                }

                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.MovingToHuntCarcass;
            hasTarget = true;
            waitTimer = Random.Range(0.04f, 0.18f);
            StrategyDebugLogger.Info(
                "Hunting",
                "CarcassMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("carcassCell", carcassCell),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void StartButcheringRabbit()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.ButcheringRabbit;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(activeHuntTarget.transform.position);
            StrategyDebugLogger.Info(
                "Hunting",
                "ButcheringStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitWorld", activeHuntTarget.transform.position),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateButcheringRabbit()
        {
            if (activeHuntTarget == null || !activeHuntTarget.IsCarcass)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            FaceWorldPoint(activeHuntTarget.transform.position);
            AnimateButcherWork();
        }

        private void StartCarryingGame(int amount)
        {
            if (amount <= 0)
            {
                ResetHunterWorkToIdle(false);
                return;
            }

            if (hunterWorkplace == null
                || !hunterWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (hunterWorkplace != null)
                {
                    hunterWorkplace.AddGame(amount);
                }

                activeHuntTarget = null;
                carriedGameAmount = 0;
                ResetHunterWorkToIdle(false);
                StrategyDebugLogger.Warn(
                    "Hunting",
                    "GameCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
                return;
            }

            carriedGameAmount = amount;
            activeHuntTarget = null;
            activity = ResidentActivity.CarryingGame;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedGameVisible(true);
            StrategyDebugLogger.Info(
                "Hunting",
                "GameCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void StartDepositingGame()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGame;
            huntingWorkTimer = Random.Range(HuntingDepositSecondsMin, HuntingDepositSecondsMax);
            if (hunterWorkplace != null)
            {
                FaceWorldPoint(hunterWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Hunting",
                "GameDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedGameAmount),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGame()
        {
            huntingWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedGameVisible(true);
            if (huntingWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedGameAmount;
            if (hunterWorkplace != null)
            {
                hunterWorkplace.AddGame(depositedAmount);
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            StrategyDebugLogger.Info(
                "Hunting",
                "GameDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("campStock", hunterWorkplace != null ? hunterWorkplace.GameStored : -1));
            CompleteHunterDelivery();
        }

        private void CompleteLumberDelivery()
        {
            activeTree = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();

            if (workplace != null
                && workplace.TryFindPlantingCell(out Vector2Int cell)
                && TryMoveToPlantingCell(cell))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            lumberWorkCooldown = Random.Range(4.0f, 8.0f);
            waitTimer = Random.Range(0.45f, 1.1f);
        }
    }
}
