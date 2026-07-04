using UnityEngine;

namespace TD.Placement
{
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.25f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.25f);
        [SerializeField] private float baseSpriteRadius = 0.5f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            Hide();
        }

        public void Show(Vector3 position, float range)
        {
            if (spriteRenderer == null || range <= 0f)
            {
                Hide();
                return;
            }

            transform.position = position;

            float safeRadius = Mathf.Max(0.01f, baseSpriteRadius);
            float diameterScale = range / safeRadius;
            transform.localScale = new Vector3(diameterScale, diameterScale, 1f);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
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
        }
    }
}
