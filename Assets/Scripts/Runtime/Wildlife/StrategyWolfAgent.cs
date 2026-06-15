using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyWolfBehaviorState
    {
        Idle,
        Roaming,
        Stalking,
        Chasing,
        Attacking,
        Feeding,
        AvoidingSettlement,
        Resting,
        Howling
    }

    internal sealed class StrategyWolfPack
    {
        private readonly List<StrategyWolfAgent> members = new();

        public StrategyWolfPack(int packId, Vector2Int denCell, int homeRadius)
        {
            PackId = packId;
            DenCell = denCell;
            RoamCenterCell = denCell;
            HomeRadius = Mathf.Max(6, homeRadius);
        }

        public int PackId { get; }
        public Vector2Int DenCell { get; }
        public Vector2Int RoamCenterCell { get; private set; }
        public int HomeRadius { get; }
        public IReadOnlyList<StrategyWolfAgent> Members => members;

        public int MemberCount
        {
            get
            {
                RemoveMissingMembers();
                return members.Count;
            }
        }

        public void AddMember(StrategyWolfAgent agent)
        {
            if (agent != null && !members.Contains(agent))
            {
                members.Add(agent);
            }
        }

        public void SetRoamCenter(Vector2Int cell)
        {
            RoamCenterCell = cell;
        }

        public void RemoveMissingMembers()
        {
            for (int i = members.Count - 1; i >= 0; i--)
            {
                if (members[i] == null)
                {
                    members.RemoveAt(i);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class StrategyWolfAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float WalkSpeed = 0.92f;
        private const float StalkSpeed = 0.68f;
        private const float RunSpeed = 2.18f;
        private const float TargetReachDistance = 0.045f;
        private const float AttackReachDistance = 0.38f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float TargetRefreshInterval = 0.32f;
        private const float HuntSearchIntervalMin = 2.0f;
        private const float HuntSearchIntervalMax = 4.8f;
        private const float HumanTargetChance = 0.18f;
        private const float MaxChaseDistance = 14.0f;
        private const float ReadabilityOutlineScale = 1.12f;
        private const float ReadabilityEffectScale = 0.78f;

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private static Sprite readabilityShadowSprite;

        private readonly List<Vector3> path = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyWildlifeController wildlife;
        private StrategyWolfPack pack;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private StrategyRabbitAgent targetRabbit;
        private StrategyDeerAgent targetDeer;
        private StrategyResidentAgent targetResident;
        private StrategyWolfBehaviorState state;
        private Vector3 feedingWorld;
        private Vector2Int homeCell;
        private int homeRadius;
        private int variant;
        private int pathIndex;
        private int frame;
        private int appliedFrame = -1;
        private float stateTimer;
        private float frameTimer;
        private float targetRefreshTimer;
        private float huntSearchTimer;
        private float roamRefreshTimer;
        private bool attackResolved;

        public int PackId => pack != null ? pack.PackId : -1;
        public int PackMemberCount => pack != null ? pack.MemberCount : 1;
        public StrategyWolfBehaviorState State => state;
        public Vector2Int HomeCell => homeCell;
        public int HomeRadius => homeRadius;

        internal void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController,
            StrategyWolfPack wolfPack,
            Vector2Int packHomeCell,
            int packHomeRadius,
            Vector3 spawnWorld,
            SpriteRenderer renderer,
            int visualVariant)
        {
            map = mapController;
            population = populationController;
            wildlife = wildlifeController;
            pack = wolfPack;
            homeCell = packHomeCell;
            homeRadius = Mathf.Max(6, packHomeRadius);
            spriteRenderer = renderer;
            variant = Mathf.Abs(visualVariant) % 4;
            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.074f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            state = StrategyWolfBehaviorState.Idle;
            stateTimer = Random.Range(0.5f, 1.6f);
            huntSearchTimer = Random.Range(0.4f, 2.0f);
            roamRefreshTimer = Random.Range(2.0f, 5.0f);
            ApplySprite(StrategyWolfSpritePose.Idle, Random.Range(0, StrategyWolfSpriteFactory.IdleFrameCount));
            EnsureClickCollider();
            EnsureReadabilityRenderers();
            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0.02f, -0.035f),
                new Vector2(0.72f, 0.30f),
                0.22f,
                -6,
                -8f,
                true);
            UpdateWorldSorting();
        }

        public void RetargetPackCenter(Vector2Int center, int radius)
        {
            homeCell = center;
            homeRadius = Mathf.Max(6, radius);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            bool hasCell = TryGetCurrentCell(out Vector2Int currentCell);
            string body = "Pack: "
                + PackId
                + "\nPack size: "
                + PackMemberCount
                + "\nState: "
                + State
                + "\nHome: "
                + homeCell.x
                + ", "
                + homeCell.y;
            info = new StrategyWorldInspectInfo(
                "Wolf",
                "Predator wildlife",
                body,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                currentCell,
                hasCell);
            return true;
        }

        private void Update()
        {
            if (map == null || spriteRenderer == null)
            {
                return;
            }

            huntSearchTimer -= Time.deltaTime;
            roamRefreshTimer -= Time.deltaTime;
            targetRefreshTimer -= Time.deltaTime;
            if (ShouldAvoidSettlementNow())
            {
                StartAvoidingSettlement();
            }

            switch (state)
            {
                case StrategyWolfBehaviorState.Roaming:
                    UpdateRoaming();
                    break;
                case StrategyWolfBehaviorState.Stalking:
                    UpdateStalking();
                    break;
                case StrategyWolfBehaviorState.Chasing:
                    UpdateChasing();
                    break;
                case StrategyWolfBehaviorState.Attacking:
                    UpdateAttacking();
                    break;
                case StrategyWolfBehaviorState.Feeding:
                    UpdateFeeding();
                    break;
                case StrategyWolfBehaviorState.AvoidingSettlement:
                    UpdateAvoidingSettlement();
                    break;
                case StrategyWolfBehaviorState.Resting:
                    UpdateResting();
                    break;
                case StrategyWolfBehaviorState.Howling:
                    UpdateHowling();
                    break;
                default:
                    UpdateIdle();
                    break;
            }
        }

        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private void UpdateIdle()
        {
            AnimateIdle();
            if (TryAcquireTarget())
            {
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            float roll = Random.value;
            if (roll < 0.58f && TryStartRoaming(false))
            {
                return;
            }

            if (roll < 0.74f)
            {
                StartHowling();
                return;
            }

            StartResting();
        }

        private void UpdateRoaming()
        {
            AnimateWalk();
            if (TryAcquireTarget())
            {
                return;
            }

            if (MoveAlongPath(WalkSpeed) || pathIndex >= path.Count)
            {
                StartIdle(Random.Range(0.35f, 1.25f));
                return;
            }

            if (roamRefreshTimer <= 0f)
            {
                roamRefreshTimer = Random.Range(2.5f, 5.2f);
                if (Vector2Int.Distance(GetCurrentCellOrHome(), homeCell) > homeRadius + 4)
                {
                    TryStartRoaming(true);
                }
            }
        }

        private void UpdateStalking()
        {
            if (!TryGetTargetWorld(out Vector3 targetWorld, out Vector2Int targetCell))
            {
                ReleaseTargets();
                StartIdle(Random.Range(0.35f, 0.9f));
                return;
            }

            if (wildlife != null && wildlife.IsWolfUnsafeSettlementCell(targetCell))
            {
                ReleaseTargets();
                StartAvoidingSettlement();
                return;
            }

            FaceWorldPoint(targetWorld);
            float distance = Vector2.Distance(transform.position, targetWorld);
            if (distance <= AttackReachDistance * 2.7f)
            {
                state = StrategyWolfBehaviorState.Chasing;
                path.Clear();
                pathIndex = 0;
                return;
            }

            if (targetRefreshTimer <= 0f)
            {
                targetRefreshTimer = TargetRefreshInterval * 1.8f;
                TryPathNearTarget(targetCell);
            }

            MoveAlongPath(StalkSpeed);
            AnimateStalk();
        }

        private void UpdateChasing()
        {
            if (!TryGetTargetWorld(out Vector3 targetWorld, out Vector2Int targetCell))
            {
                ReleaseTargets();
                StartIdle(Random.Range(0.3f, 0.8f));
                return;
            }

            float distance = Vector2.Distance(transform.position, targetWorld);
            if (distance > MaxChaseDistance || (wildlife != null && wildlife.IsWolfUnsafeSettlementCell(targetCell)))
            {
                ReleaseTargets();
                StartAvoidingSettlement();
                return;
            }

            FaceWorldPoint(targetWorld);
            if (distance <= AttackReachDistance)
            {
                StartAttack();
                return;
            }

            if (targetRefreshTimer <= 0f)
            {
                targetRefreshTimer = TargetRefreshInterval;
                TryPathNearTarget(targetCell);
            }

            if (!MoveAlongPath(RunSpeed))
            {
                Vector3 previous = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetWorld.x, targetWorld.y, transform.position.z), RunSpeed * Time.deltaTime);
                Vector3 delta = transform.position - previous;
                if (Mathf.Abs(delta.x) > 0.001f)
                {
                    spriteRenderer.flipX = delta.x < 0f;
                }
            }

            AnimateRun();
        }

        private void UpdateAttacking()
        {
            AnimateAttack();
            if (!attackResolved && frame >= 3)
            {
                ResolveAttack();
            }

            if (frame < StrategyWolfSpriteFactory.AttackFrameCount - 1)
            {
                return;
            }

            if (targetRabbit != null || targetDeer != null)
            {
                StartFeeding();
                return;
            }

            StartAvoidingSettlement();
        }

        private void UpdateFeeding()
        {
            stateTimer -= Time.deltaTime;
            AnimateEat();
            if (stateTimer > 0f)
            {
                return;
            }

            ConsumeAnimalTarget();
            ReleaseTargets();
            if (Random.value < 0.42f)
            {
                StartResting();
                return;
            }

            StartIdle(Random.Range(0.5f, 1.2f));
        }

        private void UpdateAvoidingSettlement()
        {
            AnimateRun();
            if (path.Count <= 0 || pathIndex >= path.Count)
            {
                if (!TryStartRoaming(true))
                {
                    StartIdle(Random.Range(0.5f, 1.4f));
                }

                return;
            }

            if (MoveAlongPath(RunSpeed) && !ShouldAvoidSettlementNow())
            {
                StartIdle(Random.Range(0.8f, 1.8f));
            }
        }

        private void UpdateResting()
        {
            stateTimer -= Time.deltaTime;
            AnimateIdle();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.4f, 1.2f));
            }
        }

        private void UpdateHowling()
        {
            stateTimer -= Time.deltaTime;
            AnimateHowl();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.7f, 1.6f));
            }
        }

        private bool TryAcquireTarget()
        {
            if (huntSearchTimer > 0f || wildlife == null || !TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            huntSearchTimer = Random.Range(HuntSearchIntervalMin, HuntSearchIntervalMax);
            if (Random.value < HumanTargetChance
                && wildlife.TryReserveWolfResidentTarget(this, currentCell, out StrategyResidentAgent resident))
            {
                targetResident = resident;
                state = StrategyWolfBehaviorState.Stalking;
                targetRefreshTimer = 0f;
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "WolfResidentTargetAcquired",
                    StrategyDebugLogger.F("pack", PackId),
                    StrategyDebugLogger.F("resident", resident != null ? resident.FullName : "none"),
                    StrategyDebugLogger.F("wolfCell", currentCell));
                return true;
            }

            if (wildlife.TryReserveWolfPrey(this, currentCell, out StrategyRabbitAgent rabbit, out StrategyDeerAgent deer))
            {
                targetRabbit = rabbit;
                targetDeer = deer;
                state = StrategyWolfBehaviorState.Stalking;
                targetRefreshTimer = 0f;
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "WolfPreyTargetAcquired",
                    StrategyDebugLogger.F("pack", PackId),
                    StrategyDebugLogger.F("prey", targetRabbit != null ? "rabbit" : "deer"),
                    StrategyDebugLogger.F("wolfCell", currentCell));
                return true;
            }

            return false;
        }

        private bool TryStartRoaming(bool preferSafety)
        {
            if (wildlife == null || !TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            if (!wildlife.TryFindWolfRoamCell(this, currentCell, preferSafety, out Vector2Int roamCell))
            {
                return false;
            }

            if (!TryBuildPathTo(roamCell))
            {
                return false;
            }

            state = preferSafety ? StrategyWolfBehaviorState.AvoidingSettlement : StrategyWolfBehaviorState.Roaming;
            stateTimer = Random.Range(1.0f, 2.2f);
            return true;
        }

        private void StartAttack()
        {
            state = StrategyWolfBehaviorState.Attacking;
            frame = 0;
            appliedFrame = -1;
            frameTimer = 0f;
            attackResolved = false;
            path.Clear();
            pathIndex = 0;
        }

        private void StartFeeding()
        {
            state = StrategyWolfBehaviorState.Feeding;
            stateTimer = Random.Range(4.0f, 7.0f);
            feedingWorld = transform.position;
            frame = 0;
            appliedFrame = -1;
            frameTimer = 0f;
        }

        private void StartAvoidingSettlement()
        {
            if (state == StrategyWolfBehaviorState.Attacking || state == StrategyWolfBehaviorState.Feeding)
            {
                return;
            }

            ReleaseTargets();
            if (TryStartRoaming(true))
            {
                return;
            }

            state = StrategyWolfBehaviorState.AvoidingSettlement;
            path.Clear();
            pathIndex = 0;
        }

        private void StartResting()
        {
            ReleaseTargets();
            state = StrategyWolfBehaviorState.Resting;
            stateTimer = Random.Range(2.8f, 6.5f);
            path.Clear();
            pathIndex = 0;
        }

        private void StartHowling()
        {
            ReleaseTargets();
            state = StrategyWolfBehaviorState.Howling;
            stateTimer = Random.Range(1.4f, 2.6f);
            path.Clear();
            pathIndex = 0;
        }

        private void StartIdle(float seconds)
        {
            ReleaseTargets();
            state = StrategyWolfBehaviorState.Idle;
            stateTimer = seconds;
            path.Clear();
            pathIndex = 0;
        }

        private void ResolveAttack()
        {
            attackResolved = true;
            if (targetRabbit != null)
            {
                feedingWorld = targetRabbit.transform.position;
                targetRabbit.KillByPredator(this, transform.position);
                return;
            }

            if (targetDeer != null)
            {
                feedingWorld = targetDeer.transform.position;
                targetDeer.KillByPredator(this, transform.position);
                return;
            }

            if (targetResident != null && population != null)
            {
                Vector3 attackWorld = transform.position;
                string residentName = targetResident.FullName;
                bool killed = population.TryKillResidentByWolf(targetResident, attackWorld);
                wildlife?.ReleaseWolfResidentTarget(this, targetResident);
                targetResident = null;
                StrategyDebugLogger.Info(
                    "Wildlife",
                    killed ? "WolfResidentKilled" : "WolfResidentAttackFailed",
                    StrategyDebugLogger.F("pack", PackId),
                    StrategyDebugLogger.F("resident", residentName),
                    StrategyDebugLogger.F("world", attackWorld));
            }
        }

        private void ConsumeAnimalTarget()
        {
            if (targetRabbit != null)
            {
                targetRabbit.ConsumePredatorKill(this);
            }

            if (targetDeer != null)
            {
                targetDeer.ConsumePredatorKill(this);
            }
        }

        private void ReleaseTargets()
        {
            if (targetRabbit != null)
            {
                targetRabbit.ReleasePredatorReservation(this);
                targetRabbit = null;
            }

            if (targetDeer != null)
            {
                targetDeer.ReleasePredatorReservation(this);
                targetDeer = null;
            }

            if (targetResident != null)
            {
                wildlife?.ReleaseWolfResidentTarget(this, targetResident);
                targetResident = null;
            }
        }

        private bool TryGetTargetWorld(out Vector3 world, out Vector2Int cell)
        {
            world = Vector3.zero;
            cell = default;
            if (targetRabbit != null && targetRabbit.IsAlive && targetRabbit.TryGetCurrentCell(out cell))
            {
                world = targetRabbit.transform.position;
                return true;
            }

            if (targetDeer != null && targetDeer.IsAlive && targetDeer.TryGetCurrentCell(out cell))
            {
                world = targetDeer.transform.position;
                return true;
            }

            if (targetResident != null
                && !targetResident.IsPendingRefugee
                && map != null
                && map.TryWorldToCell(targetResident.transform.position, out cell))
            {
                world = targetResident.transform.position;
                return true;
            }

            return false;
        }

        private bool ShouldAvoidSettlementNow()
        {
            return state != StrategyWolfBehaviorState.AvoidingSettlement
                && state != StrategyWolfBehaviorState.Attacking
                && state != StrategyWolfBehaviorState.Feeding
                && TryGetCurrentCell(out Vector2Int currentCell)
                && wildlife != null
                && wildlife.IsWolfUnsafeSettlementCell(currentCell);
        }

        private bool TryPathNearTarget(Vector2Int targetCell)
        {
            if (TryBuildPathTo(targetCell))
            {
                return true;
            }

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int candidate = targetCell + CardinalDirections[i];
                if (map.IsCellWalkable(candidate) && TryBuildPathTo(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveAlongPath(float speed)
        {
            if (path.Count <= 0 || pathIndex >= path.Count)
            {
                return false;
            }

            Vector3 targetWorld = path[pathIndex];
            targetWorld.z = transform.position.z;
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, speed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude <= MovingThresholdSqr)
            {
                AnimateIdle();
            }

            if (Vector2.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
            }

            return pathIndex >= path.Count;
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (map == null
                || !TryGetPathStartCell(out Vector2Int startCell)
                || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, transform.position.z));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();
            open.Enqueue(startCell);
            visited.Add(startCell);

            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited.Count < visitLimit)
            {
                Vector2Int current = open.Dequeue();
                if (current == targetCell)
                {
                    BuildWorldPath(startCell, targetCell, cameFrom);
                    return path.Count > 0;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !map.IsCellWalkable(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            return false;
        }

        private bool TryGetPathStartCell(out Vector2Int startCell)
        {
            startCell = default;
            if (map == null || !map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            if (map.IsCellWalkable(currentCell))
            {
                startCell = currentCell;
                return true;
            }

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int radius = 1; radius <= 4; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = currentCell + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        float distance = Vector2Int.Distance(currentCell, candidate);
                        if (distance < bestDistance)
                        {
                            startCell = candidate;
                            bestDistance = distance;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildWorldPath(Vector2Int startCell, Vector2Int targetCell, Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new();
            Vector2Int current = targetCell;
            cells.Add(current);
            while (current != startCell)
            {
                if (!cameFrom.TryGetValue(current, out current))
                {
                    path.Clear();
                    pathIndex = 0;
                    return;
                }

                cells.Add(current);
            }

            cells.Reverse();
            path.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 world = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                path.Add(new Vector3(world.x, world.y, transform.position.z));
            }

            pathIndex = 0;
        }

        private bool TryGetCurrentCell(out Vector2Int cell)
        {
            cell = default;
            return map != null && map.TryWorldToCell(transform.position, out cell);
        }

        private Vector2Int GetCurrentCellOrHome()
        {
            return TryGetCurrentCell(out Vector2Int currentCell) ? currentCell : homeCell;
        }

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer != null && Mathf.Abs(world.x - transform.position.x) > 0.03f)
            {
                spriteRenderer.flipX = world.x < transform.position.x;
            }
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(6.0f, StrategyWolfSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyWolfSpritePose.Idle, frame);
        }

        private void AnimateWalk()
        {
            AdvanceLoopingFrame(10.0f, StrategyWolfSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyWolfSpritePose.Walk, frame);
        }

        private void AnimateRun()
        {
            AdvanceLoopingFrame(16.0f, StrategyWolfSpriteFactory.RunFrameCount);
            ApplySprite(StrategyWolfSpritePose.Run, frame);
        }

        private void AnimateStalk()
        {
            AdvanceLoopingFrame(8.5f, StrategyWolfSpriteFactory.StalkFrameCount);
            ApplySprite(StrategyWolfSpritePose.Stalk, frame);
        }

        private void AnimateAttack()
        {
            AdvanceClampedFrame(15.5f, StrategyWolfSpriteFactory.AttackFrameCount);
            ApplySprite(StrategyWolfSpritePose.Attack, frame);
        }

        private void AnimateEat()
        {
            transform.position = new Vector3(feedingWorld.x, feedingWorld.y, transform.position.z);
            AdvanceLoopingFrame(9.0f, StrategyWolfSpriteFactory.EatFrameCount);
            ApplySprite(StrategyWolfSpritePose.Eat, frame);
        }

        private void AnimateHowl()
        {
            AdvanceLoopingFrame(7.0f, StrategyWolfSpriteFactory.HowlFrameCount);
            ApplySprite(StrategyWolfSpritePose.Howl, frame);
        }

        private void AdvanceLoopingFrame(float framesPerSecond, int count)
        {
            frameTimer += Time.deltaTime * framesPerSecond;
            int steps = Mathf.FloorToInt(frameTimer);
            if (steps <= 0)
            {
                return;
            }

            frame = (frame + steps) % Mathf.Max(1, count);
            frameTimer -= steps;
        }

        private void AdvanceClampedFrame(float framesPerSecond, int count)
        {
            frameTimer += Time.deltaTime * framesPerSecond;
            int steps = Mathf.FloorToInt(frameTimer);
            if (steps <= 0)
            {
                return;
            }

            frame = Mathf.Min(Mathf.Max(0, count - 1), frame + steps);
            frameTimer -= steps;
        }

        private void ApplySprite(StrategyWolfSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null || appliedFrame == (((int)pose * 128) + spriteFrame))
            {
                return;
            }

            spriteRenderer.sprite = pose switch
            {
                StrategyWolfSpritePose.Walk => StrategyWolfSpriteFactory.GetWalkSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Run => StrategyWolfSpriteFactory.GetRunSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Stalk => StrategyWolfSpriteFactory.GetStalkSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Attack => StrategyWolfSpriteFactory.GetAttackSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Eat => StrategyWolfSpriteFactory.GetEatSprite(variant, spriteFrame),
                StrategyWolfSpritePose.Howl => StrategyWolfSpriteFactory.GetHowlSprite(variant, spriteFrame),
                _ => StrategyWolfSpriteFactory.GetIdleSprite(variant, spriteFrame)
            };
            appliedFrame = ((int)pose * 128) + spriteFrame;
            SyncReadabilityRenderers();
        }

        private void EnsureClickCollider()
        {
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
            }

            collider.isTrigger = true;
            collider.radius = 0.33f;
            collider.offset = new Vector2(0.05f, 0.16f);
        }

        private void EnsureReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            readabilityShadowSprite ??= CreateReadabilityShadowSprite();
            if (shadowRenderer == null)
            {
                GameObject shadowObject = new GameObject("Wolf Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
                shadowObject.transform.localScale = new Vector3(0.86f * ReadabilityEffectScale, 0.52f * ReadabilityEffectScale, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.015f, 0.018f, 0.015f, 0.30f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Wolf Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.018f, 0.023f, 0.018f, 0.58f);
            }

            SyncReadabilityRenderers();
        }

        private void SyncReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.sprite = spriteRenderer.sprite;
                outlineRenderer.flipX = spriteRenderer.flipX;
                outlineRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 1);
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 2);
            }
        }

        private void UpdateWorldSorting()
        {
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            SyncReadabilityRenderers();
        }

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 42;
            const int height = 15;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Wolf Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.43f;
            float radiusY = height * 0.31f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = (dx * dx) + (dy * dy);
                    if (distance <= 1f)
                    {
                        float alpha = Mathf.Lerp(0.08f, 0.50f, 1f - distance);
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
        }

        private void OnDestroy()
        {
            ReleaseTargets();
        }
    }
}
