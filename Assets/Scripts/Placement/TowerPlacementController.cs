using System.Collections.Generic;
using TD.Tower;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
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

        [Header("Tilemaps")]
        [SerializeField] private Grid placementGrid;
        [SerializeField] private Tilemap buildableTilemap;
        [SerializeField] private Tilemap placementOverlayTilemap;
        [SerializeField] private Transform towersRoot;

        [Header("Cost")]
        [SerializeField] private MonoBehaviour costProviderBehaviour;
        [SerializeField] private bool allowPlacementWithoutCostProvider = true;

        [Header("Preview")]
        [SerializeField] private bool showTowerPreview = true;
        [SerializeField] private float previewAlpha = 0.45f;
        [SerializeField] private Color validPreviewColor = new Color(0.55f, 1f, 0.55f, 0.45f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.35f, 0.35f, 0.45f);

        [Header("Overlay")]
        [SerializeField] private TileBase buildableHoverTile;
        [SerializeField] private TileBase blockedHoverTile;
        [SerializeField] private Color buildableHoverColor = new Color(0.25f, 1f, 0.25f, 0.55f);
        [SerializeField] private Color blockedHoverColor = new Color(1f, 0.2f, 0.2f, 0.55f);

        [Header("Events")]
        [SerializeField] private UnityEvent<TowerData> towerSelected;
        [SerializeField] private UnityEvent<TowerBehaviour, Vector3Int> towerPlaced;
        [SerializeField] private UnityEvent<Vector3Int> placementFailed;
        [SerializeField] private UnityEvent<TowerData> insufficientCost;

        [Header("Debug")]
        [SerializeField] private int installedTowerCount;

        private readonly Dictionary<Vector3Int, TowerBehaviour> placedTowers = new Dictionary<Vector3Int, TowerBehaviour>();
        private Vector3Int hoveredCell;
        private bool hasHoveredCell;
        private TowerBehaviour previewTower;
        private TowerData previewData;
        private SpriteRenderer[] previewRenderers;
        private Collider2D[] previewColliders;
        private ITowerCostProvider costProvider;

        public TowerData SelectedTower => selectedTower;
        public bool HasHoveredCell => hasHoveredCell;
        public Vector3Int HoveredCell => hoveredCell;
        public int InstalledTowerCount => placedTowers.Count;
        public IReadOnlyDictionary<Vector3Int, TowerBehaviour> PlacedTowers => placedTowers;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (placementGrid == null && buildableTilemap != null)
            {
                placementGrid = buildableTilemap.layoutGrid;
            }

            CacheCostProvider();
            EnsureOverlayTiles();
        }

        private void OnValidate()
        {
            if (placementGrid == null && buildableTilemap != null)
            {
                placementGrid = buildableTilemap.layoutGrid;
            }
        }

        private void Update()
        {
            if (!allowPlacementInput)
            {
                ClearHover();
                SetPreviewVisible(false);
                return;
            }

            if (selectedTower == null)
            {
                ClearHover();
                SetPreviewVisible(false);
                return;
            }

            UpdateHoveredCell();
            UpdatePreview();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlaceSelectedTower();
            }
        }

        public void SelectTower(TowerData towerData)
        {
            selectedTower = towerData;
            towerSelected?.Invoke(selectedTower);

            if (selectedTower == null)
            {
                ClearSelectionVisuals();
            }
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

        public void ClearSelection()
        {
            DeselectTower();
        }

        public void DeselectTower()
        {
            selectedTower = null;
            towerSelected?.Invoke(null);
            ClearSelectionVisuals();
        }

        public bool TryPlaceSelectedTower()
        {
            if (selectedTower == null || !hasHoveredCell)
            {
                placementFailed?.Invoke(hoveredCell);
                return false;
            }

            if (!CanPlaceAtCell(hoveredCell) || selectedTower.Prefab == null)
            {
                placementFailed?.Invoke(hoveredCell);
                return false;
            }

            if (!CanAfford(selectedTower))
            {
                insufficientCost?.Invoke(selectedTower);
                placementFailed?.Invoke(hoveredCell);
                return false;
            }

            if (!SpendCost(selectedTower))
            {
                insufficientCost?.Invoke(selectedTower);
                placementFailed?.Invoke(hoveredCell);
                return false;
            }

            Vector3 towerPosition = GetCellCenterWorld(hoveredCell);
            TowerBehaviour tower = Instantiate(
                selectedTower.Prefab,
                towerPosition,
                Quaternion.identity,
                towersRoot);

            tower.Initialize(selectedTower);
            tower.transform.position = towerPosition;
            placedTowers.Add(hoveredCell, tower);
            RefreshInstalledTowerCount();

            towerPlaced?.Invoke(tower, hoveredCell);
            DeselectTower();

            return true;
        }

        private void UpdateHoveredCell()
        {
            bool hasNextCell = TryGetCellUnderMouse(out Vector3Int nextCell);
            if (hasHoveredCell && hasNextCell && hoveredCell == nextCell)
            {
                RefreshHoveredCellOverlay();
                return;
            }

            ClearHover();

            if (!hasNextCell)
            {
                return;
            }

            hoveredCell = nextCell;
            hasHoveredCell = true;
            RefreshHoveredCellOverlay();
        }

        private bool TryGetCellUnderMouse(out Vector3Int cell)
        {
            cell = default(Vector3Int);

            if (worldCamera == null || Mouse.current == null)
            {
                return false;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 screenPosition = new Vector3(mousePos.x, mousePos.y, GetCameraWorldDistance());
            Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(screenPosition);

            if (placementGrid != null)
            {
                cell = placementGrid.WorldToCell(mouseWorld);
                return true;
            }

            if (buildableTilemap != null)
            {
                cell = buildableTilemap.WorldToCell(mouseWorld);
                return true;
            }

            return false;
        }

        private void RefreshHoveredCellOverlay()
        {
            if (!hasHoveredCell || placementOverlayTilemap == null)
            {
                return;
            }

            bool canPlace = CanPlaceAtCell(hoveredCell);
            placementOverlayTilemap.SetTile(hoveredCell, canPlace ? buildableHoverTile : blockedHoverTile);
            placementOverlayTilemap.SetColor(hoveredCell, canPlace ? buildableHoverColor : blockedHoverColor);
        }

        private void ClearHover()
        {
            if (hasHoveredCell && placementOverlayTilemap != null)
            {
                placementOverlayTilemap.SetTile(hoveredCell, null);
            }

            hasHoveredCell = false;
            hoveredCell = default(Vector3Int);
        }

        private void ClearSelectionVisuals()
        {
            ClearHover();
            SetPreviewVisible(false);
            DestroyPreview();
        }

        private void UpdatePreview()
        {
            if (!showTowerPreview || selectedTower == null || selectedTower.Prefab == null)
            {
                SetPreviewVisible(false);
                return;
            }

            EnsurePreview();

            if (previewTower == null)
            {
                return;
            }

            SetPreviewVisible(hasHoveredCell);

            if (!hasHoveredCell)
            {
                return;
            }

            bool canPlace = CanPlaceAtCell(hoveredCell);
            previewTower.transform.position = GetCellCenterWorld(hoveredCell);
            SetPreviewColor(canPlace ? validPreviewColor : invalidPreviewColor);
        }

        private void EnsurePreview()
        {
            if (previewTower != null && previewData == selectedTower)
            {
                return;
            }

            DestroyPreview();
            previewData = selectedTower;

            if (previewData == null || previewData.Prefab == null)
            {
                return;
            }

            previewTower = Instantiate(previewData.Prefab);
            previewTower.name = $"{previewData.TowerName}_Preview";
            previewRenderers = previewTower.GetComponentsInChildren<SpriteRenderer>();
            previewColliders = previewTower.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D previewCollider in previewColliders)
            {
                if (previewCollider != null)
                {
                    previewCollider.enabled = false;
                }
            }

            SetPreviewColor(validPreviewColor);
        }

        private void SetPreviewVisible(bool visible)
        {
            if (previewTower != null)
            {
                previewTower.gameObject.SetActive(visible);
            }
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

        private void DestroyPreview()
        {
            if (previewTower != null)
            {
                Destroy(previewTower.gameObject);
            }

            previewTower = null;
            previewData = null;
            previewRenderers = null;
            previewColliders = null;
        }

        public bool CanPlaceAtCell(Vector3Int cell)
        {
            if (selectedTower == null || selectedTower.Prefab == null)
            {
                return false;
            }

            if (buildableTilemap == null || !buildableTilemap.HasTile(cell))
            {
                return false;
            }

            return !placedTowers.ContainsKey(cell);
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

        public bool TryGetPlacedTower(Vector3Int cell, out TowerBehaviour tower)
        {
            return placedTowers.TryGetValue(cell, out tower);
        }

        public bool RemovePlacedTower(Vector3Int cell, bool destroyTower = true)
        {
            if (!placedTowers.TryGetValue(cell, out TowerBehaviour tower))
            {
                return false;
            }

            placedTowers.Remove(cell);
            RefreshInstalledTowerCount();

            if (destroyTower && tower != null)
            {
                Destroy(tower.gameObject);
            }

            RefreshHoveredCellOverlay();
            return true;
        }

        private Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (placementGrid != null)
            {
                return placementGrid.GetCellCenterWorld(cell);
            }

            return buildableTilemap != null ? buildableTilemap.GetCellCenterWorld(cell) : Vector3.zero;
        }

        private float GetCameraWorldDistance()
        {
            return worldCamera != null && !worldCamera.orthographic
                ? worldCamera.nearClipPlane
                : Mathf.Abs(worldCamera != null ? worldCamera.transform.position.z : 0f);
        }

        private void EnsureOverlayTiles()
        {
            if (buildableHoverTile == null)
            {
                buildableHoverTile = CreateOverlayTile(buildableHoverColor);
            }

            if (blockedHoverTile == null)
            {
                blockedHoverTile = CreateOverlayTile(blockedHoverColor);
            }
        }

        private static Tile CreateOverlayTile(Color color)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = GetGeneratedOverlaySprite();
            tile.color = color;
            tile.flags = TileFlags.None;
            return tile;
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

        private static Sprite generatedOverlaySprite;

        private static Sprite GetGeneratedOverlaySprite()
        {
            if (generatedOverlaySprite != null)
            {
                return generatedOverlaySprite;
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            generatedOverlaySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);

            return generatedOverlaySprite;
        }
    }
}
