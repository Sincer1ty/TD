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
        [SerializeField] private UnityEvent<EnemyPathFollower> onGoalReached;

        private int currentWaypointIndex;
        private bool isFollowing;

        public event Action<EnemyPathFollower> GoalReached;

        public WaypointPath Path => path;
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
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
                moveSpeed * Time.deltaTime);

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

            Destroy(gameObject); // 죽었다는 표시?
        }
    }
}
