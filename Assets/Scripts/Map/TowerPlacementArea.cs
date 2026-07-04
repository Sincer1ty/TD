using UnityEngine;

namespace TD.Map
{
    public class TowerPlacementArea : MonoBehaviour
    {
        [SerializeField] private Collider2D[] placementColliders;
        [SerializeField] private Collider2D[] blockedColliders;
        [SerializeField] private LayerMask blockedLayerMask;
        [SerializeField] private bool requireInsidePlacementArea = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.35f, 0.25f);

        private void Reset()
        {
            placementColliders = GetComponentsInChildren<Collider2D>();
        }

        private void OnValidate()
        {
            RemoveNullColliders(ref placementColliders);
            RemoveNullColliders(ref blockedColliders);
        }

        public bool CanPlaceTowerAt(Vector2 worldPosition)
        {
            if (requireInsidePlacementArea && !IsInsidePlacementArea(worldPosition))
            {
                return false;
            }

            if (IsInsideBlockedCollider(worldPosition))
            {
                return false;
            }

            if (blockedLayerMask.value != 0)
            {
                if (Physics2D.OverlapPoint(worldPosition, blockedLayerMask) != null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInsidePlacementArea(Vector2 worldPosition)
        {
            if (placementColliders == null || placementColliders.Length == 0)
            {
                return !requireInsidePlacementArea;
            }

            foreach (Collider2D placementCollider in placementColliders)
            {
                if (placementCollider != null && placementCollider.OverlapPoint(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInsideBlockedCollider(Vector2 worldPosition)
        {
            if (blockedColliders == null)
            {
                return false;
            }

            foreach (Collider2D blockedCollider in blockedColliders)
            {
                if (blockedCollider != null && blockedCollider.OverlapPoint(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveNullColliders(ref Collider2D[] colliders)
        {
            if (colliders == null)
            {
                return;
            }

            int validCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[validCount] = colliders[i];
                    validCount++;
                }
            }

            if (validCount != colliders.Length)
            {
                System.Array.Resize(ref colliders, validCount);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || placementColliders == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            foreach (Collider2D placementCollider in placementColliders)
            {
                if (placementCollider == null)
                {
                    continue;
                }

                Bounds bounds = placementCollider.bounds;
                Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }
    }
}
