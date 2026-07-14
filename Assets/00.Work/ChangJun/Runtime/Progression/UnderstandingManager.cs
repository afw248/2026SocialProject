using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Judge;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Progression
{
    /// <summary>
    /// 문화별 이해도와 재료 해금을 관리한다.
    /// </summary>
    public sealed class UnderstandingManager : MonoBehaviour
    {
        public static UnderstandingManager Instance { get; private set; }

        private readonly Dictionary<CultureGroup, int> _values = new();
        private readonly HashSet<string> _unlockedCodes = new();
        private List<UnderstandingThresholdSO> _thresholds = new();
        private DayConfigSO _config;

        public event Action<CultureGroup, int> OnUnderstandingChanged;
        public event Action<IngredientSO> OnIngredientUnlocked;

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

        public void Initialize(IReadOnlyList<IngredientSO> ingredients,
            IReadOnlyList<UnderstandingThresholdSO> thresholds,
            DayConfigSO config)
        {
            _thresholds = new List<UnderstandingThresholdSO>(thresholds);
            _config = config ?? ScriptableObject.CreateInstance<DayConfigSO>();
            _values.Clear();
            _unlockedCodes.Clear();

            foreach (CultureGroup culture in Enum.GetValues(typeof(CultureGroup)))
            {
                _values[culture] = 0;
                TryUnlockIngredients(culture, 0);
            }

            foreach (var ing in ingredients)
            {
                if (ing == null || string.IsNullOrEmpty(ing.code)) continue;
                if (ing.isStarterUnlocked)
                    _unlockedCodes.Add(ing.code);
            }
        }

        public bool IsUnlocked(string code) => _unlockedCodes.Contains(code);

        public int GetUnderstanding(CultureGroup culture) =>
            _values.TryGetValue(culture, out var v) ? v : 0;

        public void HandleCraftResult(CraftResult result, CraftCustomerSO customer)
        {
            if (customer == null) return;
            var culture = customer.cultureGroup;
            if (culture == CultureGroup.None) return;

            int delta = result switch
            {
                CraftResult.Success => _config.understandingGainOnSuccess,
                CraftResult.WrongOrder => -_config.understandingLossOnWrongOrder,
                CraftResult.TabooViolation => -_config.understandingLossOnTaboo,
                CraftResult.WrongRecipe => -_config.understandingLossOnWrongRecipe,
                _ => 0,
            };

            if (delta == 0) return;
            ApplyDelta(culture, delta);
        }

        public bool AreMenuIngredientsUnlocked(MenuRecipeSO menu)
        {
            if (menu?.ingredientCodes == null) return false;
            foreach (var code in menu.ingredientCodes)
            {
                if (!_unlockedCodes.Contains(code)) return false;
            }
            return true;
        }

        private void ApplyDelta(CultureGroup culture, int delta)
        {
            int current = GetUnderstanding(culture);
            int next = Mathf.Clamp(current + delta, 0, 100);
            if (next == current) return;

            _values[culture] = next;
            OnUnderstandingChanged?.Invoke(culture, next);
            TryUnlockIngredients(culture, next);
        }

        private void TryUnlockIngredients(CultureGroup culture, int value)
        {
            foreach (var threshold in _thresholds)
            {
                if (threshold == null || threshold.ingredientToUnlock == null) continue;
                if (threshold.cultureGroup != culture) continue;
                if (value < threshold.threshold) continue;

                string code = threshold.ingredientToUnlock.code;
                if (_unlockedCodes.Add(code))
                    OnIngredientUnlocked?.Invoke(threshold.ingredientToUnlock);
            }
        }
    }
}
