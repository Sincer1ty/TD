using TD.Gameplay;
using TD.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TD.UI
{
    public class GameClearManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string endingSceneName = "EndingScene";
        [SerializeField] private bool resetTimeScaleOnLoad = true;

        [Header("References")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private GameStateManager gameStateManager;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private bool isLoadingEndingScene;
        private bool isGameCleared;
        private GameObject persistedRoot;

        public bool IsGameClear()
        {
            return isGameCleared;
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            SubscribeWaveManager();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnsubscribeWaveManager();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void LoadEndingScene()
        {
            if (!CanLoadEndingScene())
            {
                return;
            }

            isLoadingEndingScene = true;
            isGameCleared = true;

            if (gameStateManager != null && !gameStateManager.SetGameClear())
            {
                Log("EndingScene load canceled because GameStateManager rejected GameClear.");
                isLoadingEndingScene = false;
                isGameCleared = false;
                return;
            }

            if (resetTimeScaleOnLoad)
            {
                Time.timeScale = 1f;
            }

            Log($"Loading EndingScene: {endingSceneName}");
            PersistThroughSceneLoad();

            try
            {
                SceneManager.LoadScene(endingSceneName);
            }
            catch (System.Exception exception)
            {
                isLoadingEndingScene = false;
                isGameCleared = false;
                CleanupPersistedRoot();
                Debug.LogWarning($"Failed to load EndingScene '{endingSceneName}': {exception.Message}", this);
            }
        }

        public bool CanLoadEndingScene()
        {
            CacheReferences();

            if (isLoadingEndingScene)
            {
                Log("EndingScene load canceled because loading is already in progress.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(endingSceneName))
            {
                Debug.LogWarning("GameClearManager cannot load EndingScene because endingSceneName is empty.", this);
                return false;
            }

            if (lifeManager != null && lifeManager.IsDead())
            {
                Log("EndingScene load canceled because player life is 0.");
                return false;
            }

            if (gameStateManager != null && gameStateManager.IsGameOver())
            {
                Log("EndingScene load canceled because game state is GameOver.");
                return false;
            }

            if (waveManager != null && waveManager.BossReachedBase)
            {
                Log("EndingScene load canceled because boss reached the base.");
                return false;
            }

            return true;
        }

        private void HandleAllWavesCleared()
        {
            Log("Last wave clear detected.");
            LoadEndingScene();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != endingSceneName)
            {
                return;
            }

            Log($"EndingScene load completed: {scene.name}");

            CleanupPersistedRoot();
        }

        private void PersistThroughSceneLoad()
        {
            persistedRoot = transform.root != null ? transform.root.gameObject : gameObject;
            DontDestroyOnLoad(persistedRoot);
        }

        private void CleanupPersistedRoot()
        {
            if (persistedRoot != null)
            {
                Destroy(persistedRoot);
                persistedRoot = null;
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

        private void Log(string message)
        {
            if (debugLog)
            {
                Debug.Log(message, this);
            }
        }
    }
}
