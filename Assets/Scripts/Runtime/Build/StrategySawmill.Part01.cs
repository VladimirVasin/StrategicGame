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
            PositionStock(
                logStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Sawmill,
                    StrategyResourceType.Logs,
                    bounds));
            PositionStock(
                plankStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Sawmill,
                    StrategyResourceType.Planks,
                    bounds));
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

            Vector3 focus = StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(
                StrategyBuildTool.Sawmill,
                FootprintBounds);
            Vector3 world = new Vector3(focus.x, focus.y + FootprintBounds.size.y * 0.01f, -0.105f);
            workRenderer.sprite = StrategyBuildingSpriteFactory.GetSawmillWorkSprite(workFrame, activeSawyers.Count);
            workRenderer.gameObject.SetActive(workRenderer.sprite != null);
            workRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            workRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(workRenderer, world, 2);
        }
    }
}
