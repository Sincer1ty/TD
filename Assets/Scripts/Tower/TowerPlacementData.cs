using UnityEngine;

namespace TD.Tower
{
    [CreateAssetMenu(fileName = "TowerPlacementData", menuName = "TD/Tower Placement Data")]
    public class TowerPlacementData : ScriptableObject
    {
        [SerializeField] private TowerData towerData;
        [SerializeField] private string towerName = "Basic Tower";
        [SerializeField] private Tower towerPrefab;
        [SerializeField] private int cost = 100;
        [SerializeField] private Sprite icon;

        public TowerData TowerData => towerData;
        public string TowerName => towerData != null ? towerData.TowerName : towerName;
        public Tower TowerPrefab => towerData != null ? towerData.Prefab : towerPrefab;
        public int Cost => towerData != null ? towerData.Cost : cost;
        public Sprite Icon => towerData != null ? towerData.Icon : icon;
    }
}
