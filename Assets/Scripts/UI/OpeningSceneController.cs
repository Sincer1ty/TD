using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TD.UI
{
    public class OpeningSceneController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private bool isLoading;
        private GameObject persistedRoot;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            RegisterButtonEvents();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnregisterButtonEvents();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void StartGame()
        {
            if (isLoading)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogWarning("OpeningSceneController cannot start game because gameSceneName is empty.", this);
                return;
            }

            isLoading = true;
            SetStartButtonInteractable(false);
            PersistThroughSceneLoad();

            if (debugLog)
            {
                Debug.Log($"Loading scene '{gameSceneName}'.", this);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void ExitGame()
        {
            if (debugLog)
            {
                Debug.Log("ExitGame requested.", this);
            }

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RegisterButtonEvents()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
                startButton.onClick.AddListener(StartGame);
            }
            else
            {
                Debug.LogWarning("OpeningSceneController has no startButton assigned.", this);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ExitGame);
                exitButton.onClick.AddListener(ExitGame);
            }
        }

        private void UnregisterButtonEvents()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ExitGame);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != gameSceneName)
            {
                return;
            }

            if (debugLog)
            {
                Debug.Log($"Scene load completed: '{scene.name}'.", this);
            }

            CleanupPersistedRoot();
        }

        private void ValidateReferences()
        {
            if (startButton == null)
            {
                Debug.LogWarning("OpeningSceneController has no startButton assigned.", this);
            }

            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogWarning("OpeningSceneController gameSceneName is empty.", this);
            }
        }

        private void SetStartButtonInteractable(bool interactable)
        {
            if (startButton != null)
            {
                startButton.interactable = interactable;
            }
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
    }
}
