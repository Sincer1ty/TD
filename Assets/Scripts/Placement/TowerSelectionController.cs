using TD.UI;
using TD.Tower;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TowerBehaviour = TD.Tower.Tower;

namespace TD.Placement
{
    public class TowerSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TowerPlacementController placementController;
        [FormerlySerializedAs("towerLayer")]
        [SerializeField] private LayerMask placementTileLayer = ~0;
        [SerializeField] private bool allowSelection = true;
        [SerializeField] private RangeIndicator rangeIndicatorPrefab;
        [SerializeField] private RangeIndicator rangeIndicator;
        [SerializeField] private TowerUpgradeUI towerUpgradeUI;
        [SerializeField] private UnityEvent towerSelectionChanged = new UnityEvent();
        [SerializeField] private UnityEvent onTowerDeselected = new UnityEvent();

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private TowerBehaviour selectedTower;
        private PlacementTile selectedTile;

        public TowerBehaviour SelectedTower => selectedTower;
        public PlacementTile SelectedTile => selectedTile;
        public UnityEvent OnTowerSelected => towerSelectionChanged;
        public UnityEvent OnTowerDeselected => onTowerDeselected;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (towerUpgradeUI == null)
            {
                towerUpgradeUI = FindFirstObjectByType<TowerUpgradeUI>(FindObjectsInactive.Include);
            }

