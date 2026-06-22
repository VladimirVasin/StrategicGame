using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTradeCaravanAgent : MonoBehaviour
    {
        private const float MoveSpeed = 2.45f;

        private readonly List<Vector3> path = new();
        private SpriteRenderer spriteRenderer;
        private int pathIndex;
        private bool departing;

        public bool HasArrived { get; private set; }
        public bool HasDeparted { get; private set; }
        public float EstimatedRemainingSeconds { get; private set; }

        public void Configure(IReadOnlyList<Vector3> worldPath)
        {
            EnsureRenderer();
            path.Clear();
            if (worldPath != null)
            {
                for (int i = 0; i < worldPath.Count; i++)
                {
                    path.Add(worldPath[i]);
                }
            }

            pathIndex = path.Count > 1 ? 1 : 0;
            HasArrived = path.Count <= 1;
            HasDeparted = false;
            departing = false;
            if (path.Count > 0)
            {
                transform.position = path[0];
            }

            UpdateSorting();
            UpdateEstimatedSeconds();
        }

        public void BeginDeparture(IReadOnlyList<Vector3> worldPath)
        {
            path.Clear();
            if (worldPath != null)
            {
                for (int i = 0; i < worldPath.Count; i++)
                {
                    path.Add(worldPath[i]);
                }
            }

            pathIndex = path.Count > 1 ? 1 : 0;
            HasArrived = false;
            HasDeparted = path.Count <= 1;
            departing = true;
            if (path.Count > 0)
            {
                transform.position = path[0];
            }

            UpdateEstimatedSeconds();
        }

        private void Update()
        {
            if (HasDeparted || HasArrived && !departing)
            {
                return;
            }

            if (pathIndex < 0 || pathIndex >= path.Count)
            {
                if (departing)
                {
                    HasDeparted = true;
                }
                else
                {
                    HasArrived = true;
                }

                UpdateEstimatedSeconds();
                return;
            }

            Vector3 target = path[pathIndex];
            Vector3 current = transform.position;
            float step = MoveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, target, step);
            if ((target - transform.position).sqrMagnitude <= 0.0025f)
            {
                pathIndex++;
            }

            if (spriteRenderer != null && Mathf.Abs(target.x - current.x) > 0.01f)
            {
                spriteRenderer.flipX = target.x < current.x;
            }

            UpdateSorting();
            UpdateEstimatedSeconds();
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = StrategyTradeCaravanSpriteFactory.GetSprite();
            spriteRenderer.color = Color.white;
        }

        private void UpdateSorting()
        {
            if (spriteRenderer != null)
            {
                StrategyWorldSorting.Apply(spriteRenderer, transform.position, 2);
            }
        }

        private void UpdateEstimatedSeconds()
        {
            if (pathIndex >= path.Count)
            {
                EstimatedRemainingSeconds = 0f;
                return;
            }

            float distance = 0f;
            Vector3 current = transform.position;
            for (int i = pathIndex; i < path.Count; i++)
            {
                distance += Vector3.Distance(current, path[i]);
                current = path[i];
            }

            EstimatedRemainingSeconds = distance / MoveSpeed;
        }
    }
}
