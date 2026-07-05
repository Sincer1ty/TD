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
        }

        private void OnDisable()
        {
            DeselectTower();
        }

        private void Update()
        {
            if (!allowSelection || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (placementController != null && placementController.IsPlacementModeActive)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            PlacementTile clickedTile = TryGetPlacementTileUnderMouse();
            TowerBehaviour clickedTower = clickedTile != null ? clickedTile.GetCurrentTower() : null;
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
            selectedTower = null;
            selectedTile = null;
            HideRangeIndicator();
            HideSelectedTowerUi();
            onTowerDeselected?.Invoke();
        }

        public void RefreshRangeIndicator()
        {
            if (selectedTower == null || selectedTower.CurrentAttackRange <= 0f)
            {
                HideRangeIndicator();
                return;
            }

            RangeIndicator indicator = EnsureRangeIndicator();
            if (indicator == null)
            {
                return;
            }

            indicator.SetValidState(true);
            Vector3 indicatorPosition = selectedTile != null
                ? selectedTile.GetBuildPosition()
                : selectedTower.transform.position;
            indicator.Show(indicatorPosition, selectedTower.CurrentAttackRange);
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
                return null;
            }

            rangeIndicator = Instantiate(rangeIndicatorPrefab, transform);
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
                towerUpgradeUI.ShowTower(selectedTower);
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

        private float GetCameraWorldDistance()
        {
            return worldCamera != null && !worldCamera.orthographic
                ? worldCamera.nearClipPlane
                : Mathf.Abs(worldCamera != null ? worldCamera.transform.position.z : 0f);
        }
    }
}
