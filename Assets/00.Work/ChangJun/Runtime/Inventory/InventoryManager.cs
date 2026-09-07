using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using UnityEngine;

namespace ChangJun.Inventory
{
    /// <summary>
    /// 재료 재고·창고(배달 대기)를 관리한다.
    /// </summary>
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private readonly Dictionary<string, int> _stock = new();
        private readonly Dictionary<string, int> _warehouse = new();
        private Dictionary<string, IngredientSO> _ingredientMap = new();

        public event Action OnStockChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(IReadOnlyList<IngredientSO> ingredients, int starterQty)
        {
            _ingredientMap.Clear();
            _stock.Clear();
            _warehouse.Clear();

            foreach (var ing in ingredients)
            {
                if (ing == null || string.IsNullOrEmpty(ing.code)) continue;
                _ingredientMap[ing.code] = ing;
                if (ing.isStarterUnlocked)
                    _stock[ing.code] = starterQty;
            }

            OnStockChanged?.Invoke();
        }

        public int GetStock(string code) =>
            _stock.TryGetValue(code, out var qty) ? qty : 0;

        public int GetWarehouse(string code) =>
            _warehouse.TryGetValue(code, out var qty) ? qty : 0;

        /// <summary>해금된 재료 중 재고가 하나라도 있으면 true.</summary>
        public bool HasAnyUsableStock(System.Func<string, bool> isUnlocked = null)
        {
            foreach (var pair in _stock)
            {
                if (pair.Value <= 0) continue;
                if (isUnlocked != null && !isUnlocked(pair.Key)) continue;
                return true;
            }
            return false;
        }

        public bool HasStockForMenu(MenuRecipeSO menu)
        {
            if (menu?.ingredientCodes == null) return false;
            foreach (var code in menu.ingredientCodes)
            {
                if (GetStock(code) <= 0) return false;
            }
            return true;
        }

        public bool TryConsume(MenuRecipeSO menu)
        {
            if (!HasStockForMenu(menu)) return false;

            foreach (var code in menu.ingredientCodes)
                ReduceStock(code, 1);

            return true;
        }

        public void ReduceStock(string code, int amount)
        {
            if (amount <= 0) return;
            int current = GetStock(code);
            _stock[code] = Mathf.Max(0, current - amount);
            OnStockChanged?.Invoke();
        }

        public void PurchaseToWarehouse(string code, int qty)
        {
            qty = EconomyClamp.ClampOrderQuantity(qty);
            if (qty <= 0) return;
            _warehouse[code] = EconomyClamp.SafeAddInventory(GetWarehouse(code), qty);
            OnStockChanged?.Invoke();
        }

        public void AddStock(string code, int qty)
        {
            if (qty <= 0) return;
            qty = EconomyClamp.ClampInventoryQuantity(qty);
            _stock[code] = EconomyClamp.SafeAddInventory(GetStock(code), qty);
            OnStockChanged?.Invoke();
        }

        public void ReceiveDeliveries(out string summary)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pair in _warehouse)
            {
                if (pair.Value <= 0) continue;
                _stock[pair.Key] = EconomyClamp.SafeAddInventory(GetStock(pair.Key), pair.Value);
                if (_ingredientMap.TryGetValue(pair.Key, out var ing))
                    sb.AppendLine($"{ing.displayName} x{pair.Value}");
            }
            _warehouse.Clear();
            summary = sb.Length > 0 ? sb.ToString() : "배달된 재료가 없습니다.";
            OnStockChanged?.Invoke();
        }

        public int GetIngredientCost(string code) =>
            _ingredientMap.TryGetValue(code, out var ing) ? GetEffectivePurchasePrice(ing) : 0;

        public int EstimateMenuIngredientCost(MenuRecipeSO menu)
        {
            if (menu?.ingredientCodes == null) return 0;
            int total = 0;
            foreach (var code in menu.ingredientCodes)
                total += GetIngredientCost(code);
            return total;
        }

        public float IngredientPriceIndex { get; private set; } = 1f;

        public void ApplyInflation(float rate)
        {
            IngredientPriceIndex *= 1f + rate;
        }

        public int GetEffectivePurchasePrice(IngredientSO ing)
        {
            if (ing == null) return 0;
            float price = ing.purchasePrice * IngredientPriceIndex;
            if (ing.isLocalSourced) price *= 0.9f;
            if (ing.isFairTrade) price *= 1.2f;
            return Mathf.Max(1, Mathf.RoundToInt(price));
        }

        public int DonateAllWarehouse(out int units)
        {
            units = 0;
            foreach (var pair in _warehouse)
                units += pair.Value;
            _warehouse.Clear();
            OnStockChanged?.Invoke();
            return units;
        }

        public IngredientSO GetIngredient(string code) =>
            _ingredientMap.TryGetValue(code, out var ing) ? ing : null;

        public IReadOnlyList<IngredientSO> GetAllIngredients()
        {
            var list = new List<IngredientSO>(_ingredientMap.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.code, b.code));
            return list;
        }
    }
}
