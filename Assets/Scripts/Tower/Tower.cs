using UnityEngine;

namespace TD.Tower
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerPlacementData placementData;

        public TowerPlacementData PlacementData => placementData;

        public void Initialize(TowerPlacementData data)
        {
            placementData = data;
        }
    }
}
