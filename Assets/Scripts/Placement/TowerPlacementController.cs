using System.Collections.Generic;
using TD.Gameplay;
using TD.Tower;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TowerBehaviour = TD.Tower.Tower;

namespace TD.Placement
{
    public class TowerPlacementController : MonoBehaviour
    {
        [Header("Tower Selection")]
        [SerializeField] private TowerData[] towerOptions;
        [SerializeField] private TowerData selectedTower;

        [Header("Input")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool allowPlacementInput = true;
        [SerializeField] private LayerMask placementTileLayer = ~0;

        [Header("Game Over")]
        [SerializeField] private LifeManager lifeManager;

        [Header("Scene Roots")]
        [SerializeField] private Transform towersRoot;

        [Header("Cost")]
        [SerializeField] private MonoBehaviour costProviderBehaviour;
        [SerializeField] private bool allowPlacementWithoutCostProvider = true;

        [Header("Preview")]
        [SerializeField] private bool showTowerPreview = true;
        [SerializeField] private float previewAlpha = 0.45f;
        [SerializeField] private Color validPreviewColor = new Color(0.55f, 1f, 0.55f, 0.45f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.35f, 0.35f, 0.45f);
        [SerializeField] private int previewSortingOrder = 50;

        [Header("Range Indicator")]
        [SerializeField] private bool showRangeIndicator = true;
        [SerializeField] private RangeIndicator rangeIndicatorPrefab;
        [SerializeField] private RangeIndicator rangeIndicator;

        [Header("Events")]
        [SerializeField] private UnityEvent<TowerData> towerSelected;
        [SerializeField] private UnityEvent<bool> placementModeChanged;
        [SerializeField] private UnityEvent<TowerBehaviour, PlacementTile> towerPlaced;
        [SerializeField] private UnityEvent<PlacementTile> placementFailed;
        [SerializeField] private UnityEvent<TowerData> insufficientCost;

        [Header("Debug")]
        [SerializeField] private int installedTowerCount;

        private readonly Dictionary<PlacementTile, TowerBehaviour> placedTowers = new Dictionary<PlacementTile, TowerBehaviour>();
        private PlacementTile hoveredTile;
        private GameObject previewObject;
        private TowerData previewData;
        private SpriteRenderer[] previewRenderers;
        private bool loggedMissingPreviewPrefab;
        private ITowerCostProvider costProvider;

        public TowerData SelectedTower => selectedTower;
        public bool IsPlacementModeActive => selectedTower != null;
        public bool AllowPlacementInput => allowPlacementInput;
        public PlacementTile HoveredTile => hoveredTile;
        public int InstalledTowerCount => placedTowers.Count;
        public IReadOnlyDictionary<PlacementTile, TowerBehaviour> PlacedTowers => placedTowers;

        private void OnEnable()
        {
            SubscribeLifeManager();
        }

        private void OnDisable()
        {
            UnsubscribeLifeManager();
            ClearHover();
            HideRangeIndicator();
        }

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            CacheCostProvider();
        }

        private void Update()
        {
            if (!allowPlacementInput)
            {
                ClearSelectionVisuals();
                return;
            }

            if (selectedTower == null)
            {
                ClearSelectionVisuals();
                return;
            }

            if (IsCancelPressed())
            {
                CancelPlacement();
                return;
            }

            UpdateHoveredTile();
            UpdatePreview();
            UpdateRangeIndicator();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlaceSelectedTower();
            }
        }

        public void SelectTower(TowerData towerData)
        {
            if (!allowPlacementInput)
            {
                return;
            }

            selectedTower = towerData;
            towerSelected?.Invoke(selectedTower);
            placementModeChanged?.Invoke(IsPlacementModeActive);

            if (selectedTower == null)
            {
                ClearSelectionVisuals();
                return;
            }

            ValidateSelectedTowerRange();
            CreatePreview(selectedTower);
        }

        public void SelectTower(TowerPlacementData placementData)
        {
            SelectTower(placementData != null ? placementData.TowerData : null);
        }

