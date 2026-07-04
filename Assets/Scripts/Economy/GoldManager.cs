using TD.Tower;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Economy
{
    public class GoldManager : MonoBehaviour, ITowerCostProvider
    {
        [SerializeField] private int startingGold = 150;
        [SerializeField] private int currentGold;
        [SerializeField] private UnityEvent<int> onGoldChanged = new UnityEvent<int>();
        [SerializeField] private UnityEvent<int> onGoldAdded = new UnityEvent<int>();
        [SerializeField] private UnityEvent<int> onGoldSpent = new UnityEvent<int>();
        [SerializeField] private UnityEvent<int> onInsufficientGold = new UnityEvent<int>();

        public int CurrentGold => currentGold;
        public UnityEvent<int> OnGoldChanged => onGoldChanged;

        private void Awake()
        {
            currentGold = Mathf.Max(0, startingGold);
            NotifyGoldChanged();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentGold += amount;
            onGoldAdded?.Invoke(amount);
            NotifyGoldChanged();
        }

        public bool CanSpend(int amount)
        {
            return amount <= 0 || currentGold >= amount;
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (!CanSpend(amount))
            {
                onInsufficientGold?.Invoke(amount);
                return false;
            }

            currentGold -= amount;
            onGoldSpent?.Invoke(amount);
            NotifyGoldChanged();
            return true;
        }

        public bool CanAfford(int cost)
        {
            return CanSpend(cost);
        }

        public bool SpendCost(int cost)
        {
            return SpendGold(cost);
        }

        private void NotifyGoldChanged()
        {
            onGoldChanged?.Invoke(currentGold);
        }
    }
}
