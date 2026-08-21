using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Social;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 뉴스 — 독립 풀스크린 화면. 좌측 소식 목록 · 우측 선택한 뉴스 상세.
    /// "다음으로"를 누르면 주식 시장 화면으로 넘어간다(콜백으로 위임).
    /// </summary>
    public sealed class NewsOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _listContent;
        private readonly TextMeshProUGUI _dateChip;
        private readonly Image _illustrationImage;
        private readonly TextMeshProUGUI _headlineText;
        private readonly TextMeshProUGUI _bodyText;
        private readonly TextMeshProUGUI _economyImpactText;
        private readonly TextMeshProUGUI _reputationImpactText;
        private readonly TextMeshProUGUI _eventBanner;
        private readonly List<GameObject> _listRows = new();

        private NewsSO _selected;
        private Action _onContinue;

        public NewsOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("NewsOverlay", 200);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "뉴스", 72f);
            _dateChip = CreateHeaderChip(header, "");

            _eventBanner = UiFactory.CreateText(_root.transform, "EventBanner", "",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.895f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16, UiTheme.Danger);
            _eventBanner.fontStyle = FontStyles.Bold;
            _eventBanner.gameObject.SetActive(false);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            // ── 좌측: 소식 목록 ──
            var listPanel = UiFactory.CreatePanel(body, "List",
                new Vector2(0f, 0f), new Vector2(0.27f, 1f),
                Vector2.zero, Vector2.zero);

            UiTheme.CreateSectionLabel(listPanel, "ListLabel", "최근 소식",
                new Vector2(0f, 0.95f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16);

            var scrollRt = UiFactory.CreatePanel(listPanel, "Scroll",
                new Vector2(0f, 0f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _listContent = UiFactory.CreateStretchChild(viewport, "Content");
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);

            var vlg = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _listContent;

            // ── 우측: 선택한 뉴스 상세 ──
            var detail = UiTheme.CreateBorderedPanel(body, "Detail",
                new Vector2(0.30f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, UiTheme.CardWhite, 4f);

            UiTheme.CreateSectionLabel(detail, "DetailLabel", "선택한 뉴스",
                new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero, 16);

            var illustRt = UiTheme.CreateBorderedPanel(detail, "Illustration",
                new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.91f),
                Vector2.zero, Vector2.zero, new Color(0.94f, 0.90f, 0.82f), 2f);
            _illustrationImage = illustRt.gameObject.GetComponent<Image>();
            _illustrationImage.preserveAspect = true;
            _illustrationImage.raycastTarget = false;

            _headlineText = UiFactory.CreateText(detail, "Headline", "",
                new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.53f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 22, UiTheme.TextDark);
            _headlineText.fontStyle = FontStyles.Bold;

            _bodyText = UiFactory.CreateText(detail, "Body", "",
                new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.43f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 16, UiTheme.TextMuted);
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.overflowMode = TextOverflowModes.Ellipsis;

            var econBox = UiTheme.CreateBorderedPanel(detail, "EconBox",
                new Vector2(0.04f, 0.03f), new Vector2(0.49f, 0.22f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            UiFactory.CreateText(econBox, "Label", "경제 영향",
                new Vector2(0f, 0.6f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextMuted);
            _economyImpactText = UiFactory.CreateText(econBox, "Value", "",
                new Vector2(0f, 0f), new Vector2(1f, 0.6f), new Vector2(10f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.TopLeft, 14, UiTheme.TextDark);
            _economyImpactText.textWrappingMode = TextWrappingModes.Normal;

            var repBox = UiTheme.CreateBorderedPanel(detail, "RepBox",
                new Vector2(0.51f, 0.03f), new Vector2(0.96f, 0.22f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            UiFactory.CreateText(repBox, "Label", "평판 영향",
                new Vector2(0f, 0.6f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextMuted);
            _reputationImpactText = UiFactory.CreateText(repBox, "Value", "",
                new Vector2(0f, 0f), new Vector2(1f, 0.6f), new Vector2(10f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.TopLeft, 14, UiTheme.TextDark);
            _reputationImpactText.textWrappingMode = TextWrappingModes.Normal;

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(_root.transform, "ContinueBtn",
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 32f), new Vector2(-40f, 112f)),
                "다음으로", UiTheme.Accent, ContinueClicked, 20);
        }

        public bool TryShow(Action onContinue)
        {
            var news = ChangJun.News.NewsManager.Instance.TodayNews;
            if (news == null)
            {
                onContinue?.Invoke();
                return false;
            }

            _onContinue = onContinue;

            int day = DayLoopController.Instance.Day;
            _dateChip.text = $"{day}일차 {DayLoopController.Instance.FormatClock()}";

            RebuildList(news);
            Select(news);
            PopulateEventBanner();

            _root.SetActive(true);
            return true;
        }

        public void Hide() => _root.SetActive(false);

        private void ContinueClicked()
        {
            Hide();
            _onContinue?.Invoke();
        }

        private void RebuildList(NewsSO main)
        {
            foreach (var row in _listRows)
                UnityEngine.Object.Destroy(row);
            _listRows.Clear();

            AddListRow(main);
            foreach (var side in ChangJun.News.NewsManager.Instance.TodaySideStories)
                AddListRow(side);
        }

        private void AddListRow(NewsSO news)
        {
            if (news == null) return;

            var rowWrap = new GameObject($"Row_{news.headline}", typeof(RectTransform));
            rowWrap.transform.SetParent(_listContent, false);
            rowWrap.AddComponent<LayoutElement>().preferredHeight = 84;

            var row = UiTheme.CreateShadowCard(rowWrap.transform, "Card",
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, 0f),
                UiTheme.CardWhite, 3f, 4f);

            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = row.gameObject.GetComponent<Image>();
            btn.onClick.AddListener(() => Select(news));

            var swatch = UiFactory.CreatePanel(row, "Swatch",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -24f), new Vector2(62f, 24f));
            swatch.gameObject.AddComponent<Image>().color = CultureSwatch(news.cultureGroup);

            UiFactory.CreateText(row, "Title", news.headline,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.92f), new Vector2(76f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 15, UiTheme.TextDark);

            UiFactory.CreateText(row, "Tag", $"{SentimentLabel(news.sentiment)} · {news.sectionTag}",
                new Vector2(0f, 0.08f), new Vector2(1f, 0.5f), new Vector2(76f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 11, UiTheme.TextMuted);

            _listRows.Add(rowWrap);
        }

        private void Select(NewsSO news)
        {
            if (news == null) return;
            _selected = news;

            _headlineText.text = news.headline;
            _bodyText.text = !string.IsNullOrWhiteSpace(news.article) ? news.article
                : !string.IsNullOrWhiteSpace(news.body) ? news.body
                : "오늘의 소식이 전해지고 있습니다.";

            if (news.illustration != null)
            {
                _illustrationImage.sprite = news.illustration;
                _illustrationImage.color = Color.white;
            }
            else
            {
                _illustrationImage.sprite = null;
                _illustrationImage.color = new Color(0.94f, 0.90f, 0.82f);
            }

            string menuEffect = news.priceMultiplier >= 1f
                ? $"{CultureLabel(news.cultureGroup)} 수요 상승 (판매가 x{news.priceMultiplier:0.00})"
                : $"{CultureLabel(news.cultureGroup)} 손님 감소 가능 (판매가 x{news.priceMultiplier:0.00})";
            _economyImpactText.text = menuEffect;

            _reputationImpactText.text = news.sentiment switch
            {
                NewsSentiment.Discrimination => "차별·편견 보도 — 상생 지수에 악영향을 줄 수 있어요.",
                NewsSentiment.Positive => "긍정적인 여론 — 평판에 도움이 될 수 있어요.",
                _ => "특별한 평판 영향은 없습니다.",
            };
        }

        private void PopulateEventBanner()
        {
            var events = CulturalEventManager.Instance;
            if (events == null || events.TodayEvent == ActiveCulturalEvent.None)
            {
                _eventBanner.gameObject.SetActive(false);
                return;
            }

            _eventBanner.gameObject.SetActive(true);
            _eventBanner.text = events.TodayEvent switch
            {
                ActiveCulturalEvent.CultureFestival =>
                    $"★ 문화 축제 ★  {CultureLabel(events.FestivalCulture)} 손님 ×2 · 메뉴 +10%",
                ActiveCulturalEvent.FusionWorkshop =>
                    "★ 퓨전 워크숍 ★  M20~M23 퓨전 메뉴 주문 가능",
                _ => "",
            };
        }

        private static TextMeshProUGUI CreateHeaderChip(RectTransform header, string text)
        {
            var chip = UiTheme.CreateBorderedPanel(header,
                "DateChip", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-190f, -18f), new Vector2(-26f, 18f), UiTheme.CardWhite, 2f);
            return UiFactory.CreateText(chip, "Text", text,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, UiTheme.TextDark);
        }

        private static Color CultureSwatch(CultureGroup c) => c switch
        {
            CultureGroup.Korean => UiTheme.Accent,
            CultureGroup.Muslim => UiTheme.Success,
            CultureGroup.Hindu => UiTheme.Gold,
            CultureGroup.Vegan => UiTheme.Success,
            CultureGroup.SEAsian => UiTheme.Info,
            CultureGroup.AfricanAmerican => new Color32(0xD9, 0x8C, 0xB0, 0xFF),
            _ => UiTheme.TextFaint,
        };

        private static string CultureLabel(CultureGroup culture) => culture switch
        {
            CultureGroup.Korean => "한식·한국 문화",
            CultureGroup.Muslim => "무슬림·할랄 문화",
            CultureGroup.Hindu => "힌두·인도 문화",
            CultureGroup.Vegan => "비건·채식 문화",
            CultureGroup.SEAsian => "동남아 문화",
            CultureGroup.AfricanAmerican => "다문화·소수 문화",
            _ => "일반",
        };

        private static string SentimentLabel(NewsSentiment sentiment) => sentiment switch
        {
            NewsSentiment.Positive => "긍정",
            NewsSentiment.Negative => "부정",
            NewsSentiment.Discrimination => "차별·편견",
            _ => "보통",
        };
    }
}
