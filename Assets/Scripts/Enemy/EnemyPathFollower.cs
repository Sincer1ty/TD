using System;
using TD.Map;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Enemy
{
    public class EnemyPathFollower : MonoBehaviour
    {
        private WaypointPath path;
        
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float arriveDistance = 0.05f;
        [SerializeField] private bool startOnAwake = true;
        [SerializeField] private int damageToBase = 1;
        [SerializeField] private bool destroyOnGoalReached = true;
        [SerializeField] private UnityEvent<EnemyPathFollower> onGoalReached;
        [SerializeField] private UnityEvent<EnemyPathFollower, int> onBaseReached;

        private int currentWaypointIndex;
        private bool isFollowing;
        private float slowMultiplier = 1f;
        private Coroutine slowRoutine;

        public event Action<EnemyPathFollower> GoalReached;
        public event Action<EnemyPathFollower, int> BaseReached;

        public WaypointPath Path => path;
        public int DamageToBase => damageToBase;
        public int CurrentWaypointIndex => currentWaypointIndex;
        public float CurrentMoveSpeed => moveSpeed * slowMultiplier;
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private void OnDisable()
        {
            ClearSlow();
        }

        private void Awake()
        {
            if (startOnAwake)
            {
                StartFollowing();
            }
        }

        private void Update()
        {
            if (!isFollowing || path == null)
            {
                return;
            }

            if (!path.TryGetWaypoint(currentWaypointIndex, out Transform targetWaypoint))
            {
                StopFollowing();
                return;
            }

            MoveToward(targetWaypoint.position);
        }

        public void SetPath(WaypointPath newPath, bool snapToStart = true)
        {
            path = newPath;
            currentWaypointIndex = 0;

            if (snapToStart && path != null && path.TryGetWaypoint(0, out Transform firstWaypoint))
            {
                transform.position = firstWaypoint.position;
            }
        }

        public void Initialize(WaypointPath newPath, bool snapToStart = true)
        {
            SetPath(newPath, snapToStart);
            StartFollowing();
        }

        public void Initialize(WaypointPath newPath, float speed, int baseDamage, bool snapToStart = true)
        {
            MoveSpeed = speed;
            damageToBase = Mathf.Max(0, baseDamage);
            Initialize(newPath, snapToStart);
        }

        public void StartFollowing()
        {
            if (path == null || path.Count == 0)
            {
                isFollowing = false;
                return;
            }

            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, path.Count - 1);
            isFollowing = true;
        }

        public void StopFollowing()
        {
            isFollowing = false;
        }

        private void MoveToward(Vector3 targetPosition)
        {
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                CurrentMoveSpeed * Time.deltaTime);

            transform.position = nextPosition;

            if (Vector3.Distance(nextPosition, targetPosition) <= arriveDistance)
            {
                AdvanceWaypoint();
            }
        }

        private void AdvanceWaypoint()
        {
            currentWaypointIndex++;

            if (path != null && currentWaypointIndex < path.Count)
            {
                return;
            }

            HandleGoalReached();
        }

        private void HandleGoalReached()
        {
            isFollowing = false;
            GoalReached?.Invoke(this);
            onGoalReached?.Invoke(this);
            BaseReached?.Invoke(this, damageToBase);
            onBaseReached?.Invoke(this, damageToBase);

            if (destroyOnGoalReached)
            {
                Destroy(gameObject);
            }
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                return;
            }

            slowPercent = Mathf.Clamp01(slowPercent);
            if (slowPercent <= 0f)
            {
                return;
            }

            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
            }

            slowRoutine = StartCoroutine(SlowRoutine(1f - slowPercent, duration));
        }

        private System.Collections.IEnumerator SlowRoutine(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
            yield return new WaitForSeconds(duration);
            ClearSlow();
        }

        private void ClearSlow()
        {
            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
                slowRoutine = null;
            }

            slowMultiplier = 1f;
        }
    }
}
