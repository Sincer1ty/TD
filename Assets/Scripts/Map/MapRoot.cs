using UnityEngine;

namespace TD.Map
{
    public class MapRoot : MonoBehaviour
    {
        [Header("Visual Layers")]
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform pathRoot;
        [SerializeField] private Transform decorationsRoot;

        [Header("Gameplay References")]
        [SerializeField] private WaypointPath waypointPath;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private TowerPlacementArea towerPlacementArea;

        public Transform BackgroundRoot => backgroundRoot;
        public Transform PathRoot => pathRoot;
        public Transform DecorationsRoot => decorationsRoot;
        public WaypointPath WaypointPath => waypointPath;
        public Transform SpawnPoint => spawnPoint;
        public Transform GoalPoint => goalPoint;
        public TowerPlacementArea TowerPlacementArea => towerPlacementArea;

        private void Reset()
        {
            FindReferencesInChildren();
        }

        private void OnValidate()
        {
            if (waypointPath == null)
            {
                waypointPath = GetComponentInChildren<WaypointPath>();
            }

            if (towerPlacementArea == null)
            {
                towerPlacementArea = GetComponentInChildren<TowerPlacementArea>();
            }
        }

        [ContextMenu("Find References In Children")]
        public void FindReferencesInChildren()
        {
            backgroundRoot = FindDirectChild("Background");
            pathRoot = FindDirectChild("Path");
            decorationsRoot = FindDirectChild("Decorations");
            waypointPath = GetComponentInChildren<WaypointPath>();
            towerPlacementArea = GetComponentInChildren<TowerPlacementArea>();

            if (waypointPath != null && waypointPath.Count > 0)
            {
                waypointPath.TryGetWaypoint(0, out Transform firstWaypoint);
                waypointPath.TryGetWaypoint(waypointPath.Count - 1, out Transform lastWaypoint);
                spawnPoint = firstWaypoint;
                goalPoint = lastWaypoint;
            }
        }

        public bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;

            Transform point = spawnPoint;
            if (point == null && waypointPath != null)
            {
                waypointPath.TryGetWaypoint(0, out point);
            }

            if (point == null)
            {
                return false;
            }

            position = point.position;
            return true;
        }

        public bool TryGetGoalPosition(out Vector3 position)
        {
            position = Vector3.zero;

            Transform point = goalPoint;
            if (point == null && waypointPath != null)
            {
                waypointPath.TryGetWaypoint(waypointPath.Count - 1, out point);
            }

            if (point == null)
            {
                return false;
            }

            position = point.position;
            return true;
        }

        private Transform FindDirectChild(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child : null;
        }
    }
}
