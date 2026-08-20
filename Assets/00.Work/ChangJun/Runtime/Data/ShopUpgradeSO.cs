using UnityEngine;

namespace ChangJun.Data
{
    public enum ShopUpgradeType
    {
        HalalKitchen,
        VeganZone,
        MulticultureBadge,
    }

    [CreateAssetMenu(fileName = "Upgrade_", menuName = "CupRice/Data/ShopUpgrade")]
    public class ShopUpgradeSO : ScriptableObject
    {
        public ShopUpgradeType upgradeType;
        public string displayName;
        [TextArea(1, 2)] public string description;
        public int purchaseCost = 500;
        public CultureGroup cultureGroup;
        public float tabooPenaltyReduction = 0.2f;
        public float spawnBoost = 0.1f;
    }
}
