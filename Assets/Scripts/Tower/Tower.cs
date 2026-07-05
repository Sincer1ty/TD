using System.Collections;
using System.Collections.Generic;
using TD.Enemy;
using UnityEngine;

namespace TD.Tower
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;
        [SerializeField] private LayerMask enemyLayer = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Transform firePoint;
        [SerializeField] private TowerAnimationController animationController;
        [SerializeField] private bool attackEnabled = true;
        [SerializeField] private bool immediateDamage = true;
        [SerializeField] private float attackHitDelay = 0.1f;
        [SerializeField] private bool logMeleeHitCount;
        [SerializeField] private int upgradeLevel;

        private float attackTimer;

        public TowerData Data => data;
        public int UpgradeLevel => upgradeLevel;

        private void Awake()
        {
            if (animationController == null)
            {
                animationController = GetComponentInChildren<TowerAnimationController>();
            }
        }

        private void Reset()
        {
            firePoint = transform;
            animationController = GetComponentInChildren<TowerAnimationController>();
        }

        private void Update()
        {
            if (!attackEnabled || data == null)
            {
                return;
            }
            // Debug.Log("attack Timer");

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f)
            {
                return;
            }
            
            // Debug.Log("Find Target");
            EnemyHealth target = FindTarget();
            // Debug.Log("Target: " + (target != null ? target.name : "null"));
            if (target == null)
            {
                animationController?.PlayIdle();
                return;
            }

            Debug.Log("Attacking");
            if (Attack(target))
            {
                attackTimer = GetAttackCooldown();
            }
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

        private bool Attack(EnemyHealth target)
        {
            if (target == null || data == null)
            {
                return false;
            }

            switch (data.AttackMode)
            {
                case AttackMode.Projectile:
                    return AttackSingleTarget(target, AttackMode.Projectile);
                case AttackMode.Area:
                    return AttackSingleTarget(target, AttackMode.Area);
                case AttackMode.Slow:
                    return AttackSingleTarget(target, AttackMode.Slow);
                case AttackMode.Melee:
                default:
                    return AttackMelee(target);
            }
        }

        private bool AttackSingleTarget(EnemyHealth target, AttackMode attackMode)
        {
            if (target == null || target.IsDead || data == null)
            {
                return false;
            }

            Vector3 targetPosition = target.transform.position;
            FaceAndPlayAttack(targetPosition);

            if (immediateDamage || attackHitDelay <= 0f)
            {
                ApplySingleTargetAttack(target, attackMode, targetPosition);
            }
            else
            {
                StartCoroutine(DelayedSingleTargetAttack(target, attackMode, targetPosition));
            }

            return true;
        }

        private IEnumerator DelayedSingleTargetAttack(EnemyHealth target, AttackMode attackMode, Vector3 fallbackPosition)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, attackHitDelay));
            ApplySingleTargetAttack(target, attackMode, fallbackPosition);
        }

        private void ApplySingleTargetAttack(EnemyHealth target, AttackMode attackMode, Vector3 fallbackPosition)
        {
            switch (attackMode)
            {
                case AttackMode.Projectile:
                    if (target != null && !target.IsDead)
                    {
                        FireProjectile(target);
                    }
                    break;
                case AttackMode.Area:
                    AttackArea(target != null && !target.IsDead ? target.transform.position : fallbackPosition);
                    break;
                case AttackMode.Slow:
                    if (target != null && !target.IsDead)
                    {
                        ApplySlow(target);
                        target.TakeDamage(data.Damage);
                    }
                    break;
            }
        }

        private bool AttackMelee(EnemyHealth facingTarget)
        {
            Debug.Log("Attacking melee");
            if (data == null || data.AttackRange <= 0f)
            {
                return false;
            }

            List<EnemyHealth> meleeTargets = CollectMeleeTargets();
            if (meleeTargets.Count == 0)
            {
                return false;
            }

            EnemyHealth directionTarget = facingTarget != null && meleeTargets.Contains(facingTarget)
                ? facingTarget
                : meleeTargets[0];

            if (directionTarget != null)
            {
                FaceAndPlayAttack(directionTarget.transform.position);
            }

            if (immediateDamage || attackHitDelay <= 0f)
            {
                DamageMeleeTargets(meleeTargets);
            }
            else
            {
                StartCoroutine(DelayedMeleeAttack());
            }

            return true;
        }

        private IEnumerator DelayedMeleeAttack()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, attackHitDelay));
            DamageMeleeTargets(CollectMeleeTargets());
        }

        private List<EnemyHealth> CollectMeleeTargets()
        {
            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(
                transform.position,
                data.AttackRange,
                enemyLayer);

            HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
            List<EnemyHealth> meleeTargets = new List<EnemyHealth>();

            for (int i = 0; i < enemyHits.Length; i++)
            {
                Collider2D hit = enemyHits[i];
                if (hit == null)
                {
                    continue;
                }

                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !damagedEnemies.Add(enemy))
                {
                    continue;
                }

                meleeTargets.Add(enemy);
            }

            return meleeTargets;
        }

        private void DamageMeleeTargets(List<EnemyHealth> meleeTargets)
        {
            if (meleeTargets == null)
            {
                return;
            }

            int hitCount = 0;
            for (int i = 0; i < meleeTargets.Count; i++)
            {
                EnemyHealth enemy = meleeTargets[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                enemy.TakeDamage(data.Damage);
                hitCount++;
            }

            if (logMeleeHitCount)
            {
                Debug.Log($"Melee attack hit {hitCount} enemies.");
            }
        }

        private void FaceAndPlayAttack(Vector3 targetPosition)
        {
            Debug.Log("Face and Play Attack");
            if (animationController == null)
            {
                return;
            }

            animationController.FaceTarget(targetPosition);
            animationController.PlayAttack();
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
