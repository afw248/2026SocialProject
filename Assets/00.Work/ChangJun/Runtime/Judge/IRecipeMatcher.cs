using System.Collections.Generic;
using ChangJun.Data;

namespace ChangJun.Judge
{
    /// <summary>
    /// 선택한 재료 목록에서 일치하는 메뉴를 찾는 전략 인터페이스.
    /// CraftController 는 이 추상에만 의존하므로 매칭 전략을 자유롭게 교체할 수 있다 (DIP).
    /// </summary>
    public interface IRecipeMatcher
    {
        /// <summary>
        /// 재료 목록과 정확히 일치하는 메뉴를 반환한다.
        /// 일치하는 메뉴가 없으면 null 을 반환한다.
        /// </summary>
        MenuRecipeSO Match(IReadOnlyList<IngredientSO> selected);
    }
}
