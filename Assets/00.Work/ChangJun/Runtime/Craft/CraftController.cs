using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Judge;
using ChangJun.Progression;
using ChangJun.Social;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Craft
{
    /// <summary>
    /// 재료 선택 → 판정 → 결과 이벤트 발행을 담당한다.
    /// </summary>
    public sealed class CraftController : MonoBehaviour
    {
        [SerializeField] private int _maxSlots = 3;
        [SerializeField] private int _tabooPenalty = 100;

        private readonly List<IngredientSO> _selected = new();
        private IRecipeMatcher _matcher;
        private CraftCustomerSO _currentCustomer;
        private IReadOnlyList<IngredientSO> _allIngredients;

        public event Action<IReadOnlyList<IngredientSO>> OnSelectionChanged;
        public event Action<CraftResult, MenuRecipeSO> OnCraftJudged;

        public CraftCustomerSO CurrentCustomer => _currentCustomer;
        public bool CanCraft => _phaseAllowsCraft;
        public bool BargainAccepted { get; set; }
        private bool _phaseAllowsCraft;

        public void Initialize(IRecipeMatcher matcher, CraftCustomerSO customer,
            IReadOnlyList<IngredientSO> allIngredients)
        {
            _matcher = matcher;
            _currentCustomer = customer;
            _allIngredients = allIngredients;
            BargainAccepted = false;
            _selected.Clear();
            OnSelectionChanged?.Invoke(_selected);
        }

        public void SetCraftEnabled(bool enabled) => _phaseAllowsCraft = enabled;

        public void SelectIngredient(IngredientSO ingredient)
        {
            if (!_phaseAllowsCraft) return;
            if (!UnderstandingManager.Instance.IsUnlocked(ingredient.code))
            {
                Debug.Log($"[CraftController] 미해금 재료: {ingredient.code}");
                return;
            }
            if (InventoryManager.Instance.GetStock(ingredient.code) <= 0)
            {
                Debug.Log($"[CraftController] 재고 없음: {ingredient.code}");
                return;
            }
            foreach (var ing in _selected)
            {
                if (ing.code == ingredient.code)
                {
                    DeselectIngredient(ingredient);
                    return;
                }
            }

            if (_selected.Count >= _maxSlots) return;

            _selected.Add(ingredient);
            OnSelectionChanged?.Invoke(_selected);
        }

        public void DeselectIngredient(IngredientSO ingredient)
        {
            if (!_phaseAllowsCraft) return;
            _selected.Remove(ingredient);
            OnSelectionChanged?.Invoke(_selected);
        }

        public void ClearSelection()
        {
            _selected.Clear();
            OnSelectionChanged?.Invoke(_selected);
        }

        public void Craft()
        {
            if (!_phaseAllowsCraft) return;
            if (_matcher == null || _currentCustomer == null) return;

            var result = RecipeJudge.Judge(_selected, _currentCustomer, _matcher, out var matched);
            ApplyOutcome(result, matched);
            OnCraftJudged?.Invoke(result, matched);
            ClearSelection();
        }

        private void ApplyOutcome(CraftResult result, MenuRecipeSO matched)
        {
            var ledger = DayLoopController.Instance.Ledger;
            var money = MoneyManager.Instance;

            switch (result)
            {
                case CraftResult.Success:
                {
                    int price = matched.price;
                    if (News.NewsManager.Instance != null)
                        price = News.NewsManager.Instance.ApplyPriceMultiplier(matched, price);
                    if (Delivery.DeliveryManager.Instance != null)
                        price = Delivery.DeliveryManager.Instance.ApplyFreshnessMultiplier(price);
                    if (CulturalEventManager.Instance != null)
                    {
                        if (CulturalEventManager.Instance.TodayEvent == ActiveCulturalEvent.CultureFestival
                            && matched.cultureGroup == CulturalEventManager.Instance.FestivalCulture)
                            price = Mathf.RoundToInt(price * CulturalEventManager.Instance.FestivalPriceMultiplier);
                        price = Mathf.RoundToInt(price * CulturalEventManager.Instance.GetCulturePriceBuff(matched.cultureGroup));
                    }
                    if (HasFairTradeIngredient(matched))
                        price = Mathf.RoundToInt(price * 1.08f);
                    if (BargainAccepted && _currentCustomer != null && _currentCustomer.canBargain)
                        price = Mathf.RoundToInt(price * (1f - Mathf.Clamp01(_currentCustomer.bargainDiscount)));

                    money.AddMoney(price);
                    ledger.AddRevenue(price, matched.displayName);

                    int cogs = InventoryManager.Instance.EstimateMenuIngredientCost(matched);
                    if (cogs > 0)
                        ledger.AddIngredientCost(cogs, $"{matched.displayName} 재료원가");

                    InventoryManager.Instance.TryConsume(matched);
                    break;
                }
                case CraftResult.TabooViolation:
                {
                    int penalty = Mathf.RoundToInt(_tabooPenalty *
                        (ShopUpgradeManager.Instance?.GetTabooPenaltyMultiplier(_currentCustomer.diet) ?? 1f));
                    money.SpendMoney(penalty);
                    ledger.AddPenalty(penalty, "금기위반 환불");
                    break;
                }
                case CraftResult.WrongOrder:
                    ledger.AddPenalty(0, "오주문 (수익 없음)");
                    break;
                case CraftResult.WrongRecipe:
                    int waste = EstimateWasteCost();
                    if (waste > 0)
                    {
                        money.SpendMoney(waste);
                        ledger.AddIngredientCost(waste, "오조리 재료 손실");
                    }
                    break;
            }

            UnderstandingManager.Instance.HandleCraftResult(result, _currentCustomer);
            RegularCustomerService.Instance?.RecordResult(_currentCustomer, result);
            DayLoopController.Instance.AdvanceAfterCustomer();
        }

        private static bool HasFairTradeIngredient(MenuRecipeSO menu)
        {
            if (menu?.ingredientCodes == null) return false;
            foreach (var code in menu.ingredientCodes)
            {
                var ing = InventoryManager.Instance.GetIngredient(code);
                if (ing != null && ing.isFairTrade) return true;
            }
            return false;
        }

        private int EstimateWasteCost()
        {
            int total = 0;
            foreach (var ing in _selected)
                total += InventoryManager.Instance.GetIngredientCost(ing.code);
            return total;
        }
    }
}
