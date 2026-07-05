using System.Collections;
using System.Collections.Generic;
using TD.Enemy;
using TD.Economy;
using TD.Placement;
using UnityEngine;
using UnityEngine.Events;

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
        [SerializeField] private bool logProjectileEvents;
        [SerializeField] private bool logAreaEvents;
        [Header("Upgrade")]
        [SerializeField] private MonoBehaviour costProviderBehaviour;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private float currentDamage;
        [SerializeField] private float currentAttackRange;
        [SerializeField] private float currentAttackSpeed;
        [SerializeField] private bool allowUpgradeWithoutCostProvider;
        [SerializeField] private bool logUpgradeEvents;
        [SerializeField] private UnityEvent<Tower> onTowerUpgraded = new UnityEvent<Tower>();
        [SerializeField] private UnityEvent<Tower> onTowerStatsChanged = new UnityEvent<Tower>();
        [SerializeField] private UnityEvent<Tower> onUpgradeFailed = new UnityEvent<Tower>();
        [Header("Placement")]
        [SerializeField] private PlacementTile placedTile;

        private float attackTimer;
        private ITowerCostProvider costProvider;

        public TowerData Data => data;
        public int CurrentLevel => currentLevel;
        public int MaxLevel => maxLevel;
        public int UpgradeLevel => currentLevel;
        public float CurrentDamage => currentDamage;
        public float CurrentAttackRange => currentAttackRange;
        public float CurrentAttackSpeed => currentAttackSpeed;
        public PlacementTile PlacedTile => placedTile;
        public UnityEvent<Tower> OnTowerUpgraded => onTowerUpgraded;
        public UnityEvent<Tower> OnTowerStatsChanged => onTowerStatsChanged;
        public UnityEvent<Tower> OnUpgradeFailed => onUpgradeFailed;

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

            if (Attack(target))
            {
                attackTimer = GetAttackCooldown();
            }
        }

        public void Initialize(TowerData towerData)
        {
            data = towerData;
            currentLevel = 1;
            attackTimer = 0f;
            CacheCostProvider();
            ApplyLevelStats();
        }

        public bool CanUpgrade(ITowerCostProvider costProvider)
        {
            if (data == null || currentLevel >= maxLevel)
            {
                return false;
            }

            int cost = GetNextUpgradeCost();
            return costProvider != null
                ? costProvider.CanAfford(cost)
                : allowUpgradeWithoutCostProvider || cost <= 0;
        }

        public bool Upgrade(ITowerCostProvider costProvider)
        {
            return TryUpgrade(costProvider);
        }

        public bool CanUpgrade()
        {
            return CanUpgrade(GetCostProvider());
        }

        public int GetNextUpgradeCost()
        {
            if (data == null || currentLevel >= maxLevel)
            {
                return 0;
            }

            return data.GetUpgradeCostForNextLevel(currentLevel);
        }

        public bool TryUpgrade()
        {
            return TryUpgrade(GetCostProvider());
        }

        public bool TryUpgrade(ITowerCostProvider provider)
        {
            if (data == null)
            {
                LogUpgradeFailure("missing TowerData");
                onUpgradeFailed?.Invoke(this);
                return false;
            }

            if (currentLevel >= maxLevel)
            {
                LogUpgradeFailure("already at max level");
                onUpgradeFailed?.Invoke(this);
                return false;
            }

            int cost = GetNextUpgradeCost();
            if (provider == null && cost > 0 && !allowUpgradeWithoutCostProvider)
            {
                LogUpgradeFailure("missing gold provider");
                onUpgradeFailed?.Invoke(this);
                return false;
            }

            if (provider != null && !provider.CanAfford(cost))
            {
                LogUpgradeFailure($"not enough gold. Required: {cost}");
                onUpgradeFailed?.Invoke(this);
                return false;
            }

            if (provider != null && !provider.SpendCost(cost))
            {
                LogUpgradeFailure($"failed to spend upgrade cost: {cost}");
                onUpgradeFailed?.Invoke(this);
                return false;
            }

            currentLevel++;
            ApplyLevelStats();
            onTowerUpgraded?.Invoke(this);

            if (logUpgradeEvents)
            {
                Debug.Log($"Tower '{name}' upgraded to level {currentLevel}. Damage={currentDamage}, Range={currentAttackRange}, Speed={currentAttackSpeed}.");
            }

            return true;
        }

        public void ApplyLevelStats()
        {
            if (data == null)
            {
                currentLevel = Mathf.Max(1, currentLevel);
                maxLevel = Mathf.Max(1, maxLevel);
                currentDamage = 0f;
                currentAttackRange = 0f;
                currentAttackSpeed = 0f;
                onTowerStatsChanged?.Invoke(this);
                return;
            }

            maxLevel = Mathf.Clamp(data.MaxLevel, 1, 3);
            currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);

            TowerLevelStats stats = data.GetStatsForLevel(currentLevel);
            currentDamage = stats != null ? stats.Damage : data.Damage;
            currentAttackRange = stats != null ? stats.AttackRange : data.AttackRange;
            currentAttackSpeed = stats != null ? stats.AttackSpeed : data.AttackSpeed;
            onTowerStatsChanged?.Invoke(this);

            if (logUpgradeEvents)
            {
                Debug.Log($"Tower '{name}' applied level {currentLevel} stats. Damage={currentDamage}, Range={currentAttackRange}, Speed={currentAttackSpeed}.");
            }
        }

        public void SetAttackEnabled(bool enabled)
        {
            attackEnabled = enabled;
        }

        public void SetCostProvider(MonoBehaviour providerBehaviour)
        {
            costProviderBehaviour = providerBehaviour;
            CacheCostProvider();
        }

        public void SetPlacedTile(PlacementTile tile)
        {
            placedTile = tile;
        }

        public int GetSellRefundGold()
        {
            return data != null ? data.SellRefundGold : 0;
        }

        public bool Sell()
        {
            GoldManager goldManager = GetGoldManager();
            int refundGold = GetSellRefundGold();
            if (refundGold > 0)
            {
                if (goldManager == null)
                {
                    LogUpgradeFailure("cannot sell because GoldManager is missing");
                    return false;
                }

                goldManager.AddGold(refundGold);
            }

            if (placedTile != null)
            {
                placedTile.ClearTower();
                placedTile = null;
            }

            Destroy(gameObject);
            return true;
        }

        private EnemyHealth FindTarget()
        {
            if (data == null || CurrentAttackRange <= 0f)
            {
                return null;
            }

            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(
                transform.position,
                CurrentAttackRange,
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

            if (data.TowerType == TowerType.Archer || data.AttackMode == AttackMode.Projectile)
            {
                return AttackSingleTarget(target, AttackMode.Projectile);
            }

            if (data.TowerType == TowerType.Mage || data.AttackMode == AttackMode.Area)
            {
                return AttackSingleTarget(target, AttackMode.Area);
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
                ApplySingleTargetAttack(target, attackMode);
            }
            else
            {
                StartCoroutine(DelayedSingleTargetAttack(target, attackMode));
            }

            return true;
        }

        private IEnumerator DelayedSingleTargetAttack(EnemyHealth target, AttackMode attackMode)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, attackHitDelay));
            ApplySingleTargetAttack(target, attackMode);
        }

        private void ApplySingleTargetAttack(EnemyHealth target, AttackMode attackMode)
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
                    AttackArea();
                    break;
                case AttackMode.Slow:
                    if (target != null && !target.IsDead)
                    {
                        ApplySlow(target);
                        target.TakeDamage(CurrentDamage);
                    }
                    break;
            }
        }

        private bool AttackMelee(EnemyHealth facingTarget)
        {
            if (data == null || CurrentAttackRange <= 0f)
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
                CurrentAttackRange,
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

                enemy.TakeDamage(CurrentDamage);
                hitCount++;
            }

            if (logMeleeHitCount)
            {
                Debug.Log($"Melee attack hit {hitCount} enemies.");
            }
        }

        private void FaceAndPlayAttack(Vector3 targetPosition)
        {
            if (animationController == null)
            {
                return;
            }

            animationController.FaceTarget(targetPosition);
            animationController.PlayAttack();
        }

        private void FireProjectile(EnemyHealth target)
        {
            if (target == null || target.IsDead || data == null)
            {
                return;
            }

            GameObject projectilePrefab = data.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                if (logProjectileEvents)
                {
                    Debug.LogWarning($"Tower '{data.TowerName}' tried to fire a projectile, but projectilePrefab is not assigned.");
                }

                return;
            }

            Transform spawnPoint = firePoint != null ? firePoint : transform;
            GameObject projectileObject = Instantiate(
                projectilePrefab,
                spawnPoint.position,
                Quaternion.identity);

            ArrowProjectile projectile = projectileObject.GetComponent<ArrowProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ArrowProjectile>();
            }

            projectile.Initialize(
                target,
                CurrentDamage,
                data.ProjectileSpeed,
                data.ProjectileLifetime,
                enemyLayer,
                logProjectileEvents);

            if (logProjectileEvents)
            {
                Debug.Log($"Arrow projectile spawned from {spawnPoint.name} toward {target.name}.");
            }
        }

        private void AttackArea()
        {
            if (data == null)
            {
                return;
            }

            float radius = CurrentAttackRange;
            if (radius <= 0f)
            {
                if (logAreaEvents)
                {
                    Debug.LogWarning($"Tower '{data.TowerName}' has invalid attackRange. Area attack was skipped.");
                }

                return;
            }

            Vector3 center = transform.position;
            SpawnAreaEffect(center, radius);

            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
            HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
            int hitCount = 0;

            for (int i = 0; i < enemyHits.Length; i++)
            {
                Collider2D hit = enemyHits[i];
                if (hit == null)
                {
                    continue;
                }

                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead && damagedEnemies.Add(enemy))
                {
                    enemy.TakeDamage(CurrentDamage);
                    hitCount++;
                }
            }

            if (logAreaEvents)
            {
                Debug.Log($"Area attack hit {hitCount} enemies around tower '{name}' with attackRange {radius}.");
            }
        }

        private void SpawnAreaEffect(Vector3 center, float radius)
        {
            if (data == null || data.AreaEffectPrefab == null)
            {
                if (logAreaEvents)
                {
                    Debug.LogWarning($"Tower '{data?.TowerName ?? name}' has no areaEffectPrefab. Area damage still applies.");
                }

                return;
            }

            Vector3 effectPosition = GetAreaEffectPosition(center);
            GameObject effectObject = Instantiate(data.AreaEffectPrefab, effectPosition, Quaternion.identity);
            ApplyAreaEffectSorting(effectObject);

            AreaExplosionEffect explosionEffect = effectObject.GetComponent<AreaExplosionEffect>();
            if (explosionEffect == null)
            {
                explosionEffect = effectObject.AddComponent<AreaExplosionEffect>();
            }

            explosionEffect.ConfigureRendering(
                data.AreaEffectSortingLayerName,
                data.AreaEffectSortingOrder,
                logAreaEvents);
            explosionEffect.Play(effectPosition, radius, data.AreaEffectScaleMultiplier);
            Destroy(effectObject, data.AreaEffectLifetime);

            if (logAreaEvents)
            {
                Debug.Log($"Area effect spawned at tower position {effectPosition} with attackRange {radius}.");
            }
        }

        private Vector3 GetAreaEffectPosition(Vector3 center)
        {
            center.z = data != null ? data.AreaEffectZOffset : center.z;
            return center;
        }

        private void ApplyAreaEffectSorting(GameObject effectObject)
        {
            if (effectObject == null || data == null)
            {
                return;
            }

            foreach (ParticleSystemRenderer renderer in effectObject.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(data.AreaEffectSortingLayerName))
                {
                    renderer.sortingLayerName = data.AreaEffectSortingLayerName;
                }

                renderer.sortingOrder = data.AreaEffectSortingOrder;
                renderer.enabled = true;
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
            return data != null && CurrentAttackSpeed > 0f
                ? 1f / CurrentAttackSpeed
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
            Gizmos.DrawWireSphere(transform.position, CurrentAttackRange);
        }

        private ITowerCostProvider GetCostProvider()
        {
            if (costProvider == null)
            {
                CacheCostProvider();
            }

            return costProvider;
        }

        private void CacheCostProvider()
        {
            costProvider = costProviderBehaviour as ITowerCostProvider;
            if (costProvider == null)
            {
                GoldManager goldManager = FindFirstObjectByType<GoldManager>();
                costProvider = goldManager;
                costProviderBehaviour = goldManager;
            }
        }

        private GoldManager GetGoldManager()
        {
            GoldManager goldManager = costProviderBehaviour as GoldManager;
            return goldManager != null ? goldManager : FindFirstObjectByType<GoldManager>();
        }

        private void LogUpgradeFailure(string reason)
        {
            if (logUpgradeEvents)
            {
                Debug.Log($"Tower '{name}' upgrade failed: {reason}.");
            }
        }
    }
}
