using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyChickenAgent
    {
        public void BeginNightShelter(Vector2Int targetCell, Vector3 hiddenWorld)
        {
            if (map == null || !HasCoop())
            {
                ShelterInsideCoop(hiddenWorld);
                return;
            }

            shelterWorld = new Vector3(hiddenWorld.x, hiddenWorld.y, -0.09f);
            returningToCoopForNight = true;
            shelteredInsideCoop = false;
            isPecking = false;
            hasTarget = false;
            waitTimer = 0f;
            path.Clear();
            pathIndex = 0;
            SetChickenVisible(true);
            UseIdleSprite();

            if (TryBuildPathTo(targetCell))
            {
                hasTarget = true;
                return;
            }

            ShelterInsideCoop(shelterWorld);
        }

        public void ReleaseFromNightShelter(Vector3 exitWorld)
        {
            returningToCoopForNight = false;
            shelteredInsideCoop = false;
            isPecking = false;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.position = new Vector3(exitWorld.x, exitWorld.y, -0.09f);
            transform.localScale = BaseVisualScale();
            waitTimer = Random.Range(0.20f, 0.85f);
            peckCooldown = Random.Range(0.45f, 1.65f);
            SetChickenVisible(true);
            UseIdleSprite();
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
        }

        private void UpdateNightShelterMovement()
        {
            if (!hasTarget || pathIndex >= path.Count)
            {
                ShelterInsideCoop(shelterWorld);
                return;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    ShelterInsideCoop(shelterWorld);
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

        private void ShelterInsideCoop(Vector3 hiddenWorld)
        {
            returningToCoopForNight = false;
            shelteredInsideCoop = true;
            isPecking = false;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.position = new Vector3(hiddenWorld.x, hiddenWorld.y, -0.09f);
            transform.localScale = BaseVisualScale();
            SetChickenVisible(false);
        }

        private void SetChickenVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }
}
