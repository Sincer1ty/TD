using TD.Tower;
using TMPro;
using UnityEngine;
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
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private string maxLevelText = "MAX";

        private TowerBehaviour selectedTower;

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

            Hide();
        }

        private void OnDisable()
        {
            UnsubscribeSelectedTower();
        }

        public void ShowTower(TowerBehaviour tower)
        {
            UnsubscribeSelectedTower();
            selectedTower = tower;
            SubscribeSelectedTower();
            Refresh();

            if (panelRoot != null)
            {
                panelRoot.SetActive(selectedTower != null);
            }
        }

        public void Hide()
        {
            UnsubscribeSelectedTower();
            selectedTower = null;

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

            selectedTower.TryUpgrade();
            Refresh();
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
            SetText(upgradeCostText, hasNextLevel ? $"Upgrade: {cost} Gold" : $"Upgrade: {maxLevelText}");

            if (upgradeButton != null)
            {
                upgradeButton.interactable = hasNextLevel;
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

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
