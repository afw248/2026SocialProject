using System;
using System.Collections.Generic;
using System.Text;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.News;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 전체 화면 신문 UI — 1면(헤드라인) → 증권면(주식) → 영업 브리핑.
    /// </summary>
    public sealed class NewsOverlay
    {
        private const int PageFront = 0;
        private const int PageMarket = 1;
        private const int PageBriefing = 2;

        private readonly GameObject _root;
        private readonly GameObject[] _pages = new GameObject[3];
        private TextMeshProUGUI _dateText;
        private TextMeshProUGUI _headline;
        private TextMeshProUGUI _subheadline;
        private TextMeshProUGUI _articleBody;
        private TextMeshProUGUI _sidebarText;
        private TextMeshProUGUI _tickerStrip;
        private TextMeshProUGUI _marketHeader;
        private TextMeshProUGUI _portfolioText;
        private RectTransform _marketListRoot;
        private TextMeshProUGUI _briefingHeadline;
        private TextMeshProUGUI _briefingBody;
        private readonly TextMeshProUGUI _actionLabel;
        private readonly Button _actionButton;

        private readonly List<GameObject> _marketRows = new();
        private NewsSO _current;
        private int _page;
        private Action _onDismissed;

        public NewsOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("NewsOverlay", 200);
            _root.SetActive(false);

            var paper = UiFactory.CreateStretchChild(_root.transform, "Paper");
            paper.gameObject.AddComponent<Image>().color = new Color(0.97f, 0.95f, 0.89f);

            BuildMasthead(paper);
            _dateText = UiFactory.CreateText(paper, "Date", "",
                new Vector2(0.05f, 0.905f), new Vector2(0.95f, 0.935f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 16,
                new Color(0.38f, 0.34f, 0.3f));

            _pages[PageFront] = BuildFrontPage(paper);
            _pages[PageMarket] = BuildMarketPage(paper);
            _pages[PageBriefing] = BuildBriefingPage(paper);

            var btnRt = UiFactory.CreatePanel(paper, "Action",
                new Vector2(0.38f, 0.02f), new Vector2(0.62f, 0.08f),
                Vector2.zero, Vector2.zero);
            _actionButton = btnRt.gameObject.AddComponent<Button>();
            _actionButton.targetGraphic = btnRt.gameObject.AddComponent<Image>();
            _actionButton.targetGraphic.color = new Color(0.14f, 0.16f, 0.22f);
            _actionButton.onClick.AddListener(OnAction);

            _actionLabel = UiFactory.CreateText(btnRt, "Label", "다음",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 24, Color.white);
        }

        public bool TryShow(Action onDismissed)
        {
            var news = NewsManager.Instance.TodayNews;
            if (news == null)
            {
                onDismissed?.Invoke();
                return false;
            }

            _current = news;
            _onDismissed = onDismissed;
            _page = PageFront;
            ShowPage(_page);

            int day = DayLoopController.Instance.Day;
            _dateText.text = $"{day}일차  ·  {DateTime.Now:yyyy년 M월 d일}  ·  제{day}호  ·  가격 0원";

            PopulateFrontPage(news);
            PopulateMarketPage();
            PopulateBriefingPage(news);

            _root.SetActive(true);
            return true;
        }

        public void Hide()
        {
            _root.SetActive(false);
            _current = null;
        }

        private void OnAction()
        {
            if (_page < PageBriefing)
            {
                _page++;
                ShowPage(_page);
                return;
            }

            Hide();
            _onDismissed?.Invoke();
            _onDismissed = null;
        }

        private void ShowPage(int page)
        {
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == page);

            _actionLabel.text = page switch
            {
                PageFront => "증권면 →",
                PageMarket => "영업 브리핑 →",
                _ => "영업 시작",
            };
        }

        private void BuildMasthead(RectTransform paper)
        {
            var ruleTop = UiFactory.CreatePanel(paper, "RuleTop",
                new Vector2(0.04f, 0.94f), new Vector2(0.96f, 0.942f),
                Vector2.zero, Vector2.zero);
            ruleTop.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f);

            var masthead = UiFactory.CreateText(paper, "Masthead", "CUP RICE TIMES",
                new Vector2(0.04f, 0.855f), new Vector2(0.96f, 0.94f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 52,
                new Color(0.06f, 0.05f, 0.04f));
            masthead.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;

            UiFactory.CreateText(paper, "Tagline", "다양한 문화를 이해하는 식탁  ·  통합사회 특집",
                new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.855f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15,
                new Color(0.42f, 0.36f, 0.3f));

            var ruleMid = UiFactory.CreatePanel(paper, "RuleMid",
                new Vector2(0.04f, 0.825f), new Vector2(0.96f, 0.828f),
                Vector2.zero, Vector2.zero);
            ruleMid.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f);
        }

        private GameObject BuildFrontPage(RectTransform paper)
        {
            var page = UiFactory.CreatePanel(paper, "FrontPage",
                new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.82f),
                Vector2.zero, Vector2.zero).gameObject;

            var section = UiFactory.CreateText(page.transform, "Section", "1면  특집",
                new Vector2(0.02f, 0.94f), new Vector2(0.3f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.BottomLeft, 14,
                new Color(0.5f, 0.2f, 0.15f));
            section.fontStyle = FontStyles.Bold;

            _headline = UiFactory.CreateText(page.transform, "Headline", "",
                new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.94f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 40,
                new Color(0.07f, 0.05f, 0.04f));
            _headline.fontStyle = FontStyles.Bold;
            _headline.textWrappingMode = TextWrappingModes.Normal;
            _headline.lineSpacing = -6f;

            _subheadline = UiFactory.CreateText(page.transform, "Subhead", "",
                new Vector2(0.02f, 0.7f), new Vector2(0.98f, 0.78f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 20,
                new Color(0.28f, 0.24f, 0.2f));
            _subheadline.fontStyle = FontStyles.Italic;
            _subheadline.textWrappingMode = TextWrappingModes.Normal;

            var colRule = UiFactory.CreatePanel(page.transform, "ColRule",
                new Vector2(0.66f, 0.04f), new Vector2(0.662f, 0.68f),
                Vector2.zero, Vector2.zero);
            colRule.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.7f, 0.62f);

            _articleBody = UiFactory.CreateText(page.transform, "Article", "",
                new Vector2(0.02f, 0.04f), new Vector2(0.64f, 0.68f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 19,
                new Color(0.16f, 0.14f, 0.12f));
            _articleBody.textWrappingMode = TextWrappingModes.Normal;
            _articleBody.lineSpacing = 4f;

            var sideBox = UiFactory.CreatePanel(page.transform, "SideBox",
                new Vector2(0.68f, 0.18f), new Vector2(0.98f, 0.68f),
                Vector2.zero, Vector2.zero);
            sideBox.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.9f, 0.82f);

            UiFactory.CreateText(sideBox, "SideTitle", "함께 읽기",
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16,
                new Color(0.35f, 0.25f, 0.18f)).fontStyle = FontStyles.Bold;

            var sideScrollRt = UiFactory.CreatePanel(sideBox, "SideScroll",
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.85f),
                Vector2.zero, Vector2.zero);
            var sideScroll = sideScrollRt.gameObject.AddComponent<ScrollRect>();
            sideScroll.horizontal = false;

            var sideViewport = UiFactory.CreateStretchChild(sideScrollRt, "Viewport");
            sideViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            sideViewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

            var sideContent = UiFactory.CreateStretchChild(sideViewport, "Content");
            sideContent.pivot = new Vector2(0.5f, 1f);
            sideContent.anchorMin = new Vector2(0f, 1f);
            sideContent.anchorMax = new Vector2(1f, 1f);
            sideContent.offsetMin = Vector2.zero;
            sideContent.offsetMax = Vector2.zero;

            _sidebarText = sideContent.gameObject.AddComponent<TextMeshProUGUI>();
            _sidebarText.text = string.Empty;
            _sidebarText.fontSize = 14;
            _sidebarText.color = new Color(0.2f, 0.18f, 0.15f);
            _sidebarText.alignment = TextAlignmentOptions.TopLeft;
            _sidebarText.textWrappingMode = TextWrappingModes.Normal;
            _sidebarText.lineSpacing = 2f;
            _sidebarText.raycastTarget = false;
            KoreanUiFont.Apply(_sidebarText);

            var sideFitter = sideContent.gameObject.AddComponent<ContentSizeFitter>();
            sideFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sideScroll.viewport = sideViewport;
            sideScroll.content = sideContent;

            var tickerBg = UiFactory.CreatePanel(page.transform, "TickerBg",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 0.08f),
                Vector2.zero, Vector2.zero);
            tickerBg.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);

            _tickerStrip = UiFactory.CreateText(tickerBg, "Ticker", "",
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 15, new Color(0.9f, 0.92f, 0.95f));
            _tickerStrip.textWrappingMode = TextWrappingModes.Normal;

            return page;
        }

        private GameObject BuildMarketPage(RectTransform paper)
        {
            var page = UiFactory.CreatePanel(paper, "MarketPage",
                new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.82f),
                Vector2.zero, Vector2.zero).gameObject;
            page.SetActive(false);

            UiFactory.CreateText(page.transform, "Section", "증권  ·  문화푸드 지수",
                new Vector2(0.02f, 0.92f), new Vector2(0.6f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.BottomLeft, 18,
                new Color(0.15f, 0.25f, 0.35f)).fontStyle = FontStyles.Bold;

            _marketHeader = UiFactory.CreateText(page.transform, "MarketNote", "",
                new Vector2(0.02f, 0.84f), new Vector2(0.98f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 16,
                new Color(0.25f, 0.22f, 0.18f));
            _marketHeader.textWrappingMode = TextWrappingModes.Normal;

            _portfolioText = UiFactory.CreateText(page.transform, "Portfolio", "",
                new Vector2(0.02f, 0.77f), new Vector2(0.98f, 0.83f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 17,
                new Color(0.1f, 0.35f, 0.2f));
            _portfolioText.fontStyle = FontStyles.Bold;

            var headerRow = UiFactory.CreatePanel(page.transform, "HeaderRow",
                new Vector2(0.02f, 0.72f), new Vector2(0.98f, 0.76f),
                Vector2.zero, Vector2.zero);
            headerRow.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);
            UiFactory.CreateText(headerRow, "H", "종목          현재가    등락      보유    거래",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 15, Color.white);

            var scrollRt = UiFactory.CreatePanel(page.transform, "MarketScroll",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.71f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            _marketListRoot = UiFactory.CreateStretchChild(viewport, "Content");
            _marketListRoot.pivot = new Vector2(0.5f, 1f);
            _marketListRoot.anchorMin = new Vector2(0, 1);
            _marketListRoot.anchorMax = new Vector2(1, 1);
            _marketListRoot.offsetMin = Vector2.zero;
            _marketListRoot.offsetMax = Vector2.zero;

            var vlg = _marketListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(0, 0, 4, 4);

            var fitter = _marketListRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = _marketListRoot;

            return page;
        }

        private GameObject BuildBriefingPage(RectTransform paper)
        {
            var page = UiFactory.CreatePanel(paper, "BriefingPage",
                new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.82f),
                Vector2.zero, Vector2.zero).gameObject;
            page.SetActive(false);

            UiFactory.CreateText(page.transform, "Section", "영업 브리핑",
                new Vector2(0.02f, 0.92f), new Vector2(0.5f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.BottomLeft, 18,
                new Color(0.35f, 0.28f, 0.15f)).fontStyle = FontStyles.Bold;

            _briefingHeadline = UiFactory.CreateText(page.transform, "BriefHead", "",
                new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 30,
                new Color(0.1f, 0.08f, 0.06f));
            _briefingHeadline.fontStyle = FontStyles.Bold;
            _briefingHeadline.textWrappingMode = TextWrappingModes.Normal;

            var box = UiFactory.CreatePanel(page.transform, "BriefBox",
                new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.76f),
                Vector2.zero, Vector2.zero);
            box.gameObject.AddComponent<Image>().color = new Color(0.94f, 0.92f, 0.86f);

            _briefingBody = UiFactory.CreateText(box, "BriefBody", "",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 22,
                new Color(0.18f, 0.16f, 0.14f));
            _briefingBody.textWrappingMode = TextWrappingModes.Normal;
            _briefingBody.lineSpacing = 6f;

            return page;
        }

        private void PopulateFrontPage(NewsSO news)
        {
            _headline.text = news.headline;
            _subheadline.text = string.IsNullOrWhiteSpace(news.subheadline)
                ? news.body
                : news.subheadline;
            _articleBody.text = GetArticleText(news);
            _sidebarText.text = BuildSidebar(news);
            _tickerStrip.text = BuildTickerStrip();
        }

        private void PopulateMarketPage()
        {
            var news = _current;
            _marketHeader.text = news != null
                ? $"오늘의 헤드라인 「{news.headline}」이(가) 관련 종목 시세에 반영되었습니다. 아침 장 시작 전에 매수·매도할 수 있습니다."
                : "문화푸드 지수 시세입니다.";

            RefreshPortfolioLine();
            RebuildMarketRows();
        }

        private void PopulateBriefingPage(NewsSO news)
        {
            _briefingHeadline.text = $"오늘의 영업 포인트 — {news.headline}";
            _briefingBody.text = BuildBriefing(news);
        }

        private void RefreshPortfolioLine()
        {
            if (StockMarketManager.Instance == null)
            {
                _portfolioText.text = "현금: -";
                return;
            }

            int cash = MoneyManager.Instance.Money;
            int portfolio = StockMarketManager.Instance.GetPortfolioValue();
            _portfolioText.text = $"현금 {cash:N0}원  ·  보유 주식 평가 {portfolio:N0}원  ·  총자산 {cash + portfolio:N0}원";
        }

        private void RebuildMarketRows()
        {
            foreach (var row in _marketRows)
                UnityEngine.Object.Destroy(row);
            _marketRows.Clear();

            if (StockMarketManager.Instance == null) return;

            foreach (var ticker in StockMarketManager.Instance.Tickers)
            {
                if (ticker == null) continue;
                _marketRows.Add(CreateMarketRow(ticker));
            }
        }

        private GameObject CreateMarketRow(StockTickerSO ticker)
        {
            var row = UiFactory.CreatePanel(_marketListRoot, $"Row_{ticker.code}",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.97f, 0.94f);
            row.sizeDelta = new Vector2(0, 52);

            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;

            int price = StockMarketManager.Instance.GetPrice(ticker.code);
            float change = StockMarketManager.Instance.GetChangePercent(ticker.code);
            int holding = StockMarketManager.Instance.GetHolding(ticker.code);
            string changeStr = change >= 0 ? $"+{change:0.0}%" : $"{change:0.0}%";
            Color changeColor = change >= 0 ? new Color(0.75f, 0.15f, 0.12f) : new Color(0.12f, 0.3f, 0.65f);

            var info = UiFactory.CreateText(row, "Info",
                $"{ticker.code,-6} {ticker.displayName,-10} {price,7:N0}원",
                new Vector2(0.02f, 0.1f), new Vector2(0.55f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16,
                new Color(0.12f, 0.1f, 0.08f));

            var changeText = UiFactory.CreateText(row, "Change", changeStr,
                new Vector2(0.55f, 0.1f), new Vector2(0.66f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 16, changeColor);
            changeText.fontStyle = FontStyles.Bold;

            UiFactory.CreateText(row, "Hold", $"{holding}주",
                new Vector2(0.67f, 0.1f), new Vector2(0.76f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15,
                new Color(0.3f, 0.28f, 0.25f));

            var buyRt = UiFactory.CreatePanel(row, "Buy",
                new Vector2(0.78f, 0.12f), new Vector2(0.88f, 0.88f),
                Vector2.zero, Vector2.zero);
            var buyBtn = buyRt.gameObject.AddComponent<Button>();
            buyBtn.targetGraphic = buyRt.gameObject.AddComponent<Image>();
            buyBtn.targetGraphic.color = new Color(0.7f, 0.2f, 0.18f);
            string code = ticker.code;
            buyBtn.onClick.AddListener(() => OnBuy(code));
            UiFactory.CreateText(buyRt, "L", "매수",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14, Color.white);

            var sellRt = UiFactory.CreatePanel(row, "Sell",
                new Vector2(0.9f, 0.12f), new Vector2(0.99f, 0.88f),
                Vector2.zero, Vector2.zero);
            var sellBtn = sellRt.gameObject.AddComponent<Button>();
            sellBtn.targetGraphic = sellRt.gameObject.AddComponent<Image>();
            sellBtn.targetGraphic.color = new Color(0.2f, 0.35f, 0.55f);
            sellBtn.onClick.AddListener(() => OnSell(code));
            UiFactory.CreateText(sellRt, "L", "매도",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14, Color.white);

            return row.gameObject;
        }

        private void OnBuy(string code)
        {
            if (StockMarketManager.Instance.TryBuy(code))
            {
                RefreshPortfolioLine();
                RebuildMarketRows();
            }
        }

        private void OnSell(string code)
        {
            if (StockMarketManager.Instance.TrySell(code))
            {
                RefreshPortfolioLine();
                RebuildMarketRows();
            }
        }

        private string BuildTickerStrip()
        {
            if (StockMarketManager.Instance == null)
                return "시세 정보 없음";

            var sb = new StringBuilder();
            foreach (var ticker in StockMarketManager.Instance.Tickers)
            {
                if (ticker == null) continue;
                float ch = StockMarketManager.Instance.GetChangePercent(ticker.code);
                string arrow = ch >= 0 ? "▲" : "▼";
                sb.Append($"{ticker.code} {StockMarketManager.Instance.GetPrice(ticker.code):N0} {arrow}{Mathf.Abs(ch):0.0}%   ");
            }
            return sb.ToString().Trim();
        }

        private static string BuildSidebar(NewsSO main)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(main.sidebarNote))
            {
                sb.AppendLine("【 오늘의 시각 】");
                sb.AppendLine(main.sidebarNote);
                sb.AppendLine();
            }

            var sides = NewsManager.Instance.TodaySideStories;
            for (int i = 0; i < sides.Count; i++)
            {
                var s = sides[i];
                sb.AppendLine($"- {s.headline}");
                sb.AppendLine(string.IsNullOrWhiteSpace(s.body) ? s.subheadline : s.body);
                sb.AppendLine();
            }

            sb.AppendLine("【 오늘의 문화 키워드 】");
            sb.Append(CultureLabel(main.cultureGroup));
            sb.Append(" · ");
            sb.Append(SentimentLabel(main.sentiment));
            return sb.ToString().TrimEnd();
        }

        private static string BuildBriefing(NewsSO news)
        {
            if (!string.IsNullOrWhiteSpace(news.summary))
                return news.summary + "\n\n" + BuildEffectBlock(news);

            return BuildEffectBlock(news);
        }

        private static string BuildEffectBlock(NewsSO news)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"> 영향 문화권: {CultureLabel(news.cultureGroup)}");
            sb.AppendLine($"> 보도 톤: {SentimentLabel(news.sentiment)} ({news.sectionTag})");
            sb.AppendLine();

            string menuEffect = news.priceMultiplier >= 1f
                ? $"해당 문화권 메뉴 수요 상승  (판매가 x{news.priceMultiplier:0.00})"
                : $"해당 문화권 손님 감소 가능  (판매가 x{news.priceMultiplier:0.00})";
            sb.AppendLine("【 가게 영향 】");
            sb.AppendLine(menuEffect);

            if (!string.IsNullOrEmpty(news.primaryStockCode))
                sb.AppendLine($"> 관련 종목: {news.primaryStockCode}");

            sb.AppendLine();
            sb.AppendLine("【 증권면 안내 】");
            sb.AppendLine("앞 페이지에서 문화푸드 주식을 사고팔 수 있습니다.");
            sb.AppendLine("뉴스와 연결된 문화권 종목이 오늘 더 크게 움직일 수 있어요.");
            sb.AppendLine();
            sb.AppendLine("준비되면 영업을 시작하세요!");
            return sb.ToString();
        }

        private static string GetArticleText(NewsSO news)
        {
            if (!string.IsNullOrWhiteSpace(news.article))
                return news.article;

            if (!string.IsNullOrWhiteSpace(news.body))
                return news.body;

            return "오늘의 소식이 전해지고 있습니다.";
        }

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
