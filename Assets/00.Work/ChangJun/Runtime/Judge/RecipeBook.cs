using System.Collections.Generic;
using ChangJun.Data;
using UnityEngine;

namespace ChangJun.Judge
{
    /// <summary>
    /// 등록된 모든 MenuRecipeSO 를 보유하고 재료 집합으로 메뉴를 탐색한다.
    /// — 선택 순서 무관 (HashSet 비교)
    /// — 메뉴 추가/변경은 SO 에셋 수정만으로 끝난다 (코드 수정 불필요, OCP)
    /// </summary>
    public sealed class RecipeBook : IRecipeMatcher
    {
        private readonly IReadOnlyList<MenuRecipeSO> _menus;

        public RecipeBook(IReadOnlyList<MenuRecipeSO> menus)
        {
            _menus = menus;
        }

        /// <inheritdoc/>
        public MenuRecipeSO Match(IReadOnlyList<IngredientSO> selected)
        {
            if (selected == null || selected.Count == 0) return null;

            // 선택 재료 코드 집합
            var selectedSet = new HashSet<string>();
            foreach (var ing in selected)
                selectedSet.Add(ing.code);

            foreach (var menu in _menus)
            {
                if (menu.ingredientCodes == null) continue;
                if (menu.ingredientCodes.Length != selectedSet.Count) continue;

                // 레시피 코드 집합과 완전 일치 여부 확인
                var recipeSet = new HashSet<string>(menu.ingredientCodes);
                if (recipeSet.SetEquals(selectedSet))
                    return menu;
            }

            return null;
        }
    }
}
