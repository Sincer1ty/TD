using System;
using UnityEngine;

namespace TD.Tower
{
    [Serializable]
    public class TowerLevelStats
    {
        [SerializeField] private int level = 1;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private int upgradeCost;

        public int Level => Mathf.Max(1, level);
        public float Damage => Mathf.Max(0f, damage);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public float AttackSpeed => Mathf.Max(0f, attackSpeed);
        public int UpgradeCost => Mathf.Max(0, upgradeCost);

        public TowerLevelStats()
        {
        }

        public TowerLevelStats(int level, float damage, float attackRange, float attackSpeed, int upgradeCost)
        {
            this.level = Mathf.Max(1, level);
            this.damage = Mathf.Max(0f, damage);
            this.attackRange = Mathf.Max(0f, attackRange);
            this.attackSpeed = Mathf.Max(0f, attackSpeed);
            this.upgradeCost = Mathf.Max(0, upgradeCost);
        }
    }
}
