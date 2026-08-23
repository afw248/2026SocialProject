using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public enum MainTab
    {
        Memo,
        Recipe,
        Status,
    }

    /// <summary>
    /// 제작(홈) 화면 우측의 세로 아이콘 내비게이션.
    /// 클릭하면 해당 화면이 별개의 풀스크린 오버레이로 뜬다(스와핑이 아님).
    /// </summary>
    public sealed class SideTabBar
    {
        private readonly GameObject _barRoot;

        public event Action<MainTab> OnTabSelected;
        public event Action OnDeliveryRequested;
        public event Action OnEarlyClose;

        public SideTabBar(RectTransform parent)
        {
            var bar = UiFactory.CreatePanel(parent, "SideTabBar",
                new Vector2(0.9f, 0.1f), new Vector2(1f, 0.9f),
                new Vector2(-8, 0), new Vector2(-4, 0));
            _barRoot = bar.gameObject;

            var vlg = bar.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(4, 4, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateDeliveryButton(bar);
            CreateNavButton(bar, "메모", new Color32(0xD9, 0x8C, 0xB0, 0xFF),
                () => OnTabSelected?.Invoke(MainTab.Memo));
            CreateNavButton(bar, "도감", UiTheme.Success,
                () => OnTabSelected?.Invoke(MainTab.Recipe));
            CreateNavButton(bar, "정보", new Color32(0xF2, 0xD2, 0x4A, 0xFF),
                () => OnTabSelected?.Invoke(MainTab.Status));
            CreateNavButton(bar, "조기마감", UiTheme.Danger,
                () => OnEarlyClose?.Invoke());
        }

        private static void CreateNavButton(RectTransform bar, string label, Color iconColor, Action onClick)
        {
            var go = new GameObject($"Nav_{label}", typeof(RectTransform));
            go.transform.SetParent(bar, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 72;
            le.preferredWidth = 72;

            var borderImg = go.AddComponent<Image>();
            borderImg.color = UiTheme.Border;

            var btn = go.AddComponent<Button>();

            var fillRt = UiFactory.CreatePanel(go.transform, "Fill",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = UiTheme.CardWhite;
            btn.targetGraphic = fillImg;

            var iconRt = UiFactory.CreatePanel(fillRt, "Icon",
                new Vector2(0.24f, 0.42f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            iconRt.gameObject.AddComponent<Image>().color = iconColor;

            AddButtonLabel(fillRt, label);
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private void CreateDeliveryButton(RectTransform bar)
        {
            var go = new GameObject("Tab_Delivery", typeof(RectTransform));
            go.transform.SetParent(bar, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 72;
            le.preferredWidth = 72;

            var borderImg = go.AddComponent<Image>();
            borderImg.color = UiTheme.Border;

            var btn = go.AddComponent<Button>();

            var fillRt = UiFactory.CreatePanel(go.transform, "Fill",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = UiTheme.CardWhite;
            btn.targetGraphic = fillImg;

            var iconRt = UiFactory.CreatePanel(fillRt, "Icon",
                new Vector2(0.24f, 0.42f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            iconRt.gameObject.AddComponent<Image>().color = UiTheme.Info;

            AddButtonLabel(fillRt, "배달");
            btn.onClick.AddListener(() => OnDeliveryRequested?.Invoke());
        }

        private static TextMeshProUGUI AddButtonLabel(Transform parent, string label)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0.4f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15;
            tmp.color = UiTheme.TextDark;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
            return tmp;
        }

        public void SetVisible(bool visible) => _barRoot.SetActive(visible);
    }
}
