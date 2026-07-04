using UnityEngine;

namespace TD.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyName = "Normal Enemy";
        [SerializeField] private EnemyType enemyType = EnemyType.Normal;
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private int rewardGold = 5;
        [SerializeField] private int damageToBase = 1;
        [SerializeField] private bool isBoss;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description;

        public string EnemyName => enemyName;
        public EnemyType EnemyType => enemyType;
        public float MaxHp => Mathf.Max(1f, maxHp);
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        public int RewardGold => Mathf.Max(0, rewardGold);
        public int DamageToBase => Mathf.Max(0, damageToBase);
        public bool IsBoss => isBoss || enemyType == EnemyType.Boss;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public string Description => description;

        private void OnValidate()
        {
            maxHp = Mathf.Max(1f, maxHp);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            rewardGold = Mathf.Max(0, rewardGold);
            damageToBase = Mathf.Max(0, damageToBase);

            if (enemyType == EnemyType.Boss)
            {
                isBoss = true;
            }
        }
    }
}
