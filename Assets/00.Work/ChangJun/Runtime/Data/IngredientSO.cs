using UnityEngine;

namespace ChangJun.Data
{
    /// <summary>
    /// 재료 하나의 데이터 — 코드, 이름, 이 재료가 금기인 식이 목록.
    /// forbiddenIn: 이 비트가 켜진 손님 문화에는 이 재료를 제공하면 금기위반이다.
    /// </summary>
    [CreateAssetMenu(fileName = "Ingredient_", menuName = "CupRice/Data/Ingredient")]
    public class IngredientSO : ScriptableObject
    {
        [Tooltip("재료 고유 코드 (예: HBF, PRK, EGG)")]
        public string code;

        [Tooltip("UI에 표시할 이름")]
        public string displayName;

        [Tooltip("이 재료가 금기인 식이 — 비트 조합 가능")]
        public Diet forbiddenIn;

        [Header("이해도 / 경제")]
        public CultureGroup cultureGroup;
        [Range(0, 100)] public int unlockThreshold;
        public int purchasePrice = 50;
        public bool isStarterUnlocked;

        [Header("사회·경제 태그")]
        public bool isFairTrade;
        public bool isLocalSourced;
    }
}
