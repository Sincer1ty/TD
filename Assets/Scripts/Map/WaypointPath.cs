using System.Collections.Generic;
using UnityEngine;

namespace TD.Map
{
    public class WaypointPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private bool useChildrenAsWaypoints = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 1f);

        public IReadOnlyList<Transform> Waypoints => waypoints;
        public int Count => waypoints.Count > 0 ? waypoints.Count : transform.childCount;

        private void Awake()
        {
            if (useChildrenAsWaypoints && waypoints.Count == 0)
            {
                RefreshFromChildren();
            }
        }

        private void OnValidate()
        {
            waypoints.RemoveAll(waypoint => waypoint == null);
        }

        public void RefreshFromChildren()
        {
            waypoints.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                waypoints.Add(transform.GetChild(i));
            }
        }

        public bool TryGetWaypoint(int index, out Transform waypoint)
        {
            waypoint = null;

            if (index < 0 || index >= Count)
            {
                return false;
            }

            waypoint = waypoints.Count > 0 ? waypoints[index] : transform.GetChild(index);
            return waypoint != null;
        }

        public Vector3[] GetPositions()
        {
            List<Vector3> positions = new List<Vector3>();

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    positions.Add(waypoint.position);
                }
            }

            return positions.ToArray();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            IReadOnlyList<Transform> points = waypoints;
            if (useChildrenAsWaypoints && waypoints.Count == 0)
            {
                List<Transform> childPoints = new List<Transform>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    childPoints.Add(transform.GetChild(i));
                }

                points = childPoints;
            }

            Gizmos.color = gizmoColor;

            for (int i = 0; i < points.Count; i++)
            {
                Transform current = points[i];
                if (current == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(current.position, 0.12f);

                if (i + 1 < points.Count && points[i + 1] != null)
                {
                    Gizmos.DrawLine(current.position, points[i + 1].position);
                }
            }
        }
    }
}
