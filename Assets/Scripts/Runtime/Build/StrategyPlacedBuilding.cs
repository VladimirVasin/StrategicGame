using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyPlacedBuilding : MonoBehaviour
    {
        public const int MaxHouseResidents = 5;

        private readonly Dictionary<StrategyBuildingUpgradeType, StrategyBuildingUpgrade> upgrades = new();
        private readonly List<StrategyResidentAgent> residents = new();
        private readonly List<Vector2Int> bridgeCells = new();
        private SpriteRenderer spriteRenderer;
        private StrategyHouseResourceStore resources;

        public StrategyBuildTool Tool { get; private set; }
        public Vector2Int Origin { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public Bounds FootprintBounds { get; private set; }
        public int VisualVariant { get; private set; }
        public Vector2Int BridgeStartCell { get; private set; }
        public Vector2Int BridgeEndCell { get; private set; }

        public Vector3 HomeAnchor => new Vector3(FootprintBounds.center.x, FootprintBounds.min.y, transform.position.z);

        public Bounds SelectionBounds => FootprintBounds;
        public int InstalledUpgradeCount => upgrades.Count;
        public int ResidentCount => residents.Count;
        public int ResidentCapacity => Tool == StrategyBuildTool.House ? MaxHouseResidents : int.MaxValue;
        public bool HasFreeResidentSlot => Tool != StrategyBuildTool.House || residents.Count < ResidentCapacity;
        public IReadOnlyList<StrategyResidentAgent> Residents => residents;
        public IReadOnlyList<Vector2Int> BridgeCells => bridgeCells;
        public StrategyHouseResourceStore Resources => resources;

        public void Configure(
            StrategyBuildTool tool,
            Vector2Int origin,
            Vector2Int footprint,
            Bounds footprintBounds,
            SpriteRenderer renderer,
            int visualVariant)
        {
            Tool = tool;
            Origin = origin;
            Footprint = footprint;
            FootprintBounds = footprintBounds;
            VisualVariant = visualVariant;
            residents.Clear();
            bridgeCells.Clear();
            BridgeStartCell = origin;
            BridgeEndCell = origin;
            spriteRenderer = renderer;
            EnsureResourceStore();
            EnsureClickCollider();
        }

        public void ConfigureBridgeSpan(
            IReadOnlyList<Vector2Int> cells,
            Vector2Int startCell,
            Vector2Int endCell)
        {
            bridgeCells.Clear();
            if (cells != null)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    if (!bridgeCells.Contains(cells[i]))
                    {
                        bridgeCells.Add(cells[i]);
                    }
                }
            }

            BridgeStartCell = startCell;
            BridgeEndCell = endCell;
            EnsureClickCollider();
        }

        public bool CanAcceptResident(StrategyResidentAgent resident)
        {
            return resident != null && (residents.Contains(resident) || HasFreeResidentSlot);
        }

        public bool TryRegisterResident(StrategyResidentAgent resident)
        {
            if (!CanAcceptResident(resident))
            {
                return false;
            }

            if (!residents.Contains(resident))
            {
                residents.Add(resident);
            }

            return true;
        }

        public void RegisterResident(StrategyResidentAgent resident)
        {
            TryRegisterResident(resident);
        }

        public void UnregisterResident(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            residents.Remove(resident);
        }

        public bool HasUpgrade(StrategyBuildingUpgradeType type)
        {
            return upgrades.ContainsKey(type);
        }

        public bool TryGetUpgrade(StrategyBuildingUpgradeType type, out StrategyBuildingUpgrade upgrade)
        {
            return upgrades.TryGetValue(type, out upgrade) && upgrade != null;
        }

        public bool TryRegisterUpgrade(StrategyBuildingUpgrade upgrade)
        {
            if (upgrade == null || upgrades.ContainsKey(upgrade.Type))
            {
                return false;
            }

            upgrades.Add(upgrade.Type, upgrade);
            return true;
        }

        private void EnsureClickCollider()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider2D>();
            }

            Bounds clickBounds = spriteRenderer != null ? spriteRenderer.bounds : FootprintBounds;
            Vector3 localCenter = transform.InverseTransformPoint(clickBounds.center);
            Vector3 localSize = transform.InverseTransformVector(clickBounds.size);

            box.isTrigger = true;
            box.offset = new Vector2(localCenter.x, localCenter.y);
            box.size = new Vector2(
                Mathf.Max(0.5f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.5f, Mathf.Abs(localSize.y)));
        }

        private void EnsureResourceStore()
        {
            resources = GetComponent<StrategyHouseResourceStore>();
            if (resources == null)
            {
                resources = gameObject.AddComponent<StrategyHouseResourceStore>();
            }
        }
    }
}
