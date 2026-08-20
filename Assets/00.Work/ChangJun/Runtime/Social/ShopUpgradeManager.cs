using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 구매한 인증·업그레이드 보유 상태.
    /// </summary>
    public sealed class ShopUpgradeManager : MonoBehaviour
    {
        public static ShopUpgradeManager Instance { get; private set; }

        private readonly HashSet<ShopUpgradeType> _owned = new();
        private List<ShopUpgradeSO> _catalog = new();

        public IReadOnlyList<ShopUpgradeSO> Catalog => _catalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _catalog = new List<ShopUpgradeSO>(Resources.LoadAll<ShopUpgradeSO>("Craft/Upgrades"));
        }

        public bool Owns(ShopUpgradeType type) => _owned.Contains(type);

        public bool TryPurchase(ShopUpgradeSO upgrade)
        {
            if (upgrade == null || _owned.Contains(upgrade.upgradeType)) return false;
            if (MoneyManager.Instance.Money < upgrade.purchaseCost) return false;

            MoneyManager.Instance.SpendMoney(upgrade.purchaseCost);
            _owned.Add(upgrade.upgradeType);
            return true;
        }

        public float GetTabooPenaltyMultiplier()
        {
            float mult = 1f;
            foreach (var u in _catalog)
            {
                if (u != null && _owned.Contains(u.upgradeType))
                    mult -= u.tabooPenaltyReduction;
            }
            return Mathf.Max(0.5f, mult);
        }

        public float GetSpawnMultiplier(CultureGroup culture)
        {
            float boost = 1f;
            if (_owned.Contains(ShopUpgradeType.MulticultureBadge))
                boost += 0.05f;
            foreach (var u in _catalog)
            {
                if (u != null && _owned.Contains(u.upgradeType) && u.cultureGroup == culture)
                    boost += u.spawnBoost;
            }
            return boost;
        }
    }
}
