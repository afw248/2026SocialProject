using ChangJun.Data;
using ChangJun.Social;
using UnityEngine;

namespace ChangJun.News
{
    /// <summary>
    /// 어제 가게 행동 → 오늘 신문 헤드라인. 런타임 NewsSO.
    /// </summary>
    public static class ShopNewsFactory
    {
        public static NewsSO BuildFromYesterday()
        {
            var rep = StoreReputationService.Instance;
            var conflict = CustomerConflictService.Instance;
            if (rep == null) return null;

            if (conflict != null && conflict.IgnoredYesterday)
                return Make(
                    CultureGroup.Korean,
                    NewsSentiment.Discrimination,
                    0.85f,
                    0.35f,
                    "사회",
                    "컵밥집 대기줄 편견 발언, 방치 논란",
                    "손님이 다른 손님을 향해 한 말을 매장이 넘겼다는 제보가 나왔습니다.",
                    "지역 커뮤니티에 '그 가게는 편견을 말리지 않는다'는 이야기가 돌고 있습니다. 시민단체는 음식점이 공론장이 될 수 있다며, 침묵도 선택이라고 지적했습니다.",
                    "방관은 뉴스의 헤드라인이 됩니다.");

            if (conflict != null && conflict.IntervenedYesterday)
                return Make(
                    CultureGroup.Korean,
                    NewsSentiment.Positive,
                    1.12f,
                    0f,
                    "사회",
                    "OO컵밥, 대기줄 편견에 즉각 제지… '식탁의 규칙'",
                    "사장이 줄에서 나온 차별 발언을 막았다는 후기가 퍼졌습니다.",
                    "단골은 \"손님이 손님을 밀어낼 때 가게가 어디에 서는지가 보인다\"고 썼습니다. 다문화 상생 지수에도 작은 온기가 돌았습니다.",
                    "개입은 가게의 평판이 됩니다.");

            if (rep.Reputation >= 0.75f && rep.TodayTabooCount == 0 && rep.TodayOrderCount >= 3)
                return Make(
                    CultureGroup.Korean,
                    NewsSentiment.Positive,
                    1.18f,
                    0f,
                    "사회",
                    "OO식당, 다문화 상생 모범 사례로 소개",
                    "문화 금기를 지키며 다양한 손님을 받은 작은 컵밥집이 지자체 사례로 올랐습니다.",
                    "인증 평가단은 메뉴 설명과 식이 배려, 직원 구성을 기준으로 이 매장을 소개했습니다. 사장은 \"손님이 무엇을 먹지 못하는지 아는 것이 예의\"라고 말했습니다.",
                    "오늘의 영업이 내일의 1면이 됩니다.");

            if (rep.Reputation <= 0.25f && rep.TodayTabooCount > 0)
                return Make(
                    CultureGroup.Korean,
                    NewsSentiment.Negative,
                    0.9f,
                    0.15f,
                    "사회",
                    "컵밥집 금기 위반 민원… '우리 음식 몰라요'",
                    "식이 금기를 어긴 주문 처리에 대한 불만이 온라인에 올라왔습니다.",
                    "한 손님은 \"돼지 안 된다고 했는데 나왔다\"고 적었습니다. 상인회는 재료 표시와 직원 교육을 다시 점검하라고 권고했습니다.",
                    "실수가 기사화되면 신뢰 회복이 더딥니다.");

            if (rep.TodayOrderCount >= 2)
                return Make(
                    CultureGroup.Korean,
                    NewsSentiment.Positive,
                    1.04f,
                    0f,
                    "동네",
                    "골목 컵밥집, 어제와 같은 단골들",
                    "큰 뉴스는 아니지만 단골이 다시 줄을 섰습니다.",
                    "상생 지수 " + $"{rep.Reputation * 100f:F0}%" + ". 조용한 하루도 동네 지면의 한 줄이 됩니다.",
                    "작은 가게의 하루가 지역면에 남습니다.");

            return null;
        }

        private static NewsSO Make(CultureGroup culture, NewsSentiment sentiment,
            float price, float boycott, string tag, string headline, string sub, string article, string sidebar)
        {
            var so = ScriptableObject.CreateInstance<NewsSO>();
            so.cultureGroup = culture;
            so.sentiment = sentiment;
            so.priceMultiplier = price;
            so.boycottWeight = boycott;
            so.spawnWeight = 1f;
            so.sectionTag = tag;
            so.headline = headline;
            so.subheadline = sub;
            so.body = sub;
            so.article = article;
            so.sidebarNote = sidebar;
            so.summary = sub;
            so.primaryStockCode = "UNITY";
            so.name = "ShopNews";
            return so;
        }
    }
}
