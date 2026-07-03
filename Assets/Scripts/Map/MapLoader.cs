using UnityEngine;

namespace TD.Map
{
    public class MapLoader : MonoBehaviour
    {
        [SerializeField] private MapData mapData;
        [SerializeField] private Transform mapParent;
        [SerializeField] private bool loadOnAwake = true;

        private MapRoot loadedMap;

        public MapRoot LoadedMap => loadedMap;

        private void Awake()
        {
            if (loadOnAwake)
            {
                LoadMap();
            }
        }

        public MapRoot LoadMap()
        {
            if (mapData == null || mapData.MapPrefab == null)
            {
                return null;
            }

            if (loadedMap != null)
            {
                Destroy(loadedMap.gameObject);
            }

            Transform parent = mapParent != null ? mapParent : transform;
            loadedMap = Instantiate(mapData.MapPrefab, parent);
            loadedMap.name = mapData.DisplayName;
            ApplyMapSprites(loadedMap);

            return loadedMap;
        }

        private void ApplyMapSprites(MapRoot mapRoot)
        {
            if (mapRoot == null || mapData == null)
            {
                return;
            }

            SetSprite(mapRoot.BackgroundRoot, mapData.BackgroundSprite);
            SetSprite(mapRoot.PathRoot, mapData.PathSprite);
        }

        private static void SetSprite(Transform root, Sprite sprite)
        {
            if (root == null || sprite == null)
            {
                return;
            }

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprite;
            }
        }
    }
}
