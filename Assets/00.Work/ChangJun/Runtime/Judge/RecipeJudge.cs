using System.Collections.Generic;
using ChangJun.Data;

namespace ChangJun.Judge
{
    /// <summary>
    /// §3-3 판정 의사코드를 순수 정적 메서드로 구현한다.
    /// MonoBehaviour · UI · 씬에 일절 의존하지 않아 EditMode 단위 테스트가 가능하다 (SRP).
    ///
    /// 판정 순서:
    ///   1. 선택 재료가 유효한 메뉴와 일치하지 않으면 → WrongRecipe (오조리)
    ///   2. 유효 메뉴는 있으나 금기 재료가 포함되면 → TabooViolation (금기위반)
    ///   3. 레시피 일치 AND 금기 없음 → Success
    /// </summary>
    public static class RecipeJudge
    {
        /// <summary>
        /// 조합을 판정한다.
        /// </summary>
        /// <param name="selected">플레이어가 선택한 재료 목록</param>
        /// <param name="customer">현재 손님 데이터 (식이 문화 포함)</param>
        /// <param name="matcher">레시피 매칭 전략 (RecipeBook 등)</param>
        /// <param name="matched">판정된 메뉴 (WrongRecipe 이면 null)</param>
        public static CraftResult Judge(
            IReadOnlyList<IngredientSO> selected,
            CraftCustomerSO customer,
            IRecipeMatcher matcher,
            out MenuRecipeSO matched)
        {
            matched = matcher.Match(selected);

            // 1. 어떤 메뉴와도 일치하지 않음 → 오조리
            if (matched == null)
                return CraftResult.WrongRecipe;

            // 2. 금기 재료가 하나라도 포함되어 있는가 확인
            foreach (var ing in selected)
            {
                if ((ing.forbiddenIn & customer.diet) != 0)
                    return CraftResult.TabooViolation;
            }

            // 3. 손님이 요청한 메뉴와 일치하는가
            if (customer.requiredMenu != null && matched != customer.requiredMenu)
                return CraftResult.WrongOrder;

            // 4. 레시피 일치 + 금기 없음 + 주문 일치 → 성공
            return CraftResult.Success;
        }
    }
}
