using System.IO;
using TD.Map;
using TD.Placement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TD.Editor
{
    public static class MapHierarchyCreator
    {
        [MenuItem("GameObject/TD/Create Map Root", false, 10)]
        public static void CreateMapRoot(MenuCommand menuCommand)
        {
            GameObject map = new GameObject("Map");
            GameObjectUtility.SetParentAndAlign(map, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(map, "Create Map Root");

            MapRoot mapRoot = map.AddComponent<MapRoot>();

            Transform grid = CreateChild(map.transform, "Grid");
            grid.gameObject.AddComponent<Grid>();
            Tilemap backgroundTilemap = CreateTilemap(grid, "Background Tilemap", -30);
            Tilemap pathTilemap = CreateTilemap(grid, "Path Tilemap", -20);
            Tilemap buildableTilemap = CreateTilemap(grid, "Buildable Tilemap", -10);
            Tilemap decorationTilemap = CreateTilemap(grid, "Decoration Tilemap", 0);
            Tilemap placementOverlayTilemap = CreateTilemap(grid, "Placement Overlay Tilemap", 10);

            Transform waypointsRoot = CreateChild(map.transform, "Waypoints");
            WaypointPath waypointPath = waypointsRoot.gameObject.AddComponent<WaypointPath>();
            CreateWaypoint(waypointsRoot, "Waypoint_0", new Vector3(-4f, 0f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_1", new Vector3(-1.5f, 1.5f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_2", new Vector3(1.5f, -1.5f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_End", new Vector3(4f, 0f, 0f));
            waypointPath.RefreshFromChildren();

            Transform towers = CreateChild(map.transform, "Towers");

            SerializedObject serializedMap = new SerializedObject(mapRoot);
            serializedMap.FindProperty("grid").objectReferenceValue = grid.GetComponent<Grid>();
            serializedMap.FindProperty("backgroundTilemap").objectReferenceValue = backgroundTilemap;
            serializedMap.FindProperty("pathTilemap").objectReferenceValue = pathTilemap;
            serializedMap.FindProperty("buildableTilemap").objectReferenceValue = buildableTilemap;
            serializedMap.FindProperty("decorationTilemap").objectReferenceValue = decorationTilemap;
            serializedMap.FindProperty("placementOverlayTilemap").objectReferenceValue = placementOverlayTilemap;
            serializedMap.FindProperty("towersRoot").objectReferenceValue = towers;
            serializedMap.FindProperty("waypointPath").objectReferenceValue = waypointPath;
            serializedMap.FindProperty("spawnPoint").objectReferenceValue = waypointsRoot.GetChild(0);
            serializedMap.FindProperty("goalPoint").objectReferenceValue = waypointsRoot.GetChild(waypointsRoot.childCount - 1);
            serializedMap.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = map;
        }

        [MenuItem("Tools/TD/Migrate Placement Cells To Tilemap")]
        public static void MigrateOpenScenePlacementCellsToTilemap()
        {
            GameObject map = GameObject.Find("Map");
            if (map == null)
            {
                map = new GameObject("Map");
                Undo.RegisterCreatedObjectUndo(map, "Create Map Root");
            }

            MapRoot mapRoot = map.GetComponent<MapRoot>();
            if (mapRoot == null)
            {
                mapRoot = map.AddComponent<MapRoot>();
            }

            Transform gridTransform = map.transform.Find("Grid");
            if (gridTransform == null)
            {
                gridTransform = CreateChild(map.transform, "Grid");
            }

            Grid grid = gridTransform.GetComponent<Grid>();
            if (grid == null)
            {
                grid = gridTransform.gameObject.AddComponent<Grid>();
            }

            Tilemap backgroundTilemap = GetOrCreateTilemap(gridTransform, "Background Tilemap", -30);
            Tilemap pathTilemap = GetOrCreateTilemap(gridTransform, "Path Tilemap", -20);
            Tilemap buildableTilemap = GetOrCreateTilemap(gridTransform, "Buildable Tilemap", -10);
            Tilemap decorationTilemap = GetOrCreateTilemap(gridTransform, "Decoration Tilemap", 0);
            Tilemap overlayTilemap = GetOrCreateTilemap(gridTransform, "Placement Overlay Tilemap", 10);
            Transform towers = map.transform.Find("Towers") ?? CreateChild(map.transform, "Towers");

            Transform placementCells = map.transform.Find("PlacementCells");
            if (placementCells == null)
            {
                GameObject placementCellsObject = GameObject.Find("PlacementCells");
                placementCells = placementCellsObject != null ? placementCellsObject.transform : null;
            }

            TileBase buildableTile = GetOrCreateBuildableTile();
            if (placementCells != null && buildableTile != null)
            {
                for (int i = 0; i < placementCells.childCount; i++)
                {
                    Transform cell = placementCells.GetChild(i);
                    Vector3Int tileCell = grid.WorldToCell(cell.position);
                    buildableTilemap.SetTile(tileCell, buildableTile);
                }

                Undo.DestroyObjectImmediate(placementCells.gameObject);
            }

            WireMapRoot(mapRoot, grid, backgroundTilemap, pathTilemap, buildableTilemap, decorationTilemap, overlayTilemap, towers);
            WirePlacementControllers(grid, buildableTilemap, overlayTilemap, towers);

            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = map;
        }

        public static void MigrateGameScenePlacementCellsToTilemap()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
            MigrateOpenScenePlacementCellsToTilemap();
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            return child.transform;
        }

        private static void CreateWaypoint(Transform parent, string childName, Vector3 localPosition)
        {
            Transform waypoint = CreateChild(parent, childName);
            waypoint.localPosition = localPosition;
        }

        private static Tilemap CreateTilemap(Transform parent, string childName, int sortingOrder)
        {
            Transform tilemapTransform = CreateChild(parent, childName);
            Tilemap tilemap = tilemapTransform.gameObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapTransform.gameObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static Tilemap GetOrCreateTilemap(Transform parent, string childName, int sortingOrder)
        {
            Transform tilemapTransform = parent.Find(childName);
            if (tilemapTransform == null)
            {
                return CreateTilemap(parent, childName, sortingOrder);
            }

            Tilemap tilemap = tilemapTransform.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = tilemapTransform.gameObject.AddComponent<Tilemap>();
            }

            TilemapRenderer renderer = tilemapTransform.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = tilemapTransform.gameObject.AddComponent<TilemapRenderer>();
            }

            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static TileBase GetOrCreateBuildableTile()
        {
            const string directoryPath = "Assets/Datas";
            const string tilePath = "Assets/Datas/T_BuildablePlacement.asset";

            TileBase existingTile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
            if (existingTile != null)
            {
                return existingTile;
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "T_BuildablePlacement";
            tile.flags = TileFlags.None;
            AssetDatabase.CreateAsset(tile, tilePath);
            AssetDatabase.SaveAssets();
            return tile;
        }

        private static void WireMapRoot(
            MapRoot mapRoot,
            Grid grid,
            Tilemap backgroundTilemap,
            Tilemap pathTilemap,
            Tilemap buildableTilemap,
            Tilemap decorationTilemap,
            Tilemap overlayTilemap,
            Transform towers)
        {
            SerializedObject serializedMap = new SerializedObject(mapRoot);
            serializedMap.FindProperty("grid").objectReferenceValue = grid;
            serializedMap.FindProperty("backgroundTilemap").objectReferenceValue = backgroundTilemap;
            serializedMap.FindProperty("pathTilemap").objectReferenceValue = pathTilemap;
            serializedMap.FindProperty("buildableTilemap").objectReferenceValue = buildableTilemap;
            serializedMap.FindProperty("decorationTilemap").objectReferenceValue = decorationTilemap;
            serializedMap.FindProperty("placementOverlayTilemap").objectReferenceValue = overlayTilemap;
            serializedMap.FindProperty("towersRoot").objectReferenceValue = towers;
            serializedMap.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePlacementControllers(
            Grid grid,
            Tilemap buildableTilemap,
            Tilemap overlayTilemap,
            Transform towers)
        {
            TowerPlacementController[] controllers = Object.FindObjectsByType<TowerPlacementController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TowerPlacementController controller in controllers)
            {
                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("placementGrid").objectReferenceValue = grid;
                serializedController.FindProperty("buildableTilemap").objectReferenceValue = buildableTilemap;
                serializedController.FindProperty("placementOverlayTilemap").objectReferenceValue = overlayTilemap;
                serializedController.FindProperty("towersRoot").objectReferenceValue = towers;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
