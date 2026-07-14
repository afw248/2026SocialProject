using System;
using System.Collections.Generic;
using ChangJun.Data;
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
            if (qty <= 0) return;
            _warehouse[code] = GetWarehouse(code) + qty;
            OnStockChanged?.Invoke();
        }

        public void AddStock(string code, int qty)
        {
            if (qty <= 0) return;
            _stock[code] = GetStock(code) + qty;
            OnStockChanged?.Invoke();
        }

        public void ReceiveDeliveries(out string summary)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pair in _warehouse)
            {
                if (pair.Value <= 0) continue;
                _stock[pair.Key] = GetStock(pair.Key) + pair.Value;
                if (_ingredientMap.TryGetValue(pair.Key, out var ing))
                    sb.AppendLine($"{ing.displayName} x{pair.Value}");
            }
            _warehouse.Clear();
            summary = sb.Length > 0 ? sb.ToString() : "배달된 재료가 없습니다.";
            OnStockChanged?.Invoke();
        }

        public int GetIngredientCost(string code) =>
            _ingredientMap.TryGetValue(code, out var ing) ? ing.purchasePrice : 0;

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
