using TD.Enemy;
using UnityEngine;

namespace TD.Tower
{
    public class TowerProjectile : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float hitDistance = 0.08f;
        [SerializeField] private float lifetime = 3f;

        private EnemyHealth target;
        private float damage;
        private float lifeTimer;

        public void Initialize(EnemyHealth newTarget, float newDamage)
        {
            target = newTarget;
            damage = Mathf.Max(0f, newDamage);
            lifeTimer = lifetime;
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f || target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition = target.transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
