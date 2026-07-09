using UnityEngine;

namespace ChangJun.Data
{
    /// <summary>
    /// 조합 씬용 손님 스냅샷.
    /// 주문 대사·문화 식이·요구 메뉴를 담아 씬 간 전달용으로 활용한다.
    /// (박철민 담당 CustomerDataSO 와 직접 참조 없이 별도로 정의 — 후속 통합 시 어댑터로 연결)
    /// </summary>
    [CreateAssetMenu(fileName = "Customer_", menuName = "CupRice/Data/CraftCustomer")]
    public class CraftCustomerSO : ScriptableObject
    {
        [Tooltip("손님 이름")]
        public string customerName;

        [Tooltip("주문 대사 (힌트 없는 첫 문장)")]
        [TextArea]
        public string orderLine;

        [Tooltip("손님의 식이 문화 — 금기 판정에 사용")]
        public Diet diet;

        [Tooltip("손님이 요구하는 메뉴")]
        public MenuRecipeSO requiredMenu;

        [Tooltip("이해도·뉴스 대상 문화")]
        public CultureGroup cultureGroup;
    }
}
