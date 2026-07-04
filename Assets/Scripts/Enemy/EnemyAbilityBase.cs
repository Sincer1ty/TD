using UnityEngine;

namespace TD.Enemy
{
    public abstract class EnemyAbilityBase : MonoBehaviour
    {
        protected EnemyController Owner { get; private set; }
        protected EnemyData Data => Owner != null ? Owner.Data : null;

        public virtual void Initialize(EnemyController owner)
        {
            Owner = owner;
        }
    }
}
