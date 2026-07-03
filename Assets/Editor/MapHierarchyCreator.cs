using TD.Map;
using UnityEditor;
using UnityEngine;

namespace TD.Editor
{
    public static class MapHierarchyCreator
    {
        [MenuItem("GameObject/TD/Create Map Root", false, 10)]
        public static void CreateMapRoot(MenuCommand menuCommand)
        {
            GameObject map = new("Map");
            GameObjectUtility.SetParentAndAlign(map, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(map, "Create Map Root");

            MapRoot mapRoot = map.AddComponent<MapRoot>();

            Transform background = CreateChild(map.transform, "Background");
            SpriteRenderer backgroundRenderer = background.gameObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sortingOrder = -20;

            Transform path = CreateChild(map.transform, "Path");
            SpriteRenderer pathRenderer = path.gameObject.AddComponent<SpriteRenderer>();
            pathRenderer.sortingOrder = -10;

            Transform waypointsRoot = CreateChild(map.transform, "Waypoints");
            WaypointPath waypointPath = waypointsRoot.gameObject.AddComponent<WaypointPath>();
            CreateWaypoint(waypointsRoot, "Waypoint_0", new Vector3(-4f, 0f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_1", new Vector3(-1.5f, 1.5f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_2", new Vector3(1.5f, -1.5f, 0f));
            CreateWaypoint(waypointsRoot, "Waypoint_End", new Vector3(4f, 0f, 0f));
            waypointPath.RefreshFromChildren();

            Transform placementArea = CreateChild(map.transform, "TowerPlacementArea");
            TowerPlacementArea towerPlacementArea = placementArea.gameObject.AddComponent<TowerPlacementArea>();
            BoxCollider2D placementCollider = placementArea.gameObject.AddComponent<BoxCollider2D>();
            placementCollider.size = new Vector2(9f, 5f);

            CreateChild(map.transform, "Decorations");

            SerializedObject serializedMap = new(mapRoot);
            serializedMap.FindProperty("backgroundRoot").objectReferenceValue = background;
            serializedMap.FindProperty("pathRoot").objectReferenceValue = path;
            serializedMap.FindProperty("decorationsRoot").objectReferenceValue = map.transform.Find("Decorations");
            serializedMap.FindProperty("waypointPath").objectReferenceValue = waypointPath;
            serializedMap.FindProperty("spawnPoint").objectReferenceValue = waypointsRoot.GetChild(0);
            serializedMap.FindProperty("goalPoint").objectReferenceValue = waypointsRoot.GetChild(waypointsRoot.childCount - 1);
            serializedMap.FindProperty("towerPlacementArea").objectReferenceValue = towerPlacementArea;
            serializedMap.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedArea = new(towerPlacementArea);
            SerializedProperty placementColliders = serializedArea.FindProperty("placementColliders");
            placementColliders.arraySize = 1;
            placementColliders.GetArrayElementAtIndex(0).objectReferenceValue = placementCollider;
            serializedArea.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = map;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            GameObject child = new(childName);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            return child.transform;
        }

        private static void CreateWaypoint(Transform parent, string childName, Vector3 localPosition)
        {
            Transform waypoint = CreateChild(parent, childName);
            waypoint.localPosition = localPosition;
        }
    }
}
