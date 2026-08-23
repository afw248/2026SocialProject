using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 주식 시장 — 목록에서 상세보기를 누르면 거래 팝업이 열린다.
    /// </summary>
    public sealed class StockMarketOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _listContent;
        private readonly RectTransform _holdingsContent;
        private readonly UiTheme.HeaderMeta _headerMeta;
        private readonly TextMeshProUGUI _totalAssetsText;
        private readonly GameObject _popupRoot;
        private TextMeshProUGUI _popupName;
        private TextMeshProUGUI _popupPrice;
        private TextMeshProUGUI _popupQty;
        private TextMeshProUGUI _popupTotal;
        private TextMeshProUGUI _popupFeedback;
        private Image _popupBadge;
        private readonly List<GameObject> _listRows = new();
        private readonly List<GameObject> _holdingRows = new();

        private StockTickerSO _selected;
        private int _tradeQty = 1;
        private Action _onContinue;

        public event Action OnBack;

        public StockMarketOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("StockMarketOverlay", 200);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "주식 시장", 72f, 78f);
            UiTheme.CreateBackButton(header, () => OnBack?.Invoke());
            _headerMeta = UiTheme.CreateHeaderMeta(header);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            var listPanel = UiFactory.CreatePanel(body, "List",
                Vector2.zero, new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero);

            UiTheme.CreateSectionLabel(listPanel, "ListLabel", "종목 목록",
                new Vector2(0f, 0.95f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16);

            var headerRow = UiTheme.CreateBorderedPanel(listPanel, "HeaderRow",
                new Vector2(0f, 0.87f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            CreateColumnText(headerRow, "종목명", 0f, 0.32f, UiTheme.TextMuted);
            CreateColumnText(headerRow, "현재가", 0.32f, 0.52f, UiTheme.TextMuted);
            CreateColumnText(headerRow, "변동률", 0.52f, 0.74f, UiTheme.TextMuted);
            CreateColumnText(headerRow, "상세", 0.74f, 1f, UiTheme.TextMuted);

            var scrollRt = UiFactory.CreatePanel(listPanel, "Scroll",
                new Vector2(0f, 0f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);
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
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _listContent;

            var side = UiFactory.CreatePanel(body, "Side",
                new Vector2(0.69f, 0.12f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var assetsCard = UiTheme.CreateShadowCard(side, "Assets",
                new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            UiFactory.CreateText(assetsCard, "Label", "총 자산",
                new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);
            _totalAssetsText = UiFactory.CreateText(assetsCard, "Value", "",
                new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.6f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26, UiTheme.TextDark);

            var holdingsCard = UiTheme.CreateShadowCard(side, "Holdings",
                new Vector2(0f, 0f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            UiFactory.CreateText(holdingsCard, "Label", "보유 종목",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);
            _holdingsContent = UiFactory.CreatePanel(holdingsCard, "HoldingsContent",
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);
            var hvlg = _holdingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            hvlg.spacing = 6;
            hvlg.childControlWidth = true;
            hvlg.childControlHeight = true;
            hvlg.childForceExpandWidth = true;
            hvlg.childForceExpandHeight = false;

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(body, "ContinueBtn",
                    new Vector2(0.69f, 0f), new Vector2(1f, 0.10f), Vector2.zero, Vector2.zero),
                "영업 시작", UiTheme.Accent, ContinueClicked, 20);

            _popupRoot = BuildTradePopup();

            if (StockMarketManager.Instance != null)
                StockMarketManager.Instance.OnMarketUpdated += RefreshAll;
        }

        private GameObject BuildTradePopup()
        {
            var popup = new GameObject("TradePopup", typeof(RectTransform));
            popup.transform.SetParent(_root.transform, false);
            UiFactory.Stretch(popup.GetComponent<RectTransform>());
            popup.SetActive(false);

            var dim = UiFactory.CreateStretchChild(popup.transform, "Dim");
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.08f, 0.04f, 0.02f, 0.55f);
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(HidePopup);

            var card = UiTheme.CreateShadowCard(popup.transform, "Card",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-250f, -210f), new Vector2(250f, 210f),
                UiTheme.CardWhite, 4f, 8f);

            var badge = UiFactory.CreatePanel(card, "Badge",
                new Vector2(0.06f, 0.82f), new Vector2(0.18f, 0.94f), Vector2.zero, Vector2.zero);
            _popupBadge = badge.gameObject.AddComponent<Image>();
            _popupBadge.color = UiTheme.Accent;

            _popupName = UiFactory.CreateText(card, "Name", "",
                new Vector2(0.22f, 0.80f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, UiTheme.TextDark);
            _popupPrice = UiFactory.CreateText(card, "Price", "",
                new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.80f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 15, UiTheme.TextMuted);

            CreateStepButton(card, new Vector2(0.08f, 0.50f), new Vector2(0.22f, 0.64f), "-", () => AdjustTradeQty(-1));
            _popupQty = UiFactory.CreateText(card, "Qty", "수량 1",
                new Vector2(0.24f, 0.50f), new Vector2(0.76f, 0.64f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 16, UiTheme.TextDark);
            CreateStepButton(card, new Vector2(0.78f, 0.50f), new Vector2(0.92f, 0.64f), "+", () => AdjustTradeQty(1));

            _popupTotal = UiFactory.CreateText(card, "Total", "총비용  0원",
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.48f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 18, UiTheme.TextDark);

            _popupFeedback = UiFactory.CreateText(card, "Feedback", "",
                new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 13, UiTheme.Danger);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(card, "BuyBtn", new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.24f),
                    Vector2.zero, Vector2.zero),
                "매수", UiTheme.Success, OnBuyClicked, 16);
            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(card, "SellBtn", new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.24f),
                    Vector2.zero, Vector2.zero),
                "매도", UiTheme.Danger, OnSellClicked, 16);

            return popup;
        }

        public void Show(Action onContinue)
        {
            _onContinue = onContinue;
            _tradeQty = 1;
            HidePopup();

            if (_selected == null && StockMarketManager.Instance != null)
            {
                foreach (var ticker in StockMarketManager.Instance.Tickers)
                {
                    _selected = ticker;
                    break;
                }
            }

            RefreshAll();
            _root.SetActive(true);
        }

        public void Hide()
        {
            HidePopup();
            _root.SetActive(false);
        }

        private void ContinueClicked()
        {
            Hide();
            _onContinue?.Invoke();
        }

        private void OpenPopup(StockTickerSO ticker)
        {
            _selected = ticker;
            _tradeQty = 1;
            if (_popupFeedback != null) _popupFeedback.text = "";
            RefreshPopup();
            _popupRoot.SetActive(true);
            _popupRoot.transform.SetAsLastSibling();
        }

        private void HidePopup() => _popupRoot.SetActive(false);

        private void AddListRow(StockTickerSO ticker)
        {
            var rowWrap = new GameObject($"Row_{ticker.code}", typeof(RectTransform));
            rowWrap.transform.SetParent(_listContent, false);
            rowWrap.AddComponent<LayoutElement>().preferredHeight = 66;

            var row = UiTheme.CreateShadowCard(rowWrap.transform, "Card",
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, 0f),
                UiTheme.CardWhite, 3f, 4f);

            var badge = UiFactory.CreatePanel(row, "Badge",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -18f), new Vector2(48f, 18f));
            badge.gameObject.AddComponent<Image>().color = TickerColor(ticker);
            UiFactory.CreateText(badge, "Initial", ticker.displayName.Length > 0 ? ticker.displayName[^1..] : "?",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14, UiTheme.CardWhite);

            int holding = StockMarketManager.Instance.GetHolding(ticker.code);
            string nameLabel = holding > 0
                ? $"{ticker.displayName}\n<size=80%><color=#8A6238>보유 {holding}주</color></size>"
                : ticker.displayName;
            var nameText = UiFactory.CreateText(row, "Name", nameLabel,
                new Vector2(0f, 0f), new Vector2(0.32f, 1f), new Vector2(58f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextDark);
            nameText.textWrappingMode = TextWrappingModes.Normal;

            var price = StockMarketManager.Instance.GetPrice(ticker.code);
            UiFactory.CreateText(row, "Price", $"{price:N0}원",
                new Vector2(0.32f, 0f), new Vector2(0.52f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextDark);

            float change = StockMarketManager.Instance.GetChangePercent(ticker.code);
            string changeStr = change >= 0 ? $"+{change:0.0}%" : $"{change:0.0}%";
            var changeColor = change > 0 ? UiTheme.Success : change < 0 ? UiTheme.Danger : UiTheme.TextMuted;
            UiFactory.CreateText(row, "Change", changeStr,
                new Vector2(0.52f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, changeColor);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(row, "Detail", new Vector2(0.76f, 0.18f), new Vector2(0.97f, 0.82f),
                    Vector2.zero, Vector2.zero),
                "상세보기", UiTheme.Accent, () => OpenPopup(ticker), 13);

            _listRows.Add(rowWrap);
        }

        private void AdjustTradeQty(int delta)
        {
            _tradeQty = Mathf.Max(1, _tradeQty + delta);
            RefreshPopup();
        }

        private void OnBuyClicked()
        {
            if (_selected == null || StockMarketManager.Instance == null) return;
            if (StockMarketManager.Instance.TryBuy(_selected.code, _tradeQty))
            {
                if (_popupFeedback != null) _popupFeedback.text = "";
                RefreshAll();
                RefreshPopup();
            }
            else
            {
                ShowTradeFailure(_selected.code, _tradeQty);
            }
        }

        private void OnSellClicked()
        {
            if (_selected == null || StockMarketManager.Instance == null) return;
            if (StockMarketManager.Instance.TrySell(_selected.code, _tradeQty))
            {
                if (_popupFeedback != null) _popupFeedback.text = "";
                RefreshAll();
                RefreshPopup();
            }
            else if (_popupFeedback != null)
            {
                _popupFeedback.text = "보유 수량이 부족합니다.";
            }
        }

        private void ShowTradeFailure(string code, int qty)
        {
            if (_popupFeedback == null || StockMarketManager.Instance == null) return;
            int cost = StockMarketManager.Instance.GetPrice(code) * qty;
            int cash = MoneyManager.Instance != null ? MoneyManager.Instance.Money : 0;
            _popupFeedback.text = cash < cost
                ? $"잔액 부족 (필요 {cost:N0}원 · 보유 {cash:N0}원)"
                : "매수할 수 없습니다.";
        }

        private void RefreshAll()
        {
            if (StockMarketManager.Instance == null || MoneyManager.Instance == null) return;

            UiTheme.RefreshHeaderMeta(_headerMeta);

            int cash = MoneyManager.Instance.Money;
            int portfolio = StockMarketManager.Instance.GetPortfolioValue();
            _totalAssetsText.text = $"{cash + portfolio:N0}원";

            RebuildHoldings();

            foreach (var row in _listRows)
            {
                if (row != null) UnityEngine.Object.Destroy(row);
            }
            _listRows.Clear();
            foreach (var ticker in StockMarketManager.Instance.Tickers)
            {
                if (ticker != null) AddListRow(ticker);
            }
        }

        private void RebuildHoldings()
        {
            foreach (var row in _holdingRows)
                UnityEngine.Object.Destroy(row);
            _holdingRows.Clear();

            if (StockMarketManager.Instance == null) return;

            foreach (var ticker in StockMarketManager.Instance.Tickers)
            {
                if (ticker == null) continue;
                int qty = StockMarketManager.Instance.GetHolding(ticker.code);
                if (qty <= 0) continue;

                var row = new GameObject($"Hold_{ticker.code}", typeof(RectTransform));
                row.transform.SetParent(_holdingsContent, false);
                row.AddComponent<LayoutElement>().preferredHeight = 30;
                var tmp = row.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{ticker.displayName}  {qty}주";
                tmp.fontSize = 14;
                tmp.color = UiTheme.TextDark;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);

                _holdingRows.Add(row);
            }

            if (_holdingRows.Count == 0)
            {
                var empty = new GameObject("Empty", typeof(RectTransform));
                empty.transform.SetParent(_holdingsContent, false);
                empty.AddComponent<LayoutElement>().preferredHeight = 30;
                var tmp = empty.AddComponent<TextMeshProUGUI>();
                tmp.text = "보유한 종목이 없습니다.";
                tmp.fontSize = 13;
                tmp.color = UiTheme.TextFaint;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
                _holdingRows.Add(empty);
            }
        }

        private void RefreshPopup()
        {
            if (_selected == null || StockMarketManager.Instance == null) return;

            int unit = StockMarketManager.Instance.GetPrice(_selected.code);
            int total = unit * _tradeQty;
            int holding = StockMarketManager.Instance.GetHolding(_selected.code);

            if (_popupBadge != null) _popupBadge.color = TickerColor(_selected);
            if (_popupName != null) _popupName.text = _selected.displayName;
            if (_popupPrice != null)
                _popupPrice.text = $"현재가 {unit:N0}원  ·  보유 {holding}주";
            if (_popupQty != null) _popupQty.text = $"수량 {_tradeQty}";
            if (_popupTotal != null)
                _popupTotal.text = $"총비용  {total:N0}원\n<size=70%><color=#8A6238>{_tradeQty}주 × {unit:N0}원</color></size>";
        }

        private static void CreateStepButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string label, Action onClick)
        {
            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(parent, $"Step_{label}", anchorMin, anchorMax, Vector2.zero, Vector2.zero),
                label, UiTheme.CardWhite, () => onClick(), 18, UiTheme.TextDark);
        }

        private static void CreateColumnText(Transform parent, string label, float xMin, float xMax, Color color)
        {
            UiFactory.CreateText(parent, label, label,
                new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(10f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 12, color);
        }

        private static Color TickerColor(StockTickerSO ticker) => ticker.cultureGroup switch
        {
            CultureGroup.Korean => UiTheme.Accent,
            CultureGroup.Muslim => UiTheme.Success,
            CultureGroup.Hindu => UiTheme.Gold,
            CultureGroup.Vegan => UiTheme.Success,
            CultureGroup.SEAsian => UiTheme.Info,
            CultureGroup.AfricanAmerican => new Color32(0xD9, 0x8C, 0xB0, 0xFF),
            _ => UiTheme.TextFaint,
        };
    }
}
