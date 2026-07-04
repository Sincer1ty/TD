using System;
using TD.Map;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyPathFollower pathFollower;
        [SerializeField] private EnemyAbilityBase[] abilities;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private bool destroyOnGoalReached = true;

        [Header("Events")]
        [SerializeField] private UnityEvent<EnemyController> onEnemyKilled;
        [SerializeField] private UnityEvent<EnemyController, int> onRewardGold;
        [SerializeField] private UnityEvent<EnemyController, int> onBaseReached;
        [SerializeField] private UnityEvent<EnemyController> onBossSpawned;
        [SerializeField] private UnityEvent<EnemyController> onBossDead;

        private bool completed;

        public event Action<EnemyController> EnemyKilled;
        public event Action<EnemyController, int> RewardGoldGranted;
        public event Action<EnemyController, int> BaseReached;
        public event Action<EnemyController> BossSpawned;
        public event Action<EnemyController> BossDead;

        public EnemyData Data => data;
        public EnemyHealth Health => health;
        public EnemyPathFollower PathFollower => pathFollower;
        public bool IsBoss => data != null && data.IsBoss;
        public int RewardGold => data != null ? data.RewardGold : 0;
        public int DamageToBase => data != null ? data.DamageToBase : 0;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Initialize(EnemyData enemyData, WaypointPath waypointPath)
        {
            CacheComponents();
            data = enemyData;
            completed = false;

            if (health != null)
            {
                health.Initialize(data);
            }

            if (pathFollower != null)
            {
                pathFollower.Initialize(
                    waypointPath,
                    data != null ? data.MoveSpeed : pathFollower.MoveSpeed,
                    data != null ? data.DamageToBase : 0,
                    false);
            }

            InitializeAbilities();

            if (IsBoss)
            {
                BossSpawned?.Invoke(this);
                onBossSpawned?.Invoke(this);
            }
        }

        private void CacheComponents()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (health == null)
            {
                health = gameObject.AddComponent<EnemyHealth>();
            }

            if (pathFollower == null)
            {
                pathFollower = GetComponent<EnemyPathFollower>();
            }

            if (pathFollower == null)
            {
                pathFollower = gameObject.AddComponent<EnemyPathFollower>();
            }

            abilities = GetComponents<EnemyAbilityBase>();
        }

        private void InitializeAbilities()
        {
            if (abilities == null)
            {
                return;
            }

            foreach (EnemyAbilityBase ability in abilities)
            {
                if (ability != null)
                {
                    ability.Initialize(this);
                }
            }
        }

        private void Subscribe()
        {
            CacheComponents();

            if (health != null)
            {
                health.Death -= HandleDeath;
                health.Death += HandleDeath;
            }

            if (pathFollower != null)
            {
                pathFollower.BaseReached -= HandleBaseReached;
                pathFollower.BaseReached += HandleBaseReached;
            }
        }

        private void Unsubscribe()
        {
            if (health != null)
            {
                health.Death -= HandleDeath;
            }

            if (pathFollower != null)
            {
                pathFollower.BaseReached -= HandleBaseReached;
            }
        }

        private void HandleDeath(EnemyHealth enemyHealth)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            EnemyKilled?.Invoke(this);
            onEnemyKilled?.Invoke(this);
            RewardGoldGranted?.Invoke(this, RewardGold);
            onRewardGold?.Invoke(this, RewardGold);

            if (IsBoss)
            {
                BossDead?.Invoke(this);
                onBossDead?.Invoke(this);
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        private void HandleBaseReached(EnemyPathFollower follower, int damage)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            BaseReached?.Invoke(this, damage);
            onBaseReached?.Invoke(this, damage);

            if (destroyOnGoalReached)
            {
                Destroy(gameObject);
            }
        }
    }
}
