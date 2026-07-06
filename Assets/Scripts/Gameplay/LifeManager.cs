using UnityEngine;
using UnityEngine.Events;

namespace TD.Gameplay
{
    public class LifeManager : MonoBehaviour
    {
        [SerializeField] private int startingLife = 20;
        [SerializeField] private int currentLife;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool pauseTimeOnGameOver;
        [SerializeField] private GameStateManager gameStateManager;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onLifeChanged = new UnityEvent<int>();
        [SerializeField] private UnityEvent onGameOver = new UnityEvent();

        private bool isGameOver;
        private bool gameOverInvoked;
        private string lastGameOverReason;

        public UnityEvent<int> OnLifeChanged => onLifeChanged;
        public UnityEvent OnGameOver => onGameOver;
        public int CurrentLife => currentLife;
        public bool IsGameOver => isGameOver;
        public string LastGameOverReason => lastGameOverReason;

        private void Awake()
        {
            if (gameStateManager == null)
            {
                gameStateManager = FindFirstObjectByType<GameStateManager>();
            }

            if (initializeOnAwake)
            {
                InitializeLife();
            }
        }

        public void InitializeLife()
        {
            currentLife = Mathf.Max(0, startingLife);
            isGameOver = currentLife <= 0;
            gameOverInvoked = false;
            lastGameOverReason = string.Empty;
            onLifeChanged?.Invoke(currentLife);

            if (isGameOver)
            {
                TriggerGameOver();
            }
        }

        public void TakeDamage(int amount)
        {
            if (isGameOver || amount <= 0 || IsGameClear())
            {
                return;
            }

            currentLife = Mathf.Max(0, currentLife - amount);
            // Debug.Log($"Base took {amount} damage. Life: {currentLife}");
            onLifeChanged?.Invoke(currentLife);

            if (currentLife <= 0)
            {
                TriggerGameOver("Life reached 0.");
            }
        }

        public void Heal(int amount)
        {
            if (isGameOver || amount <= 0 || IsGameClear())
            {
                return;
            }

            currentLife = Mathf.Max(0, currentLife + amount);
            onLifeChanged?.Invoke(currentLife);
        }

        public int GetCurrentLife()
        {
            return currentLife;
        }

        public bool IsDead()
        {
            return isGameOver || currentLife <= 0;
        }

        public void ForceGameOver(string reason)
        {
            TriggerGameOver(reason, true);
        }

        private void TriggerGameOver(string reason = "Game Over", bool force = false)
        {
            if (gameOverInvoked)
            {
                return;
            }

            if (IsGameClear() && !force)
            {
                return;
            }

            if (!force && isGameOver && currentLife > 0)
            {
                return;
            }

            if (gameStateManager != null && !gameStateManager.SetGameOver())
            {
                return;
            }

            isGameOver = true;
            gameOverInvoked = true;
            lastGameOverReason = string.IsNullOrWhiteSpace(reason) ? "Game Over" : reason;

            Debug.Log($"Game Over: {lastGameOverReason}");
            onGameOver?.Invoke();

            if (pauseTimeOnGameOver)
            {
                Time.timeScale = 0f;
            }
        }

        private bool IsGameClear()
        {
            return gameStateManager != null && gameStateManager.IsGameClear();
        }
    }
}
