using TD.Gameplay;
using TMPro;
using UnityEngine;

namespace TD.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private string gameOverMessage = "Game Over";

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (canvasGroup == null && root == gameObject)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            Hide();
        }

        private void OnEnable()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.AddListener(Show);
            }
        }

        private void OnDisable()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(Show);
            }
        }

        public void SetLifeManager(LifeManager manager)
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(Show);
            }

            lifeManager = manager;

            if (isActiveAndEnabled && lifeManager != null)
            {
                lifeManager.OnGameOver.AddListener(Show);
            }
        }

        public void Show()
        {
            if (messageText != null)
            {
                messageText.text = gameOverMessage;
            }

            if (root != null)
            {
                SetVisible(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            if (root != null)
            {
                root.SetActive(visible);
            }
        }
    }
}
