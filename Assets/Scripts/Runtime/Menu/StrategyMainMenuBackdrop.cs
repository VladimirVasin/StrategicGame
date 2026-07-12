using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMainMenuBackdrop : MonoBehaviour
    {
        private const string ArtworkResourcePath = "Visual/Menu/MainMenuKeyArt";
        private const float ArtworkOverscan = 1.025f;

        private SpriteRenderer artworkRenderer;
        private Camera menuCamera;
        private float lastAspect = -1f;
        private bool configured;

        public void Configure()
        {
            if (configured)
            {
                return;
            }

            configured = true;
            menuCamera = Camera.main;
            CreateArtwork();
            UpdateArtworkScale(true);
        }

        private void Update()
        {
            if (!configured || artworkRenderer == null)
            {
                return;
            }

            UpdateArtworkScale(false);
        }

        private void CreateArtwork()
        {
            Sprite artwork = Resources.Load<Sprite>(ArtworkResourcePath);
            if (artwork == null)
            {
                StrategyDebugLogger.Error(
                    "Menu",
                    "MainMenuArtworkMissing",
                    StrategyDebugLogger.F("resourcesPath", ArtworkResourcePath));
                return;
            }

            GameObject artworkObject = new GameObject("Generated Main Menu Key Art");
            artworkObject.transform.SetParent(transform, false);
            artworkRenderer = artworkObject.AddComponent<SpriteRenderer>();
            artworkRenderer.sprite = artwork;
            artworkRenderer.sortingOrder = StrategyWorldSorting.TerrainOrder;
        }

        private void UpdateArtworkScale(bool force)
        {
            if (artworkRenderer == null || artworkRenderer.sprite == null || menuCamera == null)
            {
                return;
            }

            float aspect = Mathf.Max(0.1f, menuCamera.aspect);
            if (!force && Mathf.Abs(lastAspect - aspect) < 0.001f)
            {
                return;
            }

            lastAspect = aspect;
            Vector2 spriteSize = artworkRenderer.sprite.bounds.size;
            float viewHeight = menuCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * aspect;
            float coverScale = Mathf.Max(
                viewWidth / Mathf.Max(0.01f, spriteSize.x),
                viewHeight / Mathf.Max(0.01f, spriteSize.y));
            artworkRenderer.transform.localScale = Vector3.one * coverScale * ArtworkOverscan;
        }
    }
}
