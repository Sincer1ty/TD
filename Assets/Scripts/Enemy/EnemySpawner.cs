using System.Collections;
using System.Collections.Generic;
using TD.Economy;
using TD.Gameplay;
using TD.Map;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public class EnemyWaveEntry
        {
            [SerializeField] private EnemyData enemyData;
            [SerializeField] private int count = 1;
            [SerializeField] private float spawnInterval = 0.5f;

            public EnemyData EnemyData => enemyData;
            public int Count => Mathf.Max(0, count);
            public float SpawnInterval => Mathf.Max(0f, spawnInterval);
        }

        [SerializeField] private MapRoot mapRoot;
        [SerializeField] private EnemyData defaultEnemyData;
        [SerializeField] private EnemyData[] enemyDataList;
        [SerializeField] private EnemyWaveEntry[] waveEntries;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private int waveClearReward = 25;
        [SerializeField] private GoldManager goldManager;

        [Header("Life / Game Over")]
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private bool stopSpawningOnGameOver = true;
        [SerializeField] private bool removeAliveEnemiesOnGameOver;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        [SerializeField] private UnityEvent<EnemyController> enemySpawned;
        [SerializeField] private UnityEvent<EnemyController, int> enemyKilled;
        [SerializeField] private UnityEvent<EnemyController, int> enemyReachedBase;
        [SerializeField] private UnityEvent<int> waveCleared;
        [SerializeField] private UnityEvent<EnemyController> bossSpawned;
        [SerializeField] private UnityEvent<EnemyController> bossDead;

        private readonly HashSet<EnemyController> aliveEnemies = new HashSet<EnemyController>();
        private bool isSpawningWave;
        private bool waveRewardGranted = true;
        private bool spawningEnabled = true;
        private bool isGameOver;

        private void OnEnable()
        {
            SubscribeLifeManager();
        }

        private void OnDisable()
        {
            UnsubscribeLifeManager();
        }

        private void Start()
        {
            if (spawnOnStart && spawningEnabled)
            {
                SpawnWaveEntry(0);
            }
        }

        public EnemyController SpawnEnemy()
        {
            return SpawnEnemy(defaultEnemyData);
        }

        public void SetSpawnOnStart(bool enabled)
        {
            spawnOnStart = enabled;
        }

        public void SetSpawningEnabled(bool enabled)
        {
            spawningEnabled = enabled;

            if (!spawningEnabled)
            {
                StopSpawning(removeAliveEnemiesOnGameOver);
            }
        }

        public EnemyController SpawnEnemy(EnemyData data)
        {
            if (!spawningEnabled || isGameOver || data == null || data.Prefab == null || mapRoot == null || mapRoot.WaypointPath == null)
            {
                return null;
            }

            Vector3 spawnPosition = mapRoot.TryGetSpawnPosition(out Vector3 mapSpawnPosition)
                ? mapSpawnPosition
                : transform.position;

            GameObject enemyObject = Instantiate(data.Prefab, spawnPosition, Quaternion.identity);
            EnemyController enemy = enemyObject.GetComponent<EnemyController>();
            if (enemy == null)
            {
                enemy = enemyObject.AddComponent<EnemyController>();
            }

            WireEnemyEvents(enemy);
            aliveEnemies.Add(enemy);
            enemy.Initialize(data, mapRoot.WaypointPath);
            enemySpawned?.Invoke(enemy);
            return enemy;
        }

        public EnemyController SpawnEnemyByIndex(int index)
        {
            if (enemyDataList == null || index < 0 || index >= enemyDataList.Length)
            {
                return null;
            }

            return SpawnEnemy(enemyDataList[index]);
        }

        public void SpawnWaveEntry(int index)
        {
            if (!spawningEnabled || isGameOver || waveEntries == null || index < 0 || index >= waveEntries.Length)
            {
                return;
            }

            waveRewardGranted = false;
            StartCoroutine(SpawnWaveEntryRoutine(waveEntries[index]));
        }

        private IEnumerator SpawnWaveEntryRoutine(EnemyWaveEntry entry)
        {
            if (entry == null)
            {
                yield break;
            }

            isSpawningWave = true;

            for (int i = 0; i < entry.Count; i++)
            {
                if (!spawningEnabled || isGameOver)
                {
                    break;
                }

                SpawnEnemy(entry.EnemyData);

                if (entry.SpawnInterval > 0f && i + 1 < entry.Count)
                {
                    yield return new WaitForSeconds(entry.SpawnInterval);
                }
            }

            isSpawningWave = false;
            TryCompleteWave();
        }

        private void WireEnemyEvents(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.EnemyKilled += HandleEnemyKilled;
            enemy.BaseReached += HandleEnemyReachedBase;
            enemy.BossSpawned += HandleBossSpawned;
            enemy.BossDead += HandleBossDead;
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemyKilled?.Invoke(enemy, enemy.RewardGold);
            if (goldManager != null)
            {
                goldManager.AddGold(enemy.RewardGold);
            }

            Debug.Log($"Enemy killed: {enemy.Data?.EnemyName ?? enemy.name}, reward {enemy.RewardGold} gold");
            MarkEnemyRemoved(enemy);
        }

        private void HandleEnemyReachedBase(EnemyController enemy, int damage)
        {
            enemyReachedBase?.Invoke(enemy, damage);
            if (enemy != null && enemy.IsBoss)
            {
                if (debugLog)
                {
                    Debug.Log($"Boss reached the base: {enemy.Data?.EnemyName ?? enemy.name}. Forcing GameOver.");
                }

                if (lifeManager != null)
                {
                    lifeManager.ForceGameOver("Boss reached the base.");
                }
            }
            else if (lifeManager != null)
            {
                lifeManager.TakeDamage(damage);
            }

            MarkEnemyRemoved(enemy);
        }

        private void HandleBossSpawned(EnemyController enemy)
        {
            bossSpawned?.Invoke(enemy);
        }

        private void HandleBossDead(EnemyController enemy)
        {
            bossDead?.Invoke(enemy);
        }

        private void MarkEnemyRemoved(EnemyController enemy)
        {
            if (enemy != null)
            {
                enemy.EnemyKilled -= HandleEnemyKilled;
                enemy.BaseReached -= HandleEnemyReachedBase;
                enemy.BossSpawned -= HandleBossSpawned;
                enemy.BossDead -= HandleBossDead;
                aliveEnemies.Remove(enemy);
            }

            TryCompleteWave();
        }

        private void TryCompleteWave()
        {
            if (isGameOver || isSpawningWave || waveRewardGranted || aliveEnemies.Count > 0)
            {
                return;
            }

            waveRewardGranted = true;
            if (goldManager != null)
            {
                goldManager.AddGold(waveClearReward);
            }

            waveCleared?.Invoke(waveClearReward);
        }

        public void SetLifeManager(LifeManager manager)
        {
            UnsubscribeLifeManager();
            lifeManager = manager;
            SubscribeLifeManager();
        }

        public void StopSpawning(bool removeAliveEnemies)
        {
            spawningEnabled = false;
            isSpawningWave = false;
            StopAllCoroutines();

            if (removeAliveEnemies)
            {
                RemoveAliveEnemies();
                return;
            }

            StopAliveEnemies();
        }

        public void HandleGameOver()
        {
            isGameOver = true;

            if (stopSpawningOnGameOver)
            {
                StopSpawning(removeAliveEnemiesOnGameOver);
            }
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

        private void StopAliveEnemies()
        {
            foreach (EnemyController enemy in aliveEnemies)
            {
                if (enemy != null && enemy.PathFollower != null)
                {
                    enemy.PathFollower.StopFollowing();
                }
            }
        }

        private void RemoveAliveEnemies()
        {
            List<EnemyController> enemies = new List<EnemyController>(aliveEnemies);
            aliveEnemies.Clear();

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.EnemyKilled -= HandleEnemyKilled;
                enemy.BaseReached -= HandleEnemyReachedBase;
                enemy.BossSpawned -= HandleBossSpawned;
                enemy.BossDead -= HandleBossDead;
                Destroy(enemy.gameObject);
            }
        }
    }
}
