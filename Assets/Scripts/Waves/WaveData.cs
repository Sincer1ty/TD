using TD.Enemy;
using UnityEngine;

namespace TD.Waves
{
    [CreateAssetMenu(fileName = "WaveData", menuName = "TD/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [System.Serializable]
        public class EnemyGroup
        {
            [SerializeField] private EnemyData enemyData;
            [SerializeField] private int count = 1;
            [SerializeField] private float spawnIntervalOverride = -1f;
            [SerializeField] private float delayBeforeGroup;

            public EnemyData EnemyData => enemyData;
            public int Count => Mathf.Max(0, count);
            public float SpawnIntervalOverride => spawnIntervalOverride;
            public float DelayBeforeGroup => Mathf.Max(0f, delayBeforeGroup);
        }

        [SerializeField] private int waveIndex = 1;
        [SerializeField] private EnemyGroup[] enemyGroups;
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private int waveClearReward = 25;
        [SerializeField] private float healthMultiplier = 1f;

        public int WaveIndex => Mathf.Max(1, waveIndex);
        public EnemyGroup[] EnemyGroups => enemyGroups;
        public float SpawnInterval => Mathf.Max(0f, spawnInterval);
        public int WaveClearReward => Mathf.Max(0, waveClearReward);
        public float HealthMultiplier => Mathf.Max(0.01f, healthMultiplier);

        private void OnValidate()
        {
            waveIndex = Mathf.Max(1, waveIndex);
            spawnInterval = Mathf.Max(0f, spawnInterval);
            waveClearReward = Mathf.Max(0, waveClearReward);
            healthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        }
    }
}
