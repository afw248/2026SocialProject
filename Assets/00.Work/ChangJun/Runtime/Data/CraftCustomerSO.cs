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

        [Header("서사")]
        [Tooltip("한국에 사는 이주민·유학생 등, 음식 문화권과 별개인 배경")]
        [TextArea(1, 3)] public string originNote;

        [Tooltip("방문·호감 임계치에서 나오는 짧은 단골 대사")]
        public RegularStoryBeat[] storyBeats;

        [Header("계층·접근성 (소량)")]
        [Tooltip("저소득 손님이 가격 흥정을 건다")]
        public bool canBargain;
        [Range(0.05f, 0.5f)] public float bargainDiscount = 0.2f;
        [Tooltip("거동·청각 등 배려가 필요한 손님")]
        public bool needsAccessibleService;
        [TextArea(1, 2)] public string accessibleRequestLine;
    }
}
