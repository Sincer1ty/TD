using UnityEngine;

namespace TD.Tower
{
    [CreateAssetMenu(fileName = "TowerPlacementData", menuName = "TD/Tower Placement Data")]
    public class TowerPlacementData : ScriptableObject
    {
        [SerializeField] private string towerName = "Basic Tower";
        [SerializeField] private Tower towerPrefab;
        [SerializeField] private int cost = 100;
        [SerializeField] private Sprite icon;

        public string TowerName => towerName;
        public Tower TowerPrefab => towerPrefab;
        public int Cost => cost;
        public Sprite Icon => icon;
    }
}
