using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public enum MainTab
    {
        Craft,
        Memo,
        Recipe,
        Status,
    }

    /// <summary>
    /// 우측 세로 탭 — 한 번에 하나의 패널만 표시한다.
    /// </summary>
    public sealed class SideTabBar
    {
        private readonly Dictionary<MainTab, GameObject> _panels = new();
        private readonly Dictionary<MainTab, Image> _tabImages = new();
        private readonly Dictionary<MainTab, Color> _tabColors = new();
        private readonly GameObject _barRoot;
        private MainTab _current = MainTab.Craft;

        public event Action<MainTab> OnTabChanged;
        public event Action OnDeliveryRequested;

        public SideTabBar(RectTransform parent, Action<MainTab> onTabSelected)
        {
            var bar = UiFactory.CreatePanel(parent, "SideTabBar",
                new Vector2(0.9f, 0.08f), new Vector2(1f, 0.70f),
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

            CreateTabButton(bar, MainTab.Craft, "제작", new Color(0.2f, 0.55f, 0.35f), onTabSelected);
            CreateTabButton(bar, MainTab.Memo, "메모", new Color(0.95f, 0.85f, 0.45f), onTabSelected);
            CreateTabButton(bar, MainTab.Recipe, "도감", new Color(0.25f, 0.4f, 0.7f), onTabSelected);
            CreateTabButton(bar, MainTab.Status, "정보", new Color(0.45f, 0.35f, 0.65f), onTabSelected);
            CreateDeliveryButton(bar);

            RefreshTabVisuals();
        }

        public void RegisterPanel(MainTab tab, GameObject panel)
        {
            if (panel == null) return;
            _panels[tab] = panel;
            ApplyVisibility();
        }

        /// <summary>등록된 패널 중 현재 탭만 표시. 부모 재활성화 후에도 호출.</summary>
        public void RefreshVisibility() => ApplyVisibility();

        public void SelectTab(MainTab tab)
        {
            _current = tab;
            ApplyVisibility();
            RefreshTabVisuals();
            OnTabChanged?.Invoke(tab);
        }

        private void ApplyVisibility()
        {
            foreach (var pair in _panels)
            {
                if (pair.Value == null) continue;
                bool show = pair.Key == _current;
                pair.Value.SetActive(show);
                if (show)
                    pair.Value.transform.SetAsLastSibling();
            }
        }

        private void RefreshTabVisuals()
        {
            foreach (var pair in _tabImages)
            {
                bool selected = pair.Key == _current;
                pair.Value.color = selected
                    ? Brighten(_tabColors[pair.Key], 1.2f)
                    : _tabColors[pair.Key] * 0.75f;
                pair.Value.color = new Color(pair.Value.color.r, pair.Value.color.g,
                    pair.Value.color.b, selected ? 1f : 0.85f);
                pair.Value.transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
            }
        }

        private void CreateTabButton(RectTransform bar, MainTab tab, string label,
            Color color, Action<MainTab> onTabSelected)
        {
            var go = new GameObject($"Tab_{tab}", typeof(RectTransform));
            go.transform.SetParent(bar, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 72;
            le.preferredWidth = 72;

            var img = go.AddComponent<Image>();
            img.color = color;
            _tabImages[tab] = img;
            _tabColors[tab] = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = tab;
            btn.onClick.AddListener(() =>
            {
                if (captured == _current && captured != MainTab.Craft)
                {
                    SelectTab(MainTab.Craft);
                    onTabSelected?.Invoke(MainTab.Craft);
                    return;
                }

                SelectTab(captured);
                onTabSelected?.Invoke(captured);
            });

            AddButtonLabel(go.transform, label);
        }

        private void CreateDeliveryButton(RectTransform bar)
        {
            var go = new GameObject("Tab_Delivery", typeof(RectTransform));
            go.transform.SetParent(bar, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 72;
            le.preferredWidth = 72;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.38f, 0.62f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnDeliveryRequested?.Invoke());

            AddButtonLabel(go.transform, "배달");
        }

        private static void AddButtonLabel(Transform parent, string label)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private static Color Brighten(Color c, float factor) =>
            new Color(Mathf.Min(c.r * factor, 1f), Mathf.Min(c.g * factor, 1f),
                Mathf.Min(c.b * factor, 1f), c.a);

        public void SetVisible(bool visible) => _barRoot.SetActive(visible);
    }
}
