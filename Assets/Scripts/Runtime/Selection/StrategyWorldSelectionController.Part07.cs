using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {

        private void UpdateSelectionLinks(Transform target)
        {
            StrategyPlacedBuilding building = target != null ? target.GetComponent<StrategyPlacedBuilding>() : null;
            if (building != null)
            {
                UpdateSelectionLinks(building);
                return;
            }

            StrategyConstructionSite site = target != null ? target.GetComponent<StrategyConstructionSite>() : null;
            if (site != null)
            {
                UpdateSelectionLinks(site);
                return;
            }

            ClearSelectionLinks();
        }

        private void UpdateSelectionLinks(StrategyPlacedBuilding building)
        {
            linkedResidentsScratch.Clear();
            CollectLinkedResidents(building, linkedResidentsScratch);
            SyncLinkedResidents();
            if (building == null)
            {
                ClearSelectionLinks();
                return;
            }

            RenderSelectionLinks(GetSelectionLinkBuildingAnchor(building));
        }

        private void UpdateSelectionLinks(StrategyConstructionSite site)
        {
            linkedResidentsScratch.Clear();
            CollectLinkedResidents(site, linkedResidentsScratch);
            SyncLinkedResidents();
            if (site == null)
            {
                ClearSelectionLinks();
                return;
            }

            RenderSelectionLinks(GetSelectionLinkSiteAnchor(site));
        }

        private void SyncLinkedResidents()
        {
            bool changed = linkedResidents.Count != linkedResidentsScratch.Count;
            if (!changed)
            {
                for (int i = 0; i < linkedResidents.Count; i++)
                {
                    if (linkedResidents[i] != linkedResidentsScratch[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                linkedResidents.Clear();
                for (int i = 0; i < linkedResidentsScratch.Count; i++)
                {
                    linkedResidents.Add(linkedResidentsScratch[i]);
                }
            }
        }

        private void RenderSelectionLinks(Vector3 sourceAnchor)
        {
            if (linkedResidents.Count <= 0)
            {
                HideSelectionLinks();
                return;
            }

            EnsureSelectionLinkVisualCount(linkedResidents.Count);
            int visibleCount = 0;
            for (int i = 0; i < linkedResidents.Count; i++)
            {
                StrategyResidentAgent resident = linkedResidents[i];
                if (resident == null)
                {
                    continue;
                }

                Vector3 residentAnchor = GetSelectionLinkResidentAnchor(resident);
                SpriteRenderer marker = linkedResidentMarkers[visibleCount];
                LineRenderer line = linkedResidentLines[visibleCount];
                marker.gameObject.SetActive(true);
                line.gameObject.SetActive(true);
                marker.transform.position = new Vector3(residentAnchor.x, residentAnchor.y, -0.055f);
                marker.transform.localScale = GetLinkedResidentMarkerScale(resident);
                marker.sortingOrder = SelectionLinkSortingOrder + 2;
                line.SetPosition(0, new Vector3(sourceAnchor.x, sourceAnchor.y, -0.06f));
                line.SetPosition(1, new Vector3(residentAnchor.x, residentAnchor.y, -0.06f));
                line.sortingOrder = SelectionLinkSortingOrder;
                visibleCount++;
            }

            DisableSelectionLinkVisualsFrom(visibleCount);
        }

        private void CollectLinkedResidents(StrategyPlacedBuilding building, List<StrategyResidentAgent> results)
        {
            if (building == null || results == null)
            {
                return;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                AddResidents(building.Residents, results);
                return;
            }

            StrategyLumberjackCamp lumberjackCamp = building.GetComponent<StrategyLumberjackCamp>();
            if (lumberjackCamp != null)
            {
                AddResidents(lumberjackCamp.Workers, results);
            }

            StrategyStonecutterCamp stonecutterCamp = building.GetComponent<StrategyStonecutterCamp>();
            if (stonecutterCamp != null)
            {
                AddResidents(stonecutterCamp.Workers, results);
            }

            StrategySawmill sawmill = building.GetComponent<StrategySawmill>();
            if (sawmill != null)
            {
                AddResidents(sawmill.Workers, results);
            }

            StrategyMine mine = building.GetComponent<StrategyMine>();
            if (mine != null)
            {
                AddResidents(mine.Workers, results);
            }

            StrategyCoalPit coalPit = building.GetComponent<StrategyCoalPit>();
            if (coalPit != null)
            {
                AddResidents(coalPit.Workers, results);
            }

            StrategyClayPit clayPit = building.GetComponent<StrategyClayPit>();
            if (clayPit != null)
            {
                AddResidents(clayPit.Workers, results);
            }

            StrategyKiln kiln = building.GetComponent<StrategyKiln>();
            if (kiln != null)
            {
                AddResidents(kiln.Workers, results);
            }

            StrategyHunterCamp hunterCamp = building.GetComponent<StrategyHunterCamp>();
            if (hunterCamp != null)
            {
                AddResidents(hunterCamp.Workers, results);
            }

            StrategyFisherHut fisherHut = building.GetComponent<StrategyFisherHut>();
            if (fisherHut != null)
            {
                AddResidents(fisherHut.Workers, results);
            }

            StrategyStorageYard storageYard = building.GetComponent<StrategyStorageYard>();
            if (storageYard != null)
            {
                AddResidents(storageYard.Workers, results);
                AddResidents(storageYard.Builders, results);
            }

            StrategyGranary granary = building.GetComponent<StrategyGranary>();
            if (granary != null)
            {
                AddResidents(granary.Workers, results);
            }
        }

        private void CollectLinkedResidents(StrategyConstructionSite site, List<StrategyResidentAgent> results)
        {
            if (site == null || results == null)
            {
                return;
            }

            AddResidents(site.Builders, results);
        }

        private static void AddResidents(IReadOnlyList<StrategyResidentAgent> source, List<StrategyResidentAgent> results)
        {
            if (source == null || results == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                StrategyResidentAgent resident = source[i];
                if (resident != null && !results.Contains(resident))
                {
                    results.Add(resident);
                }
            }
        }

        private Vector3 GetSelectionLinkBuildingAnchor(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return Vector3.zero;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                return building.HomeAnchor;
            }

            Bounds bounds = building.SelectionBounds;
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.18f, -0.06f);
        }

        private static Vector3 GetSelectionLinkSiteAnchor(StrategyConstructionSite site)
        {
            if (site == null)
            {
                return Vector3.zero;
            }

            Bounds bounds = site.SelectionBounds;
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.18f, -0.06f);
        }

        private static Vector3 GetSelectionLinkResidentAnchor(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return Vector3.zero;
            }

            Bounds bounds = resident.SelectionBounds;
            if (bounds.size.sqrMagnitude > 0.001f)
            {
                return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.10f, -0.06f);
            }

            Vector3 position = resident.transform.position;
            return new Vector3(position.x, position.y, -0.06f);
        }

        private static Vector3 GetLinkedResidentMarkerScale(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return Vector3.one;
            }

            Bounds bounds = resident.SelectionBounds;
            float width = Mathf.Clamp(bounds.size.x * 0.92f, 0.26f, 0.58f);
            float height = resident.LifeStage == StrategyResidentLifeStage.Child ? 0.10f : 0.14f;
            return new Vector3(width, height, 1f);
        }

        private void ClearSelectionLinks()
        {
            linkedResidents.Clear();
            linkedResidentsScratch.Clear();
            HideSelectionLinks();
        }

        private void HideSelectionLinks()
        {
            DisableSelectionLinkVisualsFrom(0);
        }

        private void DisableSelectionLinkVisualsFrom(int startIndex)
        {
            for (int i = Mathf.Max(0, startIndex); i < linkedResidentMarkers.Count; i++)
            {
                if (linkedResidentMarkers[i] != null)
                {
                    linkedResidentMarkers[i].gameObject.SetActive(false);
                }

                if (linkedResidentLines[i] != null)
                {
                    linkedResidentLines[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureSelectionLinkVisualCount(int count)
        {
            EnsureSelectionLinksRoot();
            EnsureLinkedResidentMarkerSprite();
            EnsureLinkLineMaterial();
            while (linkedResidentMarkers.Count < count)
            {
                int index = linkedResidentMarkers.Count;
                GameObject lineObject = new GameObject("Selection Link Line " + index);
                lineObject.transform.SetParent(selectionLinksRoot, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthMultiplier = 0.026f;
                line.numCapVertices = 2;
                line.numCornerVertices = 2;
                line.material = linkLineMaterial;
                line.startColor = new Color(1f, 0.82f, 0.24f, 0.58f);
                line.endColor = new Color(1f, 0.92f, 0.44f, 0.76f);
                line.sortingOrder = SelectionLinkSortingOrder;
                line.gameObject.SetActive(false);
                linkedResidentLines.Add(line);

                GameObject markerObject = new GameObject("Linked Resident Marker " + index);
                markerObject.transform.SetParent(selectionLinksRoot, false);
                SpriteRenderer marker = markerObject.AddComponent<SpriteRenderer>();
                marker.sprite = linkedResidentMarkerSprite;
                marker.color = new Color(1f, 0.86f, 0.20f, 0.52f);
                marker.sortingOrder = SelectionLinkSortingOrder + 2;
                marker.gameObject.SetActive(false);
                linkedResidentMarkers.Add(marker);
            }
        }

        private void EnsureSelectionLinksRoot()
        {
            if (selectionLinksRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("World Selection Links");
            root.transform.SetParent(transform, false);
            selectionLinksRoot = root.transform;
        }

        private void EnsureLinkLineMaterial()
        {
            if (linkLineMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                linkLineMaterial = new Material(shader)
                {
                    name = "Selection Link Line Material"
                };
            }
        }

        private void EnsureLinkedResidentMarkerSprite()
        {
            if (linkedResidentMarkerSprite != null)
            {
                return;
            }

            const int width = 42;
            const int height = 18;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Linked Resident Marker",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.43f;
            float radiusY = height * 0.33f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = (dx * dx) + (dy * dy);
                    if (distance > 1f)
                    {
                        continue;
                    }

                    float rim = Mathf.Clamp01(Mathf.InverseLerp(0.56f, 1f, distance));
                    float alpha = Mathf.Lerp(0.18f, 0.72f, rim);
                    texture.SetPixel(x, y, new Color(1f, 0.78f, 0.16f, alpha));
                }
            }

            texture.Apply(false, false);
            linkedResidentMarkerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                38f);
        }

        private static string DescribeSelection(Transform target)
        {
            if (target == null)
            {
                return "none";
            }

            StrategyResidentAgent resident = target.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                return "resident:" + resident.FullName;
            }

            StrategyPlacedBuilding building = target.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                return "building:" + building.Tool + "@" + building.Origin.x + "," + building.Origin.y;
            }

            StrategyConstructionSite constructionSite = target.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                return "construction:" + constructionSite.Tool + "@" + constructionSite.Origin.x + "," + constructionSite.Origin.y;
            }

            StrategyGraveMarker grave = target.GetComponent<StrategyGraveMarker>();
            if (grave != null)
            {
                return "grave:" + grave.DeceasedName;
            }

            return target.name;
        }

        private void EnsureMarker()
        {
            if (markerSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Selection Marker Pixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, false);
                markerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            if (markerRenderer == null)
            {
                GameObject marker = new GameObject("World Selection Marker");
                marker.transform.SetParent(transform, false);
                markerRenderer = marker.AddComponent<SpriteRenderer>();
                markerRenderer.sprite = markerSprite;
                markerRenderer.color = new Color(1f, 0.88f, 0.18f, 0.38f);
                markerRenderer.gameObject.SetActive(false);
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
