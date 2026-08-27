using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 구매한 인증·업그레이드. 보유와 사용(장착)을 분리해 끄고 켤 수 있다.
    /// </summary>
    public sealed class ShopUpgradeManager : MonoBehaviour
    {
        public static ShopUpgradeManager Instance { get; private set; }

        private readonly HashSet<ShopUpgradeType> _owned = new();
        private readonly HashSet<ShopUpgradeType> _equipped = new();
        private List<ShopUpgradeSO> _catalog = new();

        public IReadOnlyList<ShopUpgradeSO> Catalog => _catalog;

        public event Action Changed;

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
        public bool IsEquipped(ShopUpgradeType type) => _equipped.Contains(type);

        public bool TryPurchase(ShopUpgradeSO upgrade)
        {
            if (upgrade == null || _owned.Contains(upgrade.upgradeType)) return false;
            if (MoneyManager.Instance == null || MoneyManager.Instance.Money < upgrade.purchaseCost)
                return false;

            MoneyManager.Instance.SpendMoney(upgrade.purchaseCost);
            _owned.Add(upgrade.upgradeType);
            _equipped.Add(upgrade.upgradeType);
            Changed?.Invoke();
            return true;
        }

        /// <summary>이미 산 인증을 사용/미사용으로 토글한다. 환불은 없다.</summary>
        public bool ToggleEquipped(ShopUpgradeType type)
        {
            if (!_owned.Contains(type)) return false;
            if (!_equipped.Add(type))
                _equipped.Remove(type);
            Changed?.Invoke();
            return true;
        }

        public float GetTabooPenaltyMultiplier(Diet diet = Diet.None)
        {
            float mult = 1f;
            foreach (var u in _catalog)
            {
                if (u == null || !_equipped.Contains(u.upgradeType)) continue;
                if (u.upgradeType == ShopUpgradeType.HalalKitchen && diet != Diet.None && (diet & Diet.Halal) == 0)
                    continue;
                if (u.upgradeType == ShopUpgradeType.VeganZone && diet != Diet.None && (diet & Diet.Vegan) == 0)
                    continue;
                mult -= u.tabooPenaltyReduction;
            }
            return Mathf.Max(0.4f, mult);
        }

        public float GetSpawnMultiplier(CultureGroup culture)
        {
            float boost = 1f;
            if (_equipped.Contains(ShopUpgradeType.MulticultureBadge))
                boost += 0.08f;
            foreach (var u in _catalog)
            {
                if (u != null && _equipped.Contains(u.upgradeType) && u.cultureGroup == culture)
                    boost += u.spawnBoost;
            }
            return boost;
        }

        public int GetUnderstandingBonus(CultureGroup culture, Diet diet)
        {
            int bonus = 0;
            if (_equipped.Contains(ShopUpgradeType.HalalKitchen) &&
                (culture == CultureGroup.Muslim || diet == Diet.Halal))
                bonus += 2;
            if (_equipped.Contains(ShopUpgradeType.VeganZone) &&
                (culture == CultureGroup.Vegan || diet == Diet.Vegan))
                bonus += 2;
            if (_equipped.Contains(ShopUpgradeType.MulticultureBadge))
                bonus += 1;
            return bonus;
        }
    }
}
