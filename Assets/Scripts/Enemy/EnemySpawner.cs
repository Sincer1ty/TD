using TD.Map;
using UnityEngine;

namespace TD.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private MapRoot mapRoot;
        [SerializeField] private EnemyPathFollower enemyPrefab;
        [SerializeField] private float enemyMoveSpeed = 2f;
        [SerializeField] private bool spawnOnStart = true;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnEnemy();
            }
        }

        public EnemyPathFollower SpawnEnemy()
        {
            if (enemyPrefab == null || mapRoot == null || mapRoot.WaypointPath == null)
            {
                return null;
            }

            Vector3 spawnPosition = transform.position;
            mapRoot.TryGetSpawnPosition(out spawnPosition);

            EnemyPathFollower enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemy.Initialize(mapRoot.WaypointPath, enemyMoveSpeed, false);
            return enemy;
        }
    }
}
