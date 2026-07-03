using UnityEngine;

namespace TD.Map
{
    [CreateAssetMenu(fileName = "MapData", menuName = "TD/Map Data")]
    public class MapData : ScriptableObject
    {
        [SerializeField] private string mapId = "map_01";
        [SerializeField] private string displayName = "Map 01";
        [SerializeField] private MapRoot mapPrefab;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite pathSprite;

        public string MapId => mapId;
        public string DisplayName => displayName;
        public MapRoot MapPrefab => mapPrefab;
        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite PathSprite => pathSprite;
    }
}
