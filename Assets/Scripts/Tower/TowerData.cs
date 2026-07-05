using UnityEngine;

namespace TD.Tower
{
    [CreateAssetMenu(fileName = "TowerData", menuName = "TD/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [SerializeField] private string towerName = "Swordsman";
        [SerializeField] private TowerType towerType = TowerType.Swordsman;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private AttackMode attackMode = AttackMode.Melee;
        [SerializeField] private TargetPriority targetPriority = TargetPriority.Nearest;
        [SerializeField] private int cost = 100;
        [SerializeField] private int upgradeCost = 75;
        [SerializeField] private Tower prefab;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description;

        [Header("Mode Tuning")]
        [SerializeField] private float areaRadius = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float slowPercent = 0.4f;
        [SerializeField] private float slowDuration = 1.5f;

        public string TowerName => towerName;
        public TowerType TowerType => towerType;
        public float Damage => Mathf.Max(0f, damage);
        public float AttackSpeed => Mathf.Max(0f, attackSpeed);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public AttackMode AttackMode => attackMode;
        public TargetPriority TargetPriority => targetPriority;
        public int Cost => Mathf.Max(0, cost);
        public int UpgradeCost => Mathf.Max(0, upgradeCost);
        public Tower Prefab => prefab;
        public GameObject PreviewPrefab => previewPrefab;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
        public float ProjectileLifetime => Mathf.Max(0.01f, projectileLifetime);
        public Sprite Icon => icon;
        public string Description => description;
        public float AreaRadius => Mathf.Max(0f, areaRadius);
        public float SlowPercent => Mathf.Clamp01(slowPercent);
        public float SlowDuration => Mathf.Max(0f, slowDuration);

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            attackSpeed = Mathf.Max(0f, attackSpeed);
            attackRange = Mathf.Max(0f, attackRange);
            cost = Mathf.Max(0, cost);
            upgradeCost = Mathf.Max(0, upgradeCost);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
            areaRadius = Mathf.Max(0f, areaRadius);
            slowPercent = Mathf.Clamp01(slowPercent);
            slowDuration = Mathf.Max(0f, slowDuration);
        }
    }
}
