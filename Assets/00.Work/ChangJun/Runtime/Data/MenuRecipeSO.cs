using UnityEngine;

namespace ChangJun.Data
{
    /// <summary>
    /// 메뉴 하나의 레시피 데이터.
    /// ingredientCodes 는 순서 무관한 재료 코드 집합 (RICE 베이스는 암묵).
    /// RecipeBook 이 Set&lt;string&gt; 비교로 일치 여부를 판정한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Menu_", menuName = "CupRice/Data/MenuRecipe")]
    public class MenuRecipeSO : ScriptableObject
    {
        [Tooltip("메뉴 코드 (예: M1, M2)")]
        public string code;

        [Tooltip("UI에 표시할 메뉴명")]
        public string displayName;

        [Tooltip("재료 코드 배열 — 순서 무관, RICE 제외")]
        public string[] ingredientCodes;

        [Tooltip("판매 가격(원)")]
        public int price;
    }
}
