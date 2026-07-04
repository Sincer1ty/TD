using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD.Map
{
    public class MapRoot : MonoBehaviour
    {
        [Header("Visual Layers")]
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform pathRoot;
        [SerializeField] private Transform decorationsRoot;

        [Header("Tilemap Layers")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap backgroundTilemap;
        [SerializeField] private Tilemap pathTilemap;
        [SerializeField] private Tilemap buildableTilemap;
        [SerializeField] private Tilemap decorationTilemap;
        [SerializeField] private Tilemap placementOverlayTilemap;
        [SerializeField] private Transform towersRoot;

        [Header("Gameplay References")]
        [SerializeField] private WaypointPath waypointPath;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private TowerPlacementArea towerPlacementArea;

        public Transform BackgroundRoot => backgroundRoot;
        public Transform PathRoot => pathRoot;
        public Transform DecorationsRoot => decorationsRoot;
        public Grid Grid => grid;
        public Tilemap BackgroundTilemap => backgroundTilemap;
        public Tilemap PathTilemap => pathTilemap;
        public Tilemap BuildableTilemap => buildableTilemap;
        public Tilemap DecorationTilemap => decorationTilemap;
        public Tilemap PlacementOverlayTilemap => placementOverlayTilemap;
        public Transform TowersRoot => towersRoot;
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

            if (grid == null)
            {
                grid = GetComponentInChildren<Grid>();
            }
        }

        [ContextMenu("Find References In Children")]
        public void FindReferencesInChildren()
        {
            backgroundRoot = FindDirectChild("Background");
            pathRoot = FindDirectChild("Path");
            decorationsRoot = FindDirectChild("Decorations");
            grid = GetComponentInChildren<Grid>();
            backgroundTilemap = FindTilemap("Background Tilemap");
            pathTilemap = FindTilemap("Path Tilemap");
            buildableTilemap = FindTilemap("Buildable Tilemap");
            decorationTilemap = FindTilemap("Decoration Tilemap");
            placementOverlayTilemap = FindTilemap("Placement Overlay Tilemap");
            towersRoot = FindDirectChild("Towers");
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

        private Tilemap FindTilemap(string childName)
        {
            Transform gridTransform = grid != null ? grid.transform : transform.Find("Grid");
            Transform tilemapTransform = gridTransform != null ? gridTransform.Find(childName) : null;
            return tilemapTransform != null ? tilemapTransform.GetComponent<Tilemap>() : null;
        }
    }
}
