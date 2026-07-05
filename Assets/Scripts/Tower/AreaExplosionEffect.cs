using System.Collections;
using UnityEngine;

namespace TD.Tower
{
    public class AreaExplosionEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private float baseEffectRadius = 0.5f;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private string sortingLayerName = "Effect";
        [SerializeField] private int sortingOrder = 50;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private float fallbackLifetime = 1.5f;
        [SerializeField] private bool destroyOnComplete = true;

        private ParticleSystem[] particles;

        private void Awake()
        {
            if (particle == null)
            {
                particle = GetComponentInChildren<ParticleSystem>();
            }

            CacheParticles();
        }

        public void ConfigureRendering(string layerName, int order, bool enableDiagnostics)
        {
            if (!string.IsNullOrEmpty(layerName))
            {
                sortingLayerName = layerName;
            }

            sortingOrder = order;
            logDiagnostics = enableDiagnostics;
        }

        public void Play(Vector3 position, float attackRange, float scaleMultiplier)
        {
            transform.position = position;
            float safeRange = Mathf.Max(0.01f, attackRange);
            float safeBaseRadius = baseEffectRadius > 0f ? baseEffectRadius : 1f;
            float safeMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            float visualScale = Mathf.Max(minScale, (safeRange / safeBaseRadius) * safeMultiplier);
            transform.localScale = Vector3.one * visualScale;

            CacheParticles();
            ApplyRendererSettings();

            if (logDiagnostics)
            {
                Debug.Log($"Area VFX visualScale={visualScale}, attackRange={attackRange}, baseEffectRadius={safeBaseRadius}, position={position}.");
            }

            if (particles == null || particles.Length == 0)
            {
                if (logDiagnostics)
                {
                    Debug.LogWarning($"Area VFX '{name}' has no ParticleSystem.");
                }

                if (destroyOnComplete)
                {
                    Destroy(gameObject, fallbackLifetime);
                }

                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem currentParticle = particles[i];
                if (currentParticle == null)
                {
                    continue;
                }

                WarnIfParticleLooksInvisible(currentParticle);

                if (currentParticle.isStopped || !currentParticle.isPlaying)
                {
                    currentParticle.Play(true);
                }
            }

            if (destroyOnComplete)
            {
                StartCoroutine(DestroyAfterParticleRoutine());
            }
        }

        public void Play(float radius, float scaleMultiplier)
        {
            Play(transform.position, radius, scaleMultiplier);
        }

        private void CacheParticles()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
            if (particle == null && particles != null && particles.Length > 0)
            {
                particle = particles[0];
            }
        }

        private void ApplyRendererSettings()
        {
            foreach (ParticleSystemRenderer renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = true;

                if (!string.IsNullOrEmpty(sortingLayerName))
                {
                    renderer.sortingLayerName = sortingLayerName;
                }

                renderer.sortingOrder = sortingOrder;

                if (renderer.sharedMaterial == null && logDiagnostics)
                {
                    Debug.LogWarning($"Area VFX renderer '{renderer.name}' has no material.");
                }
            }
        }

        private void WarnIfParticleLooksInvisible(ParticleSystem targetParticle)
        {
            if (!logDiagnostics || targetParticle == null)
            {
                return;
            }

            ParticleSystem.MainModule main = targetParticle.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            if (GetMaxAlpha(startColor) <= 0f)
            {
                Debug.LogWarning($"Area VFX particle '{targetParticle.name}' has startColor alpha 0.");
            }

            if (main.startSize.constantMax < 0.01f)
            {
                Debug.LogWarning($"Area VFX particle '{targetParticle.name}' has very small startSize: {main.startSize.constantMax}.");
            }

            ParticleSystemRenderer renderer = targetParticle.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Area VFX particle '{targetParticle.name}' has no ParticleSystemRenderer.");
            }
            else if (renderer.sharedMaterial == null)
            {
                Debug.LogWarning($"Area VFX particle '{targetParticle.name}' renderer material is null.");
            }
        }

        private static float GetMaxAlpha(ParticleSystem.MinMaxGradient gradient)
        {
            switch (gradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return gradient.color.a;
                case ParticleSystemGradientMode.TwoColors:
                    return Mathf.Max(gradient.colorMin.a, gradient.colorMax.a);
                case ParticleSystemGradientMode.Gradient:
                    return GetMaxGradientAlpha(gradient.gradient);
                case ParticleSystemGradientMode.TwoGradients:
                    return Mathf.Max(
                        GetMaxGradientAlpha(gradient.gradientMin),
                        GetMaxGradientAlpha(gradient.gradientMax));
                case ParticleSystemGradientMode.RandomColor:
                default:
                    return 1f;
            }
        }

        private static float GetMaxGradientAlpha(Gradient gradient)
        {
            if (gradient == null || gradient.alphaKeys == null || gradient.alphaKeys.Length == 0)
            {
                return 0f;
            }

            float maxAlpha = 0f;
            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                maxAlpha = Mathf.Max(maxAlpha, alphaKeys[i].alpha);
            }

            return maxAlpha;
        }

        private IEnumerator DestroyAfterParticleRoutine()
        {
            float duration = fallbackLifetime;
            if (particles != null && particles.Length > 0)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    ParticleSystem currentParticle = particles[i];
                    if (currentParticle == null)
                    {
                        continue;
                    }

                    ParticleSystem.MainModule main = currentParticle.main;
                    duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax);
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
            Destroy(gameObject);
        }
    }
}
