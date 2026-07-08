using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Judge;
using UnityEngine;

namespace ChangJun.Craft
{
    /// <summary>
    /// 재료 선택 → 판정 → 결과 이벤트 발행을 담당한다.
    /// RecipeJudge (순수 로직) 와 MoneyManager (경제) 에만 의존하며
    /// UI 는 이 클래스의 이벤트를 구독해 화면을 갱신한다 (단방향 의존).
    /// </summary>
    public sealed class CraftController : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("한 번에 선택 가능한 최대 재료 수")]
        [SerializeField] private int _maxSlots = 3;

        [Tooltip("금기 위반 패널티 (환불 금액)")]
        [SerializeField] private int _tabooPenalty = 100;

        // 현재 선택된 재료 목록
        private readonly List<IngredientSO> _selected = new();

        private IRecipeMatcher _matcher;
        private CraftCustomerSO _currentCustomer;

        // ── 이벤트 ──────────────────────────────────────────────
        /// <summary>재료 선택 상태 변경 (현재 목록)</summary>
        public event Action<IReadOnlyList<IngredientSO>> OnSelectionChanged;

        /// <summary>판정 완료 (결과, 일치된 메뉴 or null)</summary>
        public event Action<CraftResult, MenuRecipeSO> OnCraftJudged;

        // ── 초기화 ──────────────────────────────────────────────
        /// <summary>씬 시작 시 RecipeBook 과 현재 손님을 주입한다</summary>
        public void Initialize(IRecipeMatcher matcher, CraftCustomerSO customer)
        {
            _matcher = matcher;
            _currentCustomer = customer;
            _selected.Clear();
            OnSelectionChanged?.Invoke(_selected);
        }

        // ── 재료 선택 / 해제 ────────────────────────────────────
        /// <summary>
        /// 재료를 선택 슬롯에 추가한다.
        /// 이미 같은 코드가 있거나 슬롯이 가득 차면 무시한다.
        /// </summary>
        public void SelectIngredient(IngredientSO ingredient)
        {
            if (_selected.Count >= _maxSlots)
            {
                Debug.Log($"[CraftController] 슬롯이 가득 찼습니다 (최대 {_maxSlots}개).");
                return;
            }

            foreach (var ing in _selected)
            {
                if (ing.code == ingredient.code)
                {
                    Debug.Log($"[CraftController] 이미 선택된 재료입니다: {ingredient.code}");
                    return;
                }
            }

            _selected.Add(ingredient);
            OnSelectionChanged?.Invoke(_selected);
        }

        /// <summary>선택된 재료를 슬롯에서 제거한다</summary>
        public void DeselectIngredient(IngredientSO ingredient)
        {
            _selected.Remove(ingredient);
            OnSelectionChanged?.Invoke(_selected);
        }

        /// <summary>선택 슬롯을 모두 비운다</summary>
        public void ClearSelection()
        {
            _selected.Clear();
            OnSelectionChanged?.Invoke(_selected);
        }

        // ── 제작(판정) ──────────────────────────────────────────
        /// <summary>
        /// 현재 선택된 재료로 판정을 실행하고 결과에 따라 돈을 처리한다.
        /// 결과는 OnCraftJudged 이벤트로 발행된다.
        /// </summary>
        public void Craft()
        {
            if (_matcher == null || _currentCustomer == null)
            {
                Debug.LogWarning("[CraftController] Initialize 를 먼저 호출하세요.");
                return;
            }

            var result = RecipeJudge.Judge(_selected, _currentCustomer, _matcher, out var matched);

            ApplyEconomy(result, matched);

            Debug.Log($"[CraftController] 결과={result} | 메뉴={matched?.displayName ?? "없음"} | 현재돈={MoneyManager.Instance.Money}");

            OnCraftJudged?.Invoke(result, matched);
            ClearSelection();
        }

        // ── 내부 ────────────────────────────────────────────────
        private void ApplyEconomy(CraftResult result, MenuRecipeSO matched)
        {
            switch (result)
            {
                case CraftResult.Success:
                    MoneyManager.Instance.AddMoney(matched.price);
                    break;
                case CraftResult.TabooViolation:
                    MoneyManager.Instance.SpendMoney(_tabooPenalty);
                    break;
                case CraftResult.WrongRecipe:
                    // 재료비 손실만 (현재 프로토타입에서는 추가 패널티 없음)
                    break;
            }
        }
    }
}
