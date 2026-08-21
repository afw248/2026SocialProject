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
    /// 주식 시장 — 독립 풀스크린 화면. 종목 목록 + 보유 현황 + 매수/매도.
    /// </summary>
    public sealed class StockMarketOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _listContent;
        private readonly RectTransform _holdingsContent;
        private readonly TextMeshProUGUI _dateChip;
        private readonly TextMeshProUGUI _moneyChip;
        private readonly TextMeshProUGUI _totalAssetsText;
        private readonly RectTransform _tradeBadge;
        private readonly TextMeshProUGUI _tradeName;
        private readonly TextMeshProUGUI _tradePrice;
        private readonly TextMeshProUGUI _tradeQtyText;
        private readonly TextMeshProUGUI _feedbackText;
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
            _dateChip = CreateHeaderChip(header, "", 210f);
            _moneyChip = CreateHeaderChip(header, "", 26f);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            // ── 좌측: 종목 목록 ──
            var listPanel = UiFactory.CreatePanel(body, "List",
                Vector2.zero, new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero);

            UiTheme.CreateSectionLabel(listPanel, "ListLabel", "종목 목록",
                new Vector2(0f, 0.95f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16);

            var headerRow = UiTheme.CreateBorderedPanel(listPanel, "HeaderRow",
                new Vector2(0f, 0.87f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            CreateColumnText(headerRow, "종목명", 0f, 0.32f, UiTheme.TextMuted);
            CreateColumnText(headerRow, "현재가", 0.32f, 0.56f, UiTheme.TextMuted);
            CreateColumnText(headerRow, "변동률", 0.56f, 0.78f, UiTheme.TextMuted);

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

            // ── 우측: 자산 · 보유 · 거래 ──
            var side = UiFactory.CreatePanel(body, "Side",
                new Vector2(0.69f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var assetsCard = UiTheme.CreateShadowCard(side, "Assets",
                new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            UiFactory.CreateText(assetsCard, "Label", "총 자산",
                new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);
            _totalAssetsText = UiFactory.CreateText(assetsCard, "Value", "",
                new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.6f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26, UiTheme.TextDark);

            var holdingsCard = UiTheme.CreateShadowCard(side, "Holdings",
                new Vector2(0f, 0.58f), new Vector2(1f, 0.83f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            UiFactory.CreateText(holdingsCard, "Label", "보유 종목",
                new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);
            _holdingsContent = UiFactory.CreatePanel(holdingsCard, "HoldingsContent",
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);
            var hvlg = _holdingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            hvlg.spacing = 6;
            hvlg.childControlWidth = true;
            hvlg.childControlHeight = true;
            hvlg.childForceExpandWidth = true;
            hvlg.childForceExpandHeight = false;

            var tradeBox = UiTheme.CreateBorderedPanel(side, "TradeBox",
                new Vector2(0f, 0.16f), new Vector2(1f, 0.54f), Vector2.zero, Vector2.zero, UiTheme.TanRow, 3f);

            _tradeBadge = UiFactory.CreatePanel(tradeBox, "Badge",
                new Vector2(0.05f, 0.80f), new Vector2(0.18f, 0.95f), Vector2.zero, Vector2.zero);
            _tradeBadge.gameObject.AddComponent<Image>().color = UiTheme.Accent;

            _tradeName = UiFactory.CreateText(tradeBox, "Name", "",
                new Vector2(0.22f, 0.80f), new Vector2(0.7f, 0.95f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16, UiTheme.TextDark);
            _tradePrice = UiFactory.CreateText(tradeBox, "Price", "",
                new Vector2(0.7f, 0.80f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 14, UiTheme.TextMuted);

            CreateStepButton(tradeBox, new Vector2(0.05f, 0.56f), new Vector2(0.22f, 0.75f), "-", () => AdjustTradeQty(-1));
            _tradeQtyText = UiFactory.CreateText(tradeBox, "Qty", "수량 1",
                new Vector2(0.24f, 0.56f), new Vector2(0.76f, 0.75f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, UiTheme.TextDark);
            CreateStepButton(tradeBox, new Vector2(0.78f, 0.56f), new Vector2(0.95f, 0.75f), "+", () => AdjustTradeQty(1));

            _feedbackText = UiFactory.CreateText(tradeBox, "Feedback", "",
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.54f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.Danger);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(tradeBox, "BuyBtn", new Vector2(0.05f, 0.08f), new Vector2(0.49f, 0.38f),
                    Vector2.zero, Vector2.zero),
                "매수", UiTheme.Success, OnBuyClicked, 16);
            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(tradeBox, "SellBtn", new Vector2(0.51f, 0.08f), new Vector2(0.95f, 0.38f),
                    Vector2.zero, Vector2.zero),
                "매도", UiTheme.Danger, OnSellClicked, 16);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(_root.transform, "ContinueBtn",
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 32f), new Vector2(-40f, 112f)),
                "영업 시작", UiTheme.Accent, ContinueClicked, 20);

            if (StockMarketManager.Instance != null)
                StockMarketManager.Instance.OnMarketUpdated += RefreshAll;
        }

        public void Show(Action onContinue)
        {
            _onContinue = onContinue;
            _tradeQty = 1;
            _feedbackText.text = "";

            int day = DayLoopController.Instance.Day;
            _dateChip.text = $"{day}일차 · {DayLoopController.Instance.FormatClock()}";

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

        public void Hide() => _root.SetActive(false);

        private void ContinueClicked()
        {
            Hide();
            _onContinue?.Invoke();
        }

        private void AddListRow(StockTickerSO ticker)
        {
            var rowWrap = new GameObject($"Row_{ticker.code}", typeof(RectTransform));
            rowWrap.transform.SetParent(_listContent, false);
            rowWrap.AddComponent<LayoutElement>().preferredHeight = 66;

            var row = UiTheme.CreateShadowCard(rowWrap.transform, "Card",
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, 0f),
                UiTheme.CardWhite, 3f, 4f);

            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = row.gameObject.GetComponent<Image>();
            btn.onClick.AddListener(() => SelectTicker(ticker));

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
                new Vector2(0.32f, 0f), new Vector2(0.56f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextDark);

            float change = StockMarketManager.Instance.GetChangePercent(ticker.code);
            string changeStr = change >= 0 ? $"+{change:0.0}%" : $"{change:0.0}%";
            var changeColor = change > 0 ? UiTheme.Success : change < 0 ? UiTheme.Danger : UiTheme.TextMuted;
            UiFactory.CreateText(row, "Change", changeStr,
                new Vector2(0.56f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, changeColor);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(row, "Buy", new Vector2(0.8f, 0.15f), new Vector2(0.97f, 0.85f),
                    Vector2.zero, Vector2.zero),
                "매수", UiTheme.Accent, () =>
                {
                    _selected = ticker;
                    _tradeQty = 1;

                    if (StockMarketManager.Instance.TryBuy(ticker.code, 1))
                    {
                        _feedbackText.text = "";
                        RefreshAll();
                    }
                    else
                    {
                        RefreshTradeBox();
                        ShowTradeFailure(ticker.code, 1);
                    }
                }, 13);

            _listRows.Add(rowWrap);
        }

        private void SelectTicker(StockTickerSO ticker)
        {
            _selected = ticker;
            _tradeQty = 1;
            _feedbackText.text = "";
            RefreshTradeBox();
        }

        private void AdjustTradeQty(int delta)
        {
            _tradeQty = Mathf.Max(1, _tradeQty + delta);
            RefreshTradeBox();
        }

        private void OnBuyClicked()
        {
            if (_selected == null || StockMarketManager.Instance == null) return;
            if (StockMarketManager.Instance.TryBuy(_selected.code, _tradeQty))
            {
                _feedbackText.text = "";
                RefreshAll();
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
                _feedbackText.text = "";
                RefreshAll();
            }
            else
            {
                _feedbackText.text = "보유 수량이 부족합니다.";
            }
        }

        private void ShowTradeFailure(string code, int qty)
        {
            int cost = StockMarketManager.Instance.GetPrice(code) * qty;
            int cash = MoneyManager.Instance != null ? MoneyManager.Instance.Money : 0;
            _feedbackText.text = cash < cost
                ? $"잔액이 부족합니다 (필요 {cost:N0}원 · 보유 {cash:N0}원)"
                : "매수할 수 없습니다.";
        }

        private void RefreshAll()
        {
            if (StockMarketManager.Instance == null || MoneyManager.Instance == null) return;

            _moneyChip.text = $"{MoneyManager.Instance.Money:N0}원";

            int cash = MoneyManager.Instance.Money;
            int portfolio = StockMarketManager.Instance.GetPortfolioValue();
            _totalAssetsText.text = $"{cash + portfolio:N0}원";

            RebuildHoldings();
            RefreshTradeBox();

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

        private void RefreshTradeBox()
        {
            if (_selected == null || StockMarketManager.Instance == null) return;

            _tradeBadge.GetComponent<Image>().color = TickerColor(_selected);
            _tradeName.text = _selected.displayName;
            _tradePrice.text = $"{StockMarketManager.Instance.GetPrice(_selected.code):N0}원";
            _tradeQtyText.text = $"수량 {_tradeQty}";
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

        private static TextMeshProUGUI CreateHeaderChip(RectTransform header, string text, float rightOffset)
        {
            var chip = UiTheme.CreateBorderedPanel(header,
                "Chip", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-rightOffset - 150f, -18f), new Vector2(-rightOffset, 18f), UiTheme.CardWhite, 2f);
            return UiFactory.CreateText(chip, "Text", text,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, UiTheme.TextDark);
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
