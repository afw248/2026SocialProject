using ChangJun.Data;

namespace ChangJun.Social
{
    public interface ICustomerSpawnModifier
    {
        float GetSpawnWeight(CraftCustomerSO customer);
    }
}
