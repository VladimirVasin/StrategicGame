using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void UpdateWorldSorting()
        {
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            SyncReadabilityRenderers();
        }

        private void SyncCarriedLogsRenderer()
        {
            if (spriteRenderer == null || carriedLogsRenderer == null)
            {
                return;
            }

            carriedLogsRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedLogsSprite();
            carriedLogsRenderer.flipX = spriteRenderer.flipX;
            carriedLogsRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.14f : 0.14f;
            carriedLogsRenderer.transform.localPosition = new Vector3(side, 0.44f, -0.02f);
            carriedLogsRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedStoneRenderer()
        {
            if (spriteRenderer == null || carriedStoneRenderer == null)
            {
                return;
            }

            carriedStoneRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedStoneSprite();
            carriedStoneRenderer.flipX = spriteRenderer.flipX;
            carriedStoneRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedStoneRenderer.transform.localPosition = new Vector3(side, 0.38f, -0.02f);
            carriedStoneRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedGameRenderer()
        {
            if (spriteRenderer == null || carriedGameRenderer == null)
            {
                return;
            }

            carriedGameRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedGameSprite();
            carriedGameRenderer.flipX = spriteRenderer.flipX;
            carriedGameRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedGameRenderer.transform.localPosition = new Vector3(side, 0.40f, -0.02f);
            carriedGameRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedFishRenderer()
        {
            if (spriteRenderer == null || carriedFishRenderer == null)
            {
                return;
            }

            carriedFishRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedFishSprite();
            carriedFishRenderer.flipX = spriteRenderer.flipX;
            carriedFishRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedFishRenderer.transform.localPosition = new Vector3(side, 0.40f, -0.02f);
            carriedFishRenderer.transform.localScale = Vector3.one;
        }

        private void SyncCarriedForageRenderer()
        {
            if (spriteRenderer == null || carriedForageRenderer == null)
            {
                return;
            }

            carriedForageRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(carriedForageResource);
            carriedForageRenderer.flipX = spriteRenderer.flipX;
            carriedForageRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.12f : 0.12f;
            carriedForageRenderer.transform.localPosition = new Vector3(side, 0.43f, -0.02f);
            carriedForageRenderer.transform.localScale = Vector3.one;
        }

        private void SyncFishingLineRenderer()
        {
            if (spriteRenderer == null
                || fishingLineRenderer == null
                || fishingBobberRenderer == null
                || !fishingLineRenderer.gameObject.activeSelf)
            {
                return;
            }

            Vector3 rodWorld = GetFishingRodTipWorld();
            Vector3 bobberWorld = GetFishingBobberWorld();
            Vector3 midpoint = (rodWorld + bobberWorld) * 0.5f;
            Vector3 delta = bobberWorld - rodWorld;
            float distance = Mathf.Max(0.05f, delta.magnitude);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            fishingLineRenderer.transform.localPosition = transform.InverseTransformPoint(midpoint);
            fishingLineRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            fishingLineRenderer.transform.localScale = new Vector3(distance, 1f, 1f);
            fishingLineRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
            fishingBobberRenderer.transform.localPosition = transform.InverseTransformPoint(bobberWorld);
            fishingBobberRenderer.transform.localRotation = Quaternion.identity;
            fishingBobberRenderer.transform.localScale = Vector3.one;
            fishingBobberRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
        }

        private static Sprite CreateReadabilityShadowSprite()
        {
            const int width = 36;
            const int height = 16;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Resident Readability Shadow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.46f;
            float radiusY = height * 0.34f;
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

                    float alpha = Mathf.Lerp(0.12f, 0.62f, 1f - distance);
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateFishingLineSprite()
        {
            const int width = 32;
            const int height = 2;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fishing Line Sprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            Color line = new Color(0.86f, 0.91f, 0.84f, 0.72f);
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, 0, line);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateFishingBobberSprite()
        {
            const int width = 12;
            const int height = 14;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fishing Bobber Sprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            Color outline = new Color32(54, 42, 34, 255);
            Color red = new Color32(205, 48, 42, 255);
            Color white = new Color32(239, 232, 199, 255);
            for (int y = -4; y <= 4; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    if (x * x * 16 + y * y * 9 <= 144)
                    {
                        int px = width / 2 + x;
                        int py = height / 2 + y;
                        texture.SetPixel(px, py, y >= 0 ? red : white);
                    }
                }
            }

            for (int x = 3; x <= 9; x++)
            {
                texture.SetPixel(x, 2, outline);
                texture.SetPixel(x, 11, outline);
            }

            for (int y = 3; y <= 10; y++)
            {
                texture.SetPixel(3, y, outline);
                texture.SetPixel(9, y, outline);
            }

            texture.SetPixel(width / 2, 12, outline);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static bool IsUpgradeCell(Vector2Int cell, StrategyBuildingUpgrade upgrade)
        {
            return cell.x >= upgrade.Origin.x
                && cell.x < upgrade.Origin.x + upgrade.Footprint.x
                && cell.y >= upgrade.Origin.y
                && cell.y < upgrade.Origin.y + upgrade.Footprint.y;
        }

        private static string GetFallbackName(StrategyResidentGender residentGender, int visualVariant)
        {
            return residentGender == StrategyResidentGender.Male
                ? "Settler " + (visualVariant + 1)
                : "Settler " + (visualVariant + 1);
        }

        public bool TryChangeFamilyName(string familyName)
        {
            string normalizedFamilyName = string.IsNullOrWhiteSpace(familyName)
                ? string.Empty
                : familyName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedFamilyName) || FamilyName == normalizedFamilyName)
            {
                return false;
            }

            string givenName = ExtractGivenName(FullName);
            FamilyName = normalizedFamilyName;
            FullName = string.IsNullOrWhiteSpace(givenName)
                ? normalizedFamilyName
                : givenName + " " + normalizedFamilyName;
            gameObject.name = FullName;
            return true;
        }

        private static string ExtractGivenName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            string trimmed = fullName.Trim();
            int splitIndex = trimmed.LastIndexOf(' ');
            return splitIndex > 0 ? trimmed.Substring(0, splitIndex) : trimmed;
        }

        private static string ExtractFamilyName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            string[] parts = fullName.Split(' ');
            return parts.Length > 1 ? parts[parts.Length - 1] : string.Empty;
        }
    }
}
