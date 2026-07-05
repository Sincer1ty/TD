using TD.Placement;
using TD.Tower;
using TD.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TowerBehaviour = TD.Tower.Tower;

namespace TD.UI
{
    public class TowerUpgradeUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI towerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;
        [SerializeField] private GameObject upgradeCostGroup;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private string maxLevelText = "MAX";
        [SerializeField] private string upgradeButtonLabel = "Upgrade";
        [Header("World Anchored Buttons")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private RectTransform upgradeButtonRect;
        [SerializeField] private RectTransform sellButtonRect;
        [SerializeField] private Vector2 upgradeButtonOffset = new Vector2(64f, 0f);
        [SerializeField] private Vector2 sellButtonOffset = new Vector2(-64f, 0f);
        [SerializeField] private bool clampToScreen = true;
        [SerializeField] private Vector2 screenPadding = new Vector2(24f, 24f);
        [SerializeField] private UnityEvent onTowerSold = new UnityEvent();
        [SerializeField] private bool debugLog;

        private TowerBehaviour selectedTower;
        private PlacementTile selectedTile;
        private bool subscribedToGold;

        public UnityEvent OnTowerSold => onTowerSold;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(UpgradeSelectedTower);
            }

            if (sellButton != null)
            {
                sellButton.onClick.AddListener(SellSelectedTower);
            }

