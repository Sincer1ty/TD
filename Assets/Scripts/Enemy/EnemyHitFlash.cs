using System.Collections;
using UnityEngine;

namespace TD.Enemy
{
    public class EnemyHitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [SerializeField] private bool useChildrenRenderers = true;
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float holdDuration = 0.05f;
        [SerializeField] private float fadeDuration = 0.12f;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool debugLog;

        private Color[] originalColors;
        private Coroutine flashRoutine;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnDisable()
        {
            StopFlash();
        }

        private void OnDestroy()
        {
            StopFlash();
        }

        public void PlayFlash()
        {
            CacheRenderers();

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"EnemyHitFlash on '{name}' has no SpriteRenderer.");
                }

                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            SetRendererColors(flashColor);
            flashRoutine = StartCoroutine(FlashRoutine());

            if (debugLog)
            {
                Debug.Log($"Enemy hit flash played on '{name}'.");
            }
        }

        public void StopFlash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            ResetVisual();
        }

        public void ResetVisual()
        {
            if (spriteRenderers == null || originalColors == null)
            {
                return;
            }

            int count = Mathf.Min(spriteRenderers.Length, originalColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer != null)
                {
                    renderer.color = originalColors[i];
                }
            }
        }

        private IEnumerator FlashRoutine()
        {
            yield return WaitForSecondsSafe(holdDuration);

            float elapsed = 0f;
            float safeFadeDuration = Mathf.Max(0f, fadeDuration);
            while (elapsed < safeFadeDuration)
            {
                float t = safeFadeDuration > 0f ? elapsed / safeFadeDuration : 1f;
                LerpToOriginalColors(t);
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            ResetVisual();
            flashRoutine = null;
        }

        private object WaitForSecondsSafe(float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            return useUnscaledTime
                ? new WaitForSecondsRealtime(safeDuration)
                : new WaitForSeconds(safeDuration);
        }

        private void LerpToOriginalColors(float t)
        {
            if (spriteRenderers == null || originalColors == null)
            {
                return;
            }

            int count = Mathf.Min(spriteRenderers.Length, originalColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer != null)
                {
                    renderer.color = Color.Lerp(flashColor, originalColors[i], t);
                }
            }
        }

        private void SetRendererColors(Color color)
        {
            if (spriteRenderers == null)
            {
                return;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer != null)
                {
                    renderer.color = color;
                }
            }
        }

        private void CacheRenderers()
        {
            if (useChildrenRenderers || spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = useChildrenRenderers
                    ? GetComponentsInChildren<SpriteRenderer>(true)
                    : GetComponents<SpriteRenderer>();
            }

            if (spriteRenderers == null)
            {
                originalColors = null;
                return;
            }

            if (originalColors != null && originalColors.Length == spriteRenderers.Length)
            {
                return;
            }

            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                originalColors[i] = renderer != null ? renderer.color : Color.white;
            }
        }
    }
}
