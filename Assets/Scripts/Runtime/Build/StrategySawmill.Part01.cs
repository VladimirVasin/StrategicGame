using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySawmill
    {
        private void EnsureStockRenderers()
        {
            logStockRenderer ??= CreateChildRenderer("Sawmill Logs Stock");
            plankStockRenderer ??= CreateChildRenderer("Sawmill Planks Stock");
        }

        private SpriteRenderer CreateChildRenderer(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            return renderer;
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderers();
            logStockRenderer.sprite = StrategyBuildingSpriteFactory.GetSawmillLogStockSprite(logsStored);
            logStockRenderer.gameObject.SetActive(logStockRenderer.sprite != null);
            plankStockRenderer.sprite = StrategyBuildingSpriteFactory.GetSawmillPlankStockSprite(planksStored);
            plankStockRenderer.gameObject.SetActive(plankStockRenderer.sprite != null);
            UpdateStockPositions();
        }

        private void UpdateStockPositions()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            PositionStock(logStockRenderer, new Vector3(bounds.min.x + 0.46f, bounds.min.y + 0.36f, -0.14f));
            PositionStock(plankStockRenderer, new Vector3(bounds.max.x - 0.42f, bounds.min.y + 0.34f, -0.13f));
        }

        private void PositionStock(SpriteRenderer renderer, Vector3 world)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localPosition = transform.InverseTransformPoint(world);
            renderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(renderer, world, 1);
        }

        private void UpdateWorkAnimation()
        {
            if (activeSawyers.Count <= 0 || building == null)
            {
                if (workRenderer != null)
                {
                    workRenderer.gameObject.SetActive(false);
                }

                return;
            }

            workRenderer ??= CreateChildRenderer("Sawmill Saw Work");
            workFrameTimer += Time.deltaTime * 6f;
            int steps = Mathf.FloorToInt(workFrameTimer);
            if (steps > 0)
            {
                workFrame = (workFrame + steps) % StrategyBuildingSpriteFactory.SawmillWorkFrameCount;
                workFrameTimer -= steps;
                TrySpawnSawdustEffect();
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.46f, -0.105f);
            workRenderer.sprite = StrategyBuildingSpriteFactory.GetSawmillWorkSprite(workFrame, activeSawyers.Count);
            workRenderer.gameObject.SetActive(workRenderer.sprite != null);
            workRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            workRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(workRenderer, world, 2);
        }
    }
}
