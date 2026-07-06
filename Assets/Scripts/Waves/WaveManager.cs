using System.Collections;
using TD.Economy;
using TD.Enemy;
using TD.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Waves
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave")]
        [SerializeField] private int totalWaveCount = 10;
        [SerializeField] private WaveData[] waves;
        [SerializeField] private float autoStartDelay = 2f;
        [SerializeField] private float delayBetweenWaves = 3f;
        [SerializeField] private bool autoStartFirstWave = true;
        [SerializeField] private bool autoStartNextWave = true;

        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private bool disableSpawnerAutoStart = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onWaveStarted = new UnityEvent<int>();
        [SerializeField] private UnityEvent<int> onWaveCleared = new UnityEvent<int>();
        [SerializeField] private UnityEvent onAllWavesCleared = new UnityEvent();
        [SerializeField] private UnityEvent<int> onRemainingEnemiesChanged = new UnityEvent<int>();
        [SerializeField] private UnityEvent<string> onWaveStateChanged = new UnityEvent<string>();

        private Coroutine waveRoutine;
        private int currentWaveIndex;
        private int spawnedEnemyCount;
        private int removedEnemyCount;
        private int aliveEnemyCount;
        private bool isWaveRunning;
        private bool allEnemiesSpawned;
        private bool waveRewardGranted;
        private bool allWavesCleared;
        private bool isGameOver;
        private string currentState = "Ready";

        public UnityEvent<int> OnWaveStarted => onWaveStarted;
        public UnityEvent<int> OnWaveCleared => onWaveCleared;
        public UnityEvent OnAllWavesCleared => onAllWavesCleared;
        public UnityEvent<int> OnRemainingEnemiesChanged => onRemainingEnemiesChanged;
        public UnityEvent<string> OnWaveStateChanged => onWaveStateChanged;

        public int CurrentWaveNumber => currentWaveIndex;
        public int TotalWaveCount => Mathf.Max(1, totalWaveCount);
        public int SpawnedEnemyCount => spawnedEnemyCount;
        public int RemovedEnemyCount => removedEnemyCount;
        public int AliveEnemyCount => aliveEnemyCount;
        public bool IsWaveRunning => isWaveRunning;
        public bool AllWavesCleared => allWavesCleared;
        public string CurrentState => currentState;

        private void Awake()
        {
            totalWaveCount = Mathf.Max(1, totalWaveCount);

            if (enemySpawner != null && disableSpawnerAutoStart)
            {
                enemySpawner.SetSpawnOnStart(false);
            }
        }

        private void OnEnable()
        {
            SubscribeLifeManager();
        }

        private void OnDisable()
        {
            UnsubscribeLifeManager();
            StopWaveRoutine();
        }

        private void Start()
        {
            SetState("Ready");

            if (autoStartFirstWave && CanProgressWaves())
            {
                waveRoutine = StartCoroutine(AutoStartRoutine(autoStartDelay));
            }
        }

        public void StartWave()
        {
            if (!CanProgressWaves() || allWavesCleared || isWaveRunning)
            {
                return;
            }

            int nextWave = currentWaveIndex <= 0 ? 1 : currentWaveIndex;
            StartWave(nextWave);
        }

        public void StartNextWave()
        {
            if (!CanProgressWaves() || allWavesCleared || isWaveRunning)
            {
                return;
            }

            StartWave(currentWaveIndex + 1);
        }

        public void EndWave()
        {
            if (!isWaveRunning || waveRewardGranted)
            {
                return;
            }

            WaveData wave = GetCurrentWaveData();
            int reward = wave != null ? wave.WaveClearReward : 0;

            waveRewardGranted = true;
            isWaveRunning = false;
            SetState("Cleared");

            if (goldManager != null && reward > 0)
            {
                goldManager.AddGold(reward);
            }

            Log($"Wave {currentWaveIndex} cleared. Reward: {reward}, Removed: {removedEnemyCount}/{spawnedEnemyCount}");
            onWaveCleared?.Invoke(currentWaveIndex);

            if (IsLastWave())
            {
                CompleteAllWaves();
                return;
            }

            if (autoStartNextWave && CanProgressWaves())
            {
                waveRoutine = StartCoroutine(AutoStartRoutine(delayBetweenWaves));
            }
        }

        public bool IsLastWave()
        {
            return currentWaveIndex >= TotalWaveCount;
        }

        public void HandleGameOver()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            StopWaveRoutine();
            isWaveRunning = false;
            SetState("Game Over");
            Log("Wave progress stopped by game over.");
        }

        public void HandleGameClear()
        {
            StopWaveRoutine();
            isWaveRunning = false;
            allEnemiesSpawned = true;
            allWavesCleared = true;
            SetState("Game Clear");
            Log("Wave progress stopped by game clear.");
        }

        public void SetEnemySpawner(EnemySpawner spawner)
        {
            enemySpawner = spawner;
            if (enemySpawner != null && disableSpawnerAutoStart)
            {
                enemySpawner.SetSpawnOnStart(false);
            }
        }

        public void SetGoldManager(GoldManager manager)
        {
            goldManager = manager;
        }

        public void SetLifeManager(LifeManager manager)
        {
            UnsubscribeLifeManager();
            lifeManager = manager;
            SubscribeLifeManager();
        }

        private void StartWave(int waveNumber)
        {
            if (waveNumber < 1 || waveNumber > TotalWaveCount)
            {
                return;
            }

            StopWaveRoutine();
            currentWaveIndex = waveNumber;
            waveRoutine = StartCoroutine(WaveRoutine(GetWaveData(waveNumber)));
        }

        private IEnumerator AutoStartRoutine(float delay)
        {
            SetState("Waiting");

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            waveRoutine = null;
            StartNextWave();
        }

        private IEnumerator WaveRoutine(WaveData wave)
        {
            ResetWaveCounters();
            isWaveRunning = true;
            SetState("Spawning");

            Log($"Wave {currentWaveIndex}/{TotalWaveCount} started.");
            onWaveStarted?.Invoke(currentWaveIndex);

            if (wave == null || wave.EnemyGroups == null || wave.EnemyGroups.Length == 0)
            {
                allEnemiesSpawned = true;
                SetState("Waiting Enemies");
                TryClearWave();
                yield break;
            }

            for (int groupIndex = 0; groupIndex < wave.EnemyGroups.Length; groupIndex++)
            {
                if (!CanProgressWaves())
                {
                    yield break;
                }

                WaveData.EnemyGroup group = wave.EnemyGroups[groupIndex];
                if (group == null || group.Count <= 0 || group.EnemyData == null)
                {
                    continue;
                }

                if (group.DelayBeforeGroup > 0f)
                {
                    yield return new WaitForSeconds(group.DelayBeforeGroup);
                }

                float interval = group.SpawnIntervalOverride >= 0f
                    ? group.SpawnIntervalOverride
                    : wave.SpawnInterval;

                Log($"Wave {currentWaveIndex} group {groupIndex + 1}: {group.EnemyData.EnemyName} x{group.Count}");

                for (int i = 0; i < group.Count; i++)
                {
                    if (!CanProgressWaves())
                    {
                        yield break;
                    }

                    SpawnWaveEnemy(group.EnemyData);

                    if (interval > 0f && i + 1 < group.Count)
                    {
                        yield return new WaitForSeconds(interval);
                    }
                }
            }

            allEnemiesSpawned = true;
            SetState("Waiting Enemies");
            Log($"Wave {currentWaveIndex} finished spawning. Alive: {aliveEnemyCount}");
            TryClearWave();
        }

        private void SpawnWaveEnemy(EnemyData enemyData)
        {
            if (enemySpawner == null || enemyData == null)
            {
                return;
            }

            EnemyController enemy = enemySpawner.SpawnEnemy(enemyData);
            if (enemy == null)
            {
                return;
            }

            WaveData wave = GetCurrentWaveData();
            if (enemy.Health != null && wave != null)
            {
                enemy.Health.Initialize(enemyData.MaxHp * wave.HealthMultiplier);
            }

            spawnedEnemyCount++;
            aliveEnemyCount++;
            enemy.EnemyKilled += HandleEnemyRemoved;
            enemy.BaseReached += HandleEnemyReachedBase;
            NotifyRemainingEnemiesChanged();
            Log($"Spawned {enemyData.EnemyName}. Alive: {aliveEnemyCount}");
        }

        private void HandleEnemyRemoved(EnemyController enemy)
        {
            UnwireEnemy(enemy);
            RegisterEnemyRemoved();
        }

        private void HandleEnemyReachedBase(EnemyController enemy, int damage)
        {
            UnwireEnemy(enemy);
            RegisterEnemyRemoved();
        }

        private void RegisterEnemyRemoved()
        {
            removedEnemyCount++;
            aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
            NotifyRemainingEnemiesChanged();
            Log($"Enemy removed. Alive: {aliveEnemyCount}");
            TryClearWave();
        }

        private void TryClearWave()
        {
            if (!CanProgressWaves() || !isWaveRunning || waveRewardGranted)
            {
                return;
            }

            if (!allEnemiesSpawned || spawnedEnemyCount != removedEnemyCount || aliveEnemyCount > 0)
            {
                return;
            }

            EndWave();
        }

        private void CompleteAllWaves()
        {
            if (allWavesCleared)
            {
                return;
            }

            if (lifeManager != null && lifeManager.IsDead())
            {
                Log("Skipped game clear because player life is 0.");
                return;
            }

            if (gameStateManager != null && gameStateManager.IsGameOver())
            {
                Log("Skipped game clear because game state is GameOver.");
                return;
            }

            allWavesCleared = true;
            SetState("Game Clear");
            Log("All waves cleared.");
            onAllWavesCleared?.Invoke();
        }

        private void ResetWaveCounters()
        {
            spawnedEnemyCount = 0;
            removedEnemyCount = 0;
            aliveEnemyCount = 0;
            allEnemiesSpawned = false;
            waveRewardGranted = false;
            NotifyRemainingEnemiesChanged();
        }

        private WaveData GetCurrentWaveData()
        {
            return GetWaveData(currentWaveIndex);
        }

        private WaveData GetWaveData(int waveNumber)
        {
            if (waves == null || waveNumber < 1 || waveNumber > waves.Length)
            {
                return null;
            }

            return waves[waveNumber - 1];
        }

        private void StopWaveRoutine()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }
        }

        private void UnwireEnemy(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.EnemyKilled -= HandleEnemyRemoved;
            enemy.BaseReached -= HandleEnemyReachedBase;
        }

        private void SubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleGameOver);
                lifeManager.OnGameOver.AddListener(HandleGameOver);
            }
        }

        private void UnsubscribeLifeManager()
        {
            if (lifeManager != null)
            {
                lifeManager.OnGameOver.RemoveListener(HandleGameOver);
            }
        }

        private void SetState(string state)
        {
            currentState = state;
            onWaveStateChanged?.Invoke(currentState);
        }

        private void NotifyRemainingEnemiesChanged()
        {
            onRemainingEnemiesChanged?.Invoke(aliveEnemyCount);
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log(message);
            }
        }

        private bool CanProgressWaves()
        {
            if (isGameOver)
            {
                return false;
            }

            return gameStateManager == null || gameStateManager.IsPlaying();
        }

        private void OnValidate()
        {
            totalWaveCount = Mathf.Max(1, totalWaveCount);
            autoStartDelay = Mathf.Max(0f, autoStartDelay);
            delayBetweenWaves = Mathf.Max(0f, delayBetweenWaves);
        }
    }
}
