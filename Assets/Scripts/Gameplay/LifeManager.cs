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

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onLifeChanged = new UnityEvent<int>();
        [SerializeField] private UnityEvent onGameOver = new UnityEvent();

        private bool isGameOver;
        private bool gameOverInvoked;

        public UnityEvent<int> OnLifeChanged => onLifeChanged;
        public UnityEvent OnGameOver => onGameOver;
        public int CurrentLife => currentLife;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
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
            onLifeChanged?.Invoke(currentLife);

            if (isGameOver)
            {
                TriggerGameOver();
            }
        }

        public void TakeDamage(int amount)
        {
            if (isGameOver || amount <= 0)
            {
                return;
            }

            currentLife = Mathf.Max(0, currentLife - amount);
            // Debug.Log($"Base took {amount} damage. Life: {currentLife}");
            onLifeChanged?.Invoke(currentLife);

            if (currentLife <= 0)
            {
                TriggerGameOver();
            }
        }

        public void Heal(int amount)
        {
            if (isGameOver || amount <= 0)
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

        private void TriggerGameOver()
        {
            if (gameOverInvoked)
            {
                return;
            }

            if (isGameOver && currentLife > 0)
            {
                return;
            }

            isGameOver = true;
            gameOverInvoked = true;

            Debug.Log("Game Over");
            onGameOver?.Invoke();

            if (pauseTimeOnGameOver)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
