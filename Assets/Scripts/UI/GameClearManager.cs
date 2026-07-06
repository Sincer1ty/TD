using TD.Enemy;
using TD.Gameplay;
using TD.Placement;
using TD.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TD.UI
{
    public class GameClearManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameClearPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Scene")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string openingSceneName = "OpeningScene";

        [Header("References")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private TowerPlacementController towerPlacementController;
        [SerializeField] private TowerSelectionController towerSelectionController;

        [Header("Options")]
        [SerializeField] private bool pauseOnGameClear = true;
        [SerializeField] private bool removeAliveEnemiesOnGameClear;
        [SerializeField] private bool debugLog;

        private bool isGameClear;

        public bool IsGameClear()
        {
            return isGameClear;
        }

        private void Awake()
        {
            CacheReferences();
            RegisterButtonEvents();
            HideGameClear();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeWaveManager();
        }

        private void OnDisable()
        {
            UnsubscribeWaveManager();
            UnregisterButtonEvents();
        }

        public void ShowGameClear()
        {
            if (isGameClear)
            {
                return;
            }

            CacheReferences();

            if (lifeManager != null && lifeManager.IsDead())
            {
                Log("Ignored GameClear because player life is 0.");
                return;
            }

            if (gameStateManager != null && !gameStateManager.SetGameClear())
            {
                Log("Ignored GameClear because GameStateManager rejected the state change.");
                return;
            }

            isGameClear = true;
            Log("GameClear state entered.");

            StopGameProgress();
            HideGameOver();

            if (gameClearPanel != null)
            {
                gameClearPanel.SetActive(true);
                Log("GameClearPanel shown.");
            }
            else
            {
                Debug.LogWarning("GameClearManager has no gameClearPanel assigned.", this);
            }

            if (pauseOnGameClear)
            {
                Time.timeScale = 0f;
            }
        }

        public void HideGameClear()
        {
            if (gameClearPanel != null)
            {
                gameClearPanel.SetActive(false);
            }
        }

        public void RestartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogWarning("GameClearManager cannot restart because gameSceneName is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }

        public void GoToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(openingSceneName))
            {
                Debug.LogWarning("GameClearManager cannot load main menu because openingSceneName is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(openingSceneName);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StopGameProgress()
        {
            if (enemySpawner != null)
            {
                enemySpawner.StopSpawning(removeAliveEnemiesOnGameClear);
            }

            if (waveManager != null)
            {
                waveManager.HandleGameClear();
            }

            if (towerPlacementController != null)
            {
                towerPlacementController.SetPlacementInputEnabled(false);
            }

            if (towerSelectionController != null)
            {
                towerSelectionController.SetSelectionEnabled(false);
            }
        }

        private void HideGameOver()
        {
            if (gameOverUI != null)
            {
                gameOverUI.Hide();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void CacheReferences()
        {
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
                if (waveManager == null)
                {
                    Debug.LogWarning("GameClearManager has no WaveManager reference.", this);
                }
            }

            if (lifeManager == null)
            {
                lifeManager = FindFirstObjectByType<LifeManager>();
            }

            if (gameStateManager == null)
            {
                gameStateManager = FindFirstObjectByType<GameStateManager>();
            }

            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (towerPlacementController == null)
            {
                towerPlacementController = FindFirstObjectByType<TowerPlacementController>();
            }

            if (towerSelectionController == null)
            {
                towerSelectionController = FindFirstObjectByType<TowerSelectionController>();
            }

            if (gameOverUI == null)
            {
                gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            }
        }

        private void SubscribeWaveManager()
        {
            CacheReferences();

            if (waveManager != null)
            {
                waveManager.OnAllWavesCleared.RemoveListener(HandleAllWavesCleared);
                waveManager.OnAllWavesCleared.AddListener(HandleAllWavesCleared);
            }
        }

        private void UnsubscribeWaveManager()
        {
            if (waveManager != null)
            {
                waveManager.OnAllWavesCleared.RemoveListener(HandleAllWavesCleared);
            }
        }

        private void RegisterButtonEvents()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
                restartButton.onClick.AddListener(RestartGame);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(GoToMainMenu);
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void UnregisterButtonEvents()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        private void HandleAllWavesCleared()
        {
            Log("Last wave clear detected.");
            ShowGameClear();
        }

        private void Log(string message)
        {
            if (debugLog)
            {
                Debug.Log(message, this);
            }
        }
    }
}
