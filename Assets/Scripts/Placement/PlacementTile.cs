using TD.Tower;
using UnityEngine;
using TowerBehaviour = TD.Tower.Tower;

namespace TD.Placement
{
    public class PlacementTile : MonoBehaviour
    {
        [SerializeField] private bool isBuildable = true;
        [SerializeField] private bool isOccupied;
        [SerializeField] private TowerBehaviour currentTower;
        [SerializeField] private Transform buildPoint;
        [SerializeField] private Vector3 buildOffset;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color validHoverColor = new Color(0.35f, 1f, 0.35f, 0.85f);
        [SerializeField] private Color invalidHoverColor = new Color(1f, 0.25f, 0.25f, 0.85f);

        private Color defaultColor = Color.white;

        public bool IsBuildable => isBuildable;
        public bool IsOccupied => isOccupied;
        public TowerBehaviour CurrentTower => currentTower;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }
        }

        public bool CanPlace()
        {
            return isBuildable && !isOccupied && currentTower == null;
        }

        public TowerBehaviour GetCurrentTower()
        {
            return currentTower;
        }

        public bool PlaceTower(TowerBehaviour tower)
        {
            if (tower == null || !CanPlace())
            {
                return false;
            }

            currentTower = tower;
            isOccupied = true;
            ClearHover();
            return true;
        }

        public void ClearTower()
        {
            currentTower = null;
            isOccupied = false;
            ClearHover();
        }

        public void SetHover(bool isValid)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isValid ? validHoverColor : invalidHoverColor;
            }
        }

        public void ClearHover()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }

        public Vector3 GetBuildPosition()
        {
            return buildPoint != null ? buildPoint.position : transform.position + buildOffset;
        }

        public void SetBuildable(bool buildable)
        {
            isBuildable = buildable;
        }
    }
}