        public void SelectTowerByIndex(int index)
        {
            if (towerOptions == null || index < 0 || index >= towerOptions.Length)
            {
                SelectTower((TowerData)null);
                return;
            }

            SelectTower(towerOptions[index]);
        }

        public void CancelPlacement()
        {
            if (selectedTower == null)
            {
                return;
            }

            DeselectTower();
        }

        public void ClearSelection()
        {
            DeselectTower();
        }

        public void DeselectTower()
        {
            if (selectedTower == null)
            {
                return;
            }

            selectedTower = null;
            towerSelected?.Invoke(null);
            placementModeChanged?.Invoke(false);
            ClearSelectionVisuals();
        }

        public void SetPlacementInputEnabled(bool enabled)
        {
            allowPlacementInput = enabled;

            if (!allowPlacementInput)
            {
                DeselectTower();
                ClearSelectionVisuals();
            }
        }

        public void HandleGameOver()
        {
            SetPlacementInputEnabled(false);
        }

        public void SetLifeManager(LifeManager manager)
        {
            UnsubscribeLifeManager();
            lifeManager = manager;
            SubscribeLifeManager();
        }

        public bool TryPlaceSelectedTower()
        {
            if (selectedTower == null || hoveredTile == null)
            {
                placementFailed?.Invoke(hoveredTile);
                return false;
            }

            if (!CanUseTileForPlacement(hoveredTile) || selectedTower.Prefab == null)
            {
                placementFailed?.Invoke(hoveredTile);
                return false;
            }

            if (!CanAfford(selectedTower))
            {
                insufficientCost?.Invoke(selectedTower);
                placementFailed?.Invoke(hoveredTile);
                return false;
            }

            Vector3 towerPosition = hoveredTile.GetBuildPosition();
            TowerBehaviour tower = Instantiate(
                selectedTower.Prefab,
                towerPosition,
                Quaternion.identity,
                towersRoot);

            tower.Initialize(selectedTower);
            tower.SetCostProvider(costProviderBehaviour);
            tower.transform.position = towerPosition;

            if (!hoveredTile.PlaceTower(tower))
            {
                Destroy(tower.gameObject);
                placementFailed?.Invoke(hoveredTile);
                return false;
            }

            if (!SpendCost(selectedTower))
            {
                hoveredTile.ClearTower();
                Destroy(tower.gameObject);
                insufficientCost?.Invoke(selectedTower);
                placementFailed?.Invoke(hoveredTile);
                return false;
            }

            placedTowers[hoveredTile] = tower;
            RefreshInstalledTowerCount();
            towerPlaced?.Invoke(tower, hoveredTile);
            DeselectTower();
            return true;
        }

        private void UpdateHoveredTile()
        {
            PlacementTile nextTile = TryGetTileUnderMouse();
            if (hoveredTile == nextTile)
            {
                RefreshHoveredTile();
                return;
            }

            ClearHover();
            hoveredTile = nextTile;
            RefreshHoveredTile();
        }

        private PlacementTile TryGetTileUnderMouse()
        {
            if (worldCamera == null || Mouse.current == null)
            {
                return null;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 screenPosition = new Vector3(mousePosition.x, mousePosition.y, GetCameraWorldDistance());
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPosition, placementTileLayer);

            return hit != null ? hit.GetComponentInParent<PlacementTile>() : null;
        }

        private void RefreshHoveredTile()
        {
            if (hoveredTile == null || selectedTower == null)
            {
                return;
            }

            hoveredTile.SetHover(CanPlaceAtTile(hoveredTile));
        }

        private void ClearHover()
        {
            if (hoveredTile != null)
            {
                hoveredTile.ClearHover();
            }

            hoveredTile = null;
            HideRangeIndicator();
        }

        private void ClearSelectionVisuals()
        {
            ClearHover();
            DestroyPreview();
            HideRangeIndicator();
        }

