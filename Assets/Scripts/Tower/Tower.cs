using TD.Enemy;
using UnityEngine;

namespace TD.Tower
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;
        [SerializeField] private LayerMask enemyLayer = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool attackEnabled = true;
        [SerializeField] private int upgradeLevel;

        private float attackTimer;

        public TowerData Data => data;
        public int UpgradeLevel => upgradeLevel;

        private void Reset()
        {
            firePoint = transform;
        }

        private void Update()
        {
            if (!attackEnabled || data == null)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f)
            {
                return;
            }

            EnemyHealth target = FindTarget();
            if (target == null)
            {
                return;
            }

            Attack(target);
            attackTimer = GetAttackCooldown();
        }

        public void Initialize(TowerData towerData)
        {
            data = towerData;
            attackTimer = 0f;
        }

        public bool CanUpgrade(ITowerCostProvider costProvider)
        {
            if (data == null)
            {
                return false;
            }

            return costProvider == null || costProvider.CanAfford(data.UpgradeCost);
        }

        public bool Upgrade(ITowerCostProvider costProvider)
        {
            if (data == null)
            {
                return false;
            }

            int cost = data.UpgradeCost;
            if (costProvider != null && !costProvider.CanAfford(cost))
            {
                return false;
            }

            if (costProvider != null && !costProvider.SpendCost(cost))
            {
                return false;
            }

            upgradeLevel++;
            return true;
        }

        public void SetAttackEnabled(bool enabled)
        {
            attackEnabled = enabled;
        }

        private EnemyHealth FindTarget()
        {
            if (data == null || data.AttackRange <= 0f)
            {
                return null;
            }

            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(
                transform.position,
                data.AttackRange,
                enemyLayer);

            EnemyHealth bestTarget = null;
            for (int i = 0; i < enemyHits.Length; i++)
            {
                Collider2D hit = enemyHits[i];
                if (hit == null)
                {
                    continue;
                }
                
                EnemyHealth candidate = hit.GetComponentInParent<EnemyHealth>();
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }

                if (bestTarget == null || IsBetterTarget(candidate, bestTarget))
                {
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool IsBetterTarget(EnemyHealth candidate, EnemyHealth current)
        {
            switch (data.TargetPriority)
            {
                case TargetPriority.First:
                    return GetPathProgress(candidate) > GetPathProgress(current);
                case TargetPriority.Strongest:
                    return candidate.CurrentHp > current.CurrentHp;
                case TargetPriority.Fastest:
                    return GetMoveSpeed(candidate) > GetMoveSpeed(current);
                case TargetPriority.Nearest:
                default:
                    return Vector3.Distance(transform.position, candidate.transform.position)
                        < Vector3.Distance(transform.position, current.transform.position);
            }
        }

        private void Attack(EnemyHealth target)
        {
            if (target == null || data == null)
            {
                return;
            }

            switch (data.AttackMode)
            {
                case AttackMode.Projectile:
                    FireProjectile(target);
                    break;
                case AttackMode.Area:
                    AttackArea(target.transform.position);
                    break;
                case AttackMode.Slow:
                    ApplySlow(target);
                    target.TakeDamage(data.Damage);
                    break;
                case AttackMode.Melee:
                default:
                    target.TakeDamage(data.Damage);
                    break;
            }
        }

        private void FireProjectile(EnemyHealth target)
        {
            if (data.ProjectilePrefab == null)
            {
                target.TakeDamage(data.Damage);
                return;
            }

            Transform spawnPoint = firePoint != null ? firePoint : transform;
            GameObject projectileObject = Instantiate(
                data.ProjectilePrefab,
                spawnPoint.position,
                Quaternion.identity);

            TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<TowerProjectile>();
            }

            projectile.Initialize(target, data.Damage);
        }

        private void AttackArea(Vector3 center)
        {
            float radius = data.AreaRadius > 0f ? data.AreaRadius : data.AttackRange;
            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);

            for (int i = 0; i < enemyHits.Length; i++)
            {
                Collider2D hit = enemyHits[i];
                if (hit == null)
                {
                    continue;
                }

                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(data.Damage);
                }
            }
        }

        private void ApplySlow(EnemyHealth target)
        {
            EnemyPathFollower pathFollower = target.GetComponentInParent<EnemyPathFollower>();
            if (pathFollower != null)
            {
                pathFollower.ApplySlow(data.SlowPercent, data.SlowDuration);
            }
        }

        private float GetAttackCooldown()
        {
            return data != null && data.AttackSpeed > 0f
                ? 1f / data.AttackSpeed
                : float.PositiveInfinity;
        }

        private static int GetPathProgress(EnemyHealth enemy)
        {
            EnemyPathFollower follower = enemy != null ? enemy.GetComponentInParent<EnemyPathFollower>() : null;
            return follower != null ? follower.CurrentWaypointIndex : -1;
        }

        private static float GetMoveSpeed(EnemyHealth enemy)
        {
            EnemyPathFollower follower = enemy != null ? enemy.GetComponentInParent<EnemyPathFollower>() : null;
            return follower != null ? follower.CurrentMoveSpeed : 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.AttackRange);
        }
    }
}
