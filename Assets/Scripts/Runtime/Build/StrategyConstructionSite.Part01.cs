using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {

        private void EnsureStockRenderers()
        {
            if (logsRenderer == null)
            {
                GameObject logsObject = new GameObject("Construction Logs");
                logsObject.transform.SetParent(transform, false);
                logsRenderer = logsObject.AddComponent<SpriteRenderer>();
            }

            if (stoneRenderer == null)
            {
                GameObject stoneObject = new GameObject("Construction Stone");
                stoneObject.transform.SetParent(transform, false);
                stoneRenderer = stoneObject.AddComponent<SpriteRenderer>();
            }

            if (planksRenderer == null)
            {
                GameObject planksObject = new GameObject("Construction Planks");
                planksObject.transform.SetParent(transform, false);
                planksRenderer = planksObject.AddComponent<SpriteRenderer>();
            }
        }

        private void EnsureWorldShadow()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool isBridge = tool == StrategyBuildTool.Bridge;
            Vector2 scale = isBridge
                ? new Vector2(Mathf.Max(0.8f, footprint.x * 0.68f), Mathf.Max(0.2f, footprint.y * 0.22f))
                : new Vector2(Mathf.Max(0.9f, footprint.x * 0.78f), Mathf.Max(0.28f, footprint.y * 0.36f));
            Vector2 offset = isBridge ? new Vector2(0f, -0.04f) : new Vector2(0.14f, -0.12f);

            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.CastOval,
                offset,
                scale,
                isBridge ? 0.14f : 0.21f,
                -7,
                isBridge ? 0f : -6f,
                !isBridge);
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer != null)
            {
                int stage = ResourcesComplete
                    ? Mathf.Clamp(1 + Mathf.FloorToInt(Progress * (StrategyConstructionSpriteFactory.StageCount - 1)), 1, StrategyConstructionSpriteFactory.StageCount - 1)
                    : Mathf.Clamp(Mathf.FloorToInt(((deliveredLogs + deliveredStone + deliveredPlanks) / Mathf.Max(1f, cost.Total)) * 2f), 0, 2);
                spriteRenderer.sprite = tool == StrategyBuildTool.Bridge
                    ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(footprint, stage)
                    : StrategyConstructionSpriteFactory.GetConstructionSprite(tool, visualVariant, stage);
                spriteRenderer.color = Color.white;
            }

            EnsureStockRenderers();
            if (logsRenderer != null)
            {
                Vector3 logsWorld = new Vector3(footprintBounds.center.x - 0.82f, footprintBounds.min.y + 0.30f, -0.12f);
                logsRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionLogsSprite(deliveredLogs);
                logsRenderer.gameObject.SetActive(deliveredLogs > 0 && logsRenderer.sprite != null);
                logsRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
                StrategyWorldSorting.Apply(logsRenderer, logsWorld, 1);
            }

            if (stoneRenderer != null)
            {
                Vector3 stoneWorld = new Vector3(footprintBounds.center.x + 0.78f, footprintBounds.min.y + 0.28f, -0.12f);
                stoneRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionStoneSprite(deliveredStone);
                stoneRenderer.gameObject.SetActive(deliveredStone > 0 && stoneRenderer.sprite != null);
                stoneRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
                StrategyWorldSorting.Apply(stoneRenderer, stoneWorld, 1);
            }

            if (planksRenderer != null)
            {
                Vector3 planksWorld = new Vector3(footprintBounds.center.x, footprintBounds.min.y + 0.48f, -0.13f);
                planksRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardPlankStockSprite(deliveredPlanks);
                planksRenderer.gameObject.SetActive(deliveredPlanks > 0 && planksRenderer.sprite != null);
                planksRenderer.transform.localPosition = transform.InverseTransformPoint(planksWorld);
                planksRenderer.transform.localScale = Vector3.one * 0.72f;
                StrategyWorldSorting.Apply(planksRenderer, planksWorld, 2);
            }
        }

        private void EnsureClickCollider()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider2D>();
            }

            Bounds clickBounds = spriteRenderer != null ? spriteRenderer.bounds : footprintBounds;
            Vector3 localCenter = transform.InverseTransformPoint(clickBounds.center);
            Vector3 localSize = transform.InverseTransformVector(clickBounds.size);

            box.isTrigger = true;
            box.offset = new Vector2(localCenter.x, localCenter.y);
            box.size = new Vector2(
                Mathf.Max(0.5f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.5f, Mathf.Abs(localSize.y)));
        }

        private void OnDestroy()
        {
            StrategyStorageYard.ReleaseConstructionReservations(this);
            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.ClearConstructionSite(this, false);
                }
            }

            builders.Clear();
            bridgeCells.Clear();
            bridgeWorkCells.Clear();
            futureHomeResidentIds.Clear();
        }
    }
}
