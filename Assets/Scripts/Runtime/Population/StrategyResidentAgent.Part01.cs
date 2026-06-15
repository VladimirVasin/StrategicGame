using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        public bool TryStartHouseholdForaging(StrategyForageNode node, Vector2Int workCell)
        {
            if (node == null || !node.IsReservedBy(this) || !CanStartHouseholdForagingForHome(home))
            {
                return false;
            }

            activeForageNode = node;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activeGarden = null;
            activity = ResidentActivity.MovingToForage;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.04f, 0.18f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", node.ResourceType),
                    StrategyDebugLogger.F("nodeCell", node.Cell));
                return true;
            }

            node.Release(this);
            activeForageNode = null;
            activity = ResidentActivity.Idle;
            waitTimer = Random.Range(0.35f, 0.85f);
            StrategyDebugLogger.Warn(
                "Forage",
                "ResidentForagePathRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", node.ResourceType),
                StrategyDebugLogger.F("nodeCell", node.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }

        public bool TryStartHouseholdLooseForagePickup(
            StrategyLooseCarriedResourcePile pile,
            Vector2Int pickupCell)
        {
            if (pile == null || !pile.IsReservedBy(this) || !CanStartHouseholdForagingForHome(home))
            {
                return false;
            }

            StrategyResourceType resource = pile.Resource;
            if (resource != StrategyResourceType.Berries
                && resource != StrategyResourceType.Roots
                && resource != StrategyResourceType.Mushrooms)
            {
                return false;
            }

            activeLooseForageSource = pile;
            activeForageNode = null;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activeGarden = null;
            activity = ResidentActivity.MovingToLooseForagePickup;
            if (TryBuildPathTo(pickupCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.04f, 0.18f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentLooseForagePickupStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("pileOrigin", pile.Origin),
                    StrategyDebugLogger.F("pickupCell", pickupCell));
                return true;
            }

            pile.ReleaseReservation(this);
            activeLooseForageSource = null;
            activity = ResidentActivity.Idle;
            waitTimer = Random.Range(0.35f, 0.85f);
            StrategyDebugLogger.Warn(
                "Forage",
                "ResidentLooseForagePathRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("pileOrigin", pile.Origin),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return false;
        }

        public void Configure(
            CityMapController mapController,
            StrategyPlacedBuilding homeBuilding,
            StrategyResidentGender residentGender,
            int visualVariant,
            string fullName,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            Vector2Int initialIdleOrigin,
            Vector2Int initialIdleFootprint,
            int residentIdentifier = 0,
            float initialAgeYears = AdultAgeYears,
            StrategyResidentLifeStage initialLifeStage = StrategyResidentLifeStage.Adult,
            int fatherIdentifier = 0,
            int motherIdentifier = 0,
            string residentFamilyName = null)
        {
            map = mapController;
            population = GetComponentInParent<StrategyPopulationController>();
            home = homeBuilding;
            idleOrigin = initialIdleOrigin;
            idleFootprint = new Vector2Int(
                Mathf.Max(1, initialIdleFootprint.x),
                Mathf.Max(1, initialIdleFootprint.y));
            gender = residentGender;
            VisualVariant = visualVariant;
            FullName = string.IsNullOrWhiteSpace(fullName)
                ? GetFallbackName(residentGender, visualVariant)
                : fullName;
            FamilyName = string.IsNullOrWhiteSpace(residentFamilyName)
                ? ExtractFamilyName(FullName)
                : residentFamilyName;
            residentId = residentIdentifier;
            fatherId = fatherIdentifier;
            motherId = motherIdentifier;
            ageYears = Mathf.Max(0f, initialAgeYears);
            lastMortalityAgeChecked = Mathf.FloorToInt(ageYears);
            lifeStage = initialLifeStage == StrategyResidentLifeStage.Child && ageYears < AdultAgeYears
                ? StrategyResidentLifeStage.Child
                : StrategyResidentLifeStage.Adult;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.08f);
            transform.localScale = Vector3.one;
            UseIdleSprite();
            UpdateWorldSorting();
            waitTimer = Random.Range(0.35f, 1.1f);
            gardenWorkCooldown = Random.Range(2.5f, 6.5f);
            lumberWorkCooldown = Random.Range(1.5f, 4.5f);
            stoneWorkCooldown = Random.Range(1.5f, 4.5f);
            logisticsWorkCooldown = Random.Range(1.0f, 3.0f);
            home?.TryRegisterResident(this);
            EnsureReadabilityRenderers();
            SyncReadabilityRenderers();
            EnsureClickCollider();
            EnsureFootstepAudio();
            if (IsHomeboundYoungChild)
            {
                EnterHomeboundChildState(false);
            }
        }

        public void PrepareForDeath()
        {
            if (deathRequested)
            {
                return;
            }

            deathRequested = true;
            DropCarriedResourcesOnDeath();
            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(false);
            CancelHouseholdFoodWork(false);
            activeGarden = null;
            home?.UnregisterResident(this);
            home = null;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            SetFishingLineVisible(false);

            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void DropCarriedResourcesOnDeath()
        {
            int droppedLogs = carriedLogAmount;
            int droppedStone = carriedStoneAmount;
            int droppedGame = carriedGameAmount;
            int droppedFish = carriedFishAmount;
            int droppedForage = carriedForageAmount;
            StrategyResourceType droppedForageResource = carriedForageResource;
            if (droppedLogs <= 0
                && droppedStone <= 0
                && droppedGame <= 0
                && droppedFish <= 0
                && droppedForage <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                if (droppedLogs > 0 || droppedStone > 0)
                {
                    StrategyLooseConstructionResourcePile.Create(
                        map,
                        cell,
                        transform.position,
                        droppedLogs,
                        droppedStone);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "CarriedConstructionResourcesDroppedOnDeath",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("origin", cell),
                        StrategyDebugLogger.F("logs", droppedLogs),
                        StrategyDebugLogger.F("stone", droppedStone),
                        StrategyDebugLogger.F("reservation", "cleared"));
                }

                DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Game, droppedGame);
                DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Fish, droppedFish);
                DropLooseCarriedResourceOnDeath(cell, droppedForageResource, droppedForage);
            }
            else
            {
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourcesLostOnDeath",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("logs", droppedLogs),
                    StrategyDebugLogger.F("stone", droppedStone),
                    StrategyDebugLogger.F("game", droppedGame),
                    StrategyDebugLogger.F("fish", droppedFish),
                    StrategyDebugLogger.F("forageResource", droppedForageResource),
                    StrategyDebugLogger.F("forage", droppedForage),
                    StrategyDebugLogger.F("reason", "no_map_cell"));
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            ClearCarriedConstructionReturnReservation();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
        }

        private void DropLooseCarriedResourceOnDeath(Vector2Int cell, StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            StrategyLooseCarriedResourcePile.Create(
                map,
                cell,
                transform.position,
                resource,
                amount);
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourceDroppedOnDeath",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("origin", cell),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("reservation", "cleared"));
        }

        public bool TryStartFuneralMove(Vector3 targetWorld, ResidentActivity funeralMoveActivity)
        {
            if (map == null
                || deathRequested
                || IsPendingRefugee
                || IsHomeboundYoungChild
                || !IsFuneralMoveActivity(funeralMoveActivity))
            {
                return false;
            }

            returnCarriedResourcesImmediately = true;
            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(true);
            CancelHouseholdFoodWork(true);
            returnCarriedResourcesImmediately = false;
            activeGarden = null;
            funeralTimer = 0f;

            bool hasGridPath = map.TryWorldToCell(targetWorld, out Vector2Int targetCell)
                && TryBuildPathTo(targetCell);
            if (!hasGridPath)
            {
                path.Clear();
                pathIndex = 0;
                hasTarget = false;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "ResidentFuneralMoveFailed",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("activity", funeralMoveActivity),
                    StrategyDebugLogger.F("targetWorld", targetWorld),
                    StrategyDebugLogger.F("reason", "no_walkable_path"));
                return false;
            }

            activity = funeralMoveActivity;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            UseIdleSprite();

            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("activity", funeralMoveActivity),
                StrategyDebugLogger.F("targetWorld", targetWorld));
            return hasTarget;
        }

        public void StartFuneralMourning(float seconds)
        {
            StartTimedFuneralActivity(ResidentActivity.MourningCorpse, seconds);
        }

        public void StartFuneralBurial(float seconds)
        {
            StartTimedFuneralActivity(ResidentActivity.BuryingGrave, seconds);
        }

        public void EndFuneralDuty()
        {
            if (!IsFuneralActivity(activity))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            funeralTimer = 0f;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.9f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            UseIdleSprite();
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "Funeral",
                "ResidentFuneralDutyEnded",
                StrategyDebugLogger.F("resident", FullName));
        }

        private void StartTimedFuneralActivity(ResidentActivity funeralActivity, float seconds)
        {
            if (deathRequested || IsPendingRefugee || IsHomeboundYoungChild)
            {
                return;
            }

            activity = funeralActivity;
            funeralTimer = Mathf.Max(0.5f, seconds);
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        public void AddChildId(int childIdentifier)
        {
            if (childIdentifier > 0 && !childIds.Contains(childIdentifier))
            {
                childIds.Add(childIdentifier);
            }
        }

        public void SetPendingRefugee(bool pending)
        {
            IsPendingRefugee = pending;
            if (!pending && activity == ResidentActivity.ArrivingAsRefugee)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
            }
        }

        public void SetCampIdleOrigin(Vector2Int origin)
        {
            idleOrigin = origin;
            idleFootprint = Vector2Int.one;
            if (home == null && !IsRefugeeTraveling)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.85f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }
        }

        public bool FollowRefugeePath(IReadOnlyList<Vector3> worldPath, bool leaving)
        {
            path.Clear();
            if (worldPath != null)
            {
                for (int i = 0; i < worldPath.Count; i++)
                {
                    Vector3 point = worldPath[i];
                    path.Add(new Vector3(point.x, point.y, -0.08f));
                }
            }

            pathIndex = 0;
            hasTarget = path.Count > 0;
            activity = leaving ? ResidentActivity.LeavingSettlement : ResidentActivity.ArrivingAsRefugee;
            waitTimer = 0f;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            if (!hasTarget)
            {
                activity = ResidentActivity.Idle;
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
            }

            return hasTarget;
        }
    }
}
