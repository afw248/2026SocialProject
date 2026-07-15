using UnityEngine;

namespace ChangJun.Data
{
    /// <summary>
    /// 문화권 테마 주식 — 신문 증권면·투자 연동용.
    /// </summary>
    [CreateAssetMenu(fileName = "Stock_", menuName = "CupRice/Data/Stock Ticker")]
    public class StockTickerSO : ScriptableObject
    {
        [Tooltip("증권 코드 (예: KFOOD)")]
        public string code;

        [Tooltip("회사·지수 표시명")]
        public string displayName;

        [Tooltip("관련 문화권 — 뉴스 영향 연동")]
        public CultureGroup cultureGroup;

        [Tooltip("기준 주가 (원)")]
        public int basePrice = 10000;

        [Range(0.01f, 0.12f)]
        [Tooltip("일일 변동성")]
        public float volatility = 0.04f;

        [TextArea(1, 3)]
        public string description;
    }
}
