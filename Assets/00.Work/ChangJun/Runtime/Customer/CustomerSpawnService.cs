using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Progression;
using UnityEngine;

namespace ChangJun.Customer
{
    /// <summary>
    /// 해금·재고 조건을 만족하는 손님만 스폰 후보에 넣는다.
    /// </summary>
    public static class CustomerSpawnService
    {
        public static bool TryPick(
            IReadOnlyList<CraftCustomerSO> pool,
            CraftCustomerSO lastCustomer,
            out CraftCustomerSO picked)
        {
            picked = null;
            if (pool == null || pool.Count == 0) return false;

            var candidates = new List<CraftCustomerSO>();
            foreach (var c in pool)
            {
                if (c == null || c.requiredMenu == null) continue;
                if (!IsSpawnable(c)) continue;
                if (c == lastCustomer && pool.Count > 1) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                foreach (var c in pool)
                {
                    if (c == null || c == lastCustomer) continue;
                    if (IsSpawnableRelaxed(c))
                        candidates.Add(c);
                }
            }

            if (candidates.Count == 0) return false;

            picked = PickWeighted(candidates);
            return picked != null;
        }

        private static CraftCustomerSO PickWeighted(List<CraftCustomerSO> candidates)
        {
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                weights[i] = CustomerSpawnWeightService.GetWeight(candidates[i]);
                total += weights[i];
            }

            if (total <= 0f)
                return candidates[Random.Range(0, candidates.Count)];

            float roll = Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private static bool IsSpawnable(CraftCustomerSO customer)
        {
            var menu = customer.requiredMenu;
            if (!UnderstandingManager.Instance.AreMenuIngredientsUnlocked(menu)) return false;
            if (!InventoryManager.Instance.HasStockForMenu(menu)) return false;
            return true;
        }

        private static bool IsSpawnableRelaxed(CraftCustomerSO customer)
        {
            return UnderstandingManager.Instance.AreMenuIngredientsUnlocked(customer.requiredMenu);
        }
    }
}
