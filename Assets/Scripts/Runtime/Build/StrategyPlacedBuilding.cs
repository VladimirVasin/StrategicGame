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
        private StrategyResidentAgent householder;

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
        public StrategyResidentAgent Householder => householder;

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
            householder = null;
            BridgeStartCell = origin;
            BridgeEndCell = origin;
            spriteRenderer = renderer;
            EnsureResourceStore();
            EnsureHouseholdForaging();
            EnsureWorldShadow();
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
            EnsureWorldShadow();
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

            EnsureHouseholder();
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
            if (householder == resident)
            {
                householder = null;
            }

            EnsureHouseholder();
        }

        public void DetachResidentsForDemolition()
        {
            for (int i = residents.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null)
                {
                    resident.ClearHome(this);
                }
            }

            residents.Clear();
            householder = null;
        }

        public void EnsureHouseholder()
        {
            if (Tool != StrategyBuildTool.House)
            {
                householder = null;
                return;
            }

            StrategyResidentAgent previous = householder;
            householder = FindHouseholderCandidate();
            if (householder != null
                && (householder != previous
                    || householder.HasExternalWorkplace
                    || householder.HasConstructionAssignment))
            {
                householder.PrepareHouseholderHomeDuty();
            }

            if (householder != previous)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholderAssigned",
                    StrategyDebugLogger.F("houseOrigin", Origin),
                    StrategyDebugLogger.F("resident", householder != null ? householder.FullName : string.Empty),
                    StrategyDebugLogger.F("age", householder != null ? householder.DisplayAgeYears : -1));
            }
        }

        private StrategyResidentAgent FindHouseholderCandidate()
        {
            return FindOldestAdultFemaleResident();
        }

        private StrategyResidentAgent FindOldestAdultFemaleResident()
        {
            StrategyResidentAgent oldest = null;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (candidate == null
                    || candidate.Home != this
                    || candidate.IsPendingRefugee
                    || !candidate.IsAdult
                    || candidate.Gender != StrategyResidentGender.Female)
                {
                    continue;
                }

                if (oldest == null
                    || candidate.AgeYears > oldest.AgeYears + 0.01f
                    || (Mathf.Abs(candidate.AgeYears - oldest.AgeYears) <= 0.01f
                        && candidate.ResidentId > oldest.ResidentId))
                {
                    oldest = candidate;
                }
            }

            return oldest;
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

        private void EnsureHouseholdForaging()
        {
            if (Tool != StrategyBuildTool.House)
            {
                return;
            }

            if (GetComponent<StrategyHouseholdForagingState>() == null)
            {
                gameObject.AddComponent<StrategyHouseholdForagingState>();
            }
        }

        private void EnsureWorldShadow()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Vector2 scale = GetShadowScale();
            Vector2 offset = GetShadowOffset();
            float alpha = GetShadowOpacity();
            StrategyShadowShape shape = Tool == StrategyBuildTool.Bridge
                ? StrategyShadowShape.SoftEllipse
                : StrategyShadowShape.WideCastOval;
            float rotation = Tool == StrategyBuildTool.Bridge ? 0f : -7f;
            bool stretch = Tool != StrategyBuildTool.Bridge;
            StrategyShadowCaster2D.Attach(spriteRenderer, shape, offset, scale, alpha, -7, rotation, stretch);
        }

        private Vector2 GetShadowScale()
        {
            float width = Mathf.Max(1f, Footprint.x);
            float height = Mathf.Max(1f, Footprint.y);
            return Tool switch
            {
                StrategyBuildTool.House => new Vector2(width * 0.92f, height * 0.46f),
                StrategyBuildTool.Bridge => new Vector2(width * 0.70f, Mathf.Max(0.24f, height * 0.24f)),
                StrategyBuildTool.StorageYard => new Vector2(width * 0.82f, height * 0.38f),
                StrategyBuildTool.Granary => new Vector2(width * 0.86f, height * 0.42f),
                StrategyBuildTool.FisherHut => new Vector2(width * 0.82f, height * 0.42f),
                _ => new Vector2(width * 0.80f, height * 0.40f)
            };
        }

        private Vector2 GetShadowOffset()
        {
            return Tool == StrategyBuildTool.Bridge
                ? new Vector2(0f, -0.04f)
                : new Vector2(0.24f, -0.18f);
        }

        private float GetShadowOpacity()
        {
            return Tool switch
            {
                StrategyBuildTool.Bridge => 0.18f,
                StrategyBuildTool.StorageYard => 0.25f,
                _ => 0.31f
            };
        }
    }
}
