using System.Collections.Generic;
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
        [SerializeField] private int sellRefundGold = -1;
        [Range(0f, 1f)]
        [SerializeField] private float sellRefundRate = 0.5f;
        [SerializeField] private Tower prefab;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description;

        [Header("Mode Tuning")]
        [SerializeField] private GameObject areaEffectPrefab;
        [SerializeField] private float areaEffectScaleMultiplier = 1f;
        [SerializeField] private float areaEffectLifetime = 1.5f;
        [SerializeField] private float areaEffectZOffset = -0.1f;
        [SerializeField] private string areaEffectSortingLayerName = "Default";
        [SerializeField] private int areaEffectSortingOrder = 20;
        [Range(0f, 1f)]
        [SerializeField] private float slowPercent = 0.4f;
        [SerializeField] private float slowDuration = 1.5f;
        [SerializeField] private List<TowerLevelStats> levelStats = new List<TowerLevelStats>();

        public string TowerName => towerName;
        public TowerType TowerType => towerType;
        public float Damage => Mathf.Max(0f, damage);
        public float AttackSpeed => Mathf.Max(0f, attackSpeed);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public AttackMode AttackMode => attackMode;
        public TargetPriority TargetPriority => targetPriority;
        public int Cost => Mathf.Max(0, cost);
        public int UpgradeCost => Mathf.Max(0, upgradeCost);
        public int SellRefundGold => sellRefundGold >= 0
            ? sellRefundGold
            : Mathf.RoundToInt(Cost * Mathf.Clamp01(sellRefundRate));
        public Tower Prefab => prefab;
        public GameObject PreviewPrefab => previewPrefab;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
        public float ProjectileLifetime => Mathf.Max(0.01f, projectileLifetime);
        public Sprite Icon => icon;
        public string Description => description;
        public GameObject AreaEffectPrefab => areaEffectPrefab;
        public float AreaEffectScaleMultiplier => Mathf.Max(0.01f, areaEffectScaleMultiplier);
        public float AreaEffectLifetime => Mathf.Max(0.01f, areaEffectLifetime);
        public float AreaEffectZOffset => areaEffectZOffset;
        public string AreaEffectSortingLayerName => areaEffectSortingLayerName;
        public int AreaEffectSortingOrder => areaEffectSortingOrder;
        public float SlowPercent => Mathf.Clamp01(slowPercent);
        public float SlowDuration => Mathf.Max(0f, slowDuration);
        public IReadOnlyList<TowerLevelStats> LevelStats => levelStats;
        public int MaxLevel => Mathf.Max(1, GetMaxConfiguredLevel());

        public TowerLevelStats GetStatsForLevel(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            TowerLevelStats bestStats = null;

            if (levelStats != null)
            {
                for (int i = 0; i < levelStats.Count; i++)
                {
                    TowerLevelStats stats = levelStats[i];
                    if (stats == null)
                    {
                        continue;
                    }

                    if (stats.Level == safeLevel)
                    {
                        return stats;
                    }

                    if (stats.Level < safeLevel && (bestStats == null || stats.Level > bestStats.Level))
                    {
                        bestStats = stats;
                    }
                }
            }

            return bestStats ?? new TowerLevelStats(1, Damage, AttackRange, AttackSpeed, UpgradeCost);
        }

        public int GetUpgradeCostForNextLevel(int currentLevel)
        {
            int nextLevel = currentLevel + 1;
            if (nextLevel > MaxLevel)
            {
                return 0;
            }

            return GetStatsForLevel(nextLevel).UpgradeCost;
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            attackSpeed = Mathf.Max(0f, attackSpeed);
            attackRange = Mathf.Max(0f, attackRange);
            cost = Mathf.Max(0, cost);
            upgradeCost = Mathf.Max(0, upgradeCost);
            sellRefundGold = Mathf.Max(-1, sellRefundGold);
            sellRefundRate = Mathf.Clamp01(sellRefundRate);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
            areaEffectScaleMultiplier = Mathf.Max(0.01f, areaEffectScaleMultiplier);
            areaEffectLifetime = Mathf.Max(0.01f, areaEffectLifetime);
            slowPercent = Mathf.Clamp01(slowPercent);
            slowDuration = Mathf.Max(0f, slowDuration);

            if (levelStats == null)
            {
                levelStats = new List<TowerLevelStats>();
            }
        }

        private int GetMaxConfiguredLevel()
        {
            int maxLevel = 1;
            if (levelStats == null || levelStats.Count == 0)
            {
                return maxLevel;
            }

            for (int i = 0; i < levelStats.Count; i++)
            {
                TowerLevelStats stats = levelStats[i];
                if (stats != null)
                {
                    maxLevel = Mathf.Max(maxLevel, stats.Level);
                }
            }

            return maxLevel;
        }
    }
}
