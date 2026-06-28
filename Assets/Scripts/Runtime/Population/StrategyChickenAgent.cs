using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyChickenAgent : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float MoveSpeed = 0.48f;
        private const float TargetReachDistance = 0.035f;
        private const int IdleRadius = 3;
        private const float WalkAnimationFrameRate = 10f;
        private const float PeckAnimationFrameRate = 11f;
        private const float MovingThresholdSqr = 0.000001f;
        private const float VisualScale = 1.18f;
        private const float ShadowWidth = 0.22f;
        private const float ShadowHeight = 0.08f;
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private readonly List<Vector3> path = new();
        private CityMapController map;
        private StrategyBuildingUpgrade coop;
        private StrategyChickenCoop standaloneCoop;
        private SpriteRenderer spriteRenderer;
        private int pathIndex;
        private float waitTimer;
        private float bobPhase;
        private float walkFrameTimer;
        private float peckFrameTimer;
        private float peckCooldown;
        private Vector3 shelterWorld;
        private int walkFrame;
        private int peckFrame;
        private int appliedFrame = -1;
        private bool hasTarget;
        private bool isPecking;
        private bool returningToCoopForNight;
        private bool shelteredInsideCoop;
        private bool usingAnimatedSprite;

        public StrategyBuildingUpgrade Coop => coop;
        public StrategyChickenCoop StandaloneCoop => standaloneCoop;
        public bool IsShelteredInsideCoop => shelteredInsideCoop;

        public void Configure(
            CityMapController mapController,
            StrategyBuildingUpgrade chickenCoop,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            coop = chickenCoop;
            standaloneCoop = null;
            ConfigureShared(mapController, spawnWorld, renderer);
        }

        public void Configure(
            CityMapController mapController,
            StrategyChickenCoop chickenCoop,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            coop = null;
            standaloneCoop = chickenCoop;
            ConfigureShared(mapController, spawnWorld, renderer);
        }

        private void ConfigureShared(
            CityMapController mapController,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            map = mapController;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.09f);
            transform.localScale = BaseVisualScale();
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0f, 0.015f),
                new Vector2(ShadowWidth, ShadowHeight),
                0.24f,
                -4,
                0f,
                false);
            waitTimer = Random.Range(0.2f, 0.85f);
            peckCooldown = Random.Range(0.45f, 1.65f);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            Vector2Int cell = default;
            bool hasCell = map != null && map.TryWorldToCell(transform.position, out cell);
            info = StrategyWorldInspectInfoFactory.CreateChicken(
                GetStateTitle(),
                GetCoopOriginText(),
                spriteRenderer != null ? spriteRenderer.sprite : StrategyChickenSpriteFactory.GetSprite(),
                cell,
                hasCell);
            return true;
        }

        private void Update()
        {
            if (map == null || !HasCoop())
            {
                return;
            }

            if (shelteredInsideCoop)
            {
                return;
            }

            if (returningToCoopForNight)
            {
                UpdateNightShelterMovement();
                return;
            }

            if (isPecking)
            {
                AnimatePeck();
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                peckCooldown -= Time.deltaTime;
                if (peckCooldown <= 0f)
                {
                    StartPeck();
                    return;
                }

                AnimateIdle();
                return;
            }

            if (!hasTarget || pathIndex >= path.Count)
            {
                PickNextIdleTarget();
                return;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    hasTarget = false;
                    waitTimer = Random.Range(0.28f, 1.15f);
                    peckCooldown = Random.Range(0.25f, 1.2f);
                    UseIdleSprite();
                }

                return;
            }

            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, MoveSpeed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                AnimateWalk();
            }
            else
            {
                AnimateIdle();
            }
        }

        private void LateUpdate()
        {
            if (shelteredInsideCoop)
            {
                return;
            }

            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
        }

        private void PickNextIdleTarget()
        {
            Vector2Int origin = GetCoopOrigin();
            Vector2Int footprint = GetCoopFootprint();
            for (int attempt = 0; attempt < 18; attempt++)
            {
                int minX = origin.x - IdleRadius;
                int maxX = origin.x + footprint.x + IdleRadius - 1;
                int minY = origin.y - IdleRadius;
                int maxY = origin.y + footprint.y + IdleRadius - 1;
                Vector2Int cell = new Vector2Int(
                    Random.Range(minX, maxX + 1),
                    Random.Range(minY, maxY + 1));

                if (!IsChickenWalkCell(cell))
                {
                    continue;
                }

                if (TryBuildPathTo(cell))
                {
                    hasTarget = true;
                    waitTimer = Random.Range(0.08f, 0.35f);
                    return;
                }
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (!map.TryWorldToCell(transform.position, out Vector2Int startCell)
                || !IsChickenWalkCell(startCell)
                || !IsChickenWalkCell(targetCell))
            {
                return false;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 144)
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
                    if (visited.Contains(next) || !IsChickenWalkCell(next))
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

        private void BuildWorldPath(Vector2Int startCell, Vector2Int targetCell, Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new();
            Vector2Int current = targetCell;
            while (current != startCell)
            {
                cells.Add(current);
                if (!cameFrom.TryGetValue(current, out current))
                {
                    path.Clear();
                    pathIndex = 0;
                    return;
                }
            }

            cells.Reverse();
            path.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                if (i == cells.Count - 1)
                {
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.24f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.09f));
            }

            pathIndex = 0;
        }

        private bool IsChickenWalkCell(Vector2Int cell)
        {
            return map.IsCellWalkable(cell) && !IsCoopCell(cell);
        }

        private bool IsCoopCell(Vector2Int cell)
        {
            Vector2Int origin = GetCoopOrigin();
            Vector2Int footprint = GetCoopFootprint();
            return cell.x >= origin.x
                && cell.x < origin.x + footprint.x
                && cell.y >= origin.y
                && cell.y < origin.y + footprint.y;
        }

        private void AnimateIdle()
        {
            UseIdleSprite();
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 8f) * 0.045f;
            transform.localScale = new Vector3(VisualScale, VisualScale * pulse, 1f);
        }

        private void AnimateWalk()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localScale = BaseVisualScale();
            usingAnimatedSprite = true;

            walkFrameTimer += Time.deltaTime * WalkAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(walkFrameTimer);
            if (frameSteps > 0)
            {
                walkFrame = (walkFrame + frameSteps) % StrategyChickenSpriteFactory.WalkFrameCount;
                walkFrameTimer -= frameSteps;
            }

            if (appliedFrame != walkFrame)
            {
                spriteRenderer.sprite = StrategyChickenSpriteFactory.GetWalkSprite(walkFrame);
                appliedFrame = walkFrame;
            }
        }

        private void StartPeck()
        {
            isPecking = true;
            peckFrame = 0;
            peckFrameTimer = 0f;
            appliedFrame = -1;
            usingAnimatedSprite = true;
            transform.localScale = BaseVisualScale();
        }

        private void AnimatePeck()
        {
            if (spriteRenderer == null)
            {
                isPecking = false;
                return;
            }

            transform.localScale = BaseVisualScale();
            peckFrameTimer += Time.deltaTime * PeckAnimationFrameRate;
            int nextFrame = Mathf.FloorToInt(peckFrameTimer);
            if (nextFrame > peckFrame)
            {
                peckFrame = nextFrame;
            }

            if (peckFrame >= StrategyChickenSpriteFactory.PeckFrameCount)
            {
                isPecking = false;
                peckCooldown = Random.Range(0.65f, 2.1f);
                UseIdleSprite();
                return;
            }

            if (appliedFrame != peckFrame)
            {
                spriteRenderer.sprite = StrategyChickenSpriteFactory.GetPeckSprite(peckFrame);
                appliedFrame = peckFrame;
            }
        }

        private void UseIdleSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (usingAnimatedSprite)
            {
                spriteRenderer.sprite = StrategyChickenSpriteFactory.GetSprite();
            }

            usingAnimatedSprite = false;
            appliedFrame = -1;
            walkFrame = 0;
            walkFrameTimer = 0f;
        }

        private static Vector3 BaseVisualScale()
        {
            return new Vector3(VisualScale, VisualScale, 1f);
        }

        private string GetStateTitle()
        {
            if (shelteredInsideCoop)
            {
                return "inside coop";
            }

            if (returningToCoopForNight)
            {
                return "returning";
            }

            if (isPecking)
            {
                return "pecking";
            }

            return hasTarget ? "walking" : "idle";
        }

        private bool HasCoop()
        {
            return coop != null || standaloneCoop != null;
        }

        private Vector2Int GetCoopOrigin()
        {
            if (coop != null)
            {
                return coop.Origin;
            }

            return standaloneCoop != null ? standaloneCoop.Origin : Vector2Int.zero;
        }

        private Vector2Int GetCoopFootprint()
        {
            if (coop != null)
            {
                return coop.Footprint;
            }

            return standaloneCoop != null ? standaloneCoop.Footprint : Vector2Int.one;
        }

        private string GetCoopOriginText()
        {
            if (!HasCoop())
            {
                return "none";
            }

            Vector2Int origin = GetCoopOrigin();
            return origin.x + ", " + origin.y;
        }
    }
}
