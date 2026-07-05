using TD.Economy;
using TD.Placement;
using TD.Tower;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TD.UI
{
    public class TowerButtonUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TowerData towerData;

        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image buttonBackground;
        [SerializeField] private GameObject selectedIndicator;

        [Header("References")]
        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private GoldManager goldManager;

        [Header("Display")]
        [SerializeField] private string priceFormat = "{0} Gold";
        [SerializeField] private Color affordablePriceColor = Color.white;
        [SerializeField] private Color unaffordablePriceColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color normalButtonColor = Color.white;
        [SerializeField] private Color selectedButtonColor = new Color(0.65f, 0.9f, 1f, 1f);
        [SerializeField] private bool disableWhenUnaffordable;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private bool subscribedToGold;
        private bool subscribedToPlacement;
        private bool subscribedToButton;
        private bool loggedMissingPriceText;
        private bool loggedMissingPlacementController;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeButton();
            SubscribeGoldManager();
            SubscribePlacementController();
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeButton();
            UnsubscribeGoldManager();
            UnsubscribePlacementController();
        }

        private void OnValidate()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (buttonBackground == null)
            {
                buttonBackground = GetComponent<Image>();
            }
        }

        public void SetTowerData(TowerData data)
        {
            towerData = data;
            Refresh();
        }

        public void Refresh()
        {
            bool hasData = towerData != null;
            int cost = hasData ? towerData.Cost : 0;
            bool canAfford = !hasData || goldManager == null || goldManager.CanSpend(cost);
            bool isSelected = placementController != null && placementController.SelectedTower == towerData;

            if (button != null)
            {
                button.interactable = hasData && (!disableWhenUnaffordable || canAfford);
            }

            if (priceText != null)
            {
                priceText.text = hasData ? string.Format(priceFormat, cost) : string.Empty;
                priceText.color = canAfford ? affordablePriceColor : unaffordablePriceColor;
            }
            else
            {
                LogMissingPriceText();
            }

            if (nameText != null)
            {
                nameText.text = hasData ? towerData.TowerName : string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = hasData ? towerData.Icon : null;
                iconImage.enabled = hasData && towerData.Icon != null;
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }

            if (buttonBackground != null)
            {
                buttonBackground.color = isSelected ? selectedButtonColor : normalButtonColor;
            }
        }

        public void SelectTower()
        {
            if (towerData == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"TowerButtonUI '{name}' cannot select because towerData is null.", this);
                }

                Refresh();
                return;
            }

            if (placementController == null)
            {
                LogMissingPlacementController();
                return;
            }

            placementController.SelectTower(towerData);
            Refresh();
        }

        private void CacheReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (buttonBackground == null)
            {
                buttonBackground = GetComponent<Image>();
            }

            if (placementController == null)
            {
                placementController = FindFirstObjectByType<TowerPlacementController>();
                if (placementController == null)
                {
                    LogMissingPlacementController();
                }
            }

            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }
        }

        private void SubscribeButton()
        {
            if (button == null || subscribedToButton)
            {
                return;
            }

            button.onClick.RemoveListener(SelectTower);
            button.onClick.AddListener(SelectTower);
            subscribedToButton = true;
        }

        private void UnsubscribeButton()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(SelectTower);
            }

            subscribedToButton = false;
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
            if (goldManager != null)
            {
                goldManager.OnGoldChanged.RemoveListener(HandleGoldChanged);
            }

            subscribedToGold = false;
        }

        private void SubscribePlacementController()
        {
            if (placementController == null || subscribedToPlacement)
            {
                return;
            }

            placementController.OnTowerSelected.RemoveListener(HandleTowerSelectionChanged);
            placementController.OnTowerSelected.AddListener(HandleTowerSelectionChanged);
            subscribedToPlacement = true;
        }

        private void UnsubscribePlacementController()
        {
            if (placementController != null)
            {
                placementController.OnTowerSelected.RemoveListener(HandleTowerSelectionChanged);
            }

            subscribedToPlacement = false;
        }

        private void HandleGoldChanged(int currentGold)
        {
            Refresh();
        }

        private void HandleTowerSelectionChanged(TowerData selectedTower)
        {
            Refresh();
        }

        private void LogMissingPriceText()
        {
            if (loggedMissingPriceText)
            {
                return;
            }

            loggedMissingPriceText = true;
            Debug.LogWarning($"TowerButtonUI '{name}' has no priceText assigned.", this);
        }

        private void LogMissingPlacementController()
        {
            if (loggedMissingPlacementController)
            {
                return;
            }

            loggedMissingPlacementController = true;
            Debug.LogWarning($"TowerButtonUI '{name}' has no TowerPlacementController reference.", this);
        }
    }
}
