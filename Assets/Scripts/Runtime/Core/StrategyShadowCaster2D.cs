using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyShadowCaster2D : MonoBehaviour
    {
        private const int DefaultSortingOffset = -5;

        private SpriteRenderer targetRenderer;
        private SpriteRenderer shadowRenderer;
        private StrategyShadowShape shape = StrategyShadowShape.SoftEllipse;
        private Vector2 localOffset;
        private Vector2 localScale = Vector2.one;
        private float opacity = 0.28f;
        private float rotationDegrees;
        private int sortingOffset = DefaultSortingOffset;
        private bool stretchWithDayNight = true;

        public static StrategyShadowCaster2D Attach(
            SpriteRenderer target,
            StrategyShadowShape shadowShape,
            Vector2 offset,
            Vector2 scale,
            float alpha,
            int orderOffset = DefaultSortingOffset,
            float rotation = -7f,
            bool stretch = true)
        {
            if (target == null)
            {
                return null;
            }

            StrategyShadowCaster2D caster = target.GetComponent<StrategyShadowCaster2D>();
            if (caster == null)
            {
                caster = target.gameObject.AddComponent<StrategyShadowCaster2D>();
            }

            caster.Configure(target, shadowShape, offset, scale, alpha, orderOffset, rotation, stretch);
            return caster;
        }

        public void Configure(
            SpriteRenderer target,
            StrategyShadowShape shadowShape,
            Vector2 offset,
            Vector2 scale,
            float alpha,
            int orderOffset = DefaultSortingOffset,
            float rotation = -7f,
            bool stretch = true)
        {
            targetRenderer = target;
            shape = shadowShape;
            localOffset = offset;
            localScale = new Vector2(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y));
            opacity = Mathf.Clamp01(alpha);
            sortingOffset = orderOffset;
            rotationDegrees = rotation;
            stretchWithDayNight = stretch;
            EnsureShadowRenderer();
            SyncShadow();
        }

        private void LateUpdate()
        {
            SyncShadow();
        }

        private void EnsureShadowRenderer()
        {
            if (shadowRenderer != null)
            {
                return;
            }

            GameObject shadowObject = new GameObject("Strategy Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0f);
        }

        private void SyncShadow()
        {
            if (targetRenderer == null)
            {
                if (shadowRenderer != null)
                {
                    shadowRenderer.enabled = false;
                }

                return;
            }

            EnsureShadowRenderer();
            if (shadowRenderer == null)
            {
                return;
            }

            float opacityMultiplier = StrategyDayNightCycleController.ShadowOpacityMultiplier;
            float lengthMultiplier = stretchWithDayNight
                ? StrategyDayNightCycleController.ShadowLengthMultiplier
                : 1f;
            float finalOpacity = opacity * opacityMultiplier;

            shadowRenderer.sprite = StrategyShadowSpriteFactory.Get(shape);
            shadowRenderer.enabled = targetRenderer.enabled && finalOpacity > 0.01f;
            shadowRenderer.flipX = targetRenderer.flipX;
            shadowRenderer.color = new Color(0.015f, 0.018f, 0.014f, finalOpacity);
            shadowRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = Mathf.Max(
                StrategyWorldSorting.WaterOverlayOrder + 1,
                targetRenderer.sortingOrder + sortingOffset);

            Transform shadowTransform = shadowRenderer.transform;
            shadowTransform.localPosition = new Vector3(localOffset.x * lengthMultiplier, localOffset.y, 0.04f);
            shadowTransform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            shadowTransform.localScale = new Vector3(localScale.x * lengthMultiplier, localScale.y, 1f);
        }
    }
}
