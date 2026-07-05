using UnityEngine;

namespace TD.Placement
{
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.25f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.25f);
        [SerializeField] private float baseSpriteRadius = 0.5f;
        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private bool overrideZPosition = true;
        [SerializeField] private float zPosition = -0.1f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private bool debugLog;

        private void Awake()
        {
            CacheRenderer();
            ApplyRendererSettings();
            Hide();
        }

        public void Show(Vector3 position, float range)
        {
            CacheRenderer();

            if (spriteRenderer == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("RangeIndicator cannot show because SpriteRenderer is missing.", this);
                }

                Hide();
                return;
            }

            if (range <= 0f)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"RangeIndicator cannot show because range is {range}.", this);
                }

                Hide();
                return;
            }

            if (overrideZPosition)
            {
                position.z = zPosition;
            }

            transform.position = position;

            float safeRadius = baseSpriteRadius > 0f ? baseSpriteRadius : 1f;
            if (baseSpriteRadius <= 0f && debugLog)
            {
                Debug.LogWarning("RangeIndicator baseSpriteRadius was <= 0. Using 1 for scale calculation.", this);
            }

            float diameterScale = Mathf.Max(minScale, range / safeRadius);
            transform.localScale = new Vector3(diameterScale, diameterScale, 1f);

            gameObject.SetActive(true);
            ApplyRendererSettings();

            if (debugLog)
            {
                Debug.Log($"RangeIndicator Show position={transform.position}, range={range}, scale={diameterScale}", this);
            }
        }

        public void Hide()
        {
            if (debugLog && gameObject.activeSelf)
            {
                Debug.Log("RangeIndicator Hide", this);
            }

            gameObject.SetActive(false);
        }

        public void SetValidState(bool isValid)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isValid ? validColor : invalidColor;
            }
        }

        public void SetSpriteRenderer(SpriteRenderer renderer)
        {
            spriteRenderer = renderer;
            ApplyRendererSettings();
        }

        private void CacheRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private void ApplyRendererSettings()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.enabled = true;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;

            if (spriteRenderer.sprite == null && debugLog)
            {
                Debug.LogWarning("RangeIndicator SpriteRenderer has no sprite.", this);
            }
        }
    }
}
