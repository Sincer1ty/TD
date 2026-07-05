using System;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Enemy
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp;
        [SerializeField] private EnemyHitFlash hitFlash;
        [SerializeField] private bool autoCreateHitFlash = true;
        [SerializeField] private UnityEvent<EnemyHealth> onDeath;
        [SerializeField] private UnityEvent<float, float> onHealthChanged;

        private bool isDead;

        public event Action<EnemyHealth> Death;
        public event Action<float, float> HealthChanged;

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (hitFlash == null)
            {
                hitFlash = GetComponent<EnemyHitFlash>();
            }

            if (hitFlash == null && autoCreateHitFlash)
            {
                hitFlash = gameObject.AddComponent<EnemyHitFlash>();
            }

            if (currentHp <= 0f)
            {
                currentHp = maxHp;
            }
        }

        public void Initialize(EnemyData data)
        {
            Initialize(data != null ? data.MaxHp : maxHp);
        }

        public void Initialize(float newMaxHp)
        {
            maxHp = Mathf.Max(1f, newMaxHp);
            currentHp = maxHp;
            isDead = false;
            NotifyHealthChanged();
        }

        public void TakeDamage(float damage)
        {
            if (isDead || damage <= 0f)
            {
                return;
            }

            currentHp = Mathf.Max(0f, currentHp - damage);
            NotifyHealthChanged();

            if (hitFlash != null)
            {
                hitFlash.PlayFlash();
            }

            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            if (hitFlash != null)
            {
                hitFlash.StopFlash();
            }

            currentHp = 0f;
            NotifyHealthChanged();
            Death?.Invoke(this);
            onDeath?.Invoke(this);
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(currentHp, maxHp);
            onHealthChanged?.Invoke(currentHp, maxHp);
        }
    }
}
