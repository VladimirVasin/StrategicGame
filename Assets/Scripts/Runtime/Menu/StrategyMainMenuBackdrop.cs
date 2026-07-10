using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMainMenuBackdrop : MonoBehaviour
    {
        private const int TextureWidth = 320;
        private const int TextureHeight = 180;
        private const float PixelsPerUnit = 20f;
        private const float FireFrameDuration = 0.14f;

        private SpriteRenderer fireRenderer;
        private SpriteRenderer terrainRenderer;
        private Camera menuCamera;
        private Texture2D terrainTexture;
        private Sprite terrainSprite;
        private float fireTimer;
        private int fireFrame;
        private Vector3 basePosition;
        private bool configured;

        public void Configure()
        {
            if (configured)
            {
                return;
            }

            configured = true;
            basePosition = transform.position;
            menuCamera = Camera.main;
            CreateTerrain();
            CreateSettlement();
            UpdateTerrainScale();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            float time = Time.unscaledTime;
            transform.position = basePosition + new Vector3(Mathf.Sin(time * 0.11f) * 0.08f, Mathf.Cos(time * 0.09f) * 0.05f, 0f);
            UpdateTerrainScale();
            fireTimer += Time.unscaledDeltaTime;
            if (fireRenderer == null || fireTimer < FireFrameDuration)
            {
                return;
            }

            fireTimer -= FireFrameDuration;
            fireFrame = (fireFrame + 1) % StrategyCampfireSpriteFactory.FrameCount;
            fireRenderer.sprite = StrategyCampfireSpriteFactory.GetFrame(fireFrame);
        }

        private void CreateTerrain()
        {
            Color32[] pixels = new Color32[TextureWidth * TextureHeight];
            Color32 grass = new Color32(66, 104, 56, 255);
            Color32 meadow = new Color32(83, 119, 62, 255);
            Color32 forest = new Color32(45, 78, 48, 255);
            Color32 road = new Color32(104, 82, 48, 255);
            Color32 water = new Color32(42, 89, 112, 255);
            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    uint hash = Hash((uint)x, (uint)y);
                    Color32 pixel = (hash & 15u) < 3u ? meadow : grass;
                    if (x > 242 + Mathf.Sin(y * 0.055f) * 10f)
                    {
                        pixel = water;
                    }
                    else if ((x < 54 && y > 112) || (x > 205 && y < 48))
                    {
                        pixel = forest;
                    }

                    bool horizontalRoad = y >= 83 && y <= 89 && x > 82 && x < 252;
                    bool verticalRoad = x >= 171 && x <= 177 && y > 42 && y < 136;
                    if (horizontalRoad || verticalRoad)
                    {
                        pixel = road;
                    }

                    if ((x % 16 == 0 || y % 16 == 0) && (hash & 3u) == 0u)
                    {
                        pixel = Darken(pixel, 8);
                    }

                    pixels[y * TextureWidth + x] = pixel;
                }
            }

            terrainTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "Main Menu Settlement Terrain",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            terrainTexture.SetPixels32(pixels);
            terrainTexture.Apply(false, false);
            terrainSprite = Sprite.Create(
                terrainTexture,
                new Rect(0f, 0f, TextureWidth, TextureHeight),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            GameObject terrainObject = new GameObject("Menu Terrain");
            terrainObject.transform.SetParent(transform, false);
            terrainRenderer = terrainObject.AddComponent<SpriteRenderer>();
            terrainRenderer.sprite = terrainSprite;
            terrainRenderer.sortingOrder = StrategyWorldSorting.TerrainOrder;
        }

        private void CreateSettlement()
        {
            CreateBuilding(StrategyBuildTool.House, 1, new Vector3(1.4f, 1.55f, 0f), 1.08f);
            CreateBuilding(StrategyBuildTool.House, 3, new Vector3(4.6f, -0.2f, 0f), 0.96f);
            CreateBuilding(StrategyBuildTool.LumberjackCamp, 0, new Vector3(6.4f, 2.4f, 0f), 0.92f);
            CreateBuilding(StrategyBuildTool.StarterCaravanCart, 0, new Vector3(0.4f, -1.1f, 0f), 0.9f);

            CreateNature(StrategyNaturePropKind.LargeTree, 1, new Vector3(-5.8f, 2.7f, 0f), 1.15f);
            CreateNature(StrategyNaturePropKind.LargeTree, 3, new Vector3(-4.9f, -2.8f, 0f), 1.0f);
            CreateNature(StrategyNaturePropKind.SmallTree, 2, new Vector3(6.7f, -2.7f, 0f), 1.0f);
            CreateNature(StrategyNaturePropKind.Bush, 0, new Vector3(2.6f, -2.4f, 0f), 1.0f);
            CreateNature(StrategyNaturePropKind.Bush, 2, new Vector3(5.7f, 1.1f, 0f), 0.9f);

            fireRenderer = CreateRenderer("Menu Campfire", StrategyCampfireSpriteFactory.GetFrame(0), new Vector3(2.2f, -0.35f, 0f), 1.15f);
            CreateResident(StrategyResidentGender.Male, 2, new Vector3(1.65f, -0.7f, 0f));
            CreateResident(StrategyResidentGender.Female, 4, new Vector3(2.85f, -0.95f, 0f));
            CreateResident(StrategyResidentGender.Female, 1, new Vector3(3.25f, 1.0f, 0f));
        }

        private void CreateBuilding(StrategyBuildTool tool, int variant, Vector3 position, float scale)
        {
            if (StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, variant, out Sprite sprite))
            {
                CreateRenderer("Menu " + tool, sprite, position, scale);
            }
        }

        private void CreateNature(StrategyNaturePropKind kind, int variant, Vector3 position, float scale)
        {
            CreateRenderer("Menu " + kind, StrategyNatureSpriteFactory.GetSprite(kind, variant), position, scale);
        }

        private void CreateResident(StrategyResidentGender gender, int variant, Vector3 position)
        {
            CreateRenderer("Menu Resident", StrategyResidentSpriteFactory.GetSprite(gender, variant), position, 1.12f);
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, Vector3 position, float scale)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = position;
            child.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            StrategyWorldSorting.Apply(renderer, position);
            return renderer;
        }

        private void OnDestroy()
        {
            if (terrainSprite != null)
            {
                Destroy(terrainSprite);
            }

            if (terrainTexture != null)
            {
                Destroy(terrainTexture);
            }
        }

        private void UpdateTerrainScale()
        {
            if (terrainRenderer == null || menuCamera == null)
            {
                return;
            }

            float viewHeight = menuCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * Mathf.Max(0.1f, menuCamera.aspect);
            terrainRenderer.transform.localScale = new Vector3(
                Mathf.Max(1.04f, viewWidth / (TextureWidth / PixelsPerUnit) + 0.04f),
                Mathf.Max(1.04f, viewHeight / (TextureHeight / PixelsPerUnit) + 0.04f),
                1f);
        }

        private static uint Hash(uint x, uint y)
        {
            uint value = x * 374761393u + y * 668265263u + 2246822519u;
            value = (value ^ (value >> 13)) * 1274126177u;
            return value ^ (value >> 16);
        }

        private static Color32 Darken(Color32 color, byte amount)
        {
            return new Color32(
                (byte)Mathf.Max(0, color.r - amount),
                (byte)Mathf.Max(0, color.g - amount),
                (byte)Mathf.Max(0, color.b - amount),
                color.a);
        }
    }
}
