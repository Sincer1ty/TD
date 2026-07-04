using TD.Economy;
using TMPro;
using UnityEngine;

namespace TD.UI
{
    public class GoldUI : MonoBehaviour
    {
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private string format = "Gold: {0}";

        private void Awake()
        {
            if (goldText == null)
            {
                goldText = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            if (goldManager != null)
            {
                goldManager.OnGoldChanged.AddListener(UpdateGoldText);
                UpdateGoldText(goldManager.CurrentGold);
            }
        }

        private void OnDisable()
        {
            if (goldManager != null)
            {
                goldManager.OnGoldChanged.RemoveListener(UpdateGoldText);
            }
        }

        public void SetGoldManager(GoldManager manager)
        {
            if (goldManager != null)
            {
                goldManager.OnGoldChanged.RemoveListener(UpdateGoldText);
            }

            goldManager = manager;

            if (isActiveAndEnabled && goldManager != null)
            {
                goldManager.OnGoldChanged.AddListener(UpdateGoldText);
                UpdateGoldText(goldManager.CurrentGold);
            }
        }

        private void UpdateGoldText(int gold)
        {
            if (goldText != null)
            {
                goldText.text = string.Format(format, gold);
            }
        }
    }
}