            CacheReferences();
            Hide();
        }

        private void OnEnable()
        {
            SubscribeGoldManager();
        }

        private void LateUpdate()
        {
            if (selectedTower != null)
            {
                UpdateButtonPositions();
            }
        }

        private void OnDisable()
        {
            UnsubscribeGoldManager();
            UnsubscribeSelectedTower();
        }

        public void ShowTower(TowerBehaviour tower)
        {
            ShowTower(tower, tower != null ? tower.PlacedTile : null);
        }

        public void ShowTower(TowerBehaviour tower, PlacementTile tile)
        {
            UnsubscribeSelectedTower();
            selectedTower = tower;
            selectedTile = tile != null ? tile : tower != null ? tower.PlacedTile : null;
            SubscribeSelectedTower();
            Refresh();
            UpdateButtonPositions();

            if (panelRoot != null)
            {
                panelRoot.SetActive(selectedTower != null);
            }
        }

        public void Hide()
        {
            UnsubscribeSelectedTower();
            selectedTower = null;
            selectedTile = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void UpgradeSelectedTower()
        {
            if (selectedTower == null)
            {
                return;
            }

            bool upgraded = selectedTower.TryUpgrade();
            if (debugLog)
            {
                Debug.Log(upgraded
                    ? $"Tower '{selectedTower.name}' upgrade button succeeded."
                    : $"Tower '{selectedTower.name}' upgrade button failed.");
            }

            Refresh();
        }

        public void SellSelectedTower()
        {
            if (selectedTower == null)
            {
                return;
            }

            TowerBehaviour towerToSell = selectedTower;
            string towerName = towerToSell != null ? towerToSell.name : "Tower";
            PlacementTile tile = selectedTile != null ? selectedTile : towerToSell.PlacedTile;
            bool sold = placementController != null && tile != null
                ? placementController.SellPlacedTower(tile)
                : towerToSell.Sell();

            if (debugLog)
            {
                Debug.Log(sold
                    ? $"Tower '{towerName}' sold."
                    : $"Tower '{towerName}' sell failed.");
            }

            if (sold)
            {
                Hide();
                onTowerSold?.Invoke();
            }
        }

        public void Refresh()
        {
            if (selectedTower == null)
            {
                return;
            }

            TowerData data = selectedTower.Data;
            SetText(towerNameText, data != null ? data.TowerName : selectedTower.name);
            SetText(levelText, $"Level: {selectedTower.CurrentLevel} / {selectedTower.MaxLevel}");
            SetText(damageText, $"Damage: {selectedTower.CurrentDamage:0.##}");
            SetText(rangeText, $"Range: {selectedTower.CurrentAttackRange:0.##}");
            SetText(attackSpeedText, $"Attack Speed: {selectedTower.CurrentAttackSpeed:0.##}");

            bool hasNextLevel = selectedTower.CurrentLevel < selectedTower.MaxLevel;
            int cost = selectedTower.GetNextUpgradeCost();
            bool hasGoldProvider = goldManager != null || cost <= 0;
            bool canUpgrade = hasGoldProvider && selectedTower.CanUpgrade();
            SetText(upgradeButtonText, hasNextLevel ? upgradeButtonLabel : maxLevelText);
            SetUpgradeCostVisible(hasNextLevel);

            if (hasNextLevel)
            {
                SetText(upgradeCostText, $"{cost} Gold");
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = hasNextLevel && canUpgrade;
            }

            if (sellButton != null)
            {
                sellButton.interactable = selectedTower != null;
            }
        }

        private void SubscribeSelectedTower()
        {
            if (selectedTower == null)
            {
                return;
            }

            selectedTower.OnTowerStatsChanged.RemoveListener(HandleTowerStatsChanged);
            selectedTower.OnTowerStatsChanged.AddListener(HandleTowerStatsChanged);
            selectedTower.OnTowerUpgraded.RemoveListener(HandleTowerStatsChanged);
            selectedTower.OnTowerUpgraded.AddListener(HandleTowerStatsChanged);
            selectedTower.OnUpgradeFailed.RemoveListener(HandleTowerStatsChanged);
            selectedTower.OnUpgradeFailed.AddListener(HandleTowerStatsChanged);
        }

        private void UnsubscribeSelectedTower()
        {
            if (selectedTower == null)
            {
                return;
            }

            selectedTower.OnTowerStatsChanged.RemoveListener(HandleTowerStatsChanged);
            selectedTower.OnTowerUpgraded.RemoveListener(HandleTowerStatsChanged);
            selectedTower.OnUpgradeFailed.RemoveListener(HandleTowerStatsChanged);
        }

        private void HandleTowerStatsChanged(TowerBehaviour tower)
        {
            if (tower == selectedTower)
            {
                Refresh();
            }
        }

        private void CacheReferences()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (placementController == null)
            {
                placementController = FindFirstObjectByType<TowerPlacementController>();
            }

            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
                if (isActiveAndEnabled)
                {
                    SubscribeGoldManager();
                }
            }

            if (upgradeButton != null && upgradeButtonRect == null)
            {
                upgradeButtonRect = upgradeButton.GetComponent<RectTransform>();
            }

            if (upgradeButtonText == null && upgradeButton != null)
            {
                upgradeButtonText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (upgradeCostGroup == null && upgradeCostText != null)
            {
                upgradeCostGroup = upgradeCostText.gameObject;
            }

            if (sellButton != null && sellButtonRect == null)
            {
                sellButtonRect = sellButton.GetComponent<RectTransform>();
            }

            if (canvas == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no Canvas reference.");
            }

            if (upgradeButton == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no upgrade Button reference.");
            }

            if (sellButton == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no sell Button reference.");
            }

            if (upgradeButtonText == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no upgrade button TextMeshProUGUI reference.");
            }

            if (goldManager == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no GoldManager reference. Upgrade button affordability cannot refresh from gold changes.");
            }

            if (placementController == null && debugLog)
            {
                Debug.LogWarning("TowerUpgradeUI has no TowerPlacementController reference. Selling can still destroy the tower, but placement bookkeeping may not be updated.");
            }
        }

        private void UpdateButtonPositions()
        {
            CacheReferences();
            if (selectedTower == null || canvas == null)
            {
                return;
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera cameraForWorld = worldCamera != null ? worldCamera : Camera.main;
            if (cameraForWorld == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("TowerUpgradeUI cannot position buttons because no camera was found.");
                }

                return;
            }

            Vector3 worldPosition = selectedTower.transform.position;
            Vector2 screenPoint = cameraForWorld.WorldToScreenPoint(worldPosition);

            if (clampToScreen)
            {
                screenPoint.x = Mathf.Clamp(screenPoint.x, screenPadding.x, Screen.width - screenPadding.x);
                screenPoint.y = Mathf.Clamp(screenPoint.y, screenPadding.y, Screen.height - screenPadding.y);
            }

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            {
                return;
            }

            SetAnchoredPosition(upgradeButtonRect, localPoint + upgradeButtonOffset, canvasRect);
            SetAnchoredPosition(sellButtonRect, localPoint + sellButtonOffset, canvasRect);
        }

        private void SetAnchoredPosition(RectTransform target, Vector2 position, RectTransform canvasRect)
        {
            if (target == null)
            {
                return;
            }

            if (clampToScreen)
            {
                Rect rect = canvasRect.rect;
                float halfWidth = target.rect.width * 0.5f;
                float halfHeight = target.rect.height * 0.5f;
                position.x = Mathf.Clamp(position.x, rect.xMin + halfWidth, rect.xMax - halfWidth);
                position.y = Mathf.Clamp(position.y, rect.yMin + halfHeight, rect.yMax - halfHeight);
            }

            target.anchoredPosition = position;
        }

        private void SubscribeGoldManager()
        {
            if (goldManager == null || subscribedToGold)
            {
                return;
            }

            goldManager.OnGoldChanged.RemoveListener(HandleGoldChanged);
            goldManager.OnGoldChanged.AddListener(HandleGoldChanged);
            subscribedToGold = true;
        }

        private void UnsubscribeGoldManager()
        {
            if (goldManager == null || !subscribedToGold)
            {
                return;
            }

            goldManager.OnGoldChanged.RemoveListener(HandleGoldChanged);
            subscribedToGold = false;
        }

        private void HandleGoldChanged(int gold)
        {
            if (selectedTower != null)
            {
                Refresh();
            }
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetUpgradeCostVisible(bool visible)
        {
            if (upgradeCostGroup != null)
            {
                upgradeCostGroup.SetActive(visible);
                return;
            }

            if (upgradeCostText != null)
            {
                upgradeCostText.gameObject.SetActive(visible);
            }
        }
    }
}
