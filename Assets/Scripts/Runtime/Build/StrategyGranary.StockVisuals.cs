using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary
    {
        private void EnsureStockRenderers()
        {
            if (gameStockRenderer == null)
            {
                GameObject gameObject = new GameObject("Game Stock");
                gameObject.transform.SetParent(transform, false);
                gameStockRenderer = gameObject.AddComponent<SpriteRenderer>();
                gameStockRenderer.color = Color.white;
            }

            if (fishStockRenderer == null)
            {
                GameObject fishObject = new GameObject("Fish Stock");
                fishObject.transform.SetParent(transform, false);
                fishStockRenderer = fishObject.AddComponent<SpriteRenderer>();
                fishStockRenderer.color = Color.white;
            }

            if (eggStockRenderer == null)
            {
                GameObject eggObject = new GameObject("Egg Stock");
                eggObject.transform.SetParent(transform, false);
                eggStockRenderer = eggObject.AddComponent<SpriteRenderer>();
                eggStockRenderer.color = Color.white;
            }

            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderers();
            if (gameStockRenderer != null)
            {
                gameStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryGameStockSprite(gameStored);
                gameStockRenderer.gameObject.SetActive(gameStored > 0 && gameStockRenderer.sprite != null);
            }

            if (fishStockRenderer != null)
            {
                fishStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryFishStockSprite(fishStored);
                fishStockRenderer.gameObject.SetActive(fishStored > 0 && fishStockRenderer.sprite != null);
            }

            if (eggStockRenderer != null)
            {
                eggStockRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(StrategyResourceType.Eggs);
                eggStockRenderer.gameObject.SetActive(eggsStored > 0 && eggStockRenderer.sprite != null);
            }

            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (gameStockRenderer != null)
            {
                Vector3 gameWorld = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Granary,
                    StrategyResourceType.Game,
                    bounds);
                gameStockRenderer.transform.localPosition = transform.InverseTransformPoint(gameWorld);
                gameStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(gameStockRenderer, gameWorld, 1);
            }

            if (fishStockRenderer != null)
            {
                Vector3 fishWorld = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Granary,
                    StrategyResourceType.Fish,
                    bounds);
                fishStockRenderer.transform.localPosition = transform.InverseTransformPoint(fishWorld);
                fishStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(fishStockRenderer, fishWorld, 1);
            }

            if (eggStockRenderer != null)
            {
                Vector3 eggWorld = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Granary,
                    StrategyResourceType.Eggs,
                    bounds);
                eggStockRenderer.transform.localPosition = transform.InverseTransformPoint(eggWorld);
                eggStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(eggStockRenderer, eggWorld, 1);
            }
        }
    }
}
