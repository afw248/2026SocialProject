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
    public sealed class SettlementOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _contentRoot;
        private readonly Button _nextButton;
        private readonly Button _communityButton;
        private bool _communityUsed;
        private int _communityDonatedUnits;

        public event Action OnDismissed;

        public SettlementOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("SettlementOverlay", 90);
            _root.SetActive(false);

            ReceiptUiHelper.CreateDim(_root.transform);

            var panel = ReceiptUiHelper.CreatePaperPanel(_root.transform, "Panel",
                new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.88f));

            ReceiptUiHelper.CreateReceiptHeader(panel, "오늘의 정산", "Daily Settlement",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f));

            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _contentRoot = UiFactory.CreateStretchChild(viewport, "Content");
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            _contentRoot.anchorMin = new Vector2(0, 1);
            _contentRoot.anchorMax = new Vector2(1, 1);

            var vlg = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _contentRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _contentRoot;

            _communityButton = ReceiptUiHelper.CreatePaperButton(panel, "커뮤니티 밥상 (창고 기부)",
                new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.12f),
                OnCommunityMeal, new Color(0.35f, 0.28f, 0.55f));

            _nextButton = ReceiptUiHelper.CreatePaperButton(panel, "다음",
                new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.12f),
                Dismiss, new Color(0.2f, 0.45f, 0.25f));
        }

        public void Show()
        {
            _communityUsed = false;
            _communityDonatedUnits = 0;
            _communityButton.interactable = true;
            StoreReputationService.Instance?.PayDailySubsidy(DayLoopController.Instance.Ledger);
            RebuildContent();
            _root.SetActive(true);
        }

        private void RebuildContent()
        {
            foreach (Transform child in _contentRoot)
                UnityEngine.Object.Destroy(child.gameObject);

            var ledger = DayLoopController.Instance.Ledger;
            int day = DayLoopController.Instance.Day;

            AddInfoRow("영업일", $"{day}일차");
            AddInfoRow("손님 수", $"{ledger.CustomersServed}명");
            if (StoreReputationService.Instance != null)
                AddInfoRow("상생 지수", $"{StoreReputationService.Instance.Reputation * 100f:F0}%");
            if (_communityDonatedUnits > 0)
                AddInfoRow("커뮤니티 밥상", $"창고 {_communityDonatedUnits}개 기부 (상생 지수 ↑)");
            if (SchoolLunchContractService.Instance?.IsActive == true)
            {
                var lunch = SchoolLunchContractService.Instance;
                AddInfoRow("급식 계약", $"{lunch.Successes}/{lunch.Target} (D-{lunch.DaysLeft})");
            }

            ReceiptUiHelper.CreateDashedRule(_contentRoot, Vector2.zero, Vector2.one);

            foreach (var line in ledger.Lines)
                AddLedgerLine(line);

            ReceiptUiHelper.CreateDashedRule(_contentRoot, Vector2.zero, Vector2.one);

            AddSummaryRow("총 매출", ledger.Revenue);
            AddSummaryRow("재료 비용", ledger.IngredientCost);
            AddSummaryRow("패널티", ledger.PenaltyLoss);
            AddSummaryRow("구매 비용", ledger.PurchaseCost);
            if (ledger.SubsidyIncome > 0)
                AddSummaryRow("상생 보조금", -ledger.SubsidyIncome);
            if (ledger.DividendIncome > 0)
                AddSummaryRow("주식 배당", -ledger.DividendIncome);
            if (ledger.StockPurchaseCost > 0)
                AddSummaryRow("주식 매수", ledger.StockPurchaseCost);
            if (ledger.StockSaleRevenue > 0)
                AddSummaryRow("주식 매도", -ledger.StockSaleRevenue);

            ReceiptUiHelper.CreateDashedRule(_contentRoot, Vector2.zero, Vector2.one);
            AddSummaryRow("순이익", ledger.NetProfit, bold: true);

            UiFactory.CreateText(_contentRoot, "Footer", "감사합니다 :)",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 18, ReceiptUiHelper.MutedInk);
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

        private void AddInfoRow(string label, string value)
        {
            var row = new GameObject("Info", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);
            row.AddComponent<LayoutElement>().preferredHeight = 28;
            ReceiptUiHelper.CreateReceiptRow(row.transform, label, value, 17);
        }

        private void AddLedgerLine(string line)
        {
            var row = new GameObject("LedgerLine", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);
            row.AddComponent<LayoutElement>().preferredHeight = 24;
            UiFactory.CreateText(row.transform, "Text", line,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16, ReceiptUiHelper.InkColor);
        }

        private void AddSummaryRow(string label, int amount, bool bold = false)
        {
            var row = new GameObject("Summary", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);
            row.AddComponent<LayoutElement>().preferredHeight = bold ? 34 : 28;
            ReceiptUiHelper.CreateReceiptRow(row.transform, label, $"{amount:N0}원", bold ? 20 : 17, bold);
        }

        public void Hide() => _root.SetActive(false);

        private void Dismiss()
        {
            Hide();
            OnDismissed?.Invoke();
        }
    }
}