            SubscribeTowerUpgradeUi();
        }

        private void OnDisable()
        {
            UnsubscribeTowerUpgradeUi();
            DeselectTower();
        }

        private void Update()
        {
            if (selectedTower == null && selectedTile != null)
            {
                DeselectTower();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                DeselectTower();
                return;
            }

            if (!allowSelection || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (placementController != null && placementController.IsPlacementModeActive)
            {
                DeselectTower();
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            PlacementTile clickedTile = TryGetPlacementTileUnderMouse();
            TowerBehaviour clickedTower = clickedTile != null ? clickedTile.GetCurrentTower() : null;

            if (debugLog)
            {
                string tileName = clickedTile != null ? clickedTile.name : "None";
                string towerName = clickedTower != null ? clickedTower.name : "None";
                Debug.Log($"Tower selection click. tile={tileName}, currentTower={towerName}", this);
            }

            if (clickedTile != null && clickedTower != null)
            {
                SelectTower(clickedTower, clickedTile);
            }
            else
            {
                DeselectTower();
            }
        }

        public void SelectTower(TowerBehaviour tower)
        {
            SelectTower(tower, null);
        }

        public void SelectTower(TowerBehaviour tower, PlacementTile tile)
        {
            if (tower == null)
            {
                DeselectTower();
                return;
            }

            if (selectedTower == tower && selectedTile == tile)
            {
                RefreshRangeIndicator();
                ShowSelectedTowerUi();
                return;
            }

            UnsubscribeSelectedTower();
            selectedTower = tower;
            selectedTile = tile;
            SubscribeSelectedTower();

            if (debugLog)
            {
                Debug.Log($"Selected tower '{selectedTower.name}' from placement tile '{(selectedTile != null ? selectedTile.name : "None")}'.", this);
            }

            RefreshRangeIndicator();
            ShowSelectedTowerUi();
            towerSelectionChanged?.Invoke();
        }

        public void DeselectTower()
        {
            if (selectedTower == null)
            {
                HideRangeIndicator();
                HideSelectedTowerUi();
                return;
            }

            UnsubscribeSelectedTower();
            if (debugLog)
            {
                Debug.Log($"Deselected tower '{selectedTower.name}'.", this);
            }

            selectedTower = null;
            selectedTile = null;
            HideRangeIndicator();
            HideSelectedTowerUi();
            onTowerDeselected?.Invoke();
        }

        public void RefreshRangeIndicator()
        {
            if (selectedTower == null)
            {
                HideRangeIndicator();
                return;
            }

            if (selectedTower.CurrentAttackRange <= 0f)
            {
                Debug.LogWarning($"Selected tower '{selectedTower.name}' has CurrentAttackRange <= 0. RangeIndicator will be hidden.", this);
                HideRangeIndicator();
                return;
            }

            RangeIndicator indicator = EnsureRangeIndicator();
            if (indicator == null)
            {
                return;
            }

            indicator.SetValidState(true);
            indicator.Show(selectedTower.transform.position, selectedTower.CurrentAttackRange);

            if (debugLog)
            {
                Debug.Log($"Show selected tower range. tower={selectedTower.name}, range={selectedTower.CurrentAttackRange}, position={selectedTower.transform.position}", this);
            }
        }

        public void SetSelectionEnabled(bool enabled)
        {
            allowSelection = enabled;
            if (!allowSelection)
            {
                DeselectTower();
            }
        }

        private PlacementTile TryGetPlacementTileUnderMouse()
        {
            if (worldCamera == null)
            {
                return null;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 screenPosition = new Vector3(mousePosition.x, mousePosition.y, GetCameraWorldDistance());
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPosition, placementTileLayer);
            return hit != null ? hit.GetComponentInParent<PlacementTile>() : null;
        }

        private RangeIndicator EnsureRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                return rangeIndicator;
            }

            if (rangeIndicatorPrefab == null)
            {
                Debug.LogWarning("TowerSelectionController has no rangeIndicatorPrefab or rangeIndicator assigned. Creating a generated fallback RangeIndicator.", this);
                rangeIndicator = CreateGeneratedRangeIndicator();
                return rangeIndicator;
            }

            rangeIndicator = Instantiate(rangeIndicatorPrefab, transform);
            rangeIndicator.Hide();
            return rangeIndicator;
        }

        private void HideRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                if (debugLog)
                {
                    Debug.Log("Hide selected tower range indicator.", this);
                }

                rangeIndicator.Hide();
            }
        }

        private RangeIndicator CreateGeneratedRangeIndicator()
        {
            GameObject indicatorObject = new GameObject("SelectedTowerRangeIndicator");
            indicatorObject.transform.SetParent(transform);

            SpriteRenderer renderer = indicatorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetGeneratedCircleSprite();
            renderer.sortingOrder = 100;

            RangeIndicator indicator = indicatorObject.AddComponent<RangeIndicator>();
            indicator.SetSpriteRenderer(renderer);
            indicator.Hide();
            return indicator;
        }

        private void SubscribeSelectedTower()
        {
            if (selectedTower != null)
            {
                selectedTower.OnTowerStatsChanged.RemoveListener(HandleSelectedTowerStatsChanged);
                selectedTower.OnTowerStatsChanged.AddListener(HandleSelectedTowerStatsChanged);
            }
        }

        private void UnsubscribeSelectedTower()
        {
            if (selectedTower != null)
            {
                selectedTower.OnTowerStatsChanged.RemoveListener(HandleSelectedTowerStatsChanged);
            }
        }

        private void HandleSelectedTowerStatsChanged(TowerBehaviour tower)
        {
            if (tower == selectedTower)
            {
                RefreshRangeIndicator();
                RefreshSelectedTowerUi();
                towerSelectionChanged?.Invoke();
            }
        }

        private void ShowSelectedTowerUi()
        {
            if (towerUpgradeUI != null)
            {
                towerUpgradeUI.ShowTower(selectedTower, selectedTile);
            }
        }

        private void RefreshSelectedTowerUi()
        {
            if (towerUpgradeUI != null)
            {
                towerUpgradeUI.Refresh();
            }
        }

        private void HideSelectedTowerUi()
        {
            if (towerUpgradeUI != null)
            {
                towerUpgradeUI.Hide();
            }
        }

        private void SubscribeTowerUpgradeUi()
        {
            if (towerUpgradeUI != null)
            {
                towerUpgradeUI.OnTowerSold.RemoveListener(HandleSelectedTowerSold);
                towerUpgradeUI.OnTowerSold.AddListener(HandleSelectedTowerSold);
            }
        }

        private void UnsubscribeTowerUpgradeUi()
        {
            if (towerUpgradeUI != null)
            {
                towerUpgradeUI.OnTowerSold.RemoveListener(HandleSelectedTowerSold);
            }
        }

        private void HandleSelectedTowerSold()
        {
            UnsubscribeSelectedTower();
            selectedTower = null;
            selectedTile = null;
            HideRangeIndicator();
            HideSelectedTowerUi();
            onTowerDeselected?.Invoke();
        }

        private float GetCameraWorldDistance()
        {
            return worldCamera != null && !worldCamera.orthographic
                ? worldCamera.nearClipPlane
                : Mathf.Abs(worldCamera != null ? worldCamera.transform.position.z : 0f);
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
