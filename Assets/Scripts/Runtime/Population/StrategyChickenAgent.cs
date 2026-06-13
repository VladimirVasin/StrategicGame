using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyChickenAgent : MonoBehaviour
    {
        private const float MoveSpeed = 0.48f;
        private const float TargetReachDistance = 0.035f;
        private const int IdleRadius = 3;
        private const float WalkAnimationFrameRate = 10f;
        private const float PeckAnimationFrameRate = 11f;
        private const float MovingThresholdSqr = 0.000001f;
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
        private SpriteRenderer spriteRenderer;
        private int pathIndex;
        private float waitTimer;
        private float bobPhase;
        private float walkFrameTimer;
        private float peckFrameTimer;
        private float peckCooldown;
        private int walkFrame;
        private int peckFrame;
        private int appliedFrame = -1;
        private bool hasTarget;
        private bool isPecking;
        private bool usingAnimatedSprite;

        public StrategyBuildingUpgrade Coop => coop;

        public void Configure(
            CityMapController mapController,
            StrategyBuildingUpgrade chickenCoop,
            Vector3 spawnWorld,
            SpriteRenderer renderer)
        {
            map = mapController;
            coop = chickenCoop;
            spriteRenderer = renderer;
            bobPhase = Random.Range(0f, 100f);

            transform.position = new Vector3(spawnWorld.x, spawnWorld.y, -0.09f);
            transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0f, 0.015f),
                new Vector2(0.22f, 0.08f),
                0.24f,
                -4,
                0f,
                false);
            waitTimer = Random.Range(0.2f, 0.85f);
            peckCooldown = Random.Range(0.45f, 1.65f);
        }

        private void Update()
        {
            if (map == null || coop == null)
            {
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
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
        }

        private void PickNextIdleTarget()
        {
            for (int attempt = 0; attempt < 18; attempt++)
            {
                int minX = coop.Origin.x - IdleRadius;
                int maxX = coop.Origin.x + coop.Footprint.x + IdleRadius - 1;
                int minY = coop.Origin.y - IdleRadius;
                int maxY = coop.Origin.y + coop.Footprint.y + IdleRadius - 1;
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
            return cell.x >= coop.Origin.x
                && cell.x < coop.Origin.x + coop.Footprint.x
                && cell.y >= coop.Origin.y
                && cell.y < coop.Origin.y + coop.Footprint.y;
        }

        private void AnimateIdle()
        {
            UseIdleSprite();
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 8f) * 0.045f;
            transform.localScale = new Vector3(1f, pulse, 1f);
        }

        private void AnimateWalk()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localScale = Vector3.one;
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
            transform.localScale = Vector3.one;
        }

        private void AnimatePeck()
        {
            if (spriteRenderer == null)
            {
                isPecking = false;
                return;
            }

            transform.localScale = Vector3.one;
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
    }
}
