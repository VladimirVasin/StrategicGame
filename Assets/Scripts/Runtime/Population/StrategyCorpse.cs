using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCorpse : MonoBehaviour
    {
        private const float DeathFrameRate = 8.5f;
        private const float BurialSinkDistance = 0.22f;

        private StrategyResidentDeathSnapshot snapshot;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer ropeRenderer;
        private Vector3 baseWorld;
        private float deathFrameTimer;
        private float burialProgress;
        private int deathFrame;
        private bool deathComplete;
        private bool burialStarted;
        private bool usingCarriedSprite;
        private bool usingDraggedSprite;

        public StrategyResidentDeathSnapshot Snapshot => snapshot;
        public bool IsDeathComplete => deathComplete;
        public bool IsBurialStarted => burialStarted;

        public void Configure(StrategyResidentDeathSnapshot deathSnapshot, SpriteRenderer renderer)
        {
            snapshot = deathSnapshot;
            spriteRenderer = renderer;
            baseWorld = deathSnapshot.DeathWorld;
            transform.position = new Vector3(baseWorld.x, baseWorld.y, -0.09f);
            deathFrame = 0;
            deathFrameTimer = 0f;
            deathComplete = false;
            burialStarted = false;
            usingCarriedSprite = false;
            usingDraggedSprite = false;
            burialProgress = 0f;
            SetRopeVisible(false);
            ApplyDeathFrame();
            StrategyDebugLogger.Info(
                "Funeral",
                "CorpseCreated",
                StrategyDebugLogger.F("resident", snapshot.FullName),
                StrategyDebugLogger.F("residentId", snapshot.ResidentId),
                StrategyDebugLogger.F("deathCell", snapshot.DeathCell),
                StrategyDebugLogger.F("world", transform.position));
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (!deathComplete)
            {
                deathFrameTimer += Time.deltaTime * DeathFrameRate;
                int nextFrame = Mathf.Clamp(Mathf.FloorToInt(deathFrameTimer), 0, StrategyFuneralSpriteFactory.DeathFrameCount - 1);
                if (nextFrame != deathFrame)
                {
                    deathFrame = nextFrame;
                    ApplyDeathFrame();
                }

                if (deathFrame >= StrategyFuneralSpriteFactory.DeathFrameCount - 1)
                {
                    deathComplete = true;
                    StrategyDebugLogger.Info(
                        "Funeral",
                        "CorpseReadyForFuneral",
                        StrategyDebugLogger.F("resident", snapshot.FullName),
                        StrategyDebugLogger.F("residentId", snapshot.ResidentId));
                }
            }

            if (burialStarted)
            {
                Vector3 position = transform.position;
                position.y = Mathf.Lerp(baseWorld.y, baseWorld.y - BurialSinkDistance, burialProgress);
                transform.position = position;
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0.15f, burialProgress);
                spriteRenderer.color = color;
            }
        }

        public void SetCarriedWorld(Vector3 world)
        {
            SetRopeVisible(false);
            if (spriteRenderer != null && !usingCarriedSprite)
            {
                spriteRenderer.sprite = StrategyFuneralSpriteFactory.GetCarriedCorpseSprite();
                spriteRenderer.color = Color.white;
                usingCarriedSprite = true;
                usingDraggedSprite = false;
            }

            baseWorld = new Vector3(world.x, world.y, -0.09f);
            transform.position = baseWorld;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 2);
        }

        public void SetDraggedWorld(Vector3 carrierWorld, Vector3 corpseWorld, float maxDistance)
        {
            if (spriteRenderer != null && !usingDraggedSprite)
            {
                spriteRenderer.sprite = StrategyFuneralSpriteFactory.GetDraggedCorpseSprite();
                spriteRenderer.color = Color.white;
                usingDraggedSprite = true;
                usingCarriedSprite = false;
            }

            Vector3 delta = corpseWorld - carrierWorld;
            delta.z = 0f;
            float distance = delta.magnitude;
            float clampedDistance = Mathf.Min(Mathf.Max(0.12f, distance), Mathf.Max(0.12f, maxDistance));
            Vector3 direction = distance > 0.001f ? delta / distance : Vector3.down;
            baseWorld = carrierWorld + direction * clampedDistance;
            baseWorld.z = -0.09f;
            transform.position = baseWorld;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, -1);
            UpdateRope(carrierWorld, baseWorld);
        }

        public void SetBurialWorld(Vector3 world)
        {
            SetRopeVisible(false);
            if (spriteRenderer != null && !usingDraggedSprite)
            {
                spriteRenderer.sprite = StrategyFuneralSpriteFactory.GetDraggedCorpseSprite();
                spriteRenderer.color = Color.white;
                usingDraggedSprite = true;
                usingCarriedSprite = false;
            }

            baseWorld = new Vector3(world.x, world.y, -0.09f);
            transform.position = baseWorld;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, -1);
        }

        public void StartBurial()
        {
            SetRopeVisible(false);
            burialStarted = true;
            burialProgress = 0f;
            baseWorld = transform.position;
            StrategyDebugLogger.Info(
                "Funeral",
                "CorpseBurialStarted",
                StrategyDebugLogger.F("resident", snapshot.FullName),
                StrategyDebugLogger.F("residentId", snapshot.ResidentId));
        }

        internal void ResetToGroundCorpseVisual()
        {
            Vector3 currentWorld = transform.position;
            currentWorld.z = -0.09f;
            baseWorld = currentWorld;
            transform.position = currentWorld;
            burialStarted = false;
            burialProgress = 0f;
            deathComplete = true;
            deathFrame = StrategyFuneralSpriteFactory.DeathFrameCount - 1;
            usingCarriedSprite = false;
            usingDraggedSprite = false;
            SetRopeVisible(false);

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyFuneralSpriteFactory.GetDeathSprite(
                snapshot.Gender,
                snapshot.VisualVariant,
                snapshot.LifeStage,
                deathFrame);
            spriteRenderer.color = Color.white;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, -1);
        }

        public void SetBurialProgress(float progress)
        {
            burialProgress = Mathf.Clamp01(progress);
        }

        public void CompleteBurial()
        {
            StrategyDebugLogger.Info(
                "Funeral",
                "CorpseBuried",
                StrategyDebugLogger.F("resident", snapshot.FullName),
                StrategyDebugLogger.F("residentId", snapshot.ResidentId));
            Destroy(gameObject);
        }

        private void ApplyDeathFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyFuneralSpriteFactory.GetDeathSprite(
                snapshot.Gender,
                snapshot.VisualVariant,
                snapshot.LifeStage,
                deathFrame);
            spriteRenderer.color = Color.white;
            usingCarriedSprite = false;
            usingDraggedSprite = false;
            SetRopeVisible(false);
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, -1);
        }

        private void UpdateRope(Vector3 carrierWorld, Vector3 corpseWorld)
        {
            EnsureRopeRenderer();
            if (ropeRenderer == null)
            {
                return;
            }

            Vector3 from = new Vector3(carrierWorld.x, carrierWorld.y + 0.15f, -0.095f);
            Vector3 to = new Vector3(corpseWorld.x, corpseWorld.y + 0.09f, -0.095f);
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                SetRopeVisible(false);
                return;
            }

            ropeRenderer.enabled = true;
            Transform ropeTransform = ropeRenderer.transform;
            ropeTransform.position = (from + to) * 0.5f;
            ropeTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            ropeTransform.localScale = new Vector3(distance / 0.5f, 1f, 1f);
            StrategyWorldSorting.Apply(ropeRenderer, corpseWorld, 0);
        }

        private void EnsureRopeRenderer()
        {
            if (ropeRenderer != null)
            {
                return;
            }

            GameObject ropeObject = new GameObject("Corpse Drag Rope");
            ropeObject.transform.SetParent(transform, false);
            ropeRenderer = ropeObject.AddComponent<SpriteRenderer>();
            ropeRenderer.sprite = StrategyFuneralSpriteFactory.GetCorpseRopeSprite();
            ropeRenderer.color = Color.white;
            ropeRenderer.enabled = false;
        }

        private void SetRopeVisible(bool visible)
        {
            if (ropeRenderer != null)
            {
                ropeRenderer.enabled = visible;
            }
        }
    }
}
