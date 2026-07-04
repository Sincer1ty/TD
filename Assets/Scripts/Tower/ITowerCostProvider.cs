namespace TD.Tower
{
    public interface ITowerCostProvider
    {
        bool CanAfford(int cost);
        bool SpendCost(int cost);
    }
}
