using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Judge;
using ChangJun.Social;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Progression
{
    /// <summary>
    /// 문화별 이해도, 스킬트리 노드, 재료 해금을 관리한다.
    /// </summary>
    public sealed class UnderstandingManager : MonoBehaviour
    {
        public static UnderstandingManager Instance { get; private set; }

        private readonly Dictionary<CultureGroup, int> _values = new();
        private readonly HashSet<string> _unlockedCodes = new();
        private readonly HashSet<string> _unlockedNodeIds = new();
        private List<UnderstandingThresholdSO> _thresholds = new();
        private List<UnderstandingNodeSO> _nodes = new();
        private DayConfigSO _config;

        public event Action<CultureGroup, int> OnUnderstandingChanged;
        public event Action<IngredientSO> OnIngredientUnlocked;
        public event Action<UnderstandingNodeSO> OnNodeUnlocked;

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
            _nodes = new List<UnderstandingNodeSO>(
                Resources.LoadAll<UnderstandingNodeSO>("Craft/UnderstandingNodes"));
            _config = config ?? ScriptableObject.CreateInstance<DayConfigSO>();
            _values.Clear();
            _unlockedCodes.Clear();
            _unlockedNodeIds.Clear();

            foreach (CultureGroup culture in Enum.GetValues(typeof(CultureGroup)))
            {
                _values[culture] = 0;
                TryUnlockIngredients(culture, 0);
                TryUnlockNodes(culture, 0);
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

        public IReadOnlyList<UnderstandingNodeSO> GetNodesForCulture(CultureGroup culture)
        {
            var list = new List<UnderstandingNodeSO>();
            foreach (var n in _nodes)
            {
                if (n != null && n.cultureGroup == culture)
                    list.Add(n);
            }
            return list;
        }

        public UnderstandingNodeState GetNodeState(UnderstandingNodeSO node)
        {
            if (node == null) return UnderstandingNodeState.Locked;
            if (_unlockedNodeIds.Contains(node.nodeId))
                return UnderstandingNodeState.Unlocked;

            if (!ArePrerequisitesMet(node))
                return UnderstandingNodeState.Locked;

            int current = GetUnderstanding(node.cultureGroup);
            if (current >= node.requiredUnderstanding)
                return UnderstandingNodeState.Unlocked;
            if (current > 0)
                return UnderstandingNodeState.InProgress;
            return UnderstandingNodeState.Locked;
        }

        public void HandleCraftResult(CraftResult result, CraftCustomerSO customer)
        {
            if (customer == null) return;
            var culture = customer.cultureGroup;
            if (culture == CultureGroup.None) return;

            bool success = result == CraftResult.Success;
            bool taboo = result == CraftResult.TabooViolation;
            StoreReputationService.Instance?.RecordOrder(success, taboo);
            if (success)
                SchoolLunchContractService.Instance?.RecordSuccess();

            int delta = result switch
            {
                CraftResult.Success => _config.understandingGainOnSuccess,
                CraftResult.WrongOrder => -_config.understandingLossOnWrongOrder,
                CraftResult.TabooViolation => -_config.understandingLossOnTaboo,
                CraftResult.WrongRecipe => -_config.understandingLossOnWrongRecipe,
                _ => 0,
            };

            if (delta > 0)
                delta = StaffManager.Instance?.ScaleUnderstandingGain(culture, delta) ?? delta;
            if (success)
                delta += ShopUpgradeManager.Instance?.GetUnderstandingBonus(culture, customer.diet) ?? 0;
            if (success && customer.needsAccessibleService)
                delta += 2;

            if (delta == 0) return;
            ApplyDelta(culture, delta);
        }

        public void ApplyExternalDelta(CultureGroup culture, int delta)
        {
            if (culture == CultureGroup.None || delta == 0) return;
            ApplyDelta(culture, delta);
        }

        public bool AreMenuIngredientsUnlocked(MenuRecipeSO menu)
        {
            if (menu == null) return false;
            if (menu.requiresFusionUnlock
                && CulturalEventManager.Instance != null
                && !CulturalEventManager.Instance.IsFusionMenuUnlocked(menu.code))
                return false;

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
            TryUnlockNodes(culture, next);
            CulturalEventManager.Instance?.CheckMilestones(culture, next);
        }

        private bool ArePrerequisitesMet(UnderstandingNodeSO node)
        {
            if (node.prerequisiteNodeIds == null || node.prerequisiteNodeIds.Length == 0)
                return true;
            foreach (var id in node.prerequisiteNodeIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!_unlockedNodeIds.Contains(id))
                    return false;
            }
            return true;
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

        private void TryUnlockNodes(CultureGroup culture, int value)
        {
            foreach (var node in _nodes)
            {
                if (node == null || node.cultureGroup != culture) continue;
                if (_unlockedNodeIds.Contains(node.nodeId)) continue;
                if (value < node.requiredUnderstanding) continue;
                if (!ArePrerequisitesMet(node)) continue;

                _unlockedNodeIds.Add(node.nodeId);
                if (node.ingredientToUnlock != null)
                {
                    string code = node.ingredientToUnlock.code;
                    if (_unlockedCodes.Add(code))
                        OnIngredientUnlocked?.Invoke(node.ingredientToUnlock);
                }
                OnNodeUnlocked?.Invoke(node);
            }
        }
    }
}
