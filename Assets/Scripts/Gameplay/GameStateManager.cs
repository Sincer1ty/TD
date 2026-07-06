using UnityEngine;
using UnityEngine.Events;

namespace TD.Gameplay
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GameState currentState = GameState.Playing;
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private UnityEvent<GameState> onStateChanged = new UnityEvent<GameState>();
        [SerializeField] private UnityEvent onGameOver = new UnityEvent();
        [SerializeField] private UnityEvent onGameClear = new UnityEvent();
        [SerializeField] private bool debugLog;

        public GameState CurrentState => currentState;
        public UnityEvent<GameState> OnStateChanged => onStateChanged;
        public UnityEvent OnGameOver => onGameOver;
        public UnityEvent OnGameClear => onGameClear;

        private void Awake()
        {
            if (lifeManager == null)
            {
                lifeManager = FindFirstObjectByType<LifeManager>();
            }
        }

        private void OnEnable()
        {
            SubscribeLifeManager();
        }

        private void OnDisable()
        {
            UnsubscribeLifeManager();
        }

        public bool IsPlaying()
        {
            return currentState == GameState.Playing;
        }

        public bool IsGameOver()
        {
            return currentState == GameState.GameOver;
        }

        public bool IsGameClear()
        {
            return currentState == GameState.GameClear;
        }

        public bool SetGameOver()
        {
            if (currentState == GameState.GameOver)
            {
                return true;
            }

            if (currentState == GameState.GameClear)
            {
                Log("Ignored GameOver because game is already clear.");
                return false;
            }

            SetState(GameState.GameOver);
            onGameOver?.Invoke();
            return true;
        }

        public bool SetGameClear()
        {
            if (currentState == GameState.GameClear)
            {
                return true;
            }

            if (currentState == GameState.GameOver)
            {
                Log("Ignored GameClear because game is already over.");
                return false;
            }

            SetState(GameState.GameClear);
            onGameClear?.Invoke();
            return true;
        }

        public void ResetToPlaying()
        {
            SetState(GameState.Playing);
        }

        private void SetState(GameState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            Log($"GameState changed to {currentState}.");
            onStateChanged?.Invoke(currentState);
        }

        private void Log(string message)
        {
            if (debugLog)
            {
                Debug.Log(message, this);
            }
        }

        private void SubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleLifeGameOver);
                lifeManager.OnGameOver.AddListener(HandleLifeGameOver);
            }
        }

        private void UnsubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleLifeGameOver);
            }
        }

        private void HandleLifeGameOver()
        {
            SetGameOver();
        }
    }
}
