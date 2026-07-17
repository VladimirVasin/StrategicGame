using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCinematicCatAnimation
    {
        Idle = 0,
        Stalk = 1,
        Pounce = 2,
        Joy = 3
    }

    [DisallowMultipleComponent]
    public sealed class StrategyCinematicCatActor : MonoBehaviour
    {
        private const float DefaultIdleFrameRate = 4f;

        private SpriteRenderer spriteRenderer;
        private StrategyCinematicCatAnimation animation =
            StrategyCinematicCatAnimation.Idle;
        private StrategyCatCoat coat;
        private int sortingOffset = 3;
        private int frameIndex;
        private float frameProgress;
        private float framesPerSecond = DefaultIdleFrameRate;
        private Vector3 baseVisualScale = Vector3.one;
        private Quaternion baseVisualRotation = Quaternion.identity;
        private bool loop = true;
        private bool isPlaying;
        private bool automaticTick = true;
        private bool configured;

        public SpriteRenderer Renderer => spriteRenderer;
        public StrategyCinematicCatAnimation Animation => animation;
        public int FrameIndex => frameIndex;
        public int FrameCount => StrategySettlementFaunaSpriteFactory.CatFrameCount;
        public StrategyCatCoat Coat => coat;
        public bool IsPlaying => isPlaying;
        public bool AutomaticTick => automaticTick;

        public static StrategyCinematicCatActor Create(
            Transform parent,
            Vector3 worldPosition,
            StrategyCatCoat coat = StrategyCatCoat.Ginger,
            int worldSortingOffset = 3)
        {
            GameObject actorObject = new("Cinematic Cat", typeof(SpriteRenderer));
            if (parent != null)
            {
                actorObject.transform.SetParent(parent, true);
            }

            StrategyCinematicCatActor actor =
                actorObject.AddComponent<StrategyCinematicCatActor>();
            actor.Configure(
                actorObject.GetComponent<SpriteRenderer>(),
                coat,
                worldSortingOffset);
            actor.SetWorldPosition(worldPosition);
            return actor;
        }

        public void Configure(
            SpriteRenderer targetRenderer,
            StrategyCatCoat visualCoat = StrategyCatCoat.Ginger,
            int worldSortingOffset = 3)
        {
            ResetVisualPose();
            spriteRenderer = targetRenderer;
            coat = visualCoat;
            sortingOffset = worldSortingOffset;
            configured = spriteRenderer != null;
            frameIndex = 0;
            frameProgress = 0f;
            animation = StrategyCinematicCatAnimation.Idle;
            framesPerSecond = DefaultIdleFrameRate;
            loop = true;
            isPlaying = configured;
            if (spriteRenderer != null)
            {
                baseVisualScale = Vector3.one
                    * StrategySettlementFaunaSpriteFactory.CatWorldScale;
                baseVisualRotation = spriteRenderer.transform.localRotation;
                ResetVisualPose();
            }

            ApplyFrame(0);
            ApplySorting();
        }

        public void Play(
            StrategyCinematicCatAnimation nextAnimation,
            float frameRate,
            bool shouldLoop,
            bool restart = true)
        {
            StrategyCinematicCatAnimation normalizedAnimation = Normalize(nextAnimation);
            bool changed = animation != normalizedAnimation;
            animation = normalizedAnimation;
            framesPerSecond = Mathf.Max(0.01f, frameRate);
            loop = shouldLoop;
            isPlaying = configured;
            if (restart || changed)
            {
                frameProgress = 0f;
                ApplyFrame(0);
            }
            else
            {
                ApplyFrame(frameIndex);
            }
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void SetAutomaticTick(bool enabled)
        {
            automaticTick = enabled;
        }

        public void SetFrame(int nextFrameIndex)
        {
            frameProgress = 0f;
            ApplyFrame(nextFrameIndex);
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!configured || !isPlaying || unscaledDeltaTime <= 0f)
            {
                return;
            }

            frameProgress += unscaledDeltaTime * framesPerSecond;
            int elapsedFrames = Mathf.FloorToInt(frameProgress);
            if (elapsedFrames <= 0)
            {
                return;
            }

            frameProgress -= elapsedFrames;
            int nextFrame = frameIndex + elapsedFrames;
            if (loop)
            {
                ApplyFrame(nextFrame);
                return;
            }

            int finalFrame = FrameCount - 1;
            if (nextFrame >= finalFrame)
            {
                frameProgress = 0f;
                isPlaying = false;
                ApplyFrame(finalFrame);
                ResetVisualPose();
                return;
            }

            ApplyFrame(nextFrame);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            ApplySorting();
        }

        public bool MoveTowards(
            Vector3 worldTarget,
            float unitsPerSecond,
            float unscaledDeltaTime)
        {
            Vector3 before = transform.position;
            float distance = Mathf.Max(0f, unitsPerSecond)
                * Mathf.Max(0f, unscaledDeltaTime);
            Vector3 next = Vector3.MoveTowards(before, worldTarget, distance);
            transform.position = next;
            float horizontalMovement = next.x - before.x;
            if (Mathf.Abs(horizontalMovement) > 0.0001f)
            {
                SetFacingLeft(horizontalMovement < 0f);
            }

            ApplySorting();
            return (worldTarget - next).sqrMagnitude <= 0.000001f;
        }

        public void SetFacingLeft(bool faceLeft)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = faceLeft;
            }
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        private void Update()
        {
            if (automaticTick)
            {
                Advance(Time.unscaledDeltaTime);
            }
        }

        private void ApplyFrame(int nextFrameIndex)
        {
            frameIndex = PositiveModulo(nextFrameIndex, FrameCount);
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategySettlementFaunaSpriteFactory.GetCatSprite(
                coat,
                ResolvePose(animation),
                frameIndex);
            ApplyMotionPose();
        }

        private void ApplyMotionPose()
        {
            float phase = frameIndex / (float)FrameCount * Mathf.PI * 2f;
            float wave = Mathf.Sin(phase);
            float effort = Mathf.Abs(wave);
            Vector3 scale = baseVisualScale;
            float tilt = 0f;
            switch (animation)
            {
                case StrategyCinematicCatAnimation.Stalk:
                    scale.y *= 1f - effort * 0.05f;
                    tilt = wave * 2.2f;
                    break;
                case StrategyCinematicCatAnimation.Pounce:
                    scale.x *= 1f + effort * 0.10f;
                    scale.y *= 1f - effort * 0.08f;
                    tilt = -wave * 4.5f;
                    break;
                case StrategyCinematicCatAnimation.Joy:
                    scale.x *= 1f - effort * 0.035f;
                    scale.y *= 1f + effort * 0.09f;
                    tilt = wave * 3.2f;
                    break;
                default:
                    scale.x *= 1f - wave * 0.012f;
                    scale.y *= 1f + wave * 0.018f;
                    break;
            }

            Transform visual = spriteRenderer.transform;
            visual.localScale = scale;
            visual.localRotation = baseVisualRotation
                * Quaternion.Euler(0f, 0f, tilt);
        }

        private void ResetVisualPose()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.transform.localScale = baseVisualScale;
            spriteRenderer.transform.localRotation = baseVisualRotation;
        }

        private void ApplySorting()
        {
            if (spriteRenderer != null)
            {
                StrategyWorldSorting.Apply(
                    spriteRenderer,
                    transform.position,
                    sortingOffset);
            }
        }

        private static StrategyCatSpritePose ResolvePose(
            StrategyCinematicCatAnimation value)
        {
            return value switch
            {
                StrategyCinematicCatAnimation.Stalk => StrategyCatSpritePose.Stalk,
                StrategyCinematicCatAnimation.Pounce => StrategyCatSpritePose.Pounce,
                StrategyCinematicCatAnimation.Joy => StrategyCatSpritePose.Joy,
                _ => StrategyCatSpritePose.Idle
            };
        }

        private static StrategyCinematicCatAnimation Normalize(
            StrategyCinematicCatAnimation value)
        {
            return value is StrategyCinematicCatAnimation.Stalk
                or StrategyCinematicCatAnimation.Pounce
                or StrategyCinematicCatAnimation.Joy
                    ? value
                    : StrategyCinematicCatAnimation.Idle;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }

        private void OnDisable()
        {
            ResetVisualPose();
        }
    }
}
