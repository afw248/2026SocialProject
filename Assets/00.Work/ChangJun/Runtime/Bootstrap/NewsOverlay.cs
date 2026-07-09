using System;
using ChangJun.Data;
using ChangJun.News;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 전체 화면 신문 UI — 기사 → 결론 요약 2단.
    /// </summary>
    public sealed class NewsOverlay
    {
        private readonly GameObject _root;
        private readonly GameObject _articlePage;
        private readonly GameObject _summaryPage;
        private readonly TextMeshProUGUI _dateText;
        private readonly TextMeshProUGUI _headline;
        private readonly TextMeshProUGUI _articleBody;
        private readonly TextMeshProUGUI _summaryHeadline;
        private readonly TextMeshProUGUI _summaryBody;
        private readonly TextMeshProUGUI _actionLabel;
        private readonly Button _actionButton;
        private NewsSO _current;
        private Action _onDismissed;

        public NewsOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("NewsOverlay", 200);
            _root.SetActive(false);

            var paper = UiFactory.CreateStretchChild(_root.transform, "Paper");
            paper.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.94f, 0.88f);

            UiFactory.CreateText(paper, "Masthead", "CUP RICE TIMES",
                new Vector2(0.06f, 0.9f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 42,
                new Color(0.08f, 0.08f, 0.1f)).fontStyle = FontStyles.Bold;

            _dateText = UiFactory.CreateText(paper, "Date", "",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 18,
                new Color(0.35f, 0.32f, 0.28f));

            var rule = UiFactory.CreatePanel(paper, "Rule",
                new Vector2(0.06f, 0.845f), new Vector2(0.94f, 0.848f),
                Vector2.zero, Vector2.zero);
            rule.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f);

            _articlePage = UiFactory.CreatePanel(paper, "ArticlePage",
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.83f),
                Vector2.zero, Vector2.zero).gameObject;

            _headline = UiFactory.CreateText(_articlePage.transform, "Headline", "",
                new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 36,
                new Color(0.1f, 0.08f, 0.06f));
            _headline.fontStyle = FontStyles.Bold;
            _headline.enableWordWrapping = true;

            _articleBody = UiFactory.CreateText(_articlePage.transform, "Article", "",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.76f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 24,
                new Color(0.18f, 0.16f, 0.14f));
            _articleBody.enableWordWrapping = true;
            _articleBody.lineSpacing = 8f;

            _summaryPage = UiFactory.CreatePanel(paper, "SummaryPage",
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.83f),
                Vector2.zero, Vector2.zero).gameObject;
            _summaryPage.SetActive(false);

            UiFactory.CreateText(_summaryPage.transform, "Tag", "오늘의 결론",
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22,
                new Color(0.45f, 0.35f, 0.2f));

            _summaryHeadline = UiFactory.CreateText(_summaryPage.transform, "SummaryHeadline", "",
                new Vector2(0.02f, 0.68f), new Vector2(0.98f, 0.86f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 30,
                new Color(0.12f, 0.1f, 0.08f));
            _summaryHeadline.fontStyle = FontStyles.Bold;
            _summaryHeadline.enableWordWrapping = true;

            _summaryBody = UiFactory.CreateText(_summaryPage.transform, "SummaryBody", "",
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.66f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 24,
                new Color(0.2f, 0.18f, 0.15f));
            _summaryBody.enableWordWrapping = true;
            _summaryBody.lineSpacing = 6f;

            var btnRt = UiFactory.CreatePanel(paper, "Action",
                new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.11f),
                Vector2.zero, Vector2.zero);
            _actionButton = btnRt.gameObject.AddComponent<Button>();
            _actionButton.targetGraphic = btnRt.gameObject.AddComponent<Image>();
            _actionButton.targetGraphic.color = new Color(0.2f, 0.22f, 0.3f);
            _actionButton.onClick.AddListener(OnAction);

            _actionLabel = UiFactory.CreateText(btnRt, "Label", "다음",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 26, Color.white);
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
            _articlePage.SetActive(true);
            _summaryPage.SetActive(false);
            _actionLabel.text = "다음";

            int day = DayLoopController.Instance.Day;
            _dateText.text = $"{day}일차 아침  ·  특별 취재";

            _headline.text = news.headline;
            _articleBody.text = GetArticleText(news);

            _summaryHeadline.text = news.headline;
            _summaryBody.text = BuildSummary(news);

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
            if (_summaryPage.activeSelf)
            {
                Hide();
                _onDismissed?.Invoke();
                _onDismissed = null;
                return;
            }

            _articlePage.SetActive(false);
            _summaryPage.SetActive(true);
            _actionLabel.text = "영업 시작";
        }

        private static string GetArticleText(NewsSO news)
        {
            if (!string.IsNullOrWhiteSpace(news.article))
                return news.article;

            if (!string.IsNullOrWhiteSpace(news.body))
                return news.body;

            return "오늘의 소식이 전해지고 있습니다.";
        }

        private static string BuildSummary(NewsSO news)
        {
            if (!string.IsNullOrWhiteSpace(news.summary))
                return news.summary;

            string culture = news.cultureGroup.ToString();
            string effect = news.priceMultiplier >= 1f
                ? $"해당 문화권 메뉴 수요가 늘어날 수 있습니다. (가격 x{news.priceMultiplier:0.00})"
                : $"해당 문화권 손님의 발길이 줄어들 수 있습니다. (가격 x{news.priceMultiplier:0.00})";

            return $"영향 문화권: {culture}\n" +
                   $"분위기: {SentimentLabel(news.sentiment)}\n\n" +
                   effect + "\n\n" +
                   "오늘 영업에 반영됩니다.";
        }

        private static string SentimentLabel(NewsSentiment sentiment) => sentiment switch
        {
            NewsSentiment.Positive => "긍정",
            NewsSentiment.Negative => "부정",
            NewsSentiment.Discrimination => "차별·편견",
            _ => "보통",
        };
    }
}
