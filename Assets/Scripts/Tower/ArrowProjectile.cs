using TD.Enemy;
using UnityEngine;

namespace TD.Tower
{
    public class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float hitDistance = 0.08f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float rotationOffset;
        [SerializeField] private LayerMask enemyLayer = Physics2D.DefaultRaycastLayers;
        [SerializeField] private bool enableDebugLog;

        private EnemyHealth target;
        private float damage;
        private float lifeTimer;
        private bool hasHit;

        public void Initialize(EnemyHealth newTarget, float newDamage, float speed, float newLifetime)
        {
            Initialize(newTarget, newDamage, speed, newLifetime, enemyLayer, enableDebugLog);
        }

        public void Initialize(
            EnemyHealth newTarget,
            float newDamage,
            float speed,
            float newLifetime,
            LayerMask targetLayer,
            bool debugLog)
        {
            target = newTarget;
            damage = Mathf.Max(0f, newDamage);
            moveSpeed = Mathf.Max(0f, speed);
            lifetime = Mathf.Max(0.01f, newLifetime);
            lifeTimer = lifetime;
            enemyLayer = targetLayer;
            enableDebugLog = debugLog;
            hasHit = false;

            if (target != null)
            {
                RotateToward(target.transform.position - transform.position);
            }
        }

        private void Update()
        {
            if (hasHit)
            {
                return;
            }

            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (target == null || target.IsDead)
            {
                if (enableDebugLog)
                {
                    Debug.Log("Arrow projectile removed because target is gone.");
                }

                Destroy(gameObject);
                return;
            }

            Vector3 direction = target.transform.position - transform.position;
            if (direction.sqrMagnitude <= hitDistance * hitDistance)
            {
                HitTarget(target);
                return;
            }

            RotateToward(direction);
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.transform.position,
                moveSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit || other == null || !IsInEnemyLayer(other.gameObject.layer))
            {
                return;
            }

            EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            if (target != null && enemy != target)
            {
                return;
            }

            HitTarget(enemy);
        }

        private void HitTarget(EnemyHealth enemy)
        {
            if (hasHit || enemy == null || enemy.IsDead)
            {
                return;
            }

            hasHit = true;
            enemy.TakeDamage(damage);

            if (enableDebugLog)
            {
                Debug.Log($"Arrow projectile hit {enemy.name} for {damage} damage.");
            }

            Destroy(gameObject);
        }

        private void RotateToward(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        private bool IsInEnemyLayer(int layer)
        {
            return (enemyLayer.value & (1 << layer)) != 0;
        }
    }
}