        private void CreatePreview(TowerData data)
        {
            DestroyPreview();
            previewData = data;

            if (!showTowerPreview || data == null)
            {
                return;
            }

            GameObject sourcePrefab = data.PreviewPrefab;
            if (sourcePrefab == null && data.Prefab != null)
            {
                sourcePrefab = data.Prefab.gameObject;

                if (!loggedMissingPreviewPrefab)
                {
                    Debug.LogWarning($"TowerData '{data.TowerName}' has no previewPrefab. Using tower prefab as preview.");
                    loggedMissingPreviewPrefab = true;
                }
            }

            if (sourcePrefab == null)
            {
                return;
            }

            previewObject = Instantiate(sourcePrefab, transform);
            previewObject.name = $"{data.TowerName}_Preview";
            DisablePreviewGameplayComponents(previewObject);

            previewRenderers = previewObject.GetComponentsInChildren<SpriteRenderer>(true);
            ApplyPreviewSortingOrder();
            SetPreviewColor(validPreviewColor);
            HidePreview();
        }

        private void UpdatePreview()
        {
            if (!showTowerPreview || selectedTower == null || hoveredTile == null)
            {
                HidePreview();
                return;
            }

            if (previewObject == null || previewData != selectedTower)
            {
                CreatePreview(selectedTower);
            }

            if (previewObject == null)
            {
                return;
            }

            bool canPlace = CanPlaceAtTile(hoveredTile);
            previewObject.transform.position = hoveredTile.GetBuildPosition();
            SetPreviewColor(canPlace ? validPreviewColor : invalidPreviewColor);
            previewObject.SetActive(true);
        }

        private void HidePreview()
        {
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
        }

