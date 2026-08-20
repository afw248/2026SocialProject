using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Progression;
using ChangJun.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 문화별 패시브 이해도 스킬트리 UI.
    /// </summary>
    public sealed class UnderstandingTreePanel
    {
        private static readonly Color PanelBg = new(0.07f, 0.09f, 0.14f, 0.94f);
        private static readonly Color CardBg = new(0.12f, 0.14f, 0.22f, 0.98f);
        private static readonly Color CardBgLocked = new(0.09f, 0.10f, 0.15f, 0.85f);
        private static readonly Color TrackBg = new(0.06f, 0.07f, 0.11f, 1f);
        private static readonly Color TooltipBg = new(0.10f, 0.12f, 0.18f, 0.96f);
        private static readonly Color GoldAccent = new(0.95f, 0.78f, 0.28f);

        private readonly RectTransform _root;
        private readonly RectTransform _treeContent;
        private TextMeshProUGUI _cultureTitle;
        private TextMeshProUGUI _cultureProgress;
        private TextMeshProUGUI _reputationLabel;
        private TextMeshProUGUI _tooltipTitle;
        private TextMeshProUGUI _tooltipBody;
        private readonly Dictionary<string, NodeView> _views = new();
        private readonly Dictionary<CultureGroup, TabView> _tabs = new();
        private CultureGroup _selectedCulture = CultureGroup.Korean;

        private sealed class TabView
        {
            public Image Bg;
            public TextMeshProUGUI Label;
            public Outline Outline;
        }

        private sealed class NodeView
        {
            public UnderstandingNodeSO Node;
            public Image CardBg;
            public Image Accent;
            public Image Icon;
            public Image Fill;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Badge;
            public TextMeshProUGUI Counter;
            public GameObject Check;
            public CanvasGroup Group;
        }

        public UnderstandingTreePanel(RectTransform parent)
        {
            _root = UiFactory.CreatePanel(parent, "TreePanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root.gameObject.AddComponent<Image>().color = PanelBg;

            CreateHeader(_root);
            CreateCultureTabs(_root);

            _cultureTitle = UiFactory.CreateText(_root, "CultureTitle", "",
                new Vector2(0.04f, 0.735f), new Vector2(0.62f, 0.775f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18,
                new Color(0.95f, 0.96f, 1f));
            _cultureTitle.fontStyle = FontStyles.Bold;

            _cultureProgress = UiFactory.CreateText(_root, "CulturePct", "0%",
                new Vector2(0.62f, 0.735f), new Vector2(0.96f, 0.775f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 17,
                new Color(0.75f, 0.85f, 1f));

            var scrollRt = UiFactory.CreatePanel(_root, "TreeScroll",
                new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.725f),
                Vector2.zero, Vector2.zero);
            var scrollFrame = scrollRt.gameObject.AddComponent<Image>();
            scrollFrame.color = new Color(0.04f, 0.05f, 0.09f, 0.75f);
            AddOutline(scrollRt.gameObject, new Color(1f, 1f, 1f, 0.08f));

            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _treeContent = UiFactory.CreateStretchChild(viewport, "Content");
            _treeContent.pivot = new Vector2(0.5f, 1f);
            _treeContent.anchorMin = new Vector2(0, 1);
            _treeContent.anchorMax = new Vector2(1, 1);

            var vlg = _treeContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0;
            vlg.padding = new RectOffset(10, 10, 12, 12);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _treeContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _treeContent;

            CreateTooltipCard(_root);

            if (UnderstandingManager.Instance != null)
                UnderstandingManager.Instance.OnUnderstandingChanged += OnUnderstandingChanged;
            if (StoreReputationService.Instance != null)
                StoreReputationService.Instance.OnReputationChanged += OnReputationChanged;

            BuildTree();
            RefreshAll();
        }

        public RectTransform Root => _root;

        private void CreateHeader(RectTransform parent)
        {
            var header = UiFactory.CreatePanel(parent, "Header",
                new Vector2(0.04f, 0.855f), new Vector2(0.96f, 0.97f),
                Vector2.zero, Vector2.zero);
            header.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.26f, 0.9f);
            AddOutline(header.gameObject, new Color(0.45f, 0.55f, 0.85f, 0.25f));

            UiFactory.CreateText(header, "HeaderTitle", "문화 이해도",
                new Vector2(0.04f, 0.08f), new Vector2(0.55f, 0.92f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 17,
                new Color(0.8f, 0.85f, 0.95f));

            _reputationLabel = UiFactory.CreateText(header, "Reputation", "상생 0%",
                new Vector2(0.55f, 0.08f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 16,
                GoldAccent);
            _reputationLabel.fontStyle = FontStyles.Bold;
        }

        private void CreateCultureTabs(RectTransform parent)
        {
            var tabBar = UiFactory.CreatePanel(parent, "CultureTabRow",
                new Vector2(0.04f, 0.785f), new Vector2(0.96f, 0.845f),
                Vector2.zero, Vector2.zero);

            var hlg = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(2, 2, 2, 2);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            foreach (CultureGroup c in Enum.GetValues(typeof(CultureGroup)))
            {
                if (c == CultureGroup.None) continue;
                _tabs[c] = CreateCultureTab(tabBar.transform, c);
            }
        }

        private TabView CreateCultureTab(Transform parent, CultureGroup culture)
        {
            var cap = culture;
            var btnGo = new GameObject($"Tab_{culture}", typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            le.minHeight = 32;

            var img = btnGo.AddComponent<Image>();
            img.color = CultureTabColor(culture) * 0.55f;
            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                _selectedCulture = cap;
                RefreshAll();
            });

            var labelGo = new GameObject("L", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = ShortCultureName(culture);
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.92f);
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);

            return new TabView { Bg = img, Label = tmp, Outline = outline };
        }

        private void CreateTooltipCard(RectTransform parent)
        {
            var card = UiFactory.CreatePanel(parent, "TooltipCard",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.22f),
                Vector2.zero, Vector2.zero);
            card.gameObject.AddComponent<Image>().color = TooltipBg;
            AddOutline(card.gameObject, new Color(1f, 1f, 1f, 0.1f));

            _tooltipTitle = UiFactory.CreateText(card, "TooltipTitle", "노드를 선택하세요",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.92f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16,
                new Color(0.92f, 0.94f, 1f));
            _tooltipTitle.fontStyle = FontStyles.Bold;

            _tooltipBody = UiFactory.CreateText(card, "TooltipBody",
                "아래 노드를 누르면 통합사회 설명과 게임 효과를 볼 수 있습니다.",
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.52f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 14,
                new Color(0.72f, 0.76f, 0.86f));
            _tooltipBody.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildTree()
        {
            foreach (Transform ch in _treeContent)
                UnityEngine.Object.Destroy(ch.gameObject);
            _views.Clear();

            if (UnderstandingManager.Instance == null) return;
            var nodes = new List<UnderstandingNodeSO>(
                UnderstandingManager.Instance.GetNodesForCulture(_selectedCulture));
            nodes.Sort((a, b) => a.gridRow.CompareTo(b.gridRow));

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                    CreateConnectorRow();
                CreateNodeWidget(nodes[i]);
            }
        }

        private void CreateConnectorRow()
        {
            var row = new GameObject("Connector", typeof(RectTransform));
            row.transform.SetParent(_treeContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 20;

            var line = UiFactory.CreatePanel(row.transform, "Line",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(-1f, 0f), new Vector2(1f, 0f));
            line.sizeDelta = new Vector2(3f, 0f);
            line.gameObject.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.35f, 0.55f);
        }

        private void CreateNodeWidget(UnderstandingNodeSO node)
        {
            var cultureColor = CultureTabColor(node.cultureGroup);

            var row = new GameObject($"Node_{node.nodeId}", typeof(RectTransform));
            row.transform.SetParent(_treeContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 72;

            var group = row.AddComponent<CanvasGroup>();
            var bg = row.AddComponent<Image>();
            bg.color = CardBg;

            var accentGo = new GameObject("Accent", typeof(RectTransform));
            accentGo.transform.SetParent(row.transform, false);
            var accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0.08f);
            accentRt.anchorMax = new Vector2(0f, 0.92f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(5f, 0f);
            var accent = accentGo.AddComponent<Image>();
            accent.color = cultureColor;

            var iconRt = UiFactory.CreatePanel(row.transform, "Icon",
                new Vector2(0.04f, 0.14f), new Vector2(0.16f, 0.86f),
                Vector2.zero, Vector2.zero);
            var iconBg = iconRt.gameObject.AddComponent<Image>();
            iconBg.color = cultureColor * 0.35f;
            AddOutline(iconRt.gameObject, cultureColor * 0.6f);
            var icon = new GameObject("Glyph", typeof(RectTransform));
            icon.transform.SetParent(iconRt, false);
            UiFactory.Stretch(icon.GetComponent<RectTransform>());
            var iconImg = icon.AddComponent<Image>();
            iconImg.color = cultureColor;

            var badge = UiFactory.CreateText(row.transform, "Badge", NodeTypeLabel(node.nodeType),
                new Vector2(0.18f, 0.62f), new Vector2(0.38f, 0.88f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 12,
                cultureColor);
            badge.fontStyle = FontStyles.Bold;

            var nameTmp = UiFactory.CreateText(row.transform, "Name", node.displayName,
                new Vector2(0.18f, 0.28f), new Vector2(0.72f, 0.62f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 17,
                new Color(0.94f, 0.95f, 0.98f));
            nameTmp.fontStyle = FontStyles.Bold;

            var track = UiFactory.CreatePanel(row.transform, "Track",
                new Vector2(0.18f, 0.10f), new Vector2(0.72f, 0.24f),
                Vector2.zero, Vector2.zero);
            track.gameObject.AddComponent<Image>().color = TrackBg;
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(track, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fill = fillGo.AddComponent<Image>();
            fill.color = cultureColor;

            var counter = UiFactory.CreateText(row.transform, "Counter", "0/20",
                new Vector2(0.74f, 0.12f), new Vector2(0.96f, 0.88f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 15,
                new Color(0.82f, 0.88f, 0.98f));

            var checkGo = UiFactory.CreateText(row.transform, "Check", "완료",
                new Vector2(0.74f, 0.12f), new Vector2(0.96f, 0.88f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 14,
                new Color(0.45f, 0.95f, 0.55f));
            checkGo.fontStyle = FontStyles.Bold;
            checkGo.gameObject.SetActive(false);

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => ShowTooltip(node));

            _views[node.nodeId] = new NodeView
            {
                Node = node,
                CardBg = bg,
                Accent = accent,
                Icon = iconImg,
                Fill = fill,
                Name = nameTmp,
                Badge = badge,
                Counter = counter,
                Check = checkGo.gameObject,
                Group = group,
            };
        }

        private void ShowTooltip(UnderstandingNodeSO node)
        {
            if (node == null) return;
            _tooltipTitle.text = node.displayName;
            _tooltipBody.text = node.description;
        }

        public void RefreshAll()
        {
            RefreshTabStyles();
            _cultureTitle.text = $"{LongCultureName(_selectedCulture)} 트리";

            int current = UnderstandingManager.Instance?.GetUnderstanding(_selectedCulture) ?? 0;
            _cultureProgress.text = $"{current}%";

            BuildTree();

            if (UnderstandingManager.Instance == null) return;

            foreach (var pair in _views)
            {
                ApplyNodeVisual(pair.Value, current);
            }

            OnReputationChanged(StoreReputationService.Instance?.Reputation ?? 0f);
        }

        private void ApplyNodeVisual(NodeView view, int currentUnderstanding)
        {
            var node = view.Node;
            var state = UnderstandingManager.Instance.GetNodeState(node);
            int req = node.requiredUnderstanding;
            var cultureColor = CultureTabColor(node.cultureGroup);

            bool unlocked = state == UnderstandingNodeState.Unlocked;
            bool locked = state == UnderstandingNodeState.Locked;
            bool inProgress = state == UnderstandingNodeState.InProgress;

            float progress = req > 0
                ? Mathf.Clamp01(unlocked ? 1f : currentUnderstanding / (float)req)
                : (unlocked ? 1f : 0f);

            view.Fill.rectTransform.anchorMax = new Vector2(progress, 1f);
            view.Check.SetActive(unlocked);
            view.Counter.gameObject.SetActive(!unlocked);

            if (unlocked)
            {
                view.Counter.text = "";
            }
            else if (req <= 0)
            {
                view.Counter.text = inProgress || currentUnderstanding > 0 ? "진행" : "입문";
            }
            else
            {
                int shown = Mathf.Min(currentUnderstanding, req);
                view.Counter.text = $"{shown}/{req}";
            }

            view.CardBg.color = locked ? CardBgLocked : CardBg;
            view.Accent.color = locked ? new Color(0.35f, 0.38f, 0.45f) : cultureColor;
            view.Icon.color = locked ? new Color(0.4f, 0.42f, 0.5f) : cultureColor;
            view.Name.color = locked
                ? new Color(0.55f, 0.58f, 0.65f)
                : new Color(0.94f, 0.95f, 0.98f);
            view.Badge.color = locked ? new Color(0.45f, 0.48f, 0.55f) : cultureColor;
            view.Group.alpha = locked ? 0.72f : 1f;
        }

        private void RefreshTabStyles()
        {
            foreach (var pair in _tabs)
            {
                bool selected = pair.Key == _selectedCulture;
                var color = CultureTabColor(pair.Key);
                pair.Value.Bg.color = selected ? color * 0.95f : color * 0.42f;
                pair.Value.Label.color = selected
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.75f);
                pair.Value.Outline.effectColor = selected
                    ? new Color(1f, 1f, 1f, 0.45f)
                    : new Color(0, 0, 0, 0.35f);
            }
        }

        private void OnUnderstandingChanged(CultureGroup culture, int _)
        {
            if (culture == _selectedCulture)
                RefreshAll();
            else
            {
                foreach (var v in _views.Values)
                {
                    if (v.Node.cultureGroup == culture)
                    {
                        RefreshAll();
                        break;
                    }
                }
            }
        }

        private void OnReputationChanged(float rep)
        {
            _reputationLabel.text = $"상생 {rep * 100f:F0}%";
        }

        private static void AddOutline(GameObject go, Color color)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static string NodeTypeLabel(UnderstandingNodeType type) => type switch
        {
            UnderstandingNodeType.Milestone => "기초",
            UnderstandingNodeType.IngredientUnlock => "재료",
            UnderstandingNodeType.EventUnlock => "축제",
            UnderstandingNodeType.Certification => "인증",
            UnderstandingNodeType.Fusion => "완성",
            _ => "노드",
        };

        private static string ShortCultureName(CultureGroup c) => c switch
        {
            CultureGroup.Korean => "한식",
            CultureGroup.Muslim => "할랄",
            CultureGroup.Hindu => "힌두",
            CultureGroup.Vegan => "비건",
            CultureGroup.SEAsian => "동남",
            CultureGroup.AfricanAmerican => "소울",
            _ => c.ToString(),
        };

        private static string LongCultureName(CultureGroup c) => c switch
        {
            CultureGroup.Korean => "한식",
            CultureGroup.Muslim => "무슬림·할랄",
            CultureGroup.Hindu => "힌두·채식",
            CultureGroup.Vegan => "비건",
            CultureGroup.SEAsian => "동남아",
            CultureGroup.AfricanAmerican => "소울푸드",
            _ => c.ToString(),
        };

        private static Color CultureTabColor(CultureGroup c) => c switch
        {
            CultureGroup.Korean => new Color(0.92f, 0.42f, 0.34f),
            CultureGroup.Muslim => new Color(0.22f, 0.68f, 0.52f),
            CultureGroup.Hindu => new Color(0.92f, 0.68f, 0.22f),
            CultureGroup.Vegan => new Color(0.40f, 0.78f, 0.38f),
            CultureGroup.SEAsian => new Color(0.38f, 0.60f, 0.92f),
            CultureGroup.AfricanAmerican => new Color(0.68f, 0.45f, 0.88f),
            _ => new Color(0.5f, 0.75f, 1f),
        };
    }
}
