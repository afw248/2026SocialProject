using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Progression;
using ChangJun.Social;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 정산 — 독립 풀스크린 화면. 좌측 오늘의 판매 내역 · 우측 매출 요약 + 평판 + 액션.
    /// </summary>
    public sealed class SettlementOverlay
    {
        private readonly GameObject _root;
        private readonly TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI _moneyChip;
        private readonly RectTransform _salesContent;
        private readonly TextMeshProUGUI _satisfactionText;
        private readonly TextMeshProUGUI _missedText;
        private readonly TextMeshProUGUI _revenueText;
        private readonly TextMeshProUGUI _costText;
        private readonly TextMeshProUGUI _profitText;
        private readonly RectTransform _starsRow;
        private readonly Button _nextButton;
        private readonly Button _communityButton;
        private bool _communityUsed;
        private int _communityDonatedUnits;

        public event Action OnDismissed;

        public SettlementOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("SettlementOverlay", 90);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "");
            _titleText = UiFactory.CreateText(header, "DynamicTitle", "정산",
                Vector2.zero, Vector2.one, new Vector2(26f, 0f), new Vector2(-26f, 0f),
                TextAlignmentOptions.MidlineLeft, 24, UiTheme.CardWhite);
            var moneyChip = UiTheme.CreateBorderedPanel(header,
                "MoneyChip", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-176f, -18f), new Vector2(-26f, 18f), UiTheme.CardWhite, 2f);
            _moneyChip = UiFactory.CreateText(moneyChip, "Text", "",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, UiTheme.TextDark);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            // ── 좌측: 오늘의 판매 내역 ──
            var left = UiFactory.CreatePanel(body, "Left",
                Vector2.zero, new Vector2(0.64f, 1f), Vector2.zero, Vector2.zero);

            UiTheme.CreateSectionLabel(left, "SalesLabel", "오늘의 판매 내역",
                new Vector2(0f, 0.95f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 16);

            var salesHeaderRow = UiTheme.CreateBorderedPanel(left, "SalesHeader",
                new Vector2(0f, 0.87f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            CreateColumnText(salesHeaderRow, "메뉴", 0f, 0.4f);
            CreateColumnText(salesHeaderRow, "판매 수", 0.4f, 0.6f);
            CreateColumnText(salesHeaderRow, "단가", 0.6f, 0.8f);

            var scrollRt = UiFactory.CreatePanel(left, "Scroll",
                new Vector2(0f, 0.44f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _salesContent = UiFactory.CreateStretchChild(viewport, "Content");
            _salesContent.pivot = new Vector2(0.5f, 1f);
            _salesContent.anchorMin = new Vector2(0, 1);
            _salesContent.anchorMax = new Vector2(1, 1);

            var vlg = _salesContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _salesContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _salesContent;

            var satisBox = UiTheme.CreateBorderedPanel(left, "SatisBox",
                new Vector2(0f, 0f), new Vector2(0.49f, 0.4f), Vector2.zero, Vector2.zero, UiTheme.CardWhite, 2f);
            UiFactory.CreateText(satisBox, "Label", "손님 만족도",
                new Vector2(0f, 0.78f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                TextAlignmentOptions.MidlineLeft, 12, UiTheme.TextMuted);
            _satisfactionText = UiFactory.CreateText(satisBox, "Value", "",
                new Vector2(0f, 0f), new Vector2(1f, 0.78f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                TextAlignmentOptions.TopLeft, 14, UiTheme.TextDark);
            _satisfactionText.textWrappingMode = TextWrappingModes.Normal;

            var missedBox = UiTheme.CreateBorderedPanel(left, "MissedBox",
                new Vector2(0.51f, 0f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero, UiTheme.CardWhite, 2f);
            UiFactory.CreateText(missedBox, "Label", "놓친 주문",
                new Vector2(0f, 0.78f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                TextAlignmentOptions.MidlineLeft, 12, UiTheme.TextMuted);
            _missedText = UiFactory.CreateText(missedBox, "Value", "",
                new Vector2(0f, 0f), new Vector2(1f, 0.78f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                TextAlignmentOptions.TopLeft, 13, UiTheme.Danger);
            _missedText.textWrappingMode = TextWrappingModes.Normal;
            _missedText.overflowMode = TextOverflowModes.Ellipsis;

            // ── 우측: 요약 · 평판 · 액션 ──
            var right = UiFactory.CreatePanel(body, "Right",
                new Vector2(0.68f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var summaryCard = UiTheme.CreateShadowCard(right, "Summary",
                new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            _revenueText = CreateSummaryRow(summaryCard, "총 매출", 0.68f, 0.92f, UiTheme.TextDark);
            _costText = CreateSummaryRow(summaryCard, "재료 지출", 0.4f, 0.64f, UiTheme.Danger);
            var divider = UiFactory.CreatePanel(summaryCard, "Divider",
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.38f), Vector2.zero, Vector2.zero);
            divider.gameObject.AddComponent<Image>().color = UiTheme.Border;
            _profitText = CreateSummaryRow(summaryCard, "순이익", 0.06f, 0.32f, UiTheme.Success);

            var repCard = UiTheme.CreateBorderedPanel(right, "Reputation",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero, UiTheme.TanRow, 3f);
            UiFactory.CreateText(repCard, "Label", "평판",
                new Vector2(0f, 0.6f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, 0f),
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextMuted);
            _starsRow = UiFactory.CreatePanel(repCard, "Stars",
                new Vector2(0f, 0.15f), new Vector2(1f, 0.55f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            var starsHlg = _starsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starsHlg.spacing = 6;
            starsHlg.childAlignment = TextAnchor.MiddleLeft;
            starsHlg.childControlWidth = true;
            starsHlg.childControlHeight = true;
            starsHlg.childForceExpandWidth = false;

            _communityButton = CreateActionButton(right, new Vector2(0f, 0.28f), new Vector2(1f, 0.46f),
                "커뮤니티 밥상 (창고 기부)", UiTheme.Info, OnCommunityMeal);

            _nextButton = CreateActionButton(right, new Vector2(0f, 0.02f), new Vector2(1f, 0.24f),
                "재료 구매하러 가기", UiTheme.Accent, Dismiss);
        }

        public void Show()
        {
            _communityUsed = false;
            _communityDonatedUnits = 0;
            _communityButton.interactable = true;
            StoreReputationService.Instance?.PayDailySubsidy(DayLoopController.Instance.Ledger);

            int day = DayLoopController.Instance.Day;
            _titleText.text = $"정산 · {day}일차 마감";
            _moneyChip.text = $"{MoneyManager.Instance.Money:N0}원";

            RebuildContent();
            _root.SetActive(true);
        }

        private void RebuildContent()
        {
            var ledger = DayLoopController.Instance.Ledger;

            foreach (Transform child in _salesContent)
                UnityEngine.Object.Destroy(child.gameObject);

            bool any = false;
            foreach (var sale in ledger.MenuSales)
            {
                any = true;
                AddSalesRow(sale.name, sale.count, sale.total / Mathf.Max(1, sale.count));
            }
            if (!any)
            {
                var empty = new GameObject("Empty", typeof(RectTransform));
                empty.transform.SetParent(_salesContent, false);
                empty.AddComponent<LayoutElement>().preferredHeight = 32;
                var tmp = empty.AddComponent<TextMeshProUGUI>();
                tmp.text = "오늘 판매된 메뉴가 없습니다.";
                tmp.fontSize = 14;
                tmp.color = UiTheme.TextFaint;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }

            string satisfaction = $"손님 {ledger.CustomersServed}명 응대";
            if (StoreReputationService.Instance != null)
                satisfaction += $"\n상생 지수 {StoreReputationService.Instance.Reputation * 100f:F0}%";
            if (_communityDonatedUnits > 0)
                satisfaction += $"\n커뮤니티 밥상 창고 {_communityDonatedUnits}개 기부";
            if (SchoolLunchContractService.Instance?.IsActive == true)
            {
                var lunch = SchoolLunchContractService.Instance;
                satisfaction += $"\n급식 계약 {lunch.Successes}/{lunch.Target} (D-{lunch.DaysLeft})";
            }
            _satisfactionText.text = satisfaction;

            _missedText.text = ledger.MissedOrders.Count == 0
                ? "놓친 주문이 없습니다."
                : string.Join("\n", ledger.MissedOrders);

            _revenueText.text = $"{ledger.Revenue:N0}원";
            int cost = ledger.IngredientCost + ledger.PenaltyLoss + ledger.PurchaseCost + ledger.StockPurchaseCost;
            _costText.text = $"- {cost:N0}원";
            _profitText.text = (ledger.NetProfit >= 0 ? "+ " : "- ") + $"{Mathf.Abs(ledger.NetProfit):N0}원";

            RebuildStars();
        }

        private void RebuildStars()
        {
            foreach (Transform child in _starsRow)
                UnityEngine.Object.Destroy(child.gameObject);

            float rep = StoreReputationService.Instance?.Reputation ?? 0f;
            int filled = Mathf.Clamp(Mathf.RoundToInt(rep * 5f), 0, 5);

            for (int i = 0; i < 5; i++)
            {
                var star = new GameObject($"Star{i}", typeof(RectTransform));
                star.transform.SetParent(_starsRow, false);
                var le = star.AddComponent<LayoutElement>();
                le.preferredWidth = 22;
                le.preferredHeight = 22;
                star.AddComponent<Image>().color = i < filled ? UiTheme.Gold : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        private void AddSalesRow(string name, int count, int unitPrice)
        {
            var rowWrap = UiFactory.CreateStretchChild(_salesContent, $"Row_{name}");
            rowWrap.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            var row = UiTheme.CreateBorderedPanel(rowWrap, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UiTheme.CardWhite, 2f);

            CreateColumnText(row, name, 0f, 0.4f);
            CreateColumnText(row, $"{count}개", 0.4f, 0.6f);
            CreateColumnText(row, $"{unitPrice:N0}원", 0.6f, 0.8f);
        }

        private void OnCommunityMeal()
        {
            if (_communityUsed) return;

            int units = InventoryManager.Instance.DonateAllWarehouse(out _);
            if (units <= 0)
            {
                Debug.Log("[Settlement] 기부할 창고 재료가 없습니다.");
                return;
            }

            _communityUsed = true;
            _communityButton.interactable = false;
            _communityDonatedUnits = units;
            StoreReputationService.Instance?.ApplyCommunityMeal(units);
            RebuildContent();
        }

        public void Hide() => _root.SetActive(false);

        private void Dismiss()
        {
            Hide();
            OnDismissed?.Invoke();
        }

        private static TextMeshProUGUI CreateSummaryRow(Transform parent, string label,
            float yMin, float yMax, Color valueColor)
        {
            UiFactory.CreateText(parent, $"{label}_L", label,
                new Vector2(0f, yMin), new Vector2(0.55f, yMax), new Vector2(16f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);
            return UiFactory.CreateText(parent, $"{label}_V", "",
                new Vector2(0.5f, yMin), new Vector2(1f, yMax), Vector2.zero, new Vector2(-16f, 0f),
                TextAlignmentOptions.MidlineRight, 17, valueColor);
        }

        private static Button CreateActionButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string label, Color color, Action onClick)
        {
            return UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(parent, $"Btn_{label}", anchorMin, anchorMax, Vector2.zero, Vector2.zero),
                label, color, () => onClick(), 15);
        }

        private static void CreateColumnText(Transform parent, string label, float xMin, float xMax)
        {
            UiFactory.CreateText(parent, label, label,
                new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(10f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextDark);
        }
    }
}
