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
    public sealed partial class StrategyWolfAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float WalkSpeed = 0.92f;
        private const float StalkSpeed = 0.68f;
        private const float RunSpeed = 2.18f;
        private const float PounceSpeed = 3.15f;
        private const float TargetReachDistance = 0.045f;
        private const float AttackReachDistance = 0.38f;
        private const float PounceStartDistance = 2.65f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float TargetRefreshInterval = 0.32f;
        private const float HuntSearchIntervalMin = 2.0f;
        private const float HuntSearchIntervalMax = 4.8f;
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
        private SpriteRenderer swimRippleRenderer;
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
            AnimateWalkOrSwim();
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
            if (distance <= PounceStartDistance)
            {
                SetWolfState(StrategyWolfBehaviorState.Chasing, "target_in_pounce_band");
                path.Clear();
                pathIndex = 0;
                return;
            }

            if (targetRefreshTimer <= 0f)
            {
                targetRefreshTimer = TargetRefreshInterval * 1.8f;
                TryPathNearTarget(targetCell);
            }

            bool pathCompleted = MoveAlongPath(StalkSpeed);
            if ((pathCompleted || path.Count <= 0 || pathIndex >= path.Count)
                && TryGetCurrentCell(out Vector2Int currentCell)
                && currentCell == targetCell)
            {
                MoveDirectlyToward(targetWorld, StalkSpeed);
            }

            AnimateStalkOrSwim();
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

            bool pathCompleted = MoveAlongPath(PounceSpeed);
            if (pathCompleted || path.Count <= 0 || pathIndex >= path.Count)
            {
                MoveDirectlyToward(targetWorld, PounceSpeed);
            }

            if (Vector2.Distance(transform.position, targetWorld) <= AttackReachDistance)
            {
                StartAttack();
                return;
            }

            AnimateRunOrSwim();
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
            AnimateRunOrSwim();
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
    }
}
