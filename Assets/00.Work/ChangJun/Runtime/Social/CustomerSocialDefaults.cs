using ChangJun.Data;

namespace ChangJun.Social
{
    /// <summary>
    /// 에디터 Refresh 전에도 핵심 손님 플래그가 켜지도록 런타임에서만 채운다.
    /// </summary>
    public static class CustomerSocialDefaults
    {
        public static void Apply(System.Collections.Generic.IList<CraftCustomerSO> customers)
        {
            if (customers == null) return;
            foreach (var c in customers)
            {
                if (c == null) continue;
                switch (c.customerName)
                {
                    case "응웬":
                        if (!c.canBargain)
                        {
                            c.canBargain = true;
                            c.bargainDiscount = 0.25f;
                        }
                        if (string.IsNullOrEmpty(c.originNote))
                            c.originNote = "베트남에서 와 공장 일을 하는 손님. 한 끼 가격이 중요하다.";
                        break;
                    case "Bao":
                        if (!c.canBargain)
                        {
                            c.canBargain = true;
                            c.bargainDiscount = 0.15f;
                        }
                        break;
                    case "박영자":
                        c.needsAccessibleService = true;
                        if (string.IsNullOrEmpty(c.accessibleRequestLine))
                            c.accessibleRequestLine = "천천히 해주시고, 글씨는 크게 보여주세요.";
                        break;
                    case "첸":
                    case "왕":
                        if (string.IsNullOrEmpty(c.originNote))
                            c.originNote = "중국에서 와 한국에 정착한 손님.";
                        break;
                    case "유코":
                        if (string.IsNullOrEmpty(c.originNote))
                            c.originNote = "일본에서 유학 온 손님.";
                        break;
                    case "마르코":
                        if (string.IsNullOrEmpty(c.originNote))
                            c.originNote = "이탈리아에서 온 교환학생.";
                        break;
                }
            }
        }
    }
}
