using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyPlacedBuilding : MonoBehaviour
    {
        public const int MaxHouseResidents = 5;

        private readonly Dictionary<StrategyBuildingUpgradeType, StrategyBuildingUpgrade> upgrades = new();
        private readonly HashSet<StrategyProductionBuildingUpgradeType> productionUpgrades = new();
        private readonly List<StrategyResidentAgent> residents = new();
        private readonly List<Vector2Int> bridgeCells = new();
        private static readonly List<StrategyPlacedBuilding> activeBuildings = new();
        private SpriteRenderer spriteRenderer;
        private StrategyHouseResourceStore resources;
        private StrategyHouseWarmthState warmth;
        private StrategyResidentAgent householder;
        private bool registeredActiveBuilding;
        private string stableId;

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
        public StrategyHouseWarmthState Warmth => warmth;
        public StrategyResidentAgent Householder => householder;
        public string StableId => stableId;
        public bool IsDemolishing { get; private set; }
        public static IReadOnlyList<StrategyPlacedBuilding> ActiveBuildings => activeBuildings;

        public static int CopyActiveComponents<T>(List<T> components)
            where T : Component
        {
            if (components == null)
            {
                return 0;
            }

            components.Clear();
            for (int i = 0; i < activeBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = activeBuildings[i];
                if (building != null && building.TryGetComponent(out T component) && component != null)
                {
                    components.Add(component);
                }
            }

            return components.Count;
        }

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
            EnsureStableId();
            residents.Clear();
            bridgeCells.Clear();
            productionUpgrades.Clear();
            householder = null;
            BridgeStartCell = origin;
            BridgeEndCell = origin;
            spriteRenderer = renderer;
            RegisterActiveBuilding();
            EnsureResourceStore();
            EnsureHouseWarmthState();
            EnsureGroundDetail();
            EnsureWorldShadow();
            EnsureClickCollider();
        }

        public void RestoreStableId(string value)
        {
            stableId = string.IsNullOrWhiteSpace(value)
                ? Guid.NewGuid().ToString("N")
                : value;
        }

        private void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
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
            EnsureGroundDetail();
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

        public bool HasProductionUpgrade(StrategyProductionBuildingUpgradeType type)
        {
            return type != StrategyProductionBuildingUpgradeType.None && productionUpgrades.Contains(type);
        }

        public bool TryRegisterProductionUpgrade(StrategyProductionBuildingUpgradeType type)
        {
            if (type == StrategyProductionBuildingUpgradeType.None || productionUpgrades.Contains(type))
            {
                return false;
            }

            productionUpgrades.Add(type);
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

        private void EnsureHouseWarmthState()
        {
            warmth = Tool == StrategyBuildTool.House ? GetComponent<StrategyHouseWarmthState>() : null;
            if (Tool != StrategyBuildTool.House)
            {
                return;
            }

            if (warmth == null)
            {
                warmth = gameObject.AddComponent<StrategyHouseWarmthState>();
            }

            warmth.Configure(this);
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

        private void EnsureGroundDetail()
        {
            StrategyBuildingGroundDetail detail = GetComponent<StrategyBuildingGroundDetail>();
            if (detail == null)
            {
                detail = gameObject.AddComponent<StrategyBuildingGroundDetail>();
            }

            detail.Configure(this);
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

        private void RegisterActiveBuilding()
        {
            if (registeredActiveBuilding)
            {
                return;
            }

            activeBuildings.Add(this);
            registeredActiveBuilding = true;
        }

        public bool BeginDemolition()
        {
            if (IsDemolishing)
            {
                return false;
            }

            IsDemolishing = true;
            if (registeredActiveBuilding)
            {
                activeBuildings.Remove(this);
                registeredActiveBuilding = false;
            }

            return true;
        }

        private void OnDestroy()
        {
            if (!registeredActiveBuilding)
            {
                return;
            }

            activeBuildings.Remove(this);
            registeredActiveBuilding = false;
        }
    }
}
