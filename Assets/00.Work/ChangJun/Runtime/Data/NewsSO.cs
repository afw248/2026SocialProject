using UnityEngine;

namespace ChangJun.Data
{
    public enum NewsSentiment
    {
        Positive,
        Negative,
        Discrimination,
    }

    [CreateAssetMenu(fileName = "News_", menuName = "CupRice/Data/News")]
    public class NewsSO : ScriptableObject
    {
        public CultureGroup cultureGroup;
        public NewsSentiment sentiment;
        [Range(0.5f, 2f)] public float priceMultiplier = 1f;
        [Range(0f, 1f)] public float spawnWeight = 1f;
        [Range(0f, 1f)] public float boycottWeight;

        [Header("신문")]
        public string sectionTag = "사회";
        public string headline;
        [TextArea(1, 2)] public string subheadline;
        [TextArea(3, 8)] public string body;
        [TextArea(4, 10)] public string article;
        [TextArea(2, 4)] public string sidebarNote;
        [TextArea(2, 4)] public string summary;
        [Tooltip("신문 1면 우측 삽화 (픽셀아트)")]
        public Sprite illustration;

        [Header("주식 연동")]
        [Tooltip("이 뉴스가 직접 영향을 주는 증권 코드 (비우면 문화권 매칭)")]
        public string primaryStockCode;
    }
}
