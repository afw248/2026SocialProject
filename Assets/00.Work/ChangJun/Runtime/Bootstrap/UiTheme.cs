using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// SDW UI Sample(00.Work/SDW/UI Sample)에서 추출한 공용 팔레트·드로잉 헬퍼.
    /// 두꺼운 검정 테두리 + 하드 섀도 카드가 기본 톤이다.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Background = new Color32(0xFF, 0xF6, 0xE6, 0xFF);
        public static readonly Color Accent = new Color32(0xFF, 0x6F, 0x61, 0xFF);
        public static readonly Color Gold = new Color32(0xF2, 0xB7, 0x05, 0xFF);
        public static readonly Color TanRow = new Color32(0xF2, 0xE4, 0xC8, 0xFF);
        public static readonly Color TextDark = new Color32(0x3A, 0x24, 0x15, 0xFF);
        public static readonly Color TextMuted = new Color32(0x8A, 0x62, 0x38, 0xFF);
        public static readonly Color TextFaint = new Color32(0xB0, 0x80, 0x55, 0xFF);
        public static readonly Color Border = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
        public static readonly Color CardWhite = Color.white;
        public static readonly Color Success = new Color32(0x5F, 0xAE, 0x4E, 0xFF);
        public static readonly Color Danger = new Color32(0xD9, 0x43, 0x2E, 0xFF);
        public static readonly Color Info = new Color32(0x6F, 0xC7, 0xD9, 0xFF);

        public const float DefaultBorderWidth = 4f;
        public const float DefaultShadowOffset = 6f;

        /// <summary>검정 테두리 + 채움색 패널. 자식 콘텐츠는 반환된 inner RectTransform에 붙인다.</summary>
        public static RectTransform CreateBorderedPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Color fillColor, float borderWidth = DefaultBorderWidth)
        {
            var outer = UiFactory.CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            outer.gameObject.AddComponent<Image>().color = Border;

            var inner = UiFactory.CreatePanel(outer, name + "_Fill",
                Vector2.zero, Vector2.one, new Vector2(borderWidth, borderWidth), new Vector2(-borderWidth, -borderWidth));
            inner.gameObject.AddComponent<Image>().color = fillColor;
            return inner;
        }

        /// <summary>검정 하드 섀도(오프셋 사각형)가 뒤에 깔린 테두리 카드. 그림자·테두리·채움 3중 구조.</summary>
        public static RectTransform CreateShadowCard(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Color fillColor, float borderWidth = DefaultBorderWidth, float shadowOffset = DefaultShadowOffset)
        {
            var wrap = UiFactory.CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);

            var shadow = UiFactory.CreatePanel(wrap, name + "_Shadow",
                Vector2.zero, Vector2.one, new Vector2(shadowOffset, -shadowOffset), new Vector2(shadowOffset, -shadowOffset));
            shadow.gameObject.AddComponent<Image>().color = Border;

            return CreateBorderedPanel(wrap, name + "_Card", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                fillColor, borderWidth);
        }

        /// <summary>상단 코랄 헤더 바. 좌측에 타이틀 텍스트를 붙인다.</summary>
        public static RectTransform CreateHeaderBar(Transform parent, string title, float height = 72f,
            float titleIndent = 26f)
        {
            var bar = UiFactory.CreatePanel(parent, "Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -height), new Vector2(0f, 0f));
            bar.gameObject.AddComponent<Image>().color = Accent;

            var underline = UiFactory.CreatePanel(bar, "Underline",
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, DefaultBorderWidth));
            underline.gameObject.AddComponent<Image>().color = Border;

            UiFactory.CreateText(bar, "Title", title,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(titleIndent, 0f), new Vector2(-26f, 0f),
                TextAlignmentOptions.MidlineLeft, 24, CardWhite);

            return bar;
        }

        public struct HeaderMeta
        {
            public TextMeshProUGUI Clock;
            public TextMeshProUGUI Money;
        }

        /// <summary>모든 화면 헤더 우측 — 시간 칩 + 돈 칩. 위치 일관.</summary>
        public static HeaderMeta CreateHeaderMeta(RectTransform header)
        {
            var moneyChip = CreateBorderedPanel(header, "MoneyChip",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-188f, -18f), new Vector2(-16f, 18f), CardWhite, 2f);
            var money = UiFactory.CreateText(moneyChip, "Text", "0원",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, TextDark);

            var clockChip = CreateBorderedPanel(header, "ClockChip",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-430f, -18f), new Vector2(-198f, 18f), CardWhite, 2f);
            var clock = UiFactory.CreateText(clockChip, "Text", "",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, TextDark);

            var title = header.Find("Title") as RectTransform;
            if (title != null)
                title.offsetMax = new Vector2(-440f, title.offsetMax.y);

            return new HeaderMeta { Clock = clock, Money = money };
        }

        public static void RefreshHeaderMeta(HeaderMeta meta)
        {
            if (meta.Clock != null && ChangJun.Time.DayLoopController.Instance != null)
                meta.Clock.text = ChangJun.Time.DayLoopController.Instance.FormatDayClock();
            if (meta.Money != null && ChangJun.Economy.MoneyManager.Instance != null)
                meta.Money.text = $"{ChangJun.Economy.MoneyManager.Instance.Money:N0}원";
        }

        /// <summary>헤더 바 좌측의 뒤로가기 버튼(홈 화면으로 복귀).</summary>
        public static Button CreateBackButton(Transform headerBar, UnityAction onClick)
        {
            var go = new GameObject("BackBtn", typeof(RectTransform));
            go.transform.SetParent(headerBar, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(40f, 40f);
            rt.anchoredPosition = new Vector2(46f, 0f);

            var img = go.AddComponent<Image>();
            img.color = CardWhite;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            UiFactory.CreateText(go.transform, "Arrow", "<",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22, TextDark);
            return btn;
        }

        /// <summary>고정 높이 헤더 아래, 화면 나머지를 채우는 본문 영역.</summary>
        public static RectTransform CreateScreenBody(Transform parent, float headerHeight = 72f, float margin = 20f)
        {
            return UiFactory.CreatePanel(parent, "Body", Vector2.zero, Vector2.one,
                new Vector2(margin, margin), new Vector2(-margin, -(headerHeight + margin)));
        }

        public static TextMeshProUGUI CreateSectionLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize = 16)
        {
            return UiFactory.CreateText(parent, name, text, anchorMin, anchorMax, offsetMin, offsetMax,
                TextAlignmentOptions.MidlineLeft, fontSize, TextMuted);
        }

        /// <summary>각진 테두리 버튼(호버 시 눌림 이동은 생략 — 즉시 클릭 반응만 제공).</summary>
        public static Button CreateFlatButton(Transform parent, string label, Color bgColor,
            UnityAction onClick, int fontSize = 20, Color? textColor = null)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UiFactory.Stretch(go.GetComponent<RectTransform>());

            var borderImg = go.AddComponent<Image>();
            borderImg.color = Border;

            var btn = go.AddComponent<Button>();

            var fillRt = UiFactory.CreatePanel(go.transform, "Fill",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = bgColor;
            btn.targetGraphic = fillImg;

            UiFactory.CreateText(fillRt, "Text", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, fontSize, textColor ?? CardWhite);

            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
