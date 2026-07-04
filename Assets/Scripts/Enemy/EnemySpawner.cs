using System.Collections;
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
        [SerializeField] private UnityEvent<EnemyController> enemySpawned;
        [SerializeField] private UnityEvent<EnemyController, int> enemyKilled;
        [SerializeField] private UnityEvent<EnemyController, int> enemyReachedBase;
        [SerializeField] private UnityEvent<EnemyController> bossSpawned;
        [SerializeField] private UnityEvent<EnemyController> bossDead;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnEnemy();
            }
        }

        public EnemyController SpawnEnemy()
        {
            return SpawnEnemy(defaultEnemyData);
        }

        public EnemyController SpawnEnemy(EnemyData data)
        {
            if (data == null || data.Prefab == null || mapRoot == null || mapRoot.WaypointPath == null)
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
            if (waveEntries == null || index < 0 || index >= waveEntries.Length)
            {
                return;
            }

            StartCoroutine(SpawnWaveEntryRoutine(waveEntries[index]));
        }

        private IEnumerator SpawnWaveEntryRoutine(EnemyWaveEntry entry)
        {
            if (entry == null)
            {
                yield break;
            }

            for (int i = 0; i < entry.Count; i++)
            {
                SpawnEnemy(entry.EnemyData);

                if (entry.SpawnInterval > 0f && i + 1 < entry.Count)
                {
                    yield return new WaitForSeconds(entry.SpawnInterval);
                }
            }
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
            Debug.Log($"Enemy killed: {enemy.Data?.EnemyName ?? enemy.name}, reward {enemy.RewardGold} gold");
        }

        private void HandleEnemyReachedBase(EnemyController enemy, int damage)
        {
            enemyReachedBase?.Invoke(enemy, damage);
        }

        private void HandleBossSpawned(EnemyController enemy)
        {
            bossSpawned?.Invoke(enemy);
        }

        private void HandleBossDead(EnemyController enemy)
        {
            bossDead?.Invoke(enemy);
        }
    }
}
