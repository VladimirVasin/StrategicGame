using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBirdBehaviorState
    {
        Idle,
        Pecking,
        Hopping,
        Flying,
        Fleeing,
        Landing,
        Swimming
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyBirdAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float FlightSpeed = 4.2f;
        private const float FleeFlightSpeed = 6.2f;
        private const float SwimSpeed = 0.55f;
        private const float ThreatCheckInterval = 0.20f;
        private const float AlertRadius = 4.2f;
        private const float FleeRadius = 2.4f;
        private const float NoisyAlertRadius = 8.0f;
        private const float NoisyFleeRadius = 5.2f;
        private const float IdleAnimationRate = 5.5f;
        private const float PeckAnimationRate = 8.5f;
        private const float HopAnimationRate = 9.5f;
        private const float FlyAnimationRate = 14.0f;
        private const float LandAnimationRate = 10.0f;
        private const float SwimAnimationRate = 6.0f;
        private const float BirdGlobalScale = 0.78f;
        private const float FlightShadowScale = 0.6f;
        private const float FlyingSortOffset = 3600f;
        private const float LandedSortOffset = 16f;

        private static Sprite shadowSprite;

        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private StrategyBirdSpecies species;
        private StrategyBirdBehaviorState state;
        private StrategyBirdSpritePose appliedPose;
        private Vector2Int homeCell;
        private Vector2Int groundCell;
        private Vector3 groundWorld;
        private Vector3 flightStartGround;
        private Vector3 flightEndGround;
        private Vector3 swimTargetGround;
        private Vector3 lastThreatWorld;
        private int homeRadius;
        private int birdId;
        private int frame;
        private int appliedFrame = -1;
        private float waitTimer;
        private float stateTimer;
        private float threatCheckTimer;
        private float frameTimer;
        private float flightTimer;
        private float flightDuration = 1f;
        private float altitude;
        private float bobPhase;
        private bool hasAppliedPose;
        private bool hasSwimTarget;

        public StrategyBirdSpecies Species => species;
        public StrategyBirdBehaviorState State => state;
        public int BirdId => birdId;
        public Vector2Int HomeCell => homeCell;
        public int HomeRadius => homeRadius;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyBirdSpecies birdSpecies,
            Vector2Int home,
            int radius,
            int identifier,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            map = mapController;
            population = populationController;
            species = birdSpecies;
            homeCell = home;
            groundCell = home;
            homeRadius = Mathf.Max(5, radius);
            birdId = identifier;
            groundWorld = new Vector3(spawnWorld.x, spawnWorld.y, -0.064f);
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);
            transform.position = groundWorld;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * GetSpeciesScale();
            state = species == StrategyBirdSpecies.Duck && IsCurrentCellWater()
                ? StrategyBirdBehaviorState.Swimming
                : StrategyBirdBehaviorState.Idle;
            waitTimer = Random.Range(0.45f, 1.6f);
            stateTimer = waitTimer;
            threatCheckTimer = Random.Range(0f, ThreatCheckInterval);
            ApplySprite(state == StrategyBirdBehaviorState.Swimming ? StrategyBirdSpritePose.Swim : StrategyBirdSpritePose.Idle, Random.Range(0, StrategyBirdSpriteFactory.IdleFrameCount));
            EnsureShadowRenderer();
            UpdateWorldSorting();
        }

        public void RetargetHomeCenter(Vector2Int center, int radius)
        {
            homeCell = center;
            homeRadius = Mathf.Max(5, radius);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            Vector2Int cell = default;
            bool hasCell = map != null && map.TryWorldToCell(groundWorld, out cell);
            Vector2Int currentCell = hasCell ? cell : groundCell;
            info = StrategyWorldInspectInfoFactory.CreateBird(
                this,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                currentCell,
                hasCell);
            return true;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || map == null || spriteRenderer == null)
            {
                return;
            }

            UpdateThreatAwareness();
            switch (state)
            {
                case StrategyBirdBehaviorState.Pecking:
                    UpdatePecking();
                    break;
                case StrategyBirdBehaviorState.Hopping:
                    UpdateHopping();
                    break;
                case StrategyBirdBehaviorState.Flying:
                case StrategyBirdBehaviorState.Fleeing:
                    UpdateFlying();
                    break;
                case StrategyBirdBehaviorState.Landing:
                    UpdateLanding();
                    break;
                case StrategyBirdBehaviorState.Swimming:
                    UpdateSwimming();
                    break;
                default:
                    UpdateIdle();
                    break;
            }

            UpdateShadow();
        }

        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private void UpdateThreatAwareness()
        {
            if (state == StrategyBirdBehaviorState.Fleeing)
            {
                return;
            }

            threatCheckTimer -= Time.deltaTime;
            if (threatCheckTimer > 0f)
            {
                return;
            }

            threatCheckTimer = ThreatCheckInterval;
            if (!TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat))
            {
                return;
            }

            lastThreatWorld = threatWorld;
            float fleeDistance = noisyThreat ? NoisyFleeRadius : FleeRadius;
            float alertDistance = noisyThreat ? NoisyAlertRadius : AlertRadius;
            if (threatDistance <= fleeDistance || (threatDistance <= alertDistance && Random.value < 0.45f))
            {
                StartFleeing(threatWorld, noisyThreat);
            }
        }

        private void UpdateIdle()
        {
            waitTimer -= Time.deltaTime;
            AnimateIdle();
            if (waitTimer > 0f)
            {
                return;
            }

            PickRelaxedBehavior();
        }

        private void UpdatePecking()
        {
            stateTimer -= Time.deltaTime;
            AnimatePeck();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.95f));
            }
        }

        private void UpdateHopping()
        {
            stateTimer -= Time.deltaTime;
            AnimateHop();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.9f));
            }
        }

        private void UpdateFlying()
        {
            flightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(flightTimer / Mathf.Max(0.05f, flightDuration));
            groundWorld = Vector3.Lerp(flightStartGround, flightEndGround, SmoothStep(t));
            altitude = Mathf.Sin(t * Mathf.PI) * (state == StrategyBirdBehaviorState.Fleeing ? 1.35f : 0.92f) + 0.18f;
            transform.position = new Vector3(groundWorld.x, groundWorld.y + altitude, -0.064f);
            if (Mathf.Abs(flightEndGround.x - flightStartGround.x) > 0.02f)
            {
                spriteRenderer.flipX = flightEndGround.x < flightStartGround.x;
            }

            AnimateFly();
            if (t >= 1f)
            {
                altitude = 0f;
                groundWorld = flightEndGround;
                if (map.TryWorldToCell(groundWorld, out Vector2Int cell))
                {
                    groundCell = cell;
                }

                StartLanding();
            }
        }

        private void UpdateLanding()
        {
            stateTimer -= Time.deltaTime;
            altitude = Mathf.Max(0f, altitude - Time.deltaTime * 2.8f);
            transform.position = new Vector3(groundWorld.x, groundWorld.y + altitude, -0.064f);
            AnimateLand();
            if (stateTimer <= 0f)
            {
                altitude = 0f;
                transform.position = groundWorld;
                if (species == StrategyBirdSpecies.Duck && IsCurrentCellWater())
                {
                    StartSwimmingIdle();
                }
                else
                {
                    StartIdle(Random.Range(0.25f, 1.0f));
                }
            }
        }

        private void UpdateSwimming()
        {
            if (species != StrategyBirdSpecies.Duck)
            {
                StartIdle(Random.Range(0.2f, 0.8f));
                return;
            }

            if (!hasSwimTarget && stateTimer <= 0f && TryPickLandingCell(false, true, out Vector2Int targetCell))
            {
                swimTargetGround = GetJitteredCellWorld(targetCell, birdId + 1601, 0.18f);
                hasSwimTarget = true;
            }

            if (hasSwimTarget)
            {
                Vector3 previous = groundWorld;
                groundWorld = Vector3.MoveTowards(groundWorld, swimTargetGround, SwimSpeed * Time.deltaTime);
                transform.position = groundWorld;
                if (Mathf.Abs(groundWorld.x - previous.x) > 0.001f)
                {
                    spriteRenderer.flipX = groundWorld.x < previous.x;
                }

                if (Vector3.Distance(groundWorld, swimTargetGround) <= 0.04f)
                {
                    hasSwimTarget = false;
                    if (map.TryWorldToCell(groundWorld, out Vector2Int cell))
                    {
                        groundCell = cell;
                    }

                    stateTimer = Random.Range(0.4f, 1.5f);
                }
            }
            else
            {
                stateTimer -= Time.deltaTime;
            }

            AnimateSwim();
            if (!IsCurrentCellWater() && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.9f));
            }
        }

        private void PickRelaxedBehavior()
        {
            if (species == StrategyBirdSpecies.Duck && IsCurrentCellWater())
            {
                float duckRoll = Random.value;
                if (duckRoll < 0.58f)
                {
                    StartSwimmingIdle();
                    return;
                }

                if (duckRoll < 0.78f && TryPickLandingCell(false, false, out Vector2Int duckTarget))
                {
                    StartFlightTo(duckTarget, false, false);
                    return;
                }
            }

            float roll = Random.value;
            if (roll < 0.42f)
            {
                StartPecking();
                return;
            }

            if (roll < 0.64f)
            {
                StartHopping();
                return;
            }

            if (roll < 0.88f && TryPickLandingCell(false, false, out Vector2Int targetCell))
            {
                StartFlightTo(targetCell, false, false);
                return;
            }

            StartIdle(Random.Range(0.35f, 1.25f));
        }

        private void StartIdle(float duration)
        {
            altitude = 0f;
            hasSwimTarget = false;
            waitTimer = duration;
            SetState(StrategyBirdBehaviorState.Idle, false, false);
        }

        private void StartPecking()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(1.0f, 2.6f);
            SetState(StrategyBirdBehaviorState.Pecking, false, false);
        }

        private void StartHopping()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(0.45f, 0.95f);
            SetState(StrategyBirdBehaviorState.Hopping, false, false);
        }

        private void StartSwimmingIdle()
        {
            altitude = 0f;
            stateTimer = Random.Range(0.3f, 1.2f);
            SetState(StrategyBirdBehaviorState.Swimming, false, false);
        }

        private void StartLanding()
        {
            hasSwimTarget = false;
            stateTimer = Random.Range(0.25f, 0.48f);
            SetState(StrategyBirdBehaviorState.Landing, false, false);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            if (!TryPickLandingCell(true, false, out Vector2Int targetCell))
            {
                return;
            }

            StartFlightTo(targetCell, true, noisyThreat);
        }
        private void StartFlightTo(Vector2Int targetCell, bool fleeing, bool noisyThreat)
        {
            hasSwimTarget = false;
            flightStartGround = groundWorld;
            flightEndGround = GetJitteredCellWorld(targetCell, birdId + 1327, 0.28f);
            flightTimer = 0f;
            float speed = fleeing ? FleeFlightSpeed : FlightSpeed;
            flightDuration = Mathf.Clamp(Vector3.Distance(flightStartGround, flightEndGround) / speed, fleeing ? 0.35f : 0.55f, fleeing ? 1.6f : 2.6f);
            groundCell = targetCell;
            SetState(fleeing ? StrategyBirdBehaviorState.Fleeing : StrategyBirdBehaviorState.Flying, fleeing, noisyThreat);
        }

        private void SetState(StrategyBirdBehaviorState nextState, bool logImportant, bool noisyThreat)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            frame = 0;
            frameTimer = 0f;
            appliedFrame = -1;
            hasAppliedPose = false;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * GetSpeciesScale();

            if (logImportant)
            {
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "BirdFleeing",
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("bird", birdId),
                    StrategyDebugLogger.F("noisyThreat", noisyThreat),
                    StrategyDebugLogger.F("world", groundWorld));
            }
        }
    }
}
