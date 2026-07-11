using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildingGroundDetail : MonoBehaviour
    {
        private SpriteRenderer detailRenderer;

        public bool HasVisibleDetail => detailRenderer != null
            && detailRenderer.enabled
            && detailRenderer.sprite != null;

        public void Configure(StrategyPlacedBuilding building)
        {
            if (building == null || building.Tool == StrategyBuildTool.Bridge)
            {
                SetVisible(false);
                return;
            }

            EnsureRenderer();
            if (detailRenderer == null)
            {
                return;
            }

            Sprite sprite;
            if (!StrategyVisualCatalogProvider.TryGetBuildingGroundSprite(
                    building.Tool,
                    building.Footprint,
                    building.VisualVariant,
                    out sprite))
            {
                sprite = StrategyBuildingGroundSpriteFactory.Get(
                    building.Tool,
                    building.Footprint,
                    building.VisualVariant);
            }

            detailRenderer.sprite = sprite;
            detailRenderer.color = Color.white;
            detailRenderer.sortingOrder = StrategyWorldSorting.BuildingGroundDetailOrder;
            detailRenderer.enabled = sprite != null;
            Vector3 center = building.FootprintBounds.center;
            Vector3 local = transform.InverseTransformPoint(center);
            detailRenderer.transform.localPosition = new Vector3(local.x, local.y, 0.10f);
            detailRenderer.transform.localScale = Vector3.one;
        }

        private void EnsureRenderer()
        {
            if (detailRenderer != null)
            {
                return;
            }

            Transform existing = transform.Find("Building Ground Detail");
            if (existing != null)
            {
                detailRenderer = existing.GetComponent<SpriteRenderer>();
            }

            if (detailRenderer == null)
            {
                GameObject detailObject = new GameObject("Building Ground Detail");
                detailObject.transform.SetParent(transform, false);
                detailRenderer = detailObject.AddComponent<SpriteRenderer>();
            }
        }

        private void SetVisible(bool visible)
        {
            if (detailRenderer != null)
            {
                detailRenderer.enabled = visible;
            }
        }
    }
}
