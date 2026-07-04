using TD.Gameplay;
using TMPro;
using UnityEngine;

namespace TD.UI
{
    public class LifeUI : MonoBehaviour
    {
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private TextMeshProUGUI lifeText;
        [SerializeField] private string format = "Life: {0}";

        private void Awake()
        {
            if (lifeText == null)
            {
                lifeText = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            if (lifeManager != null)
            {
                lifeManager.OnLifeChanged.AddListener(UpdateLifeText);
                UpdateLifeText(lifeManager.GetCurrentLife());
            }
        }

        private void OnDisable()
        {
            if (lifeManager != null)
            {
                lifeManager.OnLifeChanged.RemoveListener(UpdateLifeText);
            }
        }

        public void SetLifeManager(LifeManager manager)
        {
            if (lifeManager != null)
            {
                lifeManager.OnLifeChanged.RemoveListener(UpdateLifeText);
            }

            lifeManager = manager;

            if (isActiveAndEnabled && lifeManager != null)
            {
                lifeManager.OnLifeChanged.AddListener(UpdateLifeText);
                UpdateLifeText(lifeManager.GetCurrentLife());
            }
        }

        private void UpdateLifeText(int life)
        {
            if (lifeText != null)
            {
                lifeText.text = string.Format(format, life);
            }
        }
    }
}
