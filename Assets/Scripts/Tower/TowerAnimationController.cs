using System.Collections;
using UnityEngine;

namespace TD.Tower
{
    public class TowerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string directionParamName = "Direction";
        [SerializeField] private string isAttackingParamName = "IsAttacking";
        [SerializeField] private bool useDirectionParam = true;
        [SerializeField] private bool useIsAttackingParam = true;
        [SerializeField] private bool resetAttackTriggerBeforePlay = true;
        [SerializeField] private bool allowRestartWhileAttacking = true;
        [SerializeField] private bool autoReturnToIdle = true;
        [SerializeField] private float attackStateDuration = 0.35f;

        private DirectionType currentDirection = DirectionType.Right;
        private bool isAttacking;
        private Coroutine idleRoutine;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            PlayIdle();
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            FaceDirection(targetPosition - transform.position);
        }

        public void FaceDirection(Vector2 direction)
        {
            SetDirection(direction);
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                currentDirection = direction.x < 0f ? DirectionType.Left : DirectionType.Right;
            }
            else
            {
                currentDirection = direction.y < 0f ? DirectionType.Down : DirectionType.Up;
            }

            ApplyDirection();
        }

        public void PlayAttack()
        {
            if (animator == null)
            {
                return;
            }

            if (isAttacking && !allowRestartWhileAttacking)
            {
                return;
            }

            isAttacking = true;

            if (useIsAttackingParam && !string.IsNullOrEmpty(isAttackingParamName))
            {
                animator.SetBool(isAttackingParamName, true);
            }

            if (!string.IsNullOrEmpty(attackTriggerName))
            {
                if (resetAttackTriggerBeforePlay)
                {
                    animator.ResetTrigger(attackTriggerName);
                }

                animator.SetTrigger(attackTriggerName);
            }

            if (autoReturnToIdle)
            {
                if (idleRoutine != null)
                {
                    StopCoroutine(idleRoutine);
                }

                idleRoutine = StartCoroutine(ReturnToIdleRoutine());
            }
        }

        public void PlayIdle()
        {
            isAttacking = false;

            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }

            if (animator != null && useIsAttackingParam && !string.IsNullOrEmpty(isAttackingParamName))
            {
                animator.SetBool(isAttackingParamName, false);
            }
        }

        private IEnumerator ReturnToIdleRoutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, attackStateDuration));
            idleRoutine = null;
            PlayIdle();
        }

        private void ApplyDirection()
        {
            if (spriteRenderer != null)
            {
                if (currentDirection == DirectionType.Left)
                {
                    spriteRenderer.flipX = true;
                }
                else if (currentDirection == DirectionType.Right)
                {
                    spriteRenderer.flipX = false;
                }
            }

            if (animator != null && useDirectionParam && !string.IsNullOrEmpty(directionParamName))
            {
                animator.SetInteger(directionParamName, (int)currentDirection);
            }
        }
    }
}