        private void DestroyPreview()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }

            previewObject = null;
            previewData = null;
            previewRenderers = null;
        }

        private void SetPreviewColor(Color color)
        {
            if (previewRenderers == null)
            {
                return;
            }

            color.a = previewAlpha;
            foreach (SpriteRenderer previewRenderer in previewRenderers)
            {
                if (previewRenderer != null)
                {
                    previewRenderer.color = color;
                }
            }
        }

        private void ApplyPreviewSortingOrder()
        {
            if (previewRenderers == null)
            {
                return;
            }

            foreach (SpriteRenderer previewRenderer in previewRenderers)
            {
                if (previewRenderer != null)
                {
                    previewRenderer.sortingOrder = previewSortingOrder;
                }
            }
        }

        private static void DisablePreviewGameplayComponents(GameObject preview)
        {
            if (preview == null)
            {
                return;
            }

            foreach (Collider2D previewCollider in preview.GetComponentsInChildren<Collider2D>(true))
            {
                if (previewCollider != null)
                {
                    previewCollider.enabled = false;
                }
            }

            foreach (Rigidbody2D previewRigidbody in preview.GetComponentsInChildren<Rigidbody2D>(true))
            {
                if (previewRigidbody != null)
                {
                    previewRigidbody.simulated = false;
                }
            }

            foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void UpdateRangeIndicator()
        {
            if (!showRangeIndicator || selectedTower == null || hoveredTile == null)
            {
                HideRangeIndicator();
                return;
            }

            float attackRange = selectedTower.AttackRange;
            if (attackRange <= 0f)
            {
                HideRangeIndicator();
                return;
            }

            RangeIndicator indicator = EnsureRangeIndicator();
            if (indicator == null)
            {
                return;
            }

            bool canPlace = CanPlaceAtTile(hoveredTile);
            indicator.SetValidState(canPlace);
            indicator.Show(hoveredTile.GetBuildPosition(), attackRange);
        }

        private RangeIndicator EnsureRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                return rangeIndicator;
            }

            if (rangeIndicatorPrefab != null)
            {
                rangeIndicator = Instantiate(rangeIndicatorPrefab, transform);
                return rangeIndicator;
            }

            GameObject indicatorObject = new GameObject("RangeIndicator");
            indicatorObject.transform.SetParent(transform);

            SpriteRenderer renderer = indicatorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetGeneratedCircleSprite();
            renderer.sortingOrder = 100;

            rangeIndicator = indicatorObject.AddComponent<RangeIndicator>();
            rangeIndicator.SetSpriteRenderer(renderer);
            rangeIndicator.Hide();
            return rangeIndicator;
        }

        private void HideRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.Hide();
            }
        }

        private void ValidateSelectedTowerRange()
        {
            if (selectedTower != null && selectedTower.AttackRange <= 0f)
            {
                Debug.LogWarning($"Tower '{selectedTower.TowerName}' has attackRange <= 0. Range indicator will be hidden.");
            }
        }

        public bool CanPlaceAtTile(PlacementTile tile)
        {
            return CanUseTileForPlacement(tile) && CanAfford(selectedTower);
        }

        private bool CanUseTileForPlacement(PlacementTile tile)
        {
            return selectedTower != null && selectedTower.Prefab != null && tile != null && tile.CanPlace();
        }

        public bool CanAffordSelectedTower()
        {
            return CanAfford(selectedTower);
        }

        public bool CanAfford(TowerData towerData)
        {
            if (towerData == null)
            {
                return false;
            }

            if (towerData.Cost <= 0)
            {
                return true;
            }

            ITowerCostProvider provider = GetCostProvider();
            if (provider == null)
            {
                return allowPlacementWithoutCostProvider;
            }

            return provider.CanAfford(towerData.Cost);
        }

        public bool SpendCost(TowerData towerData)
        {
            if (towerData == null)
            {
                return false;
            }

            if (towerData.Cost <= 0)
            {
                return true;
            }

            ITowerCostProvider provider = GetCostProvider();
            if (provider == null)
            {
                return allowPlacementWithoutCostProvider;
            }

            return provider.SpendCost(towerData.Cost);
        }

        public bool TryGetPlacedTower(PlacementTile tile, out TowerBehaviour tower)
        {
            tower = null;
            return tile != null && placedTowers.TryGetValue(tile, out tower);
        }

        public bool RemovePlacedTower(PlacementTile tile, bool destroyTower = true)
        {
            if (tile == null || !placedTowers.TryGetValue(tile, out TowerBehaviour tower))
            {
                return false;
            }

            placedTowers.Remove(tile);
            tile.ClearTower();
            RefreshInstalledTowerCount();

            if (destroyTower && tower != null)
            {
                Destroy(tower.gameObject);
            }

            RefreshHoveredTile();
            return true;
        }

        public bool SellPlacedTower(PlacementTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            TowerBehaviour tower = tile.GetCurrentTower();
            if (tower == null && !placedTowers.TryGetValue(tile, out tower))
            {
                return false;
            }

            if (tower == null || !tower.Sell())
            {
                return false;
            }

            placedTowers.Remove(tile);
            RefreshInstalledTowerCount();
            RefreshHoveredTile();
            return true;
        }

        private float GetCameraWorldDistance()
        {
            return worldCamera != null && !worldCamera.orthographic
                ? worldCamera.nearClipPlane
                : Mathf.Abs(worldCamera != null ? worldCamera.transform.position.z : 0f);
        }

        private void RefreshInstalledTowerCount()
        {
            installedTowerCount = placedTowers.Count;
        }

        private ITowerCostProvider GetCostProvider()
        {
            if (costProvider == null)
            {
                CacheCostProvider();
            }

            return costProvider;
        }

        private void CacheCostProvider()
        {
            costProvider = costProviderBehaviour as ITowerCostProvider;
        }

        private void SubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleGameOver);
                lifeManager.OnGameOver.AddListener(HandleGameOver);
            }
        }

        private void UnsubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleGameOver);
            }
        }

        private static bool IsCancelPressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        private static Sprite generatedCircleSprite;

        private static Sprite GetGeneratedCircleSprite()
        {
            if (generatedCircleSprite != null)
            {
                return generatedCircleSprite;
            }

            const int size = 128;
            const float radius = size * 0.5f - 1f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Texture2D texture = new Texture2D(size, size)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            generatedCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);

            return generatedCircleSprite;
        }
    }
}
